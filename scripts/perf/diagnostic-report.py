"""把诊断附件归入其实际会话，只报告与原常规会话的观测比值和条件差异。
用法：python -I -X utf8 scripts/perf/diagnostic-report.py --root <repo> --work <tool-dir> --source <session> --exit-code 0。
"""
import argparse
import csv
import json
import shutil
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.dont_write_bytecode = True
from perf_model import COSTS, costs, distribution, load_session, number, prepare_frames, read_json, scope_of
from perf_store import atomic_json, comparison_key
from perf_batches import sample_window, sampled_streams


def frame_metrics(root, directory):
    dictionary = read_json(root / 'game/data/perf-fields.json')
    manifest, streams = load_session(directory, dictionary)
    if streams is None:
        return {}, ['Recording has not closed; its raw streams are unavailable for analysis.']
    window, reasons = sample_window(manifest, streams, read_json(directory / 'run.json'))
    if reasons or window is None:
        return {}, reasons or ['Game-owned sample window is unavailable.']
    prepare_frames(streams['frames'], streams['samples'], streams['events'], dictionary)
    sampled = sampled_streams(streams, window)
    summaries = costs(sampled['frames'], sampled['samples'], sampled['hw'])
    metrics = {key: summaries[key] for key in COSTS}
    scope, _ = scope_of(manifest)
    if scope == 'full':
        metrics['dtMs'] = distribution(frame['dtMs'] for frame in sampled['frames'] if frame['_dtValid'])
    return metrics, []


def collection_result(work, tool):
    matches = []
    stdout = work / ('game.stdout.txt' if tool == 'EventPipe' else 'tool.stdout.txt')
    if stdout.is_file():
        for line in stdout.read_text(encoding='utf-8-sig').splitlines():
            if line.startswith('PERF_COLLECTION_RESULT '):
                matches.append(json.loads(line.split(' ', 1)[1]))
    return matches[0] if len(matches) == 1 else None


