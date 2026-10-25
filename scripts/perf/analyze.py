"""计算每个已关闭会话的指标，并按场景发布最近一次 AI 采集与上次差值。
用法：python -I -X utf8 scripts/perf/analyze.py [--root 仓库] [--author-root 作者perf根]。
输入：记录器原始流、场景 run.json 与 budgets.json；产出：derived 摘要和 index.json；原始记录只读。
"""
import argparse
import bisect
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.dont_write_bytecode = True
from perf_model import (CALC, COSTS, PHASES, at, costs, experience, load_session,
                        manifest_path, number, prepare_frames, read_json, target_ms)
from perf_store import atomic_json, author_root, discover, exclusive, session_run, session_run_path
from perf_waits import wait_observations

ROOT = Path(__file__).resolve().parents[2]


def session_id(value):
    if not isinstance(value, str) or not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9._-]*', value) or value in ('.', '..'):
        raise ValueError('会话 ID 不是安全目录名')
    return value


def sample_window(events, run):
    identifier = run.get('sampleSegmentId')
    if not identifier:
        return None
    marks = [e for e in events if e['event'] == 'segment' and e['payload'].get('id') == identifier]
    begin = next((e for e in marks if e['payload'].get('phase') == 'begin'), None)
    end = next((e for e in marks if e['payload'].get('phase') == 'end'), None)
    if begin is None or end is None or begin['monotonicMs'] >= end['monotonicMs']:
        raise ValueError('采样段 Begin/End 缺失或次序错误')
    return dict(segmentId=identifier, fromMs=begin['monotonicMs'], toMs=end['monotonicMs'])


def sampled_streams(streams, window):
    if window is None:
        return streams
    start, end = window['fromMs'], window['toMs']
    result = {name: [row for row in rows if start < row['monotonicMs'] <= end] for name, rows in streams.items()}
    result['frames'] = [f for f in result['frames'] if number(f.get('dtMs')) and f['monotonicMs'] - f['dtMs'] >= start]
    return result


def steady_samples(samples, frames):
    times = [f['monotonicMs'] for f in frames]
    return [s for s in samples if (i := bisect.bisect_right(times, s['monotonicMs']) - 1) >= 0
            and frames[i]['_steady'] and not frames[i]['_gap']]


def budget_checks(metrics, budgets):
    target = budgets['deviceTargets']['developer']
    keys = dict(mainViewportGpuMs='mainViewportGpuP95MsUpper', mainViewportCpuMs='viewportCpuP95MsUpper',
                frameSetupCpuMs='frameSetupCpuP95MsUpper')
    return [dict(metric=metric, value=metrics.get(metric), budget=target[key],
                 deltaToBudget=metrics[metric] - target[key] if number(metrics.get(metric)) else None,
                 budgetSource='budgets.json.deviceTargets.developer.' + key) for metric, key in keys.items()]


def summary_for(directory, manifest, streams, dictionary, budgets, run):
    from perf_model import scope_of
    scope, reason = scope_of(manifest)
    prepare_frames(streams['frames'], streams['events'], dictionary)
    window = sample_window(streams['events'], run)
    waits = wait_observations(manifest, streams['events'], run, budgets)
    selected = sampled_streams(streams, window)
    frames = selected['frames']
    summary = dict(id=session_id(manifest['id']), calc=CALC, schema=manifest['schema'], source=str(directory),
                   origin=manifest.get('origin'), scene=run.get('scene') or manifest.get('scene'),
                   endedAt=manifest.get('endedAt'), startedAt=manifest.get('startedAt'),
                   recordedSeconds=manifest.get('recordedSeconds'), scope=scope, scopeReason=reason,
                   sampleWindow=window, run=run, waits=waits)
    for title, rows in (('all', frames), ('steady', [f for f in frames if f['_steady']])):
        samples = selected['samples'] if title == 'all' else steady_samples(selected['samples'], frames)
        hardware = selected['hw'] if title == 'all' else steady_samples(selected['hw'], frames)
        summary[title] = dict(cost=costs(rows, samples, hardware),
                             experience=experience(rows, hardware, target_ms(manifest), budgets['perfAnomalies'], scope, reason))
    summary['metrics'] = {key: at(summary, 'steady.cost.' + key + '.p95') for key in COSTS + PHASES}
    summary['metrics'].update(measuredViewportGpuMs=at(summary, 'steady.cost.measuredViewportGpuMs.p95'),
                              workingSetMiB=at(summary, 'all.cost.memory.workingSetMiB.p95'),
                              engineVideoMemoryMiB=at(summary, 'all.cost.memory.engineVideoMemoryMiB.p95'))
    summary['metrics'].update(saveDurationMs=at(run, 'observed.save.durationMs'), closeDurationMs=at(run, 'close.durationMs'))
    summary['budgets'] = budget_checks(summary['metrics'], budgets)
    return summary


