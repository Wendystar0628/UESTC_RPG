"""验证快照、逐帧成本、事件时序与异常判据。
用法：由 perf.py selfcheck 编排调用，复用 selfcheck_fixtures，不写真实会话。
"""
import csv
import copy
import json
import tempfile
from pathlib import Path
from analyze import completed_diagnostic_clusters, diagnostic_identity, source_stamp, summary_for
from perf_model import COSTS, PHASES, at, load_session, prepare_frames, rolling_medians, target_ms
from perf_anomalies import DEEPEN_REPEAT_COUNT, annotate, anomaly, deepen, frame_anomalies
from selfcheck_fixtures import manifest, frame, viewport, streams, write_six, mark_sample


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
        # The recorded schema and column ordering belong to its own snapshot, not today's writer.
        older = copy.deepcopy(dictionary)
        older['schema'] -= 1
        older['streams']['hw']['columns'].reverse()
        older['streams']['hw']['columns'].append(dict(name='retiredCounter', type='integer', nullable=True, unit=None))
        source_old = Path(temporary) / 'older'
        original = manifest(older, 'OLDER-SNAPSHOT')
        write_six(source_old, original, streams([frame(older, 0)], older), older)
        old_meta, old_data = load_session(source_old, dictionary)
        recalculated = summary_for(source_old, old_meta, old_data, dictionary, budgets, {})[0]
        assert at(recalculated, 'steady.cost.mainViewportGpuMs.n') == 1
        assert old_meta['schema'] == older['schema'] and recalculated['calc'] == summary['calc']
        broken = copy.deepcopy(original)
        broken['fieldDictionary']['streams']['frames']['columns'][0]['type'] = 'string'
        (source_old / 'manifest.json').write_text(json.dumps(broken), encoding='utf-8')
        try:
            load_session(source_old, dictionary)
        except ValueError:
            pass
        else:
            raise AssertionError('当前必需字段语义变化仍被重算')


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


