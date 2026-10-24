"""小样本与收口回归自检，只验证性能分析行为，不代表真实录制或性能达标。
用法：python -I -X utf8 scripts/perf/perf.py selfcheck；输入当前字典和预算，产出自检结果，临时六件与派生自动清除。
"""
import csv
import copy
import io
import json
import sys
import tempfile
from pathlib import Path
from contextlib import redirect_stderr, redirect_stdout
from analyze import analyze_all, summary_for
from perf_model import COSTS, PHASES, at, load_session, prepare_frames, read_json, rolling_medians, target_ms
from perf_anomalies import DEEPEN_REPEAT_COUNT, annotate, anomaly, deepen, frame_anomalies
from unittest.mock import patch
from selfcheck_fixtures import ai_run, manifest, mark_sample, frame, viewport, streams, write_six
from selfcheck_context import check_context, check_window_changes, check_hardware_facts


def check_snapshot(root, dictionary, budgets):
    with tempfile.TemporaryDirectory(prefix='uestc-perf-snapshot-') as temporary:
        source = Path(temporary) / 'session'
        meta = manifest(dictionary)
        meta['fieldDictionary'] = copy.deepcopy(dictionary)
        meta['fieldDictionary']['nonSteadyRules']['appStates'] = ['playing']
        assert meta['fieldDictionary'] != dictionary, '夹具没有改变同 schema 的判定规则'
        write_six(source, meta, streams([frame(dictionary, 0)], dictionary), dictionary)
        loaded, data = load_session(source, dictionary)
        summary, _, _, _ = summary_for(source, loaded, data, dictionary, budgets, {})
        assert at(summary, 'steady.cost.mainViewportGpuMs.n') == 1, '未按当前规则重算旧快照'
        for key, value in (('file', '../frames.csv'), ('columns', [])):
            broken = copy.deepcopy(meta)
            broken['fieldDictionary']['streams']['frames'][key] = value
            (source / 'manifest.json').write_text(json.dumps(broken), encoding='utf-8')
            try:
                load_session(source, dictionary)
            except ValueError:
                pass
            else:
                raise AssertionError('快照的流路径或列结构未校验')
        meta['schema'] = dictionary['schema'] - 1
        (source / 'manifest.json').write_text(json.dumps(meta), encoding='utf-8')
        assert load_session(source, dictionary)[1] is None, '旧 schema 被兼容解析'


def check_median(root, dictionary, budgets):
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames']
    frames = [frame(dictionary, i) for i in range(warm + 3)]
    frames[warm]['appState'] = 'entering'
    frames[warm + 1]['gapBefore'] = True
    for row in frames[warm:warm + 2]:
        row['viewportSamplesJson'][0]['gpuRenderMs'] = 999
    prepare_frames(frames, [], [], dictionary)
    assert not frames[warm]['_steady'] and frames[warm + 1]['_gap'], '夹具未触发非稳态与缺口'
    medians = rolling_medians(frames, COSTS, policy)
    assert medians[-1]['mainViewportGpuMs'] == 2, '非稳态或 gap 清掉了 M 或进入了 M'
    frames[-1]['_scene'] = ('another scene',)
    assert rolling_medians(frames, COSTS, policy)[-1]['mainViewportGpuMs'] is None, '换场景没有清 M'
    frames[-1]['_scene'] = frames[-2]['_scene']
    event = dict(event='settings.quality', monotonicMs=frames[-1]['monotonicMs'], payload={'effective': {'scale': .5}})
    prepare_frames(frames, [], [event], dictionary)
    assert rolling_medians(frames, COSTS, policy)[-1]['mainViewportGpuMs'] is None, '换口径没有清 M'