def source_stamp(directory, dictionary, budgets):
    paths = [manifest_path(directory), *[directory / s['file'] for s in dictionary['streams'].values()]]
    run_path = session_run_path(directory)
    if run_path:
        paths.append(run_path)
    return dict(files={str(p): [p.stat().st_size, p.stat().st_mtime_ns] for p in paths if p.is_file()},
                calc=CALC, schema=dictionary['schema'], contextFlags=dictionary['contextFlags'],
                nonSteadyRules=dictionary['nonSteadyRules'], budgets=budgets)


def processed_input(out, directory, stamp):
    path = out / 'derived' / session_id(read_json(manifest_path(directory))['id'])
    if not (path / 'summary.json').is_file():
        return False
    summary = read_json(path / 'summary.json')
    return summary.get('source') == str(directory) and summary.get('inputStamp') == stamp


def metric_delta(current, previous):
    result = []
    for metric, value in current.items():
        before = previous.get(metric) if previous else None
        measured = number(value) and number(before)
        result.append(dict(metric=metric, previous=before, value=value,
                           delta=value - before if measured else None,
                           changeFraction=(value - before) / before if measured and before else None))
    return result


def scene_results(entries):
    groups = {}
    for entry in entries:
        if entry.get('origin') == 'ai' and entry.get('scene'):
            groups.setdefault(entry['scene'], []).append(entry)
    result = []
    for scene, rows in sorted(groups.items()):
        rows.sort(key=lambda row: (row.get('endedAt') or row.get('startedAt') or '', row['id']))
        current = rows[-1]
        previous = rows[-2] if len(rows) > 1 else None
        delta = metric_delta(current['metrics'], previous['metrics'] if previous else None)
        result.append(dict(scene=scene, id=current['id'], previousId=previous['id'] if previous else None,
                           metrics=current['metrics'], delta=delta, budgets=current['budgets']))
    return result


def analyze_all(root=ROOT, output=None, author=None):
    root = Path(root).resolve()
    out = Path(output).resolve() if output else root / 'output/perf'
    author = Path(author).resolve() if author else author_root(root)
    dictionary, budgets = read_json(root / 'game/data/perf-fields.json'), read_json(root / 'budgets.json')
    changed, skipped, errors, entries = [], [], [], []
    with exclusive(out / '.lock', timeout=30, create=True) as held:
        if not held:
            return dict(analyzed=[], skipped=['分析锁被占用'], errors=[])
        prior = read_json(out / 'index.json') if (out / 'index.json').is_file() else {}
        for directory in discover(out, author):
            stamp = None
            try:
                with exclusive(directory / '.lock') as readable:
                    if not readable or not (directory / 'manifest.json').is_file():
                        skipped.append(str(directory))
                        continue
                    manifest = read_json(directory / 'manifest.json')
                    identifier = session_id(manifest['id'])
                    target = out / 'derived' / identifier
                    stamp = source_stamp(directory, dictionary, budgets)
                    cached = next((e for e in prior.get('errors', []) if e.get('source') == str(directory)
                                   and e.get('inputStamp') == stamp), None)
                    if cached:
                        errors.append(cached)
                        continue
                    if not processed_input(out, directory, stamp):
                        if (target / 'summary.json').is_file() and read_json(target / 'summary.json').get('source') != str(directory):
                            raise ValueError('会话 ID 与另一路径冲突')
                        manifest, streams = load_session(directory, dictionary)
                        if streams is None:
                            skipped.append(str(directory))
                            continue
                        run, _ = session_run(directory)
                        summary = summary_for(directory, manifest, streams, dictionary, budgets, run)
                        summary['inputStamp'] = stamp
                        atomic_json(target / 'summary.json', summary)
                        changed.append(identifier)
                    entries.append(read_json(target / 'summary.json'))
            except (OSError, ValueError, KeyError, TypeError) as error:
                errors.append(dict(source=str(directory), reason=str(error), inputStamp=stamp))
        public = [{k: e.get(k) for k in ('id', 'calc', 'schema', 'source', 'origin', 'scene', 'endedAt', 'scope', 'metrics', 'budgets')}
                  for e in entries]
        index = dict(calc=CALC, sessions=public, scenes=scene_results(public), errors=errors)
        path = out / 'index.json'
        if not path.is_file() or read_json(path) != index:
            atomic_json(path, index)
    return dict(analyzed=changed, skipped=skipped, errors=errors)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=ROOT)
    parser.add_argument('--author-root', type=Path)
    parser.add_argument('--output', type=Path, help='共享 output/perf 目录')
    args = parser.parse_args()
    result = analyze_all(args.root, output=args.output, author=args.author_root)
    print(json.dumps(result, ensure_ascii=False))
    return 1 if result['errors'] else 0


if __name__ == '__main__':
    raise SystemExit(main())