def check_deepen(root, dictionary, budgets):
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames']
    frames = [frame(dictionary, i) for i in range(warm + 1)]
    for key in ('gpuRenderMs', 'cpuRenderMs'):
        frames[-1]['viewportSamplesJson'][0][key] = 100
    prepare_frames(frames, [], [], dictionary)
    medians = rolling_medians(frames, COSTS, policy)
    annotate(frames, [], medians, policy)
    items = frame_anomalies(frames, manifest(dictionary), budgets, medians, 'cost', {})
    spikes = [a for a in items if a['type'] == 'costSpike' and a['metric'] in COSTS[:2]]
    assert len(spikes) == 2 and all(not a['candidates'] for a in spikes), '夹具未触发主视口两项近似尖峰'
    assert not deepen(spikes * DEEPEN_REPEAT_COUNT, manifest(dictionary)), '近似主视口候选被误计为缺原因'
    exact = anomaly('costSpike', 'info', frames[-1], 100, 1, 'selfcheck', 'frameSetupCpuMs')
    assert deepen([exact] * DEEPEN_REPEAT_COUNT, manifest(dictionary)), '普通无原因重复候选没有 deepen'
    exact.update(metric=COSTS[0], firstFrame=-1)
    mixed = deepen(spikes + [exact] * DEEPEN_REPEAT_COUNT, manifest(dictionary))
    assert len(mixed) == 1 and mixed[0]['firstFrame'] == -1, '近似候选借用了精确候选的无原因计数'

    upper = budgets['deviceTargets']['developer']['mainViewportGpuP95MsUpper']
    meta = manifest(dictionary)
    meta['origin'] = 'ai'
    meta['run']['args'] = ['--map=清水河', '--perf-out=discarded']
    run = dict(scene='gpu-budget-selfcheck', conditions=dict(suite='fixed'))
    rows = [frame(dictionary, i, gpu=upper * 2) for i in range(warm + 1)]
    summary, items, _, _ = summary_for(Path('selfcheck'), meta, streams(rows, dictionary), dictionary, budgets, run)
    check = next(c for c in summary['budgets'] if c['metric'] == 'mainViewportGpuMs')
    breaches = [a for a in items if a['type'] == 'budgetExceeded' and a['metric'] == check['metric']]
    assert check['tier'] == 'above-upper' and len(breaches) == 1, '夹具未触发单次已判 GPU 上沿越限'
    assert breaches[0]['threshold'] == check['budget'] and not any(a['type'] in ('hang', 'costSpike') for a in items), '夹具借用了挂起或反复尖峰'
    plans = [entry for entry in summary['deepen'] if entry['metric'] == check['metric']]
    assert len(plans) == 1 and plans[0]['type'] == 'budgetExceeded', '单次已测 GPU 预算越限没有合法 deepen'
    plan = plans[0]
    assert plan['tools'] == ['gpu-profile', 'Nsight'] and plan['scene'] == run['scene'], 'GPU 计划工具或场景不符'
    assert plan['poseArgs'] == ['--map=清水河'] and plan['commandState'] == 'ready', 'GPU 计划未沿用正式重放条件'
    assert "-DiagnosticTool 'gpu-profile','Nsight'" in plan['command'] and '-DeepenEntry ' + str(summary['deepen'].index(plan)) in plan['command'], 'GPU 命令未引用实际条目与工具'
    _check_completed_diagnostics(dictionary, budgets, meta, run, rows, plan)
    for case in ('within', 'missing', 'mismatch', 'cpu'):
        negative_meta = copy.deepcopy(meta)
        rows = [frame(dictionary, i, gpu=None if case == 'missing' else upper * 2 if case == 'mismatch' else upper)
                for i in range(warm + 1)]
        if case == 'mismatch':
            negative_meta['graphics']['targetWidthPx'] += 1
        if case == 'cpu':
            for row in rows:
                row['viewportSamplesJson'][0]['cpuRenderMs'] = budgets['deviceTargets']['developer']['viewportCpuP95MsUpper'] * 2
        summary, items, _, _ = summary_for(Path('selfcheck'), negative_meta, streams(rows, dictionary), dictionary, budgets, run)
        assert not any(a['type'] == 'budgetExceeded' and a['metric'] == 'mainViewportGpuMs' for a in items), '反例仍触发 GPU 上沿越限：' + case
        if case == 'cpu':
            assert any(a['type'] == 'budgetExceeded' and a['metric'] == 'mainViewportCpuMs' for a in items), '夹具未触发非 GPU 预算越限'
        assert not summary['deepen'], '未超、缺测、条件不符或非 GPU 越限借用了 GPU deepen：' + case
    check_eventpipe_attachment(root, dictionary, budgets)