def check_pending(root, dictionary, budgets):
    from perf_store import AI_BATCHES_KEEP, exclusive
    with tempfile.TemporaryDirectory(prefix='uestc-perf-pending-') as temporary:
        work = Path(temporary)
        out, author = work / 'perf', work / 'author'
        source = out / 'runs/abandoned/session'
        meta = manifest(dictionary, 'ABANDONED')
        meta.update(status='recording', complete=False, endedAt=None)
        write_six(source, meta, streams([], dictionary), dictionary)
        (source / 'manifest.json').rename(source / 'manifest.pending.json')
        # A crashed stream may end in the middle of a row; pending indexing never parses it.
        (source / 'frames.csv').write_text('truncated', encoding='utf-8')
        (source / '.lock').unlink()
        missing_lock = analyze_all(root, out, author)
        assert str(source.resolve()) in missing_lock['skipped'], '没有可独占锁的 pending 被读进索引'
        (source / '.lock').touch()
        with exclusive(source / '.lock') as held:
            assert held
            locked = analyze_all(root, out, author)
            assert str(source.resolve()) in locked['skipped'], '活动 pending 没有被发现并锁定跳过'
            assert not read_json(out / 'index.json')['sessions'], '活动 pending 被读进索引'
        analyzed = analyze_all(root, out, author)
        assert analyzed['analyzed'] == ['ABANDONED'] and not analyzed['errors'], analyzed
        entry = read_json(out / 'index.json')['sessions'][0]
        assert entry['status'] == 'partial', '可独占的 pending 未作为 partial 进索引'
        assert read_json(out / 'derived/ABANDONED/summary.json')['all'] is None, 'pending 解析了未闭合流'
        for i in range(AI_BATCHES_KEEP):
            newer = out / 'runs' / f'newer-{i:02d}/session'
            write_six(newer, manifest(dictionary, f'NEWER-{i}'), streams([], dictionary), dictionary)
        with exclusive(source / '.lock') as held:
            assert held
            active = analyze_all(root, out, author)
            assert not active['errors'] and source.exists(), '修剪删除了锁仍占用的 pending'
        final = analyze_all(root, out, author)
        assert not final['errors'], final
        assert not source.exists(), '锁已释放的 pending 永不过期'
        assert not (out / 'derived/ABANDONED').exists(), '过期 pending 留下派生'


def check_deepen(root, dictionary, budgets):
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames']
    frames = [frame(dictionary, i) for i in range(warm + 1)]
    for key in ('gpuRenderMs', 'cpuRenderMs'):
        frames[-1]['viewportSamplesJson'][0][key] = 100
    prepare_frames(frames, [], [], dictionary)
    medians = rolling_medians(frames, COSTS, policy)
    annotate(frames, [], medians, policy)
    items = frame_anomalies(frames, manifest(dictionary), budgets, medians, 'cost')
    spikes = [a for a in items if a['type'] == 'costSpike' and a['metric'] in COSTS[:2]]
    assert len(spikes) == 2 and all(not a['candidates'] for a in spikes), '夹具未触发主视口两项近似尖峰'
    assert not deepen(spikes * DEEPEN_REPEAT_COUNT, manifest(dictionary)), '近似主视口候选被误计为缺原因'
    exact = anomaly('costSpike', 'info', frames[-1], 100, 1, 'selfcheck', 'frameSetupCpuMs')
    assert deepen([exact] * DEEPEN_REPEAT_COUNT, manifest(dictionary)), '普通无原因重复候选没有 deepen'
    exact.update(metric=COSTS[0], firstFrame=-1)
    mixed = deepen(spikes + [exact] * DEEPEN_REPEAT_COUNT, manifest(dictionary))
    assert len(mixed) == 1 and mixed[0]['firstFrame'] == -1, '近似候选借用了精确候选的无原因计数'


def check_manifest(root, dictionary, budgets):
    meta = manifest(dictionary)
    meta['display']['refreshHz'] = None
    assert target_ms(meta) == 1000 / meta['pacing']['maxFps'], '真实 disabled 被当成开启垂直同步'