def eventpipe_coverage(root, work, session, condition, capture):
    """核对真实样段、目标存活的 Stop/rundown 和托管样本中实际可用的方法。"""
    report = dict(status='partial', reasons=[])
    reasons = report['reasons']
    if capture.get('gameExitCode') != 0 or capture.get('exitCode') != 0:
        reasons.append('Godot and EventPipe must both exit successfully.')
    try:
        parsed = read_json(work / 'cpu-hotspots.json')
        if not isinstance(parsed, dict):
            raise ValueError('Expected an EventPipe report object.')
    except (OSError, TypeError, ValueError) as error:
        reasons.append('EventPipe parser output is unavailable: ' + str(error))
        return report
    if parsed.get('processId') != capture.get('gamePid') or not capture.get('gamePid'):
        reasons.append('Parsed managed PID differs from the owned Godot PID.')
    if parsed.get('EventsLost') != 0 or parsed.get('Truncated') is not False:
        reasons.append('EventPipe trace loss/truncation is nonzero or unknown.')
    if not any(number(row.get('samples')) and row['samples'] > 0 for row in parsed.get('threads', [])):
        reasons.append('EventPipe has no thread stack samples in the diagnostic sample window.')
    resolution = parsed.get('managedStackResolution')
    report['managedStackResolution'] = resolution
    if not isinstance(resolution, dict):
        reasons.append('Managed sample method-resolution statistics are missing.')
    else:
        counts = [resolution.get(key) for key in ('managedSamples', 'managedSamplesWithResolvedMethods',
                  'stackFrameOccurrences', 'resolvedMethodFrameOccurrences', 'distinctResolvedMethods')]
        if any(not isinstance(value, int) or isinstance(value, bool) or value < 0 for value in counts):
            reasons.append('Managed sample method-resolution counts are unavailable or invalid.')
        else:
            managed, with_methods, frames, resolved_frames, methods = counts
            report['managedSampleMethodFraction'] = with_methods / managed if managed else None
            # ENG-PERF-MEASURE: strictly more than half of Managed samples must have a method.
            if not managed or with_methods > managed or with_methods * 2 <= managed:
                reasons.append('Not more than half of Managed samples contain a resolved method.')
            if not frames or not resolved_frames or not methods or resolved_frames > frames:
                reasons.append('Managed samples did not produce usable resolved method frames.')
            fraction = resolution.get('resolvedMethodFrameFraction')
            if not number(fraction) or not 0 < fraction <= 1:
                reasons.append('Managed stack-frame resolution fraction is unavailable.')
    if capture.get('collectorStoppedWhileTargetAlive') is not True:
        reasons.append('Collector Stop/rundown did not complete while the target remained alive.')
    manifest, streams = load_session(session, read_json(root / 'game/data/perf-fields.json'))
    if parsed.get('sampleSegmentId') != condition['collection']['sampleSegmentId'] or parsed.get('session') != manifest.get('id'):
        reasons.append('Parsed EventPipe sample or recording identity differs from this diagnostic session.')
    marks = [] if streams is None else sorted((row for row in streams['events'] if row['event'] == 'segment'
        and row['payload'].get('id') == condition['collection']['sampleSegmentId']), key=lambda row: row['sequence'])
    if len(marks) != 2 or [row['payload'].get('phase') for row in marks] != ['begin', 'end'] or marks[0]['sequence'] >= marks[1]['sequence']:
        reasons.append('Diagnostic sample Begin/End are missing, duplicated or out of order.')
    else:
        try:
            anchors = manifest['clock']['anchors']
            anchor = anchors[0]
            origin = datetime.fromtimestamp(anchor['unixMs'] / 1000, timezone.utc)
            begin, end = (origin + timedelta(milliseconds=row['monotonicMs'] - anchor['monotonicMs']) for row in marks)
            uncertainty = timedelta(milliseconds=max(row['readSpanMs'] for row in anchors))
            timing = {}
            for key in ('collectorReadyObservedUtc', 'collectorStopRequestedUtc', 'toolExitUtc', 'collectorFinishedUtc', 'gameExitUtc'):
                text = capture.get(key)
                if not isinstance(text, str):
                    raise ValueError('Collector lifecycle timestamp is missing: ' + key)
                timing[key] = datetime.fromisoformat(text.replace('Z', '+00:00'))
                if timing[key].utcoffset() is None:
                    raise ValueError('Collector lifecycle timestamp lacks its timezone: ' + key)
            attached, stopped, exited, finished, game_exited = timing.values()
            report.update(sampleBeginUtc=begin.isoformat(), sampleEndUtc=end.isoformat(),
                          **{key: value.isoformat() for key, value in timing.items()},
                          clockReadSpanMs=uncertainty.total_seconds() * 1000)
            if begin >= end or attached > begin - uncertainty or stopped < end + uncertainty:
                reasons.append('Observed EventPipe attachment does not cover the complete diagnostic sample interval.')
            if not attached <= stopped <= exited <= finished <= game_exited:
                reasons.append('Collector Stop/rundown and formal game exit did not occur in the required order.')
        except (KeyError, IndexError, TypeError, ValueError, OverflowError) as error:
            reasons.append('EventPipe coverage timing is unavailable: ' + str(error))
    if not reasons:
        report['status'] = 'complete'
    return report