def check_eventpipe_attachment(root, dictionary, budgets):
    """Regression：附着身份与完整采样覆盖不借用退出成功或工具输出。"""
    import runpy
    diagnostic = runpy.run_path(str(root / 'scripts/perf/diagnostic-report.py'))
    with tempfile.TemporaryDirectory(prefix='uestc-perf-eventpipe-selfcheck-') as temporary:
        work = Path(temporary)
        session = work / 'session'
        meta = manifest(dictionary)
        meta['clock']['anchors'] = [dict(unixMs=0, monotonicMs=0, readSpanMs=.1)]
        data = streams([frame(dictionary, i) for i in range(10)], dictionary)
        mark_sample(data, dictionary, meta, begin=20, end=100)
        write_six(session, meta, data, dictionary)
        condition = dict(collection=dict(sampleSegmentId='sample'))
        capture = dict(gamePid=123, gameExitCode=0, exitCode=0,
                       collectorReadyObservedUtc='1970-01-01T00:00:00.010Z', toolExitUtc='1970-01-01T00:00:00.200Z',
                       collectorStopRequestedUtc='1970-01-01T00:00:00.110Z', collectorStoppedWhileTargetAlive=True,
                       collectorFinishedUtc='1970-01-01T00:00:00.210Z', gameExitUtc='1970-01-01T00:00:00.220Z')
        # The acceptance denominator is Managed samples, not individual frame occurrences.
        resolution = dict(managedSamples=3, managedSamplesWithResolvedMethods=2, stackFrameOccurrences=6,
                          resolvedMethodFrameOccurrences=2, resolvedMethodFrameFraction=1 / 3, distinctResolvedMethods=2)
        parsed = dict(processId=capture['gamePid'], EventsLost=0, Truncated=False, threads=[dict(samples=3)],
                      managedStackResolution=resolution, session=meta['id'], sampleSegmentId=condition['collection']['sampleSegmentId'])
        parsed_path = work / 'cpu-hotspots.json'
        parsed_path.write_text(json.dumps(parsed), encoding='utf-8')
        actual = diagnostic['eventpipe_coverage'](root, work, session, condition, capture)
        assert actual['status'] == 'complete' and not actual['reasons'], 'Managed样本方法解析严格过半且完整附着未通过'
        for changed in (dict(collectorReadyObservedUtc=None), dict(collectorReadyObservedUtc='1970-01-01T00:00:00.050Z'),
                        dict(toolExitUtc='1970-01-01T00:00:00.080Z'), dict(gamePid=124),
                        dict(collectorStopRequestedUtc=None), dict(collectorStopRequestedUtc='1970-01-01T00:00:00.095Z'),
                        dict(collectorStoppedWhileTargetAlive=False), dict(collectorFinishedUtc='1970-01-01T00:00:00.250Z'),
                        dict(gameExitCode=1), dict(exitCode=1)):
            actual = diagnostic['eventpipe_coverage'](root, work, session, condition, {**capture, **changed})
            assert actual['status'] == 'partial' and actual['reasons'], '缺失/晚附着、早退出、错 PID 或失败退出仍完整：' + str(changed)
        for changed in (dict(threads=[]), dict(EventsLost=1), dict(Truncated=True),
                        dict(session='another-session'), dict(sampleSegmentId='another-sample')):
            parsed_path.write_text(json.dumps({**parsed, **changed}), encoding='utf-8')
            actual = diagnostic['eventpipe_coverage'](root, work, session, condition, capture)
            assert actual['status'] == 'partial' and actual['reasons'], '缺样、丢失/截断或错误会话/样段仍完整：' + str(changed)
        for changed in (None, dict(managedSamples=0, managedSamplesWithResolvedMethods=0, stackFrameOccurrences=0,
                                   resolvedMethodFrameOccurrences=0, resolvedMethodFrameFraction=None, distinctResolvedMethods=0),
                        {**resolution, 'managedSamplesWithResolvedMethods': 0, 'resolvedMethodFrameOccurrences': 0,
                         'resolvedMethodFrameFraction': 0, 'distinctResolvedMethods': 0},
                        {**resolution, 'managedSamples': 2, 'managedSamplesWithResolvedMethods': 1,
                         'resolvedMethodFrameOccurrences': 4, 'resolvedMethodFrameFraction': 2 / 3}):
            parsed_path.write_text(json.dumps({**parsed, 'managedStackResolution': changed}), encoding='utf-8')
            actual = diagnostic['eventpipe_coverage'](root, work, session, condition, capture)
            assert actual['status'] == 'partial' and actual['reasons'], 'Managed方法解析缺测、零样本、零解析或恰好一半仍完整：' + str(changed)
        completion = dict(sessionId=meta['id'], status='valid')
        game_stdout = work / 'game.stdout.txt'
        game_stdout.write_text('PERF_COLLECTION_RESULT ' + json.dumps(completion) + '\n', encoding='utf-8')
        (work / 'tool.stdout.txt').write_text('PERF_COLLECTION_RESULT ' + json.dumps(dict(sessionId='collector')) + '\n', encoding='utf-8')
        assert diagnostic['collection_result'](work, 'EventPipe') == completion, 'EventPipe 读到了工具的完成输出'
        game_stdout.unlink()
        assert diagnostic['collection_result'](work, 'EventPipe') is None, '缺游戏输出时借用了工具的完成输出'