def check_sampling_batches(root, dictionary, budgets):
    """Regression：准备期极值不进样段；不完整批次与短跑不能产生基线。"""
    from perf_batches import batch_results
    from perf_store import git
    warm = budgets['perfAnomalies']['medianMinimumFrames'] + 3
    rows = [frame(dictionary, i, gpu=2 if i < warm else 100) for i in range(warm + 4)]
    rows[0]['gcPauseMs'] = 999
    start, end = rows[warm - 1]['monotonicMs'] + 5, rows[warm + 2]['monotonicMs']
    data = mark_sample(streams(rows, dictionary), dictionary, begin=start, end=end)
    meta = manifest(dictionary, 'SAMPLE')
    meta.update(origin='ai')
    meta['build']['commit'] = git(root, 'rev-parse', 'HEAD')[1]
    run_value = ai_run()
    summary, items, sampled, _ = summary_for(Path('fixture'), copy.deepcopy(meta), copy.deepcopy(data), dictionary, budgets, run_value)
    assert sampled and len(sampled) == 2 and sampled[0]['monotonicMs'] - sampled[0]['dtMs'] >= start, '夹具没有排除跨界帧'
    assert at(summary, 'all.cost.mainViewportGpuMs.n') == 2 and at(summary, 'all.cost.mainViewportGpuMs.p95') == 100
    assert at(summary, 'all.cost.gcTotals.gcPauseMs') == 0, '准备期 GC 进入样段'
    assert not any(item['type'] == 'costSpike' for item in items), '准备期 M 泄漏到短样段'
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
                sample = mark_sample(streams([frame(dictionary, i, gpu=value) for i in range(3)], dictionary), dictionary)
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
            invalid, _ = batch_results(changed)
            assert invalid[0]['status'] == 'partial' and all('all' not in phase for group in invalid[0]['groups']
                                                           for phase in group['phases'].values()), mutate
        # 真实六件往返也必须保持裁剪，且原始行数不变。
        source = out / 'runs/sample/SAMPLE'
        write_six(source, {**manifest(dictionary, 'SAMPLE'), 'origin': 'ai'}, data, dictionary)
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
                sample = mark_sample(streams([frame(dictionary, 0, gpu=7 if phase == 'b1' else 2)], dictionary), dictionary)
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


def check_processed_error(root, dictionary, budgets):
    import analyze
    with tempfile.TemporaryDirectory(prefix='uestc-perf-error-') as temporary:
        out, author = Path(temporary) / 'perf', Path(temporary) / 'author'
        source = author / 'BROKEN'
        write_six(source, manifest(dictionary, 'BROKEN'), streams([frame(dictionary, 0)], dictionary), dictionary)
        (source / 'frames.csv').write_text('truncated', encoding='utf-8')
        with patch.object(analyze, 'load_session', wraps=analyze.load_session) as loads:
            first = analyze_all(root, out, author)
            assert first['errors'] and not first['analyzed'], '夹具未触发原始流解析失败'
            second = analyze_all(root, out, author)
            assert second['errors'] == first['errors'], '错误原因没有保留'
            assert loads.call_count == 1, '同一错误输入被重复分析'
        assert not analyze.analysis_needed(root, out, author), '已处理错误仍被判为待分析'
        (source / 'frames.csv').write_text('changed', encoding='utf-8')
        assert analyze.analysis_needed(root, out, author), '错误输入变化后未重新判鲜'
        (source / 'manifest.json').unlink()
        assert analyze.analysis_needed(root, out, author), '删除错误来源后没有重新判鲜'
        assert not analyze_all(root, out, author)['errors'], '已经删除的错误仍留在索引'

        derived_source = author / 'DERIVED'
        write_six(derived_source, manifest(dictionary, 'DERIVED'), streams([frame(dictionary, 0)], dictionary), dictionary)
        with patch.object(analyze, 'curve_document', side_effect=RuntimeError('selfcheck derived content invalid')) as derives:
            first = analyze_all(root, out, author)
            assert first['errors'][0]['kind'] == 'content', first
            assert analyze_all(root, out, author)['errors'] == first['errors']
            assert derives.call_count == 1 and not analyze.analysis_needed(root, out, author), '派生内容失败反复重跑'
        (derived_source / 'frames.csv').touch()
        assert analyze.analysis_needed(root, out, author), '派生内容失败的输入变化未重试'
        assert analyze_all(root, out, author)['analyzed'] == ['DERIVED']
        (derived_source / 'manifest.json').unlink()

        source = author / 'RETRY'
        write_six(source, manifest(dictionary, 'BROKEN'), streams([frame(dictionary, 0)], dictionary), dictionary)
        assert analyze_all(root, out, author)['analyzed'] == ['BROKEN']
        with patch.object(analyze, 'load_session', side_effect=PermissionError('selfcheck read denied')) as loads:
            # 输入改变后读失败；IO 恢复不需要再次改变输入。
            (source / 'frames.csv').touch()
            failed = analyze_all(root, out, author)
            assert failed['errors'][0]['kind'] == 'io' and not read_json(out / 'index.json')['sessions']
            assert analyze.analysis_needed(root, out, author), 'IO 失败被输入戳缓存'
            index_stamp = (out / 'index.json').stat().st_mtime_ns
            analyze_all(root, out, author)
            assert loads.call_count == 2, 'IO 错误没有重新尝试'
            assert (out / 'index.json').stat().st_mtime_ns == index_stamp, '同一 IO 错误重写 index 触发通知空转'
        recovered = analyze_all(root, out, author)
        assert recovered['analyzed'] == ['BROKEN'] and not recovered['errors'], recovered
        (out / 'derived/BROKEN/anomalies.json').unlink()
        assert analyze.analysis_needed(root, out, author), '缺失派生文件没有重新判鲜'
        assert analyze_all(root, out, author)['analyzed'] == ['BROKEN']

        other = author / 'SAME-ID'
        write_six(other, manifest(dictionary, 'BROKEN'), streams([frame(dictionary, 0)], dictionary), dictionary)
        conflict = analyze_all(root, out, author)
        assert conflict['errors'][0]['kind'] == 'conflict', conflict
        assert not analyze.analysis_needed(root, out, author), '未变冲突反复启动分析器'
        (source / 'manifest.json').unlink()
        assert analyze.analysis_needed(root, out, author), '冲突持有方删除后未重判'
        resolved = analyze_all(root, out, author)
        assert resolved['analyzed'] == ['BROKEN'] and not resolved['errors'], resolved
        assert read_json(out / 'derived/BROKEN/summary.json')['source'] == str(other.resolve())
        (other / 'manifest.json').unlink()
        assert analyze.analysis_needed(root, out, author), '成功来源消失后未重判'
        analyze_all(root, out, author)
        assert not (out / 'derived/BROKEN').exists(), '已失去会话清单的来源仍保留派生'


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
                data = mark_sample(streams([frame(dictionary, 0)], dictionary), dictionary)
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


