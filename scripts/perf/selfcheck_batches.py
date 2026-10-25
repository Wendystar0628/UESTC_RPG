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

    with tempfile.TemporaryDirectory(prefix='uestc-perf-round-') as temporary:
        out, author = Path(temporary) / 'perf', Path(temporary) / 'author'
        source = out / 'runs/sample/SAMPLE'
        write_six(source, sample_meta, data, dictionary)
        (source / 'run.json').write_text(json.dumps(ai_run(round_id='sample')), encoding='utf-8')
        assert not analyze_all(root, out, author)['errors']
        derived = read_json(out / 'derived/SAMPLE/summary.json')
        assert at(derived, 'all.cost.mainViewportGpuMs.n') == 2
        curves = read_json(out / 'derived/SAMPLE/curves.json')['levels'][0]
        assert sum(row[2] for row in curves['buckets']) == 2, '曲线保留了准备期成本'
        assert len(load_session(source, dictionary)[1]['frames']) == len(rows), '分析修改了原始数据'
        assert not read_json(out / 'index.json')['batches'], '无登记的会话进入轮记录'
    check_round_selection(root, dictionary, budgets)


def check_round_selection(root, dictionary, budgets):
    from perf_batches import batch_results, ROUND_RULES, validate_rounds
    from selfcheck_fixtures import round_registration
    from perf_store import git
    from monitor.view.perf import trend_groups
    with tempfile.TemporaryDirectory(prefix='uestc-perf-round-selection-') as temporary:
        source = Path(temporary)
        entries = []
        for identifier, value in (('old', 2), ('latest-a', 7), ('latest-z', 3)):
            meta = manifest(dictionary, identifier)
            meta.update(origin='ai', desktop=dict(kind='isolated'))
            meta['build']['commit'] = git(root, 'rev-parse', 'HEAD')[1]
            run = ai_run()
            data = mark_sample(streams([frame(dictionary, 0, gpu=value)], dictionary), dictionary, meta)
            entries.append(summary_for(source / identifier, meta, data, dictionary, budgets, run)[0])
        registration = round_registration(source, ['baseline-selfcheck'])
        rounds = batch_results(entries, budgets, rounds=[registration])
        group = rounds[0]['groups'][0]
        assert group['mainSessionId'] == 'old', '同结束/开始时间未按ID择最新'
        entries[1]['endedAt'] = entries[2]['endedAt'] = '2000-01-02T00:00:05Z'
        rounds = batch_results(entries, budgets, rounds=[registration])
        group = rounds[0]['groups'][0]
        assert group['mainSessionId'] == 'latest-z' and at(group, 'steady.cost.mainViewportGpuMs.p95') == 3
        invalid = copy.deepcopy(entries[2]); invalid.update(id='invalid', baseStatus='invalid', endedAt='2000-01-03T00:00:00Z')
        invalid['run']['status'] = 'invalid'
        evidence = copy.deepcopy(invalid); evidence.update(id='functional', baseStatus='evidence'); evidence['run']['purpose']='functional'
        result = batch_results(entries + [invalid, evidence], budgets, rounds=[registration])[0]
        assert result['groups'][0]['mainSessionId'] == 'latest-z'
        ruler = next(row for row in entries[2]['budgets'] if row['metric'] == 'mainViewportGpuMs')['budget']
        threshold = ROUND_RULES['minimumImprovementFraction']
        near = ruler * (1 - threshold / 2)
        entries[2]['steady']['cost']['mainViewportGpuMs']['p95'] = near
        result = batch_results(entries, budgets, rounds=[registration])[0]
        assert result['groups'][0]['confirmationPending']
        confirm = copy.deepcopy(entries[2]); confirm.update(id='confirm', endedAt='2000-01-03T00:00:00Z')
        confirm['run']['confirmationOf'] = 'latest-z'
        confirm['steady']['cost']['mainViewportGpuMs']['p95'] = near + 1
        confirm['steady']['cost']['mainViewportGpuMs']['n'] = 7
        confirm['steady']['cost']['mainViewportCpuMs']['n'] = 9
        result = batch_results(entries + [confirm], budgets, rounds=[registration])[0]
        assert result['groups'][0]['sessionIds'] == ['latest-z','confirm']
        assert at(result['groups'][0], 'steady.cost.mainViewportGpuMs.p95') == near + 1
        assert at(result['groups'][0], 'steady.cost.mainViewportGpuMs.n') == 7
        assert at(result['groups'][0], 'steady.cost.mainViewportCpuMs.n') == at(entries[2], 'steady.cost.mainViewportCpuMs.n')
        assert result['groups'][0]['metricSources']['steady.cost.mainViewportGpuMs']['sessionId'] == 'confirm'
        assert result['groups'][0]['confirmationPending'] is False
        duplicate = copy.deepcopy(confirm); duplicate['id']='duplicate'
        assert batch_results(entries + [confirm, duplicate], budgets, rounds=[registration])[0]['status'] == 'partial'
        result['status']='stale'
        commit = entries[0]['build']['commit']
        points = trend_groups([result], [dict(summary=row) for row in entries + [confirm]], {commit:dict(seq=1,subject='fixture')})
        assert points[0]['points'][0]['roundNumber'] == 0 and points[0]['points'][0]['sessionIds'] == ['latest-z','confirm']
        assert points[0]['points'][0]['metrics']['mainViewportGpuP95Ms'] == near + 1
        bad = round_registration(source, ['baseline-selfcheck'], 'bad', 2, 'fixture')
        try:
            validate_rounds([registration, bad])
        except ValueError:
            pass
        else:
            raise AssertionError('跳轮的前轮引用被接受')