def _check_completed_diagnostics(dictionary, budgets, meta, run, rows, plan):
    with tempfile.TemporaryDirectory(prefix='uestc-perf-diagnostic-') as temporary:
        source, captured, gpu, unrelated = (Path(temporary) / name for name in ('source', 'captured', 'gpu', 'unrelated'))
        original = copy.deepcopy(meta)
        original['build']['commit'] = 'selfcheck-source-commit'
        original['build']['moduleVersionId'] = 'selfcheck-module'
        diagnostic = copy.deepcopy(original)
        diagnostic['id'] = 'DIAGNOSTIC-SELFCHECK'
        other_run = {**run, 'scene': 'unrelated-scene'}
        for directory, manifest_data, run_data in ((source, original, run), (captured, diagnostic, run),
                                                  (gpu, {**diagnostic, 'id': 'GPU-SELFCHECK'}, run),
                                                  (unrelated, original, other_run)):
            directory.mkdir()
            (directory / 'manifest.json').write_text(json.dumps(manifest_data), encoding='utf-8')
            (directory / 'run.json').write_text(json.dumps(run_data), encoding='utf-8')
        attachment = captured / 'graphics.nsys-rep'
        attachment.write_bytes(b'selfcheck-declared-attachment')
        report_path = captured / 'diagnostic.json'
        report = dict(purpose='diagnostic', status='valid', sourceSession=original['id'],
                      condition=dict(sourceDirectory=str(source), sourceSession=original['id'], scene=run['scene'], entry=plan, tool='Nsight'),
                      actualCollector=dict(status='completed', exitCode=0), attachments=[attachment.name], conditionDifferences={},
                      attachmentAvailability=dict(available=True, reasons=[]))
        report_path.write_text(json.dumps(report), encoding='utf-8')
        directories = (source, captured, gpu, unrelated)
        key = (*diagnostic_identity(original, run), plan['type'], plan['metric'], plan['phase'], 'Nsight')
        completed = completed_diagnostic_clusters(directories)
        assert completed == {key}, '成功诊断未按真实构建、比较条件、聚类和工具消重'
        before = source_stamp(source, dictionary, budgets, source / 'run.json')
        after = source_stamp(source, dictionary, budgets, source / 'run.json', completed)
        assert before != after and after['diagnosedClusters'], '成功诊断未令相关源失鲜'
        assert source_stamp(unrelated, dictionary, budgets, unrelated / 'run.json') == source_stamp(
            unrelated, dictionary, budgets, unrelated / 'run.json', completed), '诊断错误地令其他场景失鲜'
        summary, items, _, _ = summary_for(source, original, streams(copy.deepcopy(rows), dictionary), dictionary, budgets, run, completed)
        assert summary['deepen'][0]['tools'] == ['gpu-profile'] and "-DiagnosticTool 'gpu-profile'" in summary['deepen'][0]['command'], '单工具成功抹掉了其它工具或命令未同步'
        assert any(a['type'] == plan['type'] and a['metric'] == plan['metric'] for a in items), '去重删除了异常事实'
        gpu_attachment = gpu / 'gpu-evidence.csv'
        gpu_attachment.write_text('selfcheck-available-evidence\n', encoding='utf-8')
        gpu_report = {**report, 'condition': {**report['condition'], 'tool': 'gpu-profile'}, 'attachments': [gpu_attachment.name]}
        (gpu / 'diagnostic.json').write_text(json.dumps(gpu_report), encoding='utf-8')
        all_completed = completed_diagnostic_clusters(directories)
        assert all_completed == {key, (*key[:-1], 'gpu-profile')}
        assert source_stamp(source, dictionary, budgets, source / 'run.json', all_completed) != after, '不同完成工具没有各自进入判鲜键'
        assert not summary_for(source, original, streams(copy.deepcopy(rows), dictionary), dictionary, budgets, run, all_completed)[0]['deepen'], '工具全成功仍保留条目'
        (gpu / 'diagnostic.json').unlink()
        for state in ('partial', 'preparing'):
            report_path.write_text(json.dumps({**report, 'status': state}), encoding='utf-8')
            assert not completed_diagnostic_clusters(directories), '失败或准备中的诊断抑制了条目'
        failed = {**report, 'actualCollector': dict(status='failed', exitCode=1)}
        report_path.write_text(json.dumps(failed), encoding='utf-8')
        assert not completed_diagnostic_clusters(directories), '失败采集器抑制了条目'
        for changed in ({**report, 'attachmentAvailability': None},
                        {**report, 'attachmentAvailability': dict(available=False, reasons=['No usable methods'])},
                        {key: value for key, value in report.items() if key != 'attachmentAvailability'}):
            report_path.write_text(json.dumps(changed), encoding='utf-8')
            assert not completed_diagnostic_clusters(directories), '缺失或不可用附件事实仍抑制条目'
        report_path.write_text('invalid-json', encoding='utf-8')
        assert not completed_diagnostic_clusters(directories), '损坏报告抑制了条目'
        for field in ('type', 'metric', 'phase'):
            changed = copy.deepcopy(report)
            changed['condition']['entry'][field] = 'another-' + field
            report_path.write_text(json.dumps(changed), encoding='utf-8')
            remaining = summary_for(source, original, streams(copy.deepcopy(rows), dictionary), dictionary, budgets, run,
                                    completed_diagnostic_clusters(directories))[0]['deepen']
            assert remaining[0]['tools'] == plan['tools'], '其它聚类抑制了本条工具：' + field
        for build in ({**original['build'], 'commit': 'another-commit'}, {**original['build'], 'dirty': True},
                      {**original['build'], 'dirty': None}, {**original['build'], 'moduleVersionId': 'another-module'}):
            (captured / 'manifest.json').write_text(json.dumps({**diagnostic, 'build': build}), encoding='utf-8')
            report_path.write_text(json.dumps(report), encoding='utf-8')
            assert not completed_diagnostic_clusters(directories), '其它构建或dirty/未知诊断抑制了源条目'
        (captured / 'manifest.json').write_text(json.dumps(diagnostic), encoding='utf-8')
        (source / 'manifest.json').write_text(json.dumps({**original, 'build': {**original['build'], 'dirty': True}}), encoding='utf-8')
        assert not completed_diagnostic_clusters(directories), 'dirty源抑制了诊断条目'
        (source / 'manifest.json').write_text(json.dumps(original), encoding='utf-8')
        report_path.write_text(json.dumps({**report, 'conditionDifferences': {'build': {'reference': 'different'}}}), encoding='utf-8')
        assert not completed_diagnostic_clusters(directories), '报告有构建差异仍抑制条目'
        report_path.write_text(json.dumps(report), encoding='utf-8')
        restored_stamp = source_stamp(source, dictionary, budgets, source / 'run.json')
        attached_stamp = source_stamp(source, dictionary, budgets, source / 'run.json', completed_diagnostic_clusters(directories))
        assert attached_stamp != restored_stamp and attached_stamp['diagnosedClusters'], '恢复后的源未建立有附件的诊断判鲜键'
        attachment.unlink()
        completed = completed_diagnostic_clusters(directories)
        assert not completed and source_stamp(source, dictionary, budgets, source / 'run.json', completed) == restored_stamp, '附件缺失没有恢复源失鲜键'
        summary = summary_for(source, original, streams(copy.deepcopy(rows), dictionary), dictionary, budgets, run, completed)[0]
        assert any(entry['type'] == plan['type'] and entry['metric'] == plan['metric'] for entry in summary['deepen']), '附件缺失后没有恢复 GPU 诊断候选'


