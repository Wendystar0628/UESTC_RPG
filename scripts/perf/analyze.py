"""分析可独占的性能会话，串行发布派生、索引、历史并修剪 AI 数据。
用法：python -I -X utf8 scripts/perf/analyze.py [--root 仓库] [--author-root 作者perf根]。
输入：性能六件、game/data/perf-fields.json、budgets.json；产出：output/perf/derived/<id>/ 的摘要、异常、曲线与高耗时帧、index.json、history.jsonl。
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
from perf_model import (CALC, COSTS, PHASES, at, baseline_readout, costs, experience, load_session, manifest_path, number,
                        prepare_frames, read_json, rolling_medians, scope_of, target_ms)
from perf_anomalies import (LEVELS, MAX_PER_TYPE, annotate, anomaly, budget_checks,
                           frame_anomalies, hang_anomalies, merge_anomalies, session_judgement,
                           slow_anomalies, window_analysis)
from perf_store import (AI_BATCHES_KEEP, HISTORY_PER_GROUP,
                        atomic_json, author_root, basic_state, batch_states, comparison_key, discover,
                        exclusive, git, history_update, session_run, session_run_path, prune, rebuild_states)
from perf_curves import curve_document
from perf_batches import assess_rounds, batch_results, prepare_round, read_rounds, round_sources, sample_window, sampled_streams, validate_rounds
from perf_waits import wait_observations

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
    window, sampling_reasons = sample_window(manifest, streams, run) if streams is not None else (None, [])
    whole_streams = streams
    waits = wait_observations(manifest, streams['events'], run, budgets) if streams is not None else None
    if streams is not None:
        prepare_frames(streams['frames'], streams['samples'], streams['events'], dictionary)
        if window is not None:
            streams = sampled_streams(streams, window)
            if not streams['frames']:
                sampling_reasons.append('正式采样段没有完整帧区间')
        elif manifest.get('origin') == 'ai' and run.get('purpose') == 'baseline':
            streams = {name: [] for name in streams}
    status, reasons = basic_state(manifest, streams, run, missing, sampling_reasons)
    summary = dict(id=session_id(manifest['id']), calc=CALC, schema=manifest['schema'], origin=manifest.get('origin'),
                   source=str(directory), endedAt=manifest.get('endedAt'), startedAt=manifest.get('startedAt'),
                   recordedSeconds=manifest.get('recordedSeconds'), scope=scope, scopeReason=reason,
                   comparisonKey=key, comparisonLabels=labels, comparisonMissing=missing,
                   baseStatus=status, baseReasons=reasons, status=status, statusReasons=reasons,
                   build=manifest.get('build'), environmentSignature={k: at(manifest, k) for k in
                         ('machine.driver', 'machine.os', 'build.godot')}, run=run,
                   sampleWindow=window, sampleReasons=sampling_reasons,
                   all=None, steady=None, coverage=None, validity=[], budgets=[], anomalyCounts={},
                   waits=waits, judgement=None,
                   recording={key: manifest.get(key) for key in ('status', 'complete', 'fault', 'endReason', 'streams')},
                   eventDelivery=event_delivery(whole_streams['events']) if whole_streams is not None else None,
                   units=dict(frame='ms', memory='MiB', fractions='0..1', power='W', frequency='MHz', temperature='degC'))
    if streams is not None:
        frames, events = streams['frames'], streams['events']
        summary['baselineReadout'] = baseline_readout(frames, manifest, run, budgets, window, whole_streams['frames'])
    else:
        summary['baselineReadout'] = baseline_readout([], manifest, run, budgets, window, [])
    if streams is None or sampling_reasons:
        return summary, [], [], []
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
        dict(types=['mountBudget', 'mountChurn', 'cacheMiss', 'backlogStall', 'physicalStepLimit', 'presentMode', 'manualSaveThumbnail'],
             reason='本批尚未接通对应完整判据或手动缩略帧不在正式样段内，明确未判')]
    summary['streamFacts'] = {name: dict(events=len(rows), latest=rows[-1]['payload'] if rows else None)
                              for name in ('stream.tileChanged', 'bake.cache', 'map.arrival')
                              for rows in [[e for e in whole_streams['events'] if e['event'] == name]]}
    summary['timeline'] = timeline(frames, events)
    windows, window_items = window_analysis(frames, manifest, budgets, scope)
    summary['windows'] = windows
    summary['budgets'] = budget_checks(summary, manifest, budgets)
    items = (frame_anomalies(frames, manifest, budgets, medians, scope, run) + window_items
             + budget_items(summary, frames, budgets) + slow_anomalies(frames, streams, manifest, budgets, scope)
             + hang_anomalies(whole_streams['frames'], whole_streams['events']))
    summary['judgement'] = session_judgement(frames, streams, manifest, budgets, run, items, summary['budgets'])
    if frames and summary['validity']:
        mark = anomaly('dataValidity', 'info', frames[0], None, None, 'game/data/perf-fields.json', state=True)
        mark['candidates'] = [v['field'] + ': ' + v['reason'] for v in summary['validity']]
        items.append(mark)
    for item in items:
        item['bottleneck'] = next((w['bottleneck'] for w in windows if w['fromMs'] <= item['fromMs'] <= w['toMs']), None) if scope == 'full' else None
    merged = merge_anomalies(items)
    summary['anomalyCounts'] = dict(Counter(item['type'] for item in merged))
    summary['conclusion'] = ('仅成本口径；体验字段不可用。' if scope == 'cost' else '交互桌面体验与成本口径。') + '候选原因需诊断证据；不由 GPU 占用推出瓶颈。'
    return summary, merged, frames, hardware_clock(streams['hwfast'], manifest)


def event_delivery(events):
    """Project full-session delivery facts before sample clipping, including partial sessions."""
    # load_session sorts by observation time; loss neighbours must retain the actual writer order.
    ordered = sorted(events, key=lambda event: event['sequence'])
    metadata = ('sequence', 'collectionFrame', 'monotonicMs', 'event', 'source')
    gaps = []
    for index, event in enumerate(ordered):
        if event['event'] == 'record.gap':
            gaps.append(dict(**{key: event.get(key) for key in metadata}, payload=event['payload'],
                             neighbours=[{key: row.get(key) for key in metadata}
                                         for row in ordered[max(0, index - 2):index] + ordered[index + 1:index + 3]]))
    return dict(counts=dict(sorted(Counter(event['event'] for event in events).items())), gaps=gaps)


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
    return [frames[index] for index in sorted(selected)]


def source_stamp(directory, dictionary, budgets, run_path):
    paths = [manifest_path(directory)] + [directory / s['file'] for s in dictionary['streams'].values()]
    if run_path:
        paths.append(run_path)
    return dict(files={str(path): [path.stat().st_size, path.stat().st_mtime_ns] for path in paths if path.is_file()},
                schema=dictionary['schema'], budgets=budgets,
                contextFlags=dictionary['contextFlags'], nonSteadyRules=dictionary['nonSteadyRules'])


def repository_state(root):
    return [list(git(root, 'rev-parse', 'HEAD')), list(git(root, 'status', '--porcelain', '--', 'game/'))]


def conflicting_source(out, identifier, directory, sources):
    target = out / 'derived' / identifier / 'summary.json'
    if target.is_file():
        owner = read_json(target).get('source')
        if owner != str(directory) and owner in sources:
            return owner
    return None


def processed_input(out, directory, stamp, errors, sources):
    """内容错误随输入缓存；冲突每轮重查持有方，IO 失败不算已处理。"""
    if any(error.get('kind') == 'content' and error.get('source') == str(directory)
           and (error.get('calc') != CALC or error.get('inputStamp') != stamp) for error in errors):
        return False
    for error in errors:
        if (error.get('kind') == 'content' and error.get('source') == str(directory)
                and error.get('calc') == CALC and error.get('inputStamp') == stamp):
            return True
    if any(error.get('source') == str(directory) and error.get('kind') not in ('content', 'conflict') for error in errors):
        return False
    try:
        identifier = session_id(read_json(manifest_path(directory))['id'])
        owner = conflicting_source(out, identifier, directory, sources)
        if owner:
            return any(error.get('kind') == 'conflict' and error.get('source') == str(directory)
                       and error.get('conflictSource') == owner and error.get('calc') == CALC
                       and error.get('inputStamp') == stamp for error in errors)
        target = out / 'derived' / identifier
        if not (target / 'summary.json').is_file():
            return False
        prior = read_json(target / 'summary.json')
        return (prior.get('source') == str(directory) and prior.get('calc') == CALC and
                'recording' in prior and 'eventDelivery' in prior and
                prior.get('inputStamp') == stamp and all((target / name).is_file() for name in
                    ('curves.json', 'anomalies.json', 'hardware.json', 'hotframes.csv')))
    except (ValueError, OSError, KeyError, TypeError):
        return False


def analysis_needed(root=ROOT, output=None, author=None, index=None, dictionary=None, budgets=None):
    """只读判鲜入口；不读逐帧流，不发布文件，活动锁与无锁 pending 不参与。"""
    root = Path(root).resolve()
    out = Path(output).resolve() if output else root / 'output/perf'
    author = Path(author).resolve() if author else author_root(root)
    index = index if index is not None else read_json(out / 'index.json') if (out / 'index.json').is_file() else {}
    dictionary = dictionary if dictionary is not None else read_json(root / 'game/data/perf-fields.json')
    budgets = budgets if budgets is not None else read_json(root / 'budgets.json')
    rounds = read_rounds(out)
    directories = discover(out, author, [str(source) for row in rounds for source in round_sources(row)])
    sources = {str(directory) for directory in directories}
    if any(entry.get('source') not in sources for entry in index.get('sessions', []) + index.get('errors', [])):
        return True
    available = False
    for directory in directories:
        try:
            with exclusive(directory / '.lock') as readable:
                if not readable or (manifest_path(directory).name == 'manifest.pending.json' and not (directory / '.lock').is_file()):
                    continue
                available = True
                stamp = source_stamp(directory, dictionary, budgets, session_run_path(directory))
                if not processed_input(out, directory, stamp, index.get('errors', []), sources):
                    return True
        except OSError:
            return True
    return available and (index.get('calc') != CALC or index.get('rounds') != rounds or index.get('repositoryState') != repository_state(root))


def write_session(out, summary, items, frames, hwfast, dictionary, stamp, budgets, manifest, streams):
    directory = out / 'derived' / summary['id']
    directory.mkdir(parents=True, exist_ok=True)
    counts = Counter()
    limited = []
    for item in items:
        counts[item['type']] += 1
        if counts[item['type']] <= MAX_PER_TYPE:
            limited.append(item)
    atomic_json(directory / 'anomalies.json', dict(calc=CALC, id=summary['id'], total=len(items), items=limited))
    if streams is not None and summary.get('sampleWindow') is not None:
        streams = sampled_streams(streams, summary['sampleWindow'])
    if summary.get('sampleReasons'):
        streams = None
    atomic_json(directory / 'hardware.json', dict(calc=CALC, id=summary['id'], hw=streams['hw'] if streams is not None else None, hwfast=hwfast))
    hotframes = write_hotframes(directory / 'hotframes.csv', frames, items, dictionary, budgets['perfAnomalies']['associationFrames'])
    atomic_json(directory / 'curves.json', curve_document(summary, manifest, streams, hotframes))
    summary['inputStamp'] = stamp
    atomic_json(directory / 'summary.json', summary)


def analyze_all(root=ROOT, output=None, author=None, round_request=None, preview=False):
    root = Path(root).resolve()
    out = Path(output).resolve() if output else root / 'output/perf'
    author = Path(author).resolve() if author else author_root(root)
    dictionary, budgets = read_json(root / 'game/data/perf-fields.json'), read_json(root / 'budgets.json')
    changed, skipped, errors = [], [], []
    with exclusive(out / '.lock', timeout=30, create=True) as held:
        if not held:
            return dict(analyzed=[], skipped=['分析锁被占用'], errors=[], registrationError='分析锁被占用')
        prior_index = read_json(out / 'index.json') if (out / 'index.json').is_file() else {}
        # Discover and recheck under the shared lock: concurrent triggers cannot append duplicate history.
        rounds = read_rounds(out)
        proposed = prepare_round(rounds, **round_request) if round_request is not None else None
        projected_rounds = ([row for row in rounds if row['id'] != proposed['id']] + [proposed]) if proposed else rounds
        directories = discover(out, author, [str(source) for row in projected_rounds for source in round_sources(row)])
        sources = {str(directory) for directory in directories}
        for directory in directories:
            stamp, identifier, owner = None, None, None
            try:
                with exclusive(directory / '.lock') as readable:
                    if not readable or (manifest_path(directory).name == 'manifest.pending.json'
                                        and not (directory / '.lock').is_file()):
                        skipped.append(str(directory))
                        continue
                    stamp = source_stamp(directory, dictionary, budgets, session_run_path(directory))
                    prior_errors = prior_index.get('errors', [])
                    if processed_input(out, directory, stamp, prior_errors, sources):
                        errors.extend(error for error in prior_errors if error.get('source') == str(directory))
                        continue
                    manifest = read_json(manifest_path(directory))
                    identifier = session_id(manifest['id'])
                    run, _ = session_run(directory)
                    owner = conflicting_source(out, identifier, directory, sources)
                    if owner:
                        errors.append(dict(source=str(directory), id=identifier, kind='conflict',
                                           conflictSource=owner, reason='会话 ID 与另一路径冲突，不覆盖已有派生',
                                           calc=CALC, inputStamp=stamp))
                        continue
                    manifest, streams = load_session(directory, dictionary)
                    summary, items, frames, hwfast = summary_for(directory, manifest, streams, dictionary, budgets, run)
                    write_session(out, summary, items, frames, hwfast, dictionary, stamp, budgets, manifest, streams)
                    changed.append(summary)
            except OSError as error:
                errors.append(dict(source=str(directory), id=identifier, kind='io',
                                   reason=str(error), calc=CALC, inputStamp=stamp))
            except Exception as error:
                errors.append(dict(source=str(directory), id=identifier,
                                   kind='content',
                                   reason=str(error), calc=CALC, inputStamp=stamp))
        entries = []
        for path in sorted((out / 'derived').glob('*/summary.json')):
            summary = read_json(path)
            if summary['source'] in sources:
                entries.append(summary)
        failed_sources = {error['source'] for error in errors}
        entries = rebuild_states(entries, root)
        projection, registration_error = None, None
        if proposed is not None:
            projected = batch_states(batch_results(entries, budgets, failed_sources, projected_rounds), entries, root)
            assess_rounds(projected)
            projection = next(row for row in projected
                              if row['id'] == proposed['id'])
            confirmation_sources = {row['source'] for row in entries if at(row, 'run.confirmationOf')}
            pending = [source for source in skipped
                       if Path(source).resolve().is_relative_to(Path(proposed['sourceRoot']).resolve())
                       and source not in confirmation_sources]
            if pending:
                projection['statusReasons'].extend('轮来源仍被写入锁占用或尚未关闭: ' + source for source in pending)
                projection['status'], projection['gateStatus'] = 'partial', 'Incomplete'
            if projection['status'] == 'partial' or len(projection['groups']) != len(proposed['scenes']):
                registration_error = '；'.join(projection['statusReasons']) or '轮场景覆盖不完整'
            elif round_request.get('confirmation_root') is not None and not any(
                    Path(row['source']).resolve().is_relative_to(Path(round_request['confirmation_root']).resolve())
                    and row['id'] in {identifier for group in projection['groups'] for identifier in group['sessionIds'][1:]}
                    for row in entries):
                registration_error = '追加来源没有本轮所需的完整一次确认'
            if not preview and registration_error is None:
                proposed['mainSessionIds'] = {group['comparisonKey']['scene']: group['mainSessionId']
                                              for group in projection['groups']}
                # Recheck identity and N-1 under the same lock that publishes analysis and registration.
                if read_rounds(out) != rounds:
                    raise ValueError('轮登记在分析期间发生变化')
                rounds = validate_rounds([row for row in rounds if row['id'] != proposed['id']] + [proposed])
                atomic_json(out / 'rounds.json', dict(rounds=sorted(rounds, key=lambda row: row['number'])))
        batches = batch_results(entries, budgets, failed_sources, rounds)
        batches = batch_states(batches, entries, root)
        assess_rounds(batches)
        for entry in entries:
            atomic_json(out / 'derived' / entry['id'] / 'summary.json', entry)
        # 保留与身份判定仍使用失败来源上一份摘要，不能把旧会话升成当前基线。
        removed = prune(out, entries)
        errors = [error for error in errors if not any(
            Path(error['source']).is_relative_to(Path(path)) for path in removed)]
        failed_sources = {error['source'] for error in errors}
        entries = [entry for entry in entries if Path(entry['source']).exists()]
        available = [entry for entry in entries if entry['source'] not in failed_sources]
        changed_ids = {summary['id'] for summary in changed}
        history_update(out / 'history.jsonl', [entry for entry in available if entry['id'] in changed_ids])
        index = dict(calc=CALC, rounds=rounds, repositoryState=repository_state(root), batches=batches,
                                            sessions=[index_entry(e) for e in available], errors=errors,
                                            retention=dict(aiBatches=AI_BATCHES_KEEP, historyPerGroup=HISTORY_PER_GROUP), removed=removed)
        # 相同 IO 故障仍可由下一次外部采集重试，但不自写 index 制造文件通知空转。
        if index != prior_index:
            atomic_json(out / 'index.json', index)
    return dict(analyzed=[s['id'] for s in changed], skipped=skipped, errors=errors,
                projection=projection, registrationError=registration_error)


def index_entry(summary):
    return {key: summary.get(key) for key in ('id', 'calc', 'schema', 'source', 'origin', 'endedAt', 'scope',
            'comparisonKey', 'comparisonLabels', 'status', 'statusReasons', 'build', 'anomalyCounts', 'recordedSeconds',
            'judgement', 'waits')}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=ROOT)
    parser.add_argument('--author-root', type=Path)
    args = parser.parse_args()
    result = analyze_all(args.root, author=args.author_root)
    print(json.dumps(result, ensure_ascii=False))
    return 1 if result['errors'] else 0


if __name__ == '__main__':
    raise SystemExit(main())
