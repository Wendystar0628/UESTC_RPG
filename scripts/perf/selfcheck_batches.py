"""验证样段裁剪、批次完整性、基线失败与提交判鲜。
用法：由 perf.py selfcheck 编排调用，复用 selfcheck_fixtures，不写真实会话。
"""
import copy
import io
import json
import shutil
import subprocess
import sys
import tempfile
from contextlib import redirect_stderr, redirect_stdout
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
    check_round_registration(root, dictionary, budgets)


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
        inconsistent = copy.deepcopy(confirm)
        next(row for row in inconsistent['budgets'] if row['metric'] == 'mainViewportGpuMs')['stop'] = ruler
        inconsistent_result = batch_results(entries + [inconsistent], budgets, rounds=[registration])[0]
        assert any(row['tier'] == 'rerun' for row in inconsistent_result['groups'][0]['distanceTable']), '夹具未触发主采/确认判据不一致'
        assert inconsistent_result['gateStatus'] == 'Incomplete' and inconsistent_result['statusReasons']
        inconsistent = copy.deepcopy(confirm)
        inconsistent['run']['conditions']['logicalPauseVerified'] = True
        assert batch_results(entries + [inconsistent], budgets, rounds=[registration])[0]['gateStatus'] == 'Incomplete'
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
            registered = analyze_all(repo, out, author, round_request=dict(identifier=identifier, number_=number_,
                                      source_root=source, scenes=['baseline-selfcheck'], previous_id='round-0' if number_ else None))
            assert not registered['registrationError'], registered
        assert not analyze_all(repo, out, author)['errors']
        batches = read_json(out / 'index.json')['batches']
        assert batches[0]['status'] == 'stale' and batches[1]['status'] == 'current', batches
        assert any('improved' in line for line in compare(out, Namespace(left='round-0', right='round-1')))
        marker.write_text('C', encoding='utf-8')
        assert not analyze_all(repo, out, author)['errors']
        assert read_json(out / 'index.json')['batches'][1]['status'] == 'stale'


