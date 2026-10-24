"""分析已关闭性能会话，串行发布派生、索引、历史并修剪 AI 数据。
用法：python -I -X utf8 scripts/perf/analyze.py [会话或批次目录 ...] [--root 仓库] [--author-root 作者perf根]。
输入：性能六件、game/data/perf-fields.json、budgets.json；产出：output/perf/derived/<id>/{summary.json,anomalies.json,hotframes.csv}、index.json、history.jsonl。
"""
import argparse
import csv
import json
import re
import sys
import bisect
from collections import Counter
from pathlib import Path

# Python -I excludes the script directory; resolve only this repository-owned module directory.
sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.dont_write_bytecode = True
from perf_model import (CALC, COSTS, PHASES, at, costs, experience, load_session, number,
                        prepare_frames, read_json, rolling_medians, scope_of, target_ms)
from perf_anomalies import (LEVELS, MAX_PER_TYPE, annotate, anomaly, budget_checks,
                           deepen, frame_anomalies, merge_anomalies, slow_anomalies, window_analysis)
from perf_store import (AI_BATCHES_KEEP, DIAGNOSTIC_BYTES_KEEP, HISTORY_PER_GROUP,
                        atomic_json, author_root, basic_state, comparison_key, discover,
                        exclusive, history_update, nearest_run, prune, rebuild_states)

ROOT = Path(__file__).resolve().parents[2]


def session_id(value):
    if not isinstance(value, str) or not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9._-]*', value) or value in ('.', '..'):
        raise ValueError('会话 ID 不是安全目录名')
    return value


def coverage(streams, dictionary, manifest):
    states = {item['file']: item for item in manifest.get('streams', [])}
    return {name: dict(rows=len(streams[name]), state=states.get(spec['file'], {}).get('state'),
                       droppedRows=states.get(spec['file'], {}).get('droppedRows'),
                       fields={c['name']: sum(row.get(c['name']) is not None for row in streams[name]) / len(streams[name])
                               if streams[name] else None for c in spec['columns']})
            for name, spec in dictionary['streams'].items()}


def validity(frames, streams, manifest):
    marks = []
    if not manifest.get('complete'):
        marks.append(dict(field='session', reason='记录不完整；只禁用受影响字段', rows=None))
    checks = {'dtMs': sum(not f['_dtValid'] for f in frames),
              'phases': sum(not f['_phaseValid'] for f in frames),
              'context': sum(not f['_contextKnown'] for f in frames),
              'renderSource': sum(f['_measuredGpuMs'] is None for f in frames),
              'framesGap': sum(f['_gap'] for f in frames)}
    for field, count in checks.items():
        if count:
            marks.append(dict(field=field, reason='缺测、来源未确认或缺口', rows=count))
    for name in ('samples', 'hw', 'hwfast'):
        if not streams[name]:
            marks.append(dict(field=name, reason='未采集，只影响本流字段', rows=0))
    return marks


def timeline(frames, events):
    result = []
    current = None
    for frame in frames:
        state = 'steady' if frame['_steady'] else 'nonSteady' if frame['_contextKnown'] else 'unknown'
        if current and current['state'] == state and not frame['_gap']:
            current.update(toMs=frame['monotonicMs'], lastFrame=frame['collectionFrame'])
        else:
            current = dict(state=state, fromMs=frame['monotonicMs'] - (frame.get('dtMs') or 0),
                           toMs=frame['monotonicMs'], firstFrame=frame['collectionFrame'], lastFrame=frame['collectionFrame'])
            result.append(current)
    return dict(context=result, segments=[e for e in events if e['event'] == 'segment'],
                gaps=[e for e in events if e['event'] == 'record.gap'])


