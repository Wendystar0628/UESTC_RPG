"""验证样段裁剪、批次完整性、基线失败与提交判鲜。
用法：由 perf.py selfcheck 编排调用，复用 selfcheck_fixtures，不写真实会话。
"""
import copy
import json
import sys
import tempfile
from pathlib import Path
from unittest.mock import patch
from analyze import analyze_all, summary_for
from perf_model import at, load_session, read_json
from selfcheck_fixtures import ai_run, manifest, mark_sample, frame, streams, write_six


def check_sampling_batches(root, dictionary, budgets):
    """Regression：准备期极值不进样段；不完整批次与短跑不能产生基线。"""
    from perf_batches import batch_results
    from perf_store import git
    warm = budgets['perfAnomalies']['medianMinimumFrames'] + 3
    rows = [frame(dictionary, i, gpu=2 if i < warm else 100) for i in range(warm + 4)]
    rows[0]['gcPauseMs'] = 999
    start, end = rows[warm - 1]['monotonicMs'] + 5, rows[warm + 2]['monotonicMs']
    meta = manifest(dictionary, 'SAMPLE')
    data = mark_sample(streams(rows, dictionary), dictionary, meta, begin=start, end=end)
    meta.update(origin='ai')
    meta['build']['commit'] = git(root, 'rev-parse', 'HEAD')[1]
    sample_meta = copy.deepcopy(meta)
    run_value = ai_run()
    summary, items, sampled, _ = summary_for(Path('fixture'), copy.deepcopy(meta), copy.deepcopy(data), dictionary, budgets, run_value)
    assert sampled and len(sampled) == 2 and sampled[0]['monotonicMs'] - sampled[0]['dtMs'] >= start, '夹具没有排除跨界帧'
    assert at(summary, 'all.cost.mainViewportGpuMs.n') == 2 and at(summary, 'all.cost.mainViewportGpuMs.p95') == 100
    assert at(summary, 'all.cost.gcTotals.gcPauseMs') == 0, '准备期 GC 进入样段'
    assert not any(item['type'] == 'costSpike' for item in items), '准备期 M 泄漏到短样段'
    for change in ({'samplingConditions': None}, {'samplingConditions': {**meta['samplingConditions'], 'monotonicMs': start + 1}}):
        missing = summary_for(Path('fixture'), {**meta, **change}, copy.deepcopy(data), dictionary, budgets, run_value)[0]
        assert missing['baseStatus'] == 'partial' and missing['sampleReasons'] and missing['all'] is None
    author = copy.deepcopy(meta)
    author['origin'] = 'play'
    play = summary_for(Path('fixture'), author, copy.deepcopy(data), dictionary, budgets, run_value)[0]
    assert at(play, 'all.cost.mainViewportGpuMs.n') == len(rows), '作者会话被 AI 标记裁剪'
    evidence = summary_for(Path('fixture'), meta, copy.deepcopy(data), dictionary, budgets, {})[0]
    assert evidence['baseStatus'] == 'evidence' and at(evidence, 'all.cost.mainViewportGpuMs.n') == len(rows)
    short = summary_for(Path('fixture'), meta, copy.deepcopy(data), dictionary, budgets, {**run_value, 'purpose': 'functional'})[0]
    assert short['baseStatus'] == 'evidence', '功能短跑冒充基线'
    for phases in ([], ['begin'], ['end', 'begin'], ['begin', 'begin', 'end'], ['begin', 'end', 'begin', 'end']):
        broken = copy.deepcopy(data)
        broken['events'] = [event for event in broken['events'] if event['event'] != 'segment']
        for sequence, phase in enumerate(phases, 100):
            broken['events'].append(dict(event='segment', sequence=sequence, monotonicMs=start + sequence,
                                         payload=dict(id='sample', phase=phase)))
        bad = summary_for(Path('fixture'), meta, broken, dictionary, budgets, run_value)[0]
        assert bad['baseStatus'] == 'partial' and bad['sampleReasons'] and bad['all'] is None, phases
        from perf import overview
        conclusion = list(overview(bad))[-1]
        assert '未完成' in conclusion and 'schema 不支持' not in conclusion and bad['sampleReasons'][0] in conclusion, conclusion

    delivery_data = copy.deepcopy(data)
    # This source order differs from observation-time order, as it does after load_session.
    delivery_data['events'] = [dict(event='stream.tileChanged', sequence=1, collectionFrame=4,
                                    monotonicMs=30, source='main', payload={}),
                               dict(event='record.gap', sequence=5, collectionFrame=4, monotonicMs=20,
                                    source='writer', payload=dict(stream='events', rows=3, firstSequence=2,
                                    lastSequence=4, firstCollectionFrame=4, lastCollectionFrame=4, fromMs=10, toMs=20)),
                               dict(event='session.end', sequence=6, collectionFrame=None, monotonicMs=40,
                                    source='writer', payload={})]
    delivery_data['events'].sort(key=lambda event: event['monotonicMs'])
    delivery_meta = copy.deepcopy(meta)
    delivery_meta['complete'] = False
    next(row for row in delivery_meta['streams'] if row['file'] == 'events.jsonl')['droppedRows'] = 3
    failed_run = {**run_value, 'status': 'partial', 'reasons': ['events dropped rows']}
    partial = summary_for(Path('fixture'), delivery_meta, delivery_data, dictionary, budgets, failed_run)[0]
    assert partial['sampleReasons'] and partial['all'] is None
    assert partial['recording']['complete'] is False and partial['eventDelivery']['counts']['stream.tileChanged'] == 1
    gap = partial['eventDelivery']['gaps'][0]
    assert gap['payload']['rows'] == 3 and [row['sequence'] for row in gap['neighbours']] == [1, 6]
    projection = '\n'.join(overview(partial))
    assert '采集运行状态：partial' in projection and 'events dropped rows' in projection
    assert 'events.jsonl | recorded | 缺值 | 3' in projection and 'stream.tileChanged=1' in projection
    assert '缺口源区间' in projection and '1 | 4 | 30 | stream.tileChanged | main' in projection

    with tempfile.TemporaryDirectory(prefix='uestc-perf-batch-') as temporary:
        out, author = Path(temporary) / 'perf', Path(temporary) / 'author'
        for phase_index, (phase, gpu) in enumerate((('a1', 2), ('b1', 7), ('a2', 2))):
            for repetition, value in enumerate((gpu, gpu + 10, gpu + 1), 1):
                identifier = f'{phase}-{repetition}'
                source = out / 'runs/aba' / identifier
                meta = manifest(dictionary, identifier)
                second = phase_index * 10 + repetition * 2
                meta.update(origin='ai', startedAt=f'2000-01-01T00:00:{second:02d}Z',
                            endedAt=f'2000-01-01T00:00:{second + 1:02d}Z')
                meta['build']['commit'] = git(root, 'rev-parse', 'HEAD')[1]
                sample = mark_sample(streams([frame(dictionary, i, gpu=value) for i in range(3)], dictionary), dictionary, meta)
                write_six(source, meta, sample, dictionary)
                (source / 'run.json').write_text(json.dumps(ai_run(batch='aba', phase=phase, repetition=repetition, kind='aba')), encoding='utf-8')
                if phase == 'a1' and repetition == 1:
                    assert not analyze_all(root, out, author)['errors']
                    first = read_json(out / 'index.json')
                    assert first['batches'][0]['status'] == 'partial' and first['sessions'][0]['status'] == 'partial'
                    assert 'all' not in first['batches'][0]['groups'][0]['phases']['a1'], '缺次批次输出了中位'
        result = analyze_all(root, out, author)
        assert not result['errors'], result
        batch = read_json(out / 'index.json')['batches'][0]
        assert batch['status'] in ('current', 'stale'), batch
        assert at(batch['groups'][0], 'phases.a1.steady.cost.mainViewportGpuMs.p95') == 3
        assert at(batch['groups'][0], 'phases.b1.steady.cost.mainViewportGpuMs.p95') == 8, '未按三次指标取中位'
        entries = [read_json(path) for path in (out / 'derived').glob('*/summary.json')]
        sys.path.insert(0, str(root / '.collab/tools'))
        from monitor.view.perf import trend_groups
        commits = {meta['build']['commit']: dict(seq=1, subject='selfcheck')}
        projected_entries = [{'summary': row} for row in entries]
        trends = trend_groups([batch], projected_entries, commits)
        assert len(trends) == 1 and len(trends[0]['points']) == 3, '完整 A/B/A 阶段未独立投影'
        points = {point['phase']: point for point in trends[0]['points']}
        assert points['b1']['metrics']['mainViewportGpuP95Ms'] == 8 and points['a1']['metrics']['mainViewportGpuP95Ms'] == 3
        assert all(point['anomaliesPerMinute'] is None and point['anomalyRateReason'] for point in points.values()), '展示层重算了批次未提供的异常率'
        authoritative = copy.deepcopy(batch)
        authoritative['status'], authoritative['statusReasons'] = 'stale', ['构建之后 game/ 已变更']
        authoritative['groups'][0]['phases']['b1']['steady']['cost']['mainViewportGpuMs']['p95'] = 123
        projected = trend_groups([authoritative], projected_entries, commits)[0]['points']
        assert next(point for point in projected if point['phase'] == 'b1')['metrics']['mainViewportGpuP95Ms'] == 123, '展示没有直接读取批次阶段中位'
        assert all(point['status'] == 'stale' and point['statusReasons'] for point in projected), '历史批次失去 stale 说明'
        for state in ('partial', 'invalid', 'evidence', 'unsupported', 'candidate'):
            assert not trend_groups([{**batch, 'status': state}], projected_entries, commits), state
        assert not trend_groups([], projected_entries, commits), '会话摘要绕过批次进入趋势'
        for mutate in ('duplicate', 'missing', 'key', 'commit', 'phaseCommit', 'order', 'functional'):
            changed = copy.deepcopy(entries)
            if mutate == 'duplicate':
                changed.append(copy.deepcopy(changed[0]))
            elif mutate == 'missing':
                changed.pop()
            elif mutate == 'key':
                changed[0]['comparisonKey']['preset'] = 'another'
            elif mutate == 'commit':
                next(row for row in changed if at(row, 'run.protocol.phase') == 'a2')['build']['commit'] = 'another'
            elif mutate == 'phaseCommit':
                next(row for row in changed if at(row, 'run.protocol.phase') == 'b1')['build']['commit'] = 'another'
            elif mutate == 'order':
                next(row for row in changed if at(row, 'run.protocol.phase') == 'a1')['endedAt'] = '2000-01-01T00:00:30Z'
            else:
                changed[0].update(baseStatus='evidence')
                changed[0]['run']['purpose'] = 'functional'
            invalid, _ = batch_results(changed, budgets)
            assert invalid[0]['status'] == 'partial' and all('all' not in phase for group in invalid[0]['groups']
                                                           for phase in group['phases'].values()), mutate
            assert not trend_groups(invalid, [{'summary': row} for row in changed], commits), '无效或功能批次进入趋势: ' + mutate
        # 真实六件往返也必须保持裁剪，且原始行数不变。
        source = out / 'runs/sample/SAMPLE'
        write_six(source, sample_meta, data, dictionary)
        (source / 'run.json').write_text(json.dumps(ai_run(batch='sample')), encoding='utf-8')
        assert not analyze_all(root, out, author)['errors']
        derived = read_json(out / 'derived/SAMPLE/summary.json')
        assert at(derived, 'all.cost.mainViewportGpuMs.n') == 2
        curves = read_json(out / 'derived/SAMPLE/curves.json')['levels'][0]
        assert sum(row[2] for row in curves['buckets']) == 2, '曲线保留了准备期成本'
        assert len(load_session(source, dictionary)[1]['frames']) == len(rows), '分析修改了原始数据'