def check_derived_curves(root, dictionary, budgets):
    with tempfile.TemporaryDirectory(prefix='uestc-perf-curves-') as temporary:
        out, author = Path(temporary) / 'perf', Path(temporary) / 'author'
        source = author / 'CURVES'
        gpu = at(budgets, 'deviceTargets.high.developer4kFineGpuP95MsMax') * 2
        rows = [frame(dictionary, i, gpu=gpu) for i in range(2)]
        rows[1]['dtMs'] = 30
        rows[1]['phaseValidity'] = 0
        data = streams(rows, dictionary)
        data['hw'] = [dict(monotonicMs=10, gpuUtilFraction=.5, gpuClockMHz=1000, gpuMaxClockMHz=2000)]
        # The round trip uses the real schema; unspecified nullable hardware fields remain missing.
        data['hw'][0] = {**{c['name']: None for c in dictionary['streams']['hw']['columns']},
                         **data['hw'][0], 'schema': dictionary['schema'], 'sequence': 1, 'rowKind': 'nvml'}
        write_six(source, manifest(dictionary, 'CURVES'), data, dictionary)
        analyzed = analyze_all(root, out, author)
        assert not analyzed['errors'], analyzed
        curves = read_json(out / 'derived/CURVES/curves.json')
        detail = curves['levels'][0]
        values = dict(zip(detail['columns'], detail['buckets'][0]))
        assert values['fpsMean'] == 50 and values['fpsMax'] == 100, '派生帧率未按有效帧时计算'
        assert values['normalizedGpuLoadMean'] == .25, '派生负载未沿硬件同条读数归一化'
        assert detail['seriesReasons']['gpuUtilSampleAgeMs'], '缺测原生时间原因丢失'
        assert curves['hotframesTotal'] == len(rows) == len(curves['hotframes']), '越预算夹具未选出两帧高耗时证据'
        assert curves['hotframes'][1][3:8] == [None] * 5, '无效五段泄漏到逐帧投影'
        assert all(len(level['buckets']) <= level['limit'] for level in curves['levels']), '派生桶数超过声明上限'