def hardware_clock(samples, manifest):
    anchors = at(manifest, 'clock.anchors', []) or []
    result = []
    for sample in samples:
        driver = sample.get('driverTimestampUs')
        if driver is None or not anchors:
            result.append({**sample, 'approximateMonotonicMs': None, 'alignmentUncertaintyMs': None})
            continue
        wall = driver / 1000
        anchor = min(anchors, key=lambda a: abs(a['unixMs'] - wall))
        span = sample['queryEndMs'] - sample['queryStartMs'] if number(sample.get('queryStartMs')) and number(sample.get('queryEndMs')) else None
        result.append({**sample, 'approximateMonotonicMs': anchor['monotonicMs'] + wall - anchor['unixMs'],
                       'alignmentUncertaintyMs': anchor.get('readSpanMs', 0) + span if span is not None else None,
                       'alignmentNote': '最近挂钟锚点近似关联；不保证捕获短卡顿'})
    return result


def budget_items(summary, frames, budgets):
    if not frames:
        return []
    result = [anomaly('budgetExceeded', 'heavy', max(frames, key=lambda f: f.get(check['metric']) or -1), check['value'], check['budget'],
                    check['budgetSource'], check['metric'], True)
            for check in summary['budgets'] if check['exceeded'] is True]
    pacing = at(summary, 'steady.experience.framePacingP95Ms')
    threshold = at(budgets, 'perfAnomalies.framePacingP95Ms')
    if number(pacing) and pacing > threshold:
        result.append(anomaly('framePacing', 'info', frames[0], pacing, threshold,
                              'budgets.json.perfAnomalies.framePacingP95Ms', 'framePacingP95Ms', True))
    return result


def summary_for(directory, manifest, streams, dictionary, budgets, run):
    scope, reason = scope_of(manifest)
    key, labels, missing = comparison_key(manifest, run)
    status, reasons = basic_state(manifest, streams, run, missing)
    summary = dict(id=session_id(manifest['id']), calc=CALC, schema=manifest['schema'], origin=manifest.get('origin'),
                   source=str(directory), endedAt=manifest.get('endedAt'), startedAt=manifest.get('startedAt'),
                   recordedSeconds=manifest.get('recordedSeconds'), scope=scope, scopeReason=reason,
                   comparisonKey=key, comparisonLabels=labels, comparisonMissing=missing,
                   baseStatus=status, baseReasons=reasons, status=status, statusReasons=reasons,
                   build=manifest.get('build'), environmentSignature={k: at(manifest, k) for k in
                         ('machine.driver', 'machine.os', 'build.godot')}, run=run,
                   all=None, steady=None, coverage=None, validity=[], budgets=[], anomalyCounts={}, deepen=[],
                   units=dict(frame='ms', memory='MiB', fractions='0..1', power='W', frequency='MHz', temperature='degC'))
    if streams is None:
        return summary, [], [], []
    frames, events = streams['frames'], streams['events']
    prepare_frames(frames, streams['samples'], events, dictionary)
    viewport_fields = tuple(sorted({key for f in frames for key in f['_viewportCpuFields']}))
    medians = rolling_medians(frames, ('dtMs',) + COSTS + PHASES + viewport_fields, budgets['perfAnomalies'])
    annotate(frames, events, medians, budgets['perfAnomalies'])
    for title, rows in (('all', frames), ('steady', [f for f in frames if f['_steady']])):
        samples = streams['samples'] if title == 'all' else steady_samples(streams['samples'], frames)
        hardware = streams['hw'] if title == 'all' else steady_samples(streams['hw'], frames)
        summary[title] = dict(cost=costs(rows, samples, hardware),
                             experience=experience(rows, hardware, target_ms(manifest), budgets['perfAnomalies'], scope, reason))
    summary['coverage'] = coverage(streams, dictionary, manifest)
    summary['validity'] = validity(frames, streams, manifest)
    summary['unavailableDetectors'] = [
        dict(types=['hang', 'mountBudget', 'mountChurn', 'cacheMiss', 'backlogStall', 'loadingDuration', 'physicalStepLimit', 'presentMode'],
             reason='schema 1 尚无对应已接通字段或已批准的事件 payload；不猜字段、不输出正常结论')]
    summary['timeline'] = timeline(frames, events)
    windows, window_items = window_analysis(frames, manifest, budgets, scope)
    summary['windows'] = windows
    summary['budgets'] = budget_checks(summary, manifest, budgets)
    items = (frame_anomalies(frames, manifest, budgets, medians, scope) + window_items
             + budget_items(summary, frames, budgets) + slow_anomalies(frames, streams, manifest, budgets, scope))
    if frames and summary['validity']:
        mark = anomaly('dataValidity', 'info', frames[0], None, None, 'game/data/perf-fields.json', state=True)
        mark['candidates'] = [v['field'] + ': ' + v['reason'] for v in summary['validity']]
        items.append(mark)
    for item in items:
        item['bottleneck'] = next((w['bottleneck'] for w in windows if w['fromMs'] <= item['fromMs'] <= w['toMs']), None) if scope == 'full' else None
    merged = merge_anomalies(items)
    # Repeated clusters are counted before truncating the human-facing anomaly list.
    summary['deepen'] = deepen(merged, {**manifest, 'scene': key['scene']})
    for item in summary['deepen']:
        frame = next((f for f in frames if f['collectionFrame'] == item['firstFrame']), None)
        item['sourceSessionDirectory'] = str(directory)
        item['context'] = {name: frame['_sample'].get(name) if frame else None for name in
                           ('map', 'mode', 'xM', 'yM', 'zM', 'season', 'worldHours', 'weather')}
        item['context'].update({name: frame.get(name) if frame else None for name in ('observerYawDeg', 'cameraPitchDeg')})
    summary['anomalyCounts'] = dict(Counter(item['type'] for item in merged))
    summary['conclusion'] = ('仅成本口径；体验字段不可用。' if scope == 'cost' else '交互桌面体验与成本口径。') + '候选原因需诊断证据；不由 GPU 占用推出瓶颈。'
    return summary, merged, frames, hardware_clock(streams['hwfast'], manifest)