def check_batch_freshness(root, dictionary, budgets):
    """Regression：真实 A→B 提交差异下，A2 留 stale，批次只由 B 目标判鲜。"""
    from argparse import Namespace
    from perf import compare
    from perf_store import git
    with tempfile.TemporaryDirectory(prefix='uestc-perf-aba-code-') as temporary:
        repo = Path(temporary) / 'repo'
        data_root = repo / 'game/data'
        data_root.mkdir(parents=True)
        (data_root / 'perf-fields.json').write_text(json.dumps(dictionary), encoding='utf-8')
        (repo / 'budgets.json').write_text(json.dumps(budgets), encoding='utf-8')
        marker = repo / 'game/fixture.txt'
        marker.write_text('A', encoding='utf-8')
        for arguments in (('init',), ('add', '.'), ('-c', 'user.name=selfcheck', '-c', 'user.email=selfcheck@example.invalid', 'commit', '-m', 'A')):
            assert git(repo, *arguments)[0] == 0, arguments
        commit_a = git(repo, 'rev-parse', 'HEAD')[1]
        marker.write_text('B', encoding='utf-8')
        assert git(repo, 'add', '.')[0] == 0
        assert git(repo, '-c', 'user.name=selfcheck', '-c', 'user.email=selfcheck@example.invalid', 'commit', '-m', 'B')[0] == 0
        commit_b = git(repo, 'rev-parse', 'HEAD')[1]
        assert commit_a != commit_b and git(repo, 'diff', '--name-only', commit_a, commit_b, '--', 'game/')[1], '夹具未产生真实 game 差异'
        out, author = repo / 'output/perf', Path(temporary) / 'author'
        for phase_index, phase in enumerate(('a1', 'b1', 'a2')):
            for repetition in (1, 2, 3):
                identifier = f'{phase}-{repetition}'
                meta = manifest(dictionary, identifier)
                second = phase_index * 10 + repetition * 2
                meta.update(origin='ai', startedAt=f'2000-01-01T00:00:{second:02d}Z',
                            endedAt=f'2000-01-01T00:00:{second + 1:02d}Z')
                meta['build']['commit'] = commit_b if phase == 'b1' else commit_a
                source = out / 'runs/aba' / identifier
                sample = mark_sample(streams([frame(dictionary, 0, gpu=7 if phase == 'b1' else 2)], dictionary), dictionary, meta)
                write_six(source, meta, sample, dictionary)
                (source / 'run.json').write_text(json.dumps(ai_run(batch='aba', phase=phase, repetition=repetition, kind='aba')), encoding='utf-8')
        assert not analyze_all(repo, out, author)['errors']
        assert read_json(out / 'derived/a2-3/summary.json')['status'] == 'stale', 'A2 被伪称当前单次基线'
        batch = read_json(out / 'index.json')['batches'][0]
        assert batch['status'] == 'current' and batch['targetPhase'] == 'b1', batch
        assert any('可比：是' in line for line in compare(out, Namespace(left='a1-1', right='b1-1')))
        marker.write_text('C', encoding='utf-8')
        assert git(repo, 'add', '.')[0] == 0
        assert git(repo, '-c', 'user.name=selfcheck', '-c', 'user.email=selfcheck@example.invalid', 'commit', '-m', 'C')[0] == 0
        assert not analyze_all(repo, out, author)['errors']
        assert read_json(out / 'index.json')['batches'][0]['status'] == 'stale', 'B 目标过时代码被冒称当前批次'
        assert any('可比：否' in line for line in compare(out, Namespace(left='a1-1', right='b1-1')))
        path = out / 'runs/aba/a1-1/manifest.json'
        original = read_json(path)
        for change in ('environment', 'dirty', 'lineage'):
            changed = copy.deepcopy(original)
            if change == 'environment':
                changed['build']['godot'] = 'another'
            elif change == 'dirty':
                changed['build']['dirty'] = True
            else:
                changed['build']['commit'] = '0' * len(commit_a)
            path.write_text(json.dumps(changed), encoding='utf-8')
            assert not analyze_all(repo, out, author)['errors']
            batch = read_json(out / 'index.json')['batches'][0]
            assert batch['status'] == 'partial' and 'all' not in batch['groups'][0]['phases']['a1'], change