def attachment_state(work, session, tool, capture):
    attachment_reasons = []
    gpu_structure = None
    if tool == 'gpu-profile':
        steps = session / 'gpusteps.csv'
        if steps.is_file():
            with steps.open(newline='', encoding='utf-8-sig') as stream:
                rows = list(csv.DictReader(stream))
            valid_steps = []
            for row in rows:
                if row.get('valid') != 'true' or row.get('index') == '0':
                    continue
                try:
                    cpu = float(row['cpuSincePreviousMarkerMs'])
                    gpu = float(row['gpuSincePreviousMarkerMs'])
                except (KeyError, TypeError, ValueError) as error:
                    attachment_reasons.append('GPU marker interval could not be read: ' + str(error))
                    continue
                if number(cpu) and number(gpu) and cpu >= 0 and gpu >= 0 and row.get('name') not in (None, '', 'null'):
                    valid_steps.append(row)
                else:
                    attachment_reasons.append('GPU marker interval is nonfinite, negative or unnamed.')
            gpu_structure = dict(rows=len(rows), validRows=sum(row.get('valid') == 'true' for row in rows), usableStepRows=len(valid_steps),
                                 invalidReasons=sorted({row.get('reason', 'missing-reason') for row in rows if row.get('valid') != 'true'}),
                                 segments=len({row.get('structureSegment') for row in rows}))
            if not valid_steps:
                attachment_reasons.append('GPU timestamps did not produce a rechecked structure with usable numeric marker intervals.')
        else:
            attachment_reasons.append('gpusteps.csv is missing.')
    elif tool == 'EventPipe' and not (work / 'cpu-hotspots.json').is_file():
        attachment_reasons.append('EventPipe parser did not produce cpu-hotspots.json.')
    elif tool == 'Nsight':
        if not list(work.glob('*.nsys-rep')):
            attachment_reasons.append('Nsight did not publish an nsys-rep attachment.')
        if capture.get('nsightStatsExitCode') != 0:
            attachment_reasons.append('Native Nsight stats did not successfully parse the captured report.')
        reports = capture.get('nsightGpuReports', [])
        usable_rows = 0
        for name in reports:
            path = work / name
            if path.parent != work or not path.is_file():
                attachment_reasons.append('Native Nsight GPU report is unavailable: ' + str(name))
                continue
            with path.open(newline='', encoding='utf-8-sig') as stream:
                for row in csv.DictReader(stream):
                    try:
                        duration, instances = float(row['Total Time (ns)']), int(row['Instances'])
                    except (KeyError, TypeError, ValueError) as error:
                        attachment_reasons.append('Native Nsight GPU range could not be read: ' + str(error))
                        continue
                    if number(duration) and duration >= 0 and instances > 0 and row.get('Range'):
                        usable_rows += 1
                    else:
                        attachment_reasons.append('Native Nsight GPU range has no finite duration, instances or name.')
        if not usable_rows:
            attachment_reasons.append('Native Nsight GPU range reports contain no usable measured GPU ranges.')
    return attachment_reasons, gpu_structure