def check_manifest(root, dictionary, budgets):
    meta = manifest(dictionary)
    meta['display']['refreshHz'] = None
    assert target_ms(meta) == 1000 / meta['pacing']['maxFps'], '真实 disabled 被当成开启垂直同步'


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


def check_cost_windows(root, dictionary, budgets):
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames']
    window = policy['medianWindowFrames']
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


def check_cold_gap(root, dictionary, budgets):
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames']
    count = min(60, warm - 1)
    frames = [frame(dictionary, i, gpu=budgets['deviceTargets']['developer']['mainViewportGpuP95MsUpper'] * 2) for i in range(count)]
    frames[20]['gapBefore'] = True
    frames[20]['dtMs'] = 500
    meta = manifest(dictionary)
    meta['origin'] = 'ai'
    summary, items, _, _ = summary_for(Path('selfcheck'), meta, streams(frames, dictionary), dictionary, budgets, dict(conditions=dict(suite='fixed')))
    assert not any(a['type'] == 'costSpike' for a in items), '冷启动不足暖窗仍出了相对判据'
    assert any(a['type'] == 'budgetExceeded' for a in items), '不足暖窗漏了绝对预算判据'
    assert at(summary, 'all.experience.averageFps') == 100, '跨 gap 间隔进入体验计算'
    assert at(summary, 'all.experience.framePacingP95Ms') == 0, '帧节奏跨了 gap'
    assert at(summary, 'all.experience.low1Fps') is None, '不足样本仍计算 low'


def check_viewport_sources(root, dictionary, budgets):
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