def check_monitor_projection(root, dictionary, budgets):
    sys.path.insert(0, str(root / '.collab/tools'))
    from monitor.read import perf as reader
    from monitor.view import perf as view
    import analyze
    with tempfile.TemporaryDirectory(prefix='uestc-perf-monitor-') as temporary:
        out, author = Path(temporary) / 'output/perf', Path(temporary) / 'author'
        source = author / 'MONITOR'
        write_six(source, manifest(dictionary, 'MONITOR'), streams([frame(dictionary, 0)], dictionary), dictionary)
        analyzed = analyze_all(root, out, author)
        assert not analyzed['errors'], analyzed
        history_path = out / 'history.jsonl'
        reads = []
        original_read = Path.read_text

        def read_text(path, *args, **kwargs):
            if path == history_path:
                reads.append(path)
            assert path.suffix != '.csv', '观察台读取了逐帧或硬件流'
            return original_read(path, *args, **kwargs)

        with patch.object(reader, 'OUT', out), patch.object(reader.store, 'author_root', return_value=author), \
             patch.object(reader.repo, 'mainline_commits', return_value={'commits': []}), \
             patch.object(Path, 'read_text', read_text), \
             patch.object(analyze, 'load_session', side_effect=AssertionError('观察台重新解析了原始流')):
            payload = view.build(None)
        assert reads == [history_path], '同轮读取了不止一次性能历史'
        assert payload['details']['MONITOR']['columns'], '观察台未投影现成派生曲线'
        assert payload['retention']['historyPerGroup']['value'] == 1, '历史组现值错误'
        good = author / 'GOOD'
        write_six(good, manifest(dictionary, 'GOOD'), streams([frame(dictionary, 0)], dictionary), dictionary)
        (source / 'frames.csv').write_text('truncated', encoding='utf-8')
        with patch.object(reader, 'OUT', out), patch.object(reader.store, 'author_root', return_value=author), \
             patch.object(reader.repo, 'mainline_commits', return_value={'commits': []}), \
             patch.object(analyze, 'analyze_all', side_effect=lambda **args: analyze_all(output=out, **args)) as calls:
            for _ in range(2):
                payload = view.build(None)
                assert [entry['id'] for entry in payload['sessions']] == ['GOOD'], '坏会话拖垮好会话或旧历史伪装当前数据'
                assert payload['sessionErrors'][0]['source'] == str(source.resolve()) and payload['sessionErrors'][0]['reason']
                assert payload['sessionErrors'][0]['kindLabel'] == '内容错误', '坏会话缺少后端中文种类'
                assert payload['latest']['play'] == 'GOOD' and payload['details']['GOOD']['columns']
            assert calls.call_count == 1, '连续采集为同一坏会话空转分析器'
        (source / 'manifest.json').write_text('{broken manifest', encoding='utf-8')
        with patch.object(reader, 'OUT', out), patch.object(reader.store, 'author_root', return_value=author), \
             patch.object(reader.repo, 'mainline_commits', return_value={'commits': []}), \
             patch.object(analyze, 'analyze_all', side_effect=lambda **args: analyze_all(output=out, **args)):
            payload = view.build(None)
        assert payload['latest']['play'] == 'GOOD' and len(payload['sessionErrors']) == 1, '坏作者清单拖垮整页或重复报错'
        assert (out / 'derived/MONITOR/summary.json').is_file(), '夹具没有留下坏来源的旧派生'
        import perf
        for arguments, expected in ((['show', 'MONITOR'], 1), (['compare', 'GOOD', 'MONITOR'], 1),
                                    (['compare', 'MONITOR', 'GOOD'], 1), (['slice', 'MONITOR'], 1),
                                    (['anomalies', 'MONITOR'], 1), (['show', 'GOOD'], 0),
                                    (['compare', 'GOOD', 'GOOD'], 0), (['slice', 'GOOD'], 0),
                                    (['anomalies', 'GOOD'], 0), (['latest'], 0)):
            stdout, stderr = io.StringIO(), io.StringIO()
            with patch.object(sys, 'argv', ['perf.py', '--root', temporary, *arguments]), \
                 redirect_stdout(stdout), redirect_stderr(stderr):
                status = perf.main()
            assert status == expected, (arguments, status, stdout.getvalue(), stderr.getvalue())
            if expected:
                assert str(source.resolve()) in stderr.getvalue(), 'CLI 未指明失败来源'
            elif arguments == ['latest']:
                error = payload['sessionErrors'][0]
                assert '会话 GOOD |' in stdout.getvalue() and '会话 MONITOR |' not in stdout.getvalue()
                assert all(error[key] in stdout.getvalue() for key in ('source', 'kind', 'reason')), 'latest 未列全来源错误'