def publish(root, work, source, exit_code):
    condition = read_json(work / 'condition.json')
    sessions = [path.parent for path in work.glob('*/manifest.json')]
    if len(sessions) != 1:
        atomic_json(work / 'diagnostic.json', dict(purpose='diagnostic', status='partial', condition=condition,
                    exitCode=exit_code, attachmentAvailability=dict(available=False,
                    reasons=['Expected exactly one closed diagnostic recording. Raw collector output retained.']),
                    reasons=['Expected exactly one closed diagnostic recording. Raw collector output retained.']))
        return False
    session = sessions[0]
    capture_path = work / 'capture-result.json'
    capture = read_json(capture_path) if capture_path.is_file() else {}
    observed = collection_result(work, condition['tool'])
    recorded = read_json(session / 'manifest.json')
    valid = bool(exit_code == 0 and capture.get('status') == 'completed' and observed and observed.get('status') == 'valid'
                 and observed.get('scene') == condition['scene']
                 and observed.get('sampleSegmentId') == condition['collection']['sampleSegmentId']
                 and observed.get('sessionId') == recorded.get('id')
                 and recorded.get('status') == 'closed' and recorded.get('complete') is True)
    try:
        attachment_reasons, gpu_structure = attachment_state(work, session, condition['tool'], capture)
    except (OSError, TypeError, ValueError, csv.Error) as error:
        attachment_reasons = ['Diagnostic attachment could not be read: ' + type(error).__name__ + ': ' + str(error)]
        gpu_structure = None
    trace_coverage = None
    if condition['tool'] == 'EventPipe' and (work / 'cpu-hotspots.json').is_file():
        trace_coverage = eventpipe_coverage(root, work, session, condition, capture)
        attachment_reasons.extend(trace_coverage['reasons'])
    valid = valid and not attachment_reasons
    source_run = read_json(source / 'run.json')
    run = dict(purpose='diagnostic', diagnostic=True, sourceSession=condition['sourceSession'], scene=condition['scene'],
               status='valid' if valid else 'partial', reasons=attachment_reasons + ([] if valid else ['Collector or game completion was incomplete.']),
               exitCode=exit_code, timedOut=False if capture.get('status') == 'completed' else None,
               sampleSegmentId=condition['collection']['sampleSegmentId'], conditions=condition['collection'], observed=observed,
               polluted=observed.get('polluted') if observed else None, pollution=observed.get('pollution', []) if observed else [],
               coverage=observed.get('coverage', {}) if observed else {})
    atomic_json(session / 'run.json', run)
    attachments = []
    for path in work.iterdir():
        if path.is_file() and path.name not in ('capture.log',):
            target = session / path.name
            if not target.exists():
                shutil.copyfile(path, target)
            attachments.append(path.name)
    if condition['tool'] == 'gpu-profile' and (session / 'gpusteps.csv').is_file():
        attachments.append('gpusteps.csv')
    reference, reference_reasons = frame_metrics(root, source)
    current, current_reasons = frame_metrics(root, session)
    before, _, missing_before = comparison_key(read_json(source / 'manifest.json'), source_run)
    after, _, missing_after = comparison_key(read_json(session / 'manifest.json'), run)
    differences = {key: dict(reference=before[key], diagnostic=after[key]) for key in before if before[key] != after[key]}
    reference_build = read_json(source / 'manifest.json').get('build')
    diagnostic_build = read_json(session / 'manifest.json').get('build')
    if reference_build != diagnostic_build:
        differences['build'] = dict(reference=reference_build, diagnostic=diagnostic_build)
    ratios = {}
    if source_run.get('purpose') != 'baseline':
        reference_reasons.append('Reference is not a regular baseline run; no diagnostic-to-regular ratio is asserted.')
    if valid and source_run.get('purpose') == 'baseline' and not differences and not missing_before and not missing_after:
        for metric in current:
            ratios[metric] = {stat: current[metric][stat] / reference[metric][stat]
                             if number(current[metric][stat]) and number(reference.get(metric, {}).get(stat)) and reference[metric][stat] > 0 else None
                             for stat in ('mean', 'p95')}
    report = dict(purpose='diagnostic', status=run['status'], sourceSession=condition['sourceSession'], referencePurpose=source_run.get('purpose'), condition=condition,
                  attachments=attachments, actualCollector=capture, gpuStructure=gpu_structure, eventPipeCoverage=trace_coverage,
                  attachmentAvailability=dict(available=not attachment_reasons, reasons=attachment_reasons),
                  reference=reference, diagnostic=current,
                  diagnosticToRegularRatios=ratios, conditionDifferences=differences,
                  ratioUnavailableReasons=reference_reasons + current_reasons + missing_before + missing_after,
                  observedPerturbations=dict(polluted=run['polluted'], pollution=run['pollution'],
                      collectionSeconds=condition['collection']['sampleSeconds'], collectorElapsedUtc=[capture.get('startedUtc'), capture.get('endedUtc')]),
                  limitations=['Single diagnostic replay; ratios are observations, not calibrated profiler overhead or optimization conclusions.',
                              'Diagnostic runs never enter baseline, round comparison or regular means.',
                              'EventPipe sampled-thread-time stacks are wall-clock samples, never CPU utilization.'])
    atomic_json(session / 'diagnostic.json', report)
    print(str(session))
    return valid


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, required=True)
    parser.add_argument('--work', type=Path, required=True)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--exit-code', type=int, required=True)
    options = parser.parse_args()
    raise SystemExit(0 if publish(options.root, options.work, options.source, options.exit_code) else 2)
