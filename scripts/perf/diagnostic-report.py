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
    """只按实际附着发布、退出时刻和成功入流的采样边界确认覆盖。"""
    report = dict(status='partial', reasons=[])
    reasons = report['reasons']
    if capture.get('gameExitCode') != 0 or capture.get('exitCode') != 0:
        reasons.append('Godot and EventPipe must both exit successfully.')
    parsed = read_json(work / 'cpu-hotspots.json')
    if parsed.get('processId') != capture.get('gamePid') or not capture.get('gamePid'):
        reasons.append('Parsed managed PID differs from the owned Godot PID.')
    if parsed.get('EventsLost') != 0 or parsed.get('Truncated') is not False:
        reasons.append('EventPipe trace loss/truncation is nonzero or unknown.')
    if not any(number(row.get('samples')) and row['samples'] > 0 for row in parsed.get('threads', [])):
        reasons.append('EventPipe has no managed stack samples in the recording session.')
    manifest, streams = load_session(session, read_json(root / 'game/data/perf-fields.json'))
    marks = [] if streams is None else sorted((row for row in streams['events'] if row['event'] == 'segment'
        and row['payload'].get('id') == condition['collection']['sampleSegmentId']), key=lambda row: row['sequence'])
    if len(marks) != 2 or [row['payload'].get('phase') for row in marks] != ['begin', 'end']:
        reasons.append('Diagnostic sample Begin/End are missing, duplicated or out of order.')
    else:
        try:
            anchors = manifest['clock']['anchors']
            anchor = anchors[0]
            origin = datetime.fromtimestamp(anchor['unixMs'] / 1000, timezone.utc)
            begin, end = (origin + timedelta(milliseconds=row['monotonicMs'] - anchor['monotonicMs']) for row in marks)
            uncertainty = timedelta(milliseconds=max(row['readSpanMs'] for row in anchors))
            attached_text, exited_text = capture.get('collectorReadyObservedUtc'), capture.get('toolExitUtc')
            if not isinstance(attached_text, str) or not isinstance(exited_text, str):
                raise ValueError('Collector attachment/exit timing is missing.')
            attached = datetime.fromisoformat(attached_text.replace('Z', '+00:00'))
            exited = datetime.fromisoformat(exited_text.replace('Z', '+00:00'))
            report.update(sampleBeginUtc=begin.isoformat(), sampleEndUtc=end.isoformat(),
                          collectorReadyObservedUtc=attached.isoformat(), collectorExitUtc=exited.isoformat(),
                          clockReadSpanMs=uncertainty.total_seconds() * 1000)
            if begin >= end or attached > begin - uncertainty or exited < end + uncertainty:
                reasons.append('Observed EventPipe attachment does not cover the complete diagnostic sample interval.')
        except (KeyError, IndexError, TypeError, ValueError, OverflowError) as error:
            reasons.append('EventPipe coverage timing is unavailable: ' + str(error))
    if not reasons:
        report['status'] = 'complete'
    return report


def attachment_state(work, session, tool):
    attachment_reasons = []
    gpu_structure = None
    if tool == 'gpu-profile':
        steps = session / 'gpusteps.csv'
        if steps.is_file():
            with steps.open(newline='', encoding='utf-8-sig') as stream:
                rows = list(csv.DictReader(stream))
            gpu_structure = dict(rows=len(rows), validRows=sum(row['valid'] == 'true' for row in rows),
                                 invalidReasons=sorted({row['reason'] for row in rows if row['valid'] != 'true'}),
                                 segments=len({row['structureSegment'] for row in rows}))
            if not gpu_structure['validRows']:
                attachment_reasons.append('GPU timestamps did not produce a rechecked valid structure segment.')
        else:
            attachment_reasons.append('gpusteps.csv is missing.')
    elif tool == 'EventPipe' and not (work / 'cpu-hotspots.json').is_file():
        attachment_reasons.append('EventPipe parser did not produce cpu-hotspots.json.')
    elif tool == 'Nsight' and not list(work.glob('*.nsys-rep')):
        attachment_reasons.append('Nsight did not publish an nsys-rep attachment.')
    return attachment_reasons, gpu_structure


def publish(root, work, source, exit_code):
    condition = read_json(work / 'condition.json')
    sessions = [path.parent for path in work.glob('*/manifest.json')]
    if len(sessions) != 1:
        atomic_json(work / 'diagnostic.json', dict(purpose='diagnostic', status='partial', condition=condition,
                    exitCode=exit_code, reasons=['Expected exactly one closed diagnostic recording. Raw collector output retained.']))
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
    attachment_reasons, gpu_structure = attachment_state(work, session, condition['tool'])
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