def check_retention_authority(root, dictionary, budgets):
    from perf_store import diagnostic_attachments, history_group
    with tempfile.TemporaryDirectory(prefix='uestc-perf-retention-') as temporary:
        runs = Path(temporary) / 'runs'
        record = runs / 'batch'
        record.mkdir(parents=True)
        attachment, raw = record / 'capture.bin', record / 'frames.csv'
        attachment.write_bytes(b'abcd')
        raw.write_text('raw', encoding='utf-8')
        (record / 'diagnostics.json').write_text(json.dumps({'attachments': ['capture.bin', 'capture.bin', 'frames.csv', '../outside.bin']}), encoding='utf-8')
        (runs / 'outside.bin').write_bytes(b'outside')
        assert diagnostic_attachments(runs) == [attachment.resolve()], '诊断附件重复计数或混入原始/越界文件'
        a = dict(origin='play', comparisonKey={'scene': 'a'}, scope='full')
        b = dict(origin='play', comparisonKey={'scene': 'b'}, scope='cost')
        assert history_group(a) == history_group(b), '作者历史没有共用单组'
        a['origin'], b['origin'] = 'ai', 'ai'
        assert history_group(a) != history_group(b), 'AI 比较键与口径未分组'


def check_missing_projection(root, dictionary, budgets):
    """Regression：不完整窗口仍展示已判规则数，缺钩子日志保留缺数原因。"""
    sys.path.insert(0, str(root / '.collab/tools'))
    from monitor.view import collab, health
    now = 2 * 86400
    at = collab.U.iso(now - 30)
    event = dict(at=at, sourceAt=at, event='started', payload=dict(id='selfcheck', side='codex',
            name='invalid-name', model='gpt-6.1-sol', effort='high'))
    reasons = collab._window_reasons(collab.U.iso(now - 3600), 7, now, ['n', 'ok', 'rate'])
    result = collab._dispatch([event], now - 3600, now, False, reasons)
    assert len(result['bad']) == 1 and result['bad'][0]['rules'] == ['R2'], '夹具未触发明确命名违规'
    for rule in result['rules']:
        assert rule['bad'] == sum(rule['key'] in item['rules'] for item in result['bad']), '规则计数与违规明细不符'
    assert result['rules'][0]['unknown'] == 1 and result['n'] is None, '未判个数或窗口未知被抹掉'
    with patch.object(health.hooklog, 'selftest_result', return_value=None), \
         patch.object(health.hooklog, 'log_records', return_value=None):
        hooks = health._hooks({})
    assert hooks['errors'] is None and any('errors' in r['fields'] for r in hooks['reasons']), '缺日志没有错误统计原因'
    with patch.object(health.hooklog, 'selftest_result', return_value=None), \
         patch.object(health.hooklog, 'log_records', return_value=[]):
        hooks = health._hooks({})
    assert hooks['errors'] == [] and not hooks['reasons'], '存在空日志被当成缺日志'


def check_event_order(root, dictionary, budgets):
    with tempfile.TemporaryDirectory(prefix='uestc-perf-events-') as temporary:
        source = Path(temporary) / 'session'
        data = streams([frame(dictionary, i) for i in range(2)], dictionary)
        anchor = data['events'][0]
        data['events'] = [{**anchor, 'sequence': sequence, 'monotonicMs': stamp, 'event': event}
                          for sequence, stamp, event in ((1, 100, 'clock.anchor'), (2, 30, 'gc.detail'),
                                                         (3, 30, 'hang.begin'), (4, 20, 'record.gap'))]
        write_six(source, manifest(dictionary), data, dictionary)
        loaded = load_session(source, dictionary)[1]
        assert [row['sequence'] for row in loaded['events']] == [4, 2, 3, 1], '事件未按时刻与序号稳定排序'
        path = source / dictionary['streams']['events']['file']
        data['events'][1]['sequence'] = data['events'][0]['sequence']
        path.write_text(''.join(json.dumps(row) + '\n' for row in data['events']), encoding='utf-8')
        try:
            load_session(source, dictionary)
        except ValueError:
            pass
        else:
            raise AssertionError('乱序事件的重复 sequence 未拒绝')
        path.write_text(json.dumps(anchor) + '\n', encoding='utf-8')
        frames_path = source / dictionary['streams']['frames']['file']
        rows = list(csv.DictReader(frames_path.read_text(encoding='utf-8').splitlines()))
        rows[1]['monotonicMs'] = str(float(rows[0]['monotonicMs']) - 1)
        with frames_path.open('w', encoding='utf-8', newline='') as stream:
            writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
            writer.writeheader()
            writer.writerows(rows)
        try:
            load_session(source, dictionary)
        except ValueError:
            pass
        else:
            raise AssertionError('逐帧流时刻倒退被接受')