def check_round_registration(root, dictionary, budgets):
    """Regression：失败不占轮、历史错误隔离、同轮确认保主采，G10 纯读最新轮。"""
    import perf
    from perf_store import git
    with tempfile.TemporaryDirectory(prefix='uestc-perf-registration-') as temporary:
        repo, author = Path(temporary) / 'repo', Path(temporary) / 'author'
        (repo / 'game/data').mkdir(parents=True)
        (repo / 'game/data/perf-fields.json').write_text(json.dumps(dictionary), encoding='utf-8')
        (repo / 'budgets.json').write_text(json.dumps(budgets), encoding='utf-8')
        for arguments in (('init',), ('add', '.'), ('-c', 'user.name=selfcheck', '-c', 'user.email=selfcheck@example.invalid', 'commit', '-m', 'fixture')):
            assert git(repo, *arguments)[0] == 0, arguments
        commit = git(repo, 'rev-parse', 'HEAD')[1]
        out, source = repo / 'output/perf', repo / 'output/perf/runs/main'
        source.mkdir(parents=True)
        upper = budgets['deviceTargets']['developer']['mainViewportGpuP95MsUpper']

        def session(parent, identifier, scene, confirmation=None, condition_change=None, gpu=None, state='valid'):
            meta = manifest(dictionary, identifier)
            meta.update(origin='ai', desktop=dict(kind='isolated'), fault='none', endReason='exit')
            meta['build']['commit'] = commit
            run = ai_run(scene=scene, confirmation_of=confirmation)
            run['conditions'].update(logicalPauseVerified=False, mainThreadLongStopVerified=False,
                                     tuningSelectedMetrics=[], saveAfterSample=scene == 'first')
            run['conditions'].update(condition_change or {})
            run['coverage'] = {key: dict(observed=1, required=1) for key in ('sampleBegin', 'sampleEnd', 'gpu', 'sample')}
            run['close'] = dict(completed=True, durationMs=1, source='formal-close-task', reason=None)
            run['observed'] = dict(save=dict(reason='Manual', status='Saved', durationMs=1))
            rows = [frame(dictionary, i, gpu=upper * .98 if gpu is None else gpu) for i in range(3)]
            for row in rows:
                row['frameSetupCpuMs'] = budgets['deviceTargets']['developer']['frameSetupCpuP95MsUpper'] / 2
            data = streams(rows, dictionary)
            if state == 'valid':
                data = mark_sample(data, dictionary, meta)
            else:
                meta['complete'] = False
                run.update(status=state, reasons=['Pre-sample GPU contamination' if state == 'invalid' else 'Interrupted sample'], polluted=state == 'invalid')
            write_six(parent / identifier, meta, data, dictionary)
            (parent / identifier / 'run.json').write_text(json.dumps(run), encoding='utf-8')

        def cli(*arguments):
            stdout, stderr = io.StringIO(), io.StringIO()
            with patch.object(sys, 'argv', ['perf.py', '--root', str(repo), *map(str, arguments)]), redirect_stdout(stdout), redirect_stderr(stderr):
                status = perf.main()
            return status, stdout.getvalue(), stderr.getvalue()

        record = ['record', 'fixture-round-0', '--number', '0', '--source-root', str(source),
                  '--scene', 'first', '--scene', 'second', '--author-root', str(author)]
        assert cli(*record)[0] == 1 and not (out / 'rounds.json').exists(), '空采集占用了轮号'
        missing = Path(temporary) / 'not-created'
        absent = [*record]
        absent[absent.index(str(source))] = str(missing)
        preview = cli(*absent, '--preview')
        assert preview[0] == 0 and not missing.exists() and not (out / 'rounds.json').exists(), '预检创建来源或登记了空轮'
        session(source, 'main-first', 'first')
        broken = author / 'historical-error'
        write_six(broken, manifest(dictionary, 'historical-error'), streams([frame(dictionary, 0)], dictionary), dictionary)
        (broken / 'frames.csv').write_text('truncated', encoding='utf-8')
        assert cli(*record)[0] == 1 and not (out / 'rounds.json').exists(), '场景覆盖不全占用了轮号'
        session(source, 'main-second', 'second')
        session(source, 'discarded-before-begin', 'first', state='invalid')
        session(source, 'failed-confirmation', 'first', 'main-first', state='partial')
        preview = cli(*record, '--preview')
        assert preview[0] == 0 and len(json.loads(preview[1])['groups']) == 2 and not (out / 'rounds.json').exists()
        result = cli(*record)
        assert result[0] == 0, result
        discarded = read_json(out / 'derived/discarded-before-begin/summary.json')
        assert discarded['recording']['complete'] is False and discarded['sampleReasons'] and discarded['baseStatus'] == 'invalid'
        registered = read_json(out / 'rounds.json')['rounds'][0]
        assert registered['mainSessionIds'] == dict(first='main-first', second='main-second')
        assert read_json(out / 'index.json')['errors'][0]['source'] == str(broken.resolve()), '历史错误被吞掉'
        assert next(row for row in read_json(out / 'index.json')['batches'] if row['id'] == registered['id'])['confirmationPending']

        bad = Path(temporary) / 'mismatched-confirmation'
        session(bad, 'bad-confirm', 'first', 'main-first', dict(logicalPauseVerified=True))
        append = ['record', registered['id'], '--confirmation-root', str(bad), '--author-root', str(author)]
        preview = cli(*append, '--preview')
        assert preview[0] == 0 and json.loads(preview[1])['gateStatus'] == 'Incomplete'
        prior = (out / 'rounds.json').read_bytes()
        assert cli(*append)[0] == 1 and (out / 'rounds.json').read_bytes() == prior, '不符冻结条件的确认被登记'
        (bad / 'bad-confirm/frames.csv').write_text('truncated', encoding='utf-8')
        assert cli(*append)[0] == 1 and (out / 'rounds.json').read_bytes() == prior, '失败确认改写了主轮'
        assert any(error['source'] == str((bad / 'bad-confirm').resolve())
                   for error in read_json(out / 'index.json')['errors']), '确认源错误被吞掉'

        mixed = Path(temporary) / 'mixed-confirmations'
        session(mixed, 'confirm-first', 'first', 'main-first')
        session(mixed, 'interrupted-confirm', 'second', 'main-second', state='partial')
        session(mixed, 'broken-confirm', 'second', 'main-second')
        (mixed / 'broken-confirm/frames.csv').write_text('truncated', encoding='utf-8')
        assert cli('record', registered['id'], '--confirmation-root', mixed, '--author-root', author)[0] == 0
        interim = read_json(out / 'index.json')['batches'][0]
        assert interim['status'] != 'partial' and [g['comparisonKey']['scene'] for g in interim['groups']
                                                 if g['confirmationPending']] == ['second'], interim
        assert read_json(out / 'derived/interrupted-confirm/summary.json')['baseStatus'] == 'partial'
        assert any(error['source'] == str((mixed / 'broken-confirm').resolve())
                   for error in read_json(out / 'index.json')['errors']), '同来源确认错误被吞掉'
        confirm = Path(temporary) / 'confirmations'
        session(confirm, 'confirm-second', 'second', 'main-second')
        append = ['record', registered['id'], '--confirmation-root', str(confirm), '--author-root', str(author)]
        result = cli(*append)
        assert result[0] == 0, result
        updated = read_json(out / 'rounds.json')['rounds'][0]
        assert all(updated[key] == registered[key] for key in registered), '追加确认改写了轮身份或主采'
        assert updated['confirmationRoots'] == [str(mixed.resolve()), str(confirm.resolve())]
        group = read_json(out / 'index.json')['batches'][0]['groups'][0]
        assert group['sessionIds'] == ['main-first', 'confirm-first'] and not group['confirmationPending']
        duplicate = Path(temporary) / 'duplicate'
        session(duplicate, 'duplicate-confirm', 'first', 'main-first')
        prior = (out / 'rounds.json').read_bytes()
        assert cli('record', registered['id'], '--confirmation-root', duplicate, '--author-root', author)[0] == 1
        assert (out / 'rounds.json').read_bytes() == prior, '重复确认改写了登记'
        session(source, 'replacement', 'first', gpu=1)
        assert cli(*record)[0] == 0
        assert read_json(out / 'rounds.json')['rounds'][0]['mainSessionIds'] == registered['mainSessionIds']

        next_source = out / 'runs/next'
        for scene in ('first', 'second'):
            session(next_source, 'next-' + scene, scene, gpu=upper / 2)
        result = cli('record', 'fixture-round-1', '--number', '1', '--source-root', next_source,
                     '--scene', 'first', '--scene', 'second', '--previous-id', registered['id'], '--author-root', author)
        assert result[0] == 0, result
        assert cli('compare', 'fixture-round-0', 'fixture-round-1')[0] == 0
        assert cli('compare', 'fixture-round-1', 'fixture-round-0')[0] == 1
        check_readonly_gate(root, repo, out)
        (mixed / 'confirm-first/frames.csv').write_text('truncated', encoding='utf-8')
        assert cli(*record)[0] == 0, '确认来源变坏废掉了完整主采'
        retained = read_json(out / 'index.json')['batches'][0]
        assert retained['status'] != 'partial' and retained['groups'][0]['confirmationPending'], retained
        assert read_json(out / 'rounds.json')['rounds'][0]['mainSessionIds'] == registered['mainSessionIds']
        session(source, 'interrupted', 'first', state='partial')
        prior = (out / 'rounds.json').read_bytes()
        result = cli(*record)
        assert result[0] == 1 and '轮来源存在未完成运行' in result[2], result
        assert (out / 'rounds.json').read_bytes() == prior
        for identifier in ('faulted-invalid', 'dropped-invalid'):
            session(source, identifier, 'first', state='invalid')
            path = source / identifier / 'manifest.json'
            meta = read_json(path)
            if identifier == 'faulted-invalid':
                meta['fault'] = 'writerFailed'
            else:
                meta['streams'][0]['droppedRows'] = 1
            path.write_text(json.dumps(meta), encoding='utf-8')
        assert cli(*record)[0] == 1
        assert all(read_json(out / 'derived' / identifier / 'summary.json')['baseStatus'] == 'partial'
                   for identifier in ('faulted-invalid', 'dropped-invalid')), 'invalid 掩盖了记录服务故障或丢行'
        (source / 'main-first/frames.csv').write_text('truncated', encoding='utf-8')
        prior = (out / 'rounds.json').read_bytes()
        result = cli(*record)
        assert result[0] == 1 and '轮来源分析失败' in result[2], result
        assert (out / 'rounds.json').read_bytes() == prior, '当前主采源失败后登记仍被改写'
    check_confirmation_errors(root)