def check_failed_baseline(root, dictionary, budgets):
    """Regression：超过批次窗口的当前来源读失败，旧会话不晋升、原始与错误保留。"""
    import analyze
    import os
    from perf_store import AI_BATCHES_KEEP, git
    with tempfile.TemporaryDirectory(prefix='uestc-perf-baseline-') as temporary:
        out, author = Path(temporary) / 'perf', Path(temporary) / 'author'
        batch = out / 'runs/baseline'
        for identifier, day in (('OLDER', 1), ('CURRENT', 2)):
            for repeat in (1, 2, 3):
                name = identifier if repeat == 3 else f'{identifier}-{repeat}'
                meta = manifest(dictionary, name)
                meta.update(origin='ai', endedAt=f'2000-01-{day:02d}T00:00:0{repeat}Z')
                meta['build']['commit'] = git(root, 'rev-parse', 'HEAD')[1]
                data = mark_sample(streams([frame(dictionary, 0)], dictionary), dictionary, meta)
                write_six(batch / name, meta, data, dictionary)
                (batch / name / 'run.json').write_text(json.dumps(ai_run(batch=identifier, repetition=repeat)), encoding='utf-8')
        assert not analyze_all(root, out, author)['errors']
        baseline = read_json(out / 'derived/CURRENT/summary.json')
        assert baseline['status'] in ('current', 'stale'), '夹具没有建立当前基线'
        assert read_json(out / 'derived/OLDER/summary.json')['status'] == 'superseded'
        expired = out / 'runs/expired'
        for identifier in ('CONTENT', 'IO', 'OLDER'):
            write_six(expired / identifier, manifest(dictionary, identifier), streams([frame(dictionary, 0)], dictionary), dictionary)
        (expired / 'CONTENT/frames.csv').write_text('truncated', encoding='utf-8')
        original_load = analyze.load_session
        def fail_current(directory, fields):
            if directory in ((batch / 'CURRENT').resolve(), (expired / 'IO').resolve()):
                raise PermissionError('selfcheck baseline read denied')
            return original_load(directory, fields)
        (batch / 'CURRENT/frames.csv').touch()
        with patch.object(analyze, 'load_session', side_effect=fail_current):
            initial_errors = analyze_all(root, out, author)['errors']
        assert len(initial_errors) == 4 and {e['kind'] for e in initial_errors} == {'io', 'content', 'conflict'}, '夹具未触发三类失败'
        for index in range(AI_BATCHES_KEEP):
            write_six(out / 'runs' / f'newer-{index}/session', manifest(dictionary, f'NEWER-{index}'),
                      streams([frame(dictionary, 0)], dictionary), dictionary)
        for path in list(batch.rglob('manifest.json')) + list(expired.rglob('manifest.json')):
            os.utime(path, (1, 1))
        newer_batches = [p for p in (out / 'runs').iterdir() if p not in (batch, expired)]
        assert len(newer_batches) == AI_BATCHES_KEEP and all(
            next(p.rglob('manifest.json')).stat().st_mtime_ns > (batch / 'CURRENT/manifest.json').stat().st_mtime_ns
            for p in newer_batches), '夹具基线批次未落到保留窗口外'
        with patch.object(analyze, 'load_session', side_effect=fail_current):
            failed = analyze_all(root, out, author)
        assert len(failed['errors']) == 1 and failed['errors'][0]['kind'] == 'io', failed
        public = read_json(out / 'index.json')
        assert not expired.exists() and str(expired.resolve()) in public['removed'], '坏来源额外保护了过期普通批次'
        assert all(entry['id'] != 'CURRENT' for entry in public['sessions']), '失败来源仍展示可用指标'
        assert next(entry for entry in public['sessions'] if entry['id'] == 'OLDER')['status'] == 'superseded', '旧会话冒充当前基线'
        assert read_json(out / 'derived/CURRENT/summary.json')['status'] == baseline['status'], '失败抹掉了基线身份'
        assert (batch / 'CURRENT/frames.csv').is_file() and public['errors'] == failed['errors'], '修剪丢掉了错误来源或错误事实'
        assert analyze.analysis_needed(root, out, author), '一次 IO 故障不能在恢复后重试'
        recovered = analyze_all(root, out, author)
        assert 'CURRENT' in recovered['analyzed'] and not recovered['errors'], recovered
        assert read_json(out / 'derived/CURRENT/summary.json')['status'] == baseline['status']