def steady_samples(samples, frames):
    times = [frame['monotonicMs'] for frame in frames]
    result = []
    for sample in samples:
        index = bisect.bisect_right(times, sample['monotonicMs']) - 1
        if index >= 0 and frames[index]['_steady'] and not frames[index]['_gap']:
            result.append(sample)
    return result


def write_hotframes(path, frames, items, dictionary, radius):
    selected = set()
    for item in items:
        if LEVELS[item['level']] < 2:
            continue
        hits = [i for i, frame in enumerate(frames) if item['firstSequence'] <= frame['sequence'] <= item['lastSequence']]
        if hits:
            selected.update(range(max(0, hits[0] - radius), min(len(frames), hits[-1] + radius + 1)))
    columns = dictionary['streams']['frames']['columns']
    names = [c['name'] for c in columns]
    with path.open('w', encoding='utf-8', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=names + ['steady', 'associationPhase'])
        writer.writeheader()
        for index in sorted(selected):
            frame = frames[index]
            row = {key: json.dumps(frame.get(key), ensure_ascii=False, separators=(',', ':'))
                   if not isinstance(frame.get(key), str) else frame[key] for key in names}
            writer.writerow({**row, 'steady': str(frame['_steady']).lower(), 'associationPhase': frame['_phase'] or 'null'})


def source_stamp(directory, dictionary, budgets, run_path):
    paths = [directory / 'manifest.json'] + [directory / s['file'] for s in dictionary['streams'].values()]
    if run_path:
        paths.append(run_path)
    return dict(files={str(path): [path.stat().st_size, path.stat().st_mtime_ns] for path in paths if path.is_file()},
                schema=dictionary['schema'], budgets=budgets)


def write_session(out, summary, items, frames, hwfast, dictionary, stamp, budgets):
    directory = out / 'derived' / summary['id']
    directory.mkdir(parents=True, exist_ok=True)
    counts = Counter()
    limited = []
    for item in items:
        counts[item['type']] += 1
        if counts[item['type']] <= MAX_PER_TYPE:
            limited.append(item)
    atomic_json(directory / 'anomalies.json', dict(calc=CALC, id=summary['id'], total=len(items), items=limited))
    atomic_json(directory / 'hardware.json', dict(calc=CALC, id=summary['id'], hwfast=hwfast))
    write_hotframes(directory / 'hotframes.csv', frames, items, dictionary, budgets['perfAnomalies']['associationFrames'])
    summary['inputStamp'] = stamp
    atomic_json(directory / 'summary.json', summary)