def check_confirmation_errors(root):
    """Regression：逐条确认发布保留所有失败，且不改写完整主轮事实。"""
    script = "'" + str(root / 'scripts/perf/run.ps1').replace("'", "''") + "'"
    command = r"""$ErrorActionPreference='Stop'; $WarningPreference='SilentlyContinue'
[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
$tokens=$null; $errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile(SCRIPT,[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw ($errors | Out-String) }
$definition=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Publish-CollectionConfirmations'},$true)
if (-not $definition) { throw 'Confirmation publisher function is missing.' }
. ([scriptblock]::Create($definition.Extent.Text))
$confirmationSources=@('first-confirmation-source','second-confirmation-source','successful-confirmation-source')
$roundRecord=[ordered]@{completed=$true; status='completed'; number=0; purpose='baseline'; scenes=@('first','second')}
$published=@()
function Publish-CollectionRound {
    param($Root,$Id,$Number,$SourceRoot,$Scenes,$PreviousId,$ConfirmationRoot)
    $script:published+=@($ConfirmationRoot)
    if ($ConfirmationRoot -ne 'successful-confirmation-source') { throw ('Original confirmation failure: ' + $ConfirmationRoot) }
}
$failed=Publish-CollectionConfirmations 'fixture' 'fixture-round' $confirmationSources $roundRecord
@{published=$published; failed=$failed; round=$roundRecord} | ConvertTo-Json -Depth 12 -Compress
""".replace('SCRIPT', script)
    shell = shutil.which('powershell.exe') or shutil.which('powershell') or shutil.which('pwsh')
    assert shell, '确认失败回归需要 PowerShell'
    result = subprocess.run([shell, '-NoProfile', '-NonInteractive', '-Command', command], capture_output=True,
                            text=True, encoding='utf-8', timeout=30)
    assert result.returncode == 0, (result.stdout, result.stderr)
    observed = json.loads(result.stdout)
    assert observed['failed'] and observed['published'] == ['first-confirmation-source', 'second-confirmation-source',
                                                          'successful-confirmation-source'], observed
    round_record = observed['round']
    assert round_record['completed'] and round_record['status'] == 'completed', observed
    assert round_record['number'] == 0 and round_record['purpose'] == 'baseline' and round_record['scenes'] == ['first', 'second'], observed
    failures = round_record['confirmationErrors']
    assert [failure['source'] for failure in failures] == ['first-confirmation-source', 'second-confirmation-source'], observed
    assert all('Original confirmation failure: ' + failure['source'] in failure['error'] for failure in failures), observed