def check_batch_freshness(root, dictionary, budgets):
    """Regression：两轮跨提交仍只引用明确前轮；当前轮判鲜不被旧构建拖回。"""
    from argparse import Namespace
    from perf import compare
    from perf_store import git
    from perf_batches import register_round
    with tempfile.TemporaryDirectory(prefix='uestc-perf-round-code-') as temporary:
        repo = Path(temporary) / 'repo'
        data_root = repo / 'game/data'
        data_root.mkdir(parents=True)
        (data_root / 'perf-fields.json').write_text(json.dumps(dictionary), encoding='utf-8')
        (repo / 'budgets.json').write_text(json.dumps(budgets), encoding='utf-8')
        marker = repo / 'game/fixture.txt'
        marker.write_text('A', encoding='utf-8')
        for arguments in (('init',), ('add', '.'), ('-c', 'user.name=selfcheck', '-c', 'user.email=selfcheck@example.invalid', 'commit', '-m', 'A')):
            assert git(repo, *arguments)[0] == 0, arguments
        first = git(repo, 'rev-parse', 'HEAD')[1]
        marker.write_text('B', encoding='utf-8')
        assert git(repo, 'add', '.')[0] == 0
        assert git(repo, '-c', 'user.name=selfcheck', '-c', 'user.email=selfcheck@example.invalid', 'commit', '-m', 'B')[0] == 0
        second = git(repo, 'rev-parse', 'HEAD')[1]
        assert first != second
        out, author = repo / 'output/perf', Path(temporary) / 'author'
        for number_, commit in enumerate((first, second)):
            identifier = 'round-' + str(number_)
            meta = manifest(dictionary, identifier)
            meta.update(origin='ai', desktop=dict(kind='isolated'), endedAt=f'2000-01-0{number_ + 1}T00:00:05Z')
            meta['build']['commit'] = commit
            source = out / 'runs' / identifier
            sample_rows = [frame(dictionary, 0, gpu=2-number_)]
            sample_rows[0]['frameSetupCpuMs'] = budgets['deviceTargets']['developer']['frameSetupCpuP95MsUpper'] / 2
            sample = mark_sample(streams(sample_rows, dictionary), dictionary, meta)
            write_six(source / identifier, meta, sample, dictionary)
            (source / identifier / 'run.json').write_text(json.dumps(ai_run(round_id=identifier)), encoding='utf-8')
            register_round(out, identifier, number_, source, ['baseline-selfcheck'], 'round-0' if number_ else None)
        assert not analyze_all(repo, out, author)['errors']
        batches = read_json(out / 'index.json')['batches']
        assert batches[0]['status'] == 'stale' and batches[1]['status'] == 'current', batches
        assert any('improved' in line for line in compare(out, Namespace(left='round-0', right='round-1')))
        marker.write_text('C', encoding='utf-8')
        assert not analyze_all(repo, out, author)['errors']
        assert read_json(out / 'index.json')['batches'][1]['status'] == 'stale'


def check_failed_baseline(root, dictionary, budgets):
    """Regression：超过批次窗口的当前来源读失败，旧会话不晋升、原始与错误保留。"""
    import analyze
    import os
    from perf_store import AI_BATCHES_KEEP, git
    with tempfile.TemporaryDirectory(prefix='uestc-perf-baseline-') as temporary:
        out, author = Path(temporary) / 'perf', Path(temporary) / 'author'
        batch = out / 'runs/baseline'
        for identifier, day in (('OLDER', 1), ('CURRENT', 2)):
            meta = manifest(dictionary, identifier)
            meta.update(origin='ai', endedAt=f'2000-01-{day:02d}T00:00:05Z')
            meta['build']['commit'] = git(root, 'rev-parse', 'HEAD')[1]
            data = mark_sample(streams([frame(dictionary, 0)], dictionary), dictionary, meta)
            write_six(batch / identifier, meta, data, dictionary)
            (batch / identifier / 'run.json').write_text(json.dumps(ai_run(round_id=identifier)), encoding='utf-8')
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