def run(root):
    dictionary, budgets = read_json(root / 'game/data/perf-fields.json'), read_json(root / 'budgets.json')
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames']
    window = policy['medianWindowFrames']
    result = []
    for check in (check_snapshot, check_median, check_pending, check_deepen, check_manifest, check_context, check_window_changes, check_hardware_facts):
        check(root, dictionary, budgets)
    result.append('通过 TOOL-447：帧上下文权威、命令事件不重建悬窗、窗口变化共享稳定时长，schema4 硬件事实与原生时间保真。')
    result.append('通过 TOOL-435：同 schema 重算与结构校验、M 保窗与换口径、pending 锁/索引/过期、deepen、清单枚举与上下文权威回归。')
    check_sampling_batches(root, dictionary, budgets)
    check_batch_freshness(root, dictionary, budgets)
    result.append('通过 TOOL-448：真实样段裁剪、跨界帧排除与 M 重建，作者/非协议证据保留；三次/A-B-A 完整性与中位由分析器产出。')
    check_processed_error(root, dictionary, budgets)
    check_failed_baseline(root, dictionary, budgets)
    check_derived_curves(root, dictionary, budgets)
    check_monitor_projection(root, dictionary, budgets)
    check_retention_authority(root, dictionary, budgets)
    check_missing_projection(root, dictionary, budgets)
    result.append('通过 TOOL-437：错误输入只处理一次，输入变化重算；曲线、帧率与归一化负载由分析器派生。')
    result.append('通过 TOOL-441：内容错误缓存、IO 重试与恢复、冲突重查、来源删除与派生缺失判鲜；好坏会话隔离，重复故障不自激通知。')
    result.append('通过 TOOL-445/446：超保留窗口基线一次 IO 失败不晋升旧会话、不删原始；派生内容错误缓存，规则计数与缺日志原因通过。')
    result.append('通过 TOOL-451：CLI 拒绝失败来源的旧派生且好会话可读；普通坏批按预算删除、三类错误按真实 removed 清除，非 IO 派生异常缓存。')
    check_event_order(root, dictionary, budgets)
    result.append('通过 TOOL-438：事件 sequence 先校验再按时刻稳定排序；重复序号与逐帧时刻倒退仍拒绝。')
    frames = [frame(dictionary, i, gpu=2 if i < warm else 8) for i in range(warm + window + warm)]
    prepare_frames(frames, [], [], dictionary)
    medians = rolling_medians(frames, COSTS + PHASES + ('dtMs',), policy)
    assert medians[-1]['mainViewportGpuMs'] == 8, '稳定抬升未进入 M'
    summary, items, _, _ = summary_for(Path('selfcheck'), manifest(dictionary), streams(frames, dictionary), dictionary, budgets, {})
    late = [a for a in items if a['type'] == 'costSpike' and a['lastFrame'] == frames[-1]['collectionFrame']]
    assert not late, '稳定抬升被持续当尖峰剔除'
    spikes = [a for a in items if a['type'] == 'costSpike' and a['metric'] == 'mainViewportGpuMs']
    assert spikes and all(a['phase'] is None and not a['candidates'] for a in spikes), '主视口近似成本被逐帧归因'
    assert all(a['lastFrame'] > a['firstFrame'] and '±2' in a['note'] for a in spikes), '近似候选未保留关联窗口'
    assert at(summary, 'steady.cost.mainViewportGpuMs.n') == len(frames), '近似主视口成本未进入分布'
    assert all(f['_measuredGpuMs'] is None for f in frames), '近似主视口成本进入了精确合计'
    result.append('通过 1/4：近似主视口成本进入分布与 M；尖峰仅报±2帧窗口，无逐帧阶段/原因归因。')

    count = min(60, warm - 1)
    frames = [frame(dictionary, i, gpu=6) for i in range(count)]
    frames[20]['gapBefore'] = True
    frames[20]['dtMs'] = 500
    summary, items, _, _ = summary_for(Path('selfcheck'), manifest(dictionary), streams(frames, dictionary), dictionary, budgets, {})
    assert not any(a['type'] == 'costSpike' for a in items), '冷启动不足暖窗仍出了相对判据'
    assert any(a['type'] == 'budgetExceeded' for a in items), '不足暖窗漏了绝对预算判据'
    assert at(summary, 'all.experience.averageFps') == 100, '跨 gap 间隔进入体验计算'
    assert at(summary, 'all.experience.framePacingP95Ms') == 0, '帧节奏跨了 gap'
    assert at(summary, 'all.experience.low1Fps') is None, '不足样本仍计算 low'
    result.append('通过 2/4：冷启动无相对判据、绝对预算照报；缺口不跨算，low 缺样为 null。')

    frames = [frame(dictionary, i) for i in range(8)]
    for i, row in enumerate(frames):
        # Explicit confirmed-source fixture, distinct from main's approximate native readback.
        row['viewportSamplesJson'][0].update(sourceDrawFrame=i, resultUpdated=True)
        row['viewportSamplesJson'].append(viewport(2, 'reflection', i, 1 if i % 2 == 0 else 999, skipped=bool(i % 2)))
    prepare_frames(frames, [], [], dictionary)
    assert [f['_measuredGpuMs'] for f in frames] == [3, 2] * 4, '隔帧倒影重用了旧值或重复加了主视口'
    frames[0]['viewportSamplesJson'][0]['sourceDrawFrame'] = None
    prepare_frames(frames, [], [], dictionary)
    assert frames[0]['_measuredGpuMs'] is None, '来源未知仍算了合计'
    assert frames[0]['mainViewportGpuMs'] == 2, '近似主视口成本被未知精确来源抹掉'
    frames[1]['viewportSamplesJson'][0]['drewThisFrame'] = False
    frames[2]['viewportSamplesJson'][1].update(sourceDrawFrame=None, resultUpdated=None)
    prepare_frames(frames, [], [], dictionary)
    assert frames[1]['mainViewportGpuMs'] is None and frames[1]['mainViewportCpuMs'] is None, '未绘制仍复用主视口读数'
    assert frames[2]['_viewports']['reflection:2']['gpuMs'] is None and frames[2]['_measuredGpuMs'] is None, '未确认倒影仍参与成本或合计'
    result.append('通过 3/4：近似主视口不进合计；跳过行不复用主成本，未确认倒影留 null，已确认来源不重加。')

    with tempfile.TemporaryDirectory(prefix='uestc-perf-selfcheck-') as temporary:
        work = Path(temporary)
        out, author = work / 'perf', work / 'author'
        source = author / 'SELFCHECK'
        meta = manifest(dictionary)
        meta['desktop'] = dict(kind='isolated', isInputDesktop=False, windowFocused=False)
        data = streams([frame(dictionary, i, gpu=6) for i in range(count)], dictionary)
        write_six(source, meta, data, dictionary)
        first = analyze_all(root, out, author)
        assert first['analyzed'] == ['SELFCHECK'] and not first['errors'], first
        second = analyze_all(root, out, author)
        assert not second['analyzed'] and not second['errors'], second
        summary = read_json(out / 'derived/SELFCHECK/summary.json')
        assert all(value is None for key, value in summary['all']['experience'].items() if key != 'reason'), 'cost 会话泄漏了体验字段'
        assert summary['scopeReason'] == '隔离桌面不测'
        items = read_json(out / 'derived/SELFCHECK/anomalies.json')['items']
        assert any(a['type'] == 'budgetExceeded' for a in items) and not any(a['type'] == 'costSpike' for a in items)
        assert len((out / 'history.jsonl').read_text(encoding='utf-8').splitlines()) == 1, '重复触发重复追加了历史'
        from perf_store import AI_BATCHES_KEEP, exclusive, prune
        with exclusive(source / '.lock') as held:
            assert held
            locked = analyze_all(root, out, author)
            assert str(source.resolve()) in locked['skipped'], '写入锁占用时仍读取会话'
        for index in range(AI_BATCHES_KEEP + 2):
            batch = out / 'runs' / f'batch-{index:02d}' / 'run/session'
            batch.mkdir(parents=True)
            (batch / 'manifest.json').write_text('{"status":"closed"}', encoding='utf-8')
            (batch / '.lock').touch()
        removed = prune(out, [])
        assert len(list((out / 'runs').iterdir())) == AI_BATCHES_KEEP and len(removed) == 2, 'AI 批次未按条数修剪'
        from perf import MAX_CHARS, MAX_LINES, bounded
        text = bounded(('中文行' * 100 for _ in range(1000)))
        assert len(text) <= MAX_CHARS and len(text.splitlines()) <= MAX_LINES
    result.append('通过 4/4：预算独立报、cost 体验全 null；六件往返、写入锁跳过、历史去重、按条数修剪和输出上限通过。')
    return result