def check_readonly_gate(root, repo, out):
    """同一离线夹具直接调用既有 G10；保留文件字节与修改时刻，禁止隐式采集。"""
    scripts = repo / 'scripts/perf'
    scripts.mkdir(parents=True)
    shutil.copyfile(root / 'scripts/perf/derived-fields.json', scripts / 'derived-fields.json')
    plan = dict(map='qingshuihe', fixed=[dict(id='first', saveAfterSample=True), dict(id='second', saveAfterSample=False)], route=[], travel=[])
    (scripts / 'scenarios.json').write_text(json.dumps(plan), encoding='utf-8')
    before = {str(path): (path.read_bytes(), path.stat().st_mtime_ns) for path in out.rglob('*') if path.is_file()}
    def quoted(value):
        return "'" + str(value).replace("'", "''") + "'"
    command = ("$ErrorActionPreference='Stop'; [Console]::OutputEncoding=[Text.UTF8Encoding]::new($false); "
               ". " + quoted(root / 'scripts/gates/performance.ps1') + "; "
               "function Assert-UESTCChildPath($Path,$Parent) { [IO.Path]::GetFullPath($Path) }; "
               "function Invoke-UESTCProcess { throw 'G10 attempted process execution' }; "
               "Invoke-UESTCPerformanceGate -Root " + quoted(repo) + " -Map qingshuihe | ConvertTo-Json -Depth 32 -Compress")
    shell = shutil.which('powershell.exe') or shutil.which('powershell') or shutil.which('pwsh')
    assert shell, '离线 G10 回归需要 PowerShell'
    result = subprocess.run([shell, '-NoProfile', '-NonInteractive', '-Command', command], capture_output=True, text=True, encoding='utf-8', timeout=30)
    assert result.returncode == 0, (result.stdout, result.stderr)
    detail = json.loads(result.stdout)
    assert detail.get('Batch') == 'fixture-round-1' and set(detail['SessionIds']) == {'next-first', 'next-second'} and detail['Comparison'], detail
    assert detail['Status'] == 'Passed' and not detail['IncompleteReasons'] and len(detail['SaveAndClose']) == 2, detail
    after = {str(path): (path.read_bytes(), path.stat().st_mtime_ns) for path in out.rglob('*') if path.is_file()}
    assert before == after, 'G10 改写了采集、登记或分析产物'


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