def analyze_all(root=ROOT, sessions=(), output=None, author=None):
    root = Path(root).resolve()
    out = Path(output).resolve() if output else root / 'output/perf'
    author = Path(author).resolve() if author else author_root(root)
    dictionary, budgets = read_json(root / 'game/data/perf-fields.json'), read_json(root / 'budgets.json')
    changed, skipped, errors = [], [], []
    with exclusive(out / '.lock', timeout=30, create=True) as held:
        if not held:
            return dict(analyzed=[], skipped=['分析锁被占用'], errors=[])
        # Discover and recheck under the shared lock: concurrent triggers cannot append duplicate history.
        for directory in discover(out, author, sessions):
            with exclusive(directory / '.lock') as readable:
                if not readable:
                    skipped.append(str(directory))
                    continue
                try:
                    manifest = read_json(directory / 'manifest.json')
                    identifier = session_id(manifest['id'])
                    if manifest.get('status') != 'closed':
                        skipped.append(str(directory))
                        continue
                    run, run_path = nearest_run(directory, out / 'runs')
                    stamp = source_stamp(directory, dictionary, budgets, run_path)
                    target = out / 'derived' / identifier / 'summary.json'
                    if target.exists():
                        prior = read_json(target)
                        if prior.get('source') != str(directory):
                            raise ValueError('会话 ID 与另一路径冲突，不覆盖已有派生')
                        if prior.get('calc') == CALC and prior.get('inputStamp') == stamp:
                            continue
                    manifest, streams = load_session(directory, dictionary)
                    summary, items, frames, hwfast = summary_for(directory, manifest, streams, dictionary, budgets, run)
                    write_session(out, summary, items, frames, hwfast, dictionary, stamp, budgets)
                    changed.append(summary)
                except (ValueError, OSError, KeyError, TypeError) as error:
                    errors.append(dict(source=str(directory), reason=str(error)))
        entries = []
        for path in sorted((out / 'derived').glob('*/summary.json')):
            summary = read_json(path)
            if Path(summary['source']).exists():
                entries.append(summary)
        entries = rebuild_states(entries, root)
        for entry in entries:
            atomic_json(out / 'derived' / entry['id'] / 'summary.json', entry)
        removed = prune(out, entries)
        entries = [entry for entry in entries if Path(entry['source']).exists()]
        changed_ids = {summary['id'] for summary in changed}
        history_update(out / 'history.jsonl', [entry for entry in entries if entry['id'] in changed_ids])
        atomic_json(out / 'index.json', dict(calc=CALC, sessions=[index_entry(e) for e in entries], errors=errors,
                                            retention=dict(aiBatches=AI_BATCHES_KEEP, historyPerGroup=HISTORY_PER_GROUP,
                                                           diagnosticBytes=DIAGNOSTIC_BYTES_KEEP), removed=removed))
    return dict(analyzed=[s['id'] for s in changed], skipped=skipped, errors=errors)


def index_entry(summary):
    return {key: summary.get(key) for key in ('id', 'calc', 'schema', 'source', 'origin', 'endedAt', 'scope',
            'comparisonKey', 'comparisonLabels', 'status', 'statusReasons', 'build', 'anomalyCounts', 'recordedSeconds')}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('sessions', nargs='*', type=Path)
    parser.add_argument('--root', type=Path, default=ROOT)
    parser.add_argument('--author-root', type=Path)
    args = parser.parse_args()
    result = analyze_all(args.root, args.sessions, author=args.author_root)
    print(json.dumps(result, ensure_ascii=False))
    return 1 if result['errors'] else 0


if __name__ == '__main__':
    raise SystemExit(main())
