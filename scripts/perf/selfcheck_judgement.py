"""验证判停必判缺测、绝对成本线、原始帧计次、等待单边判与散布边界。
用法：由 perf.py selfcheck 编排；只用小样本，不启动游戏、不产生性能基线。
"""
import copy
from pathlib import Path
from analyze import summary_for
from perf_anomalies import merge_anomalies
from perf_batches import batch_pain, batch_results, distance_table
from perf_model import COSTS, at
from perf_waits import visible_intervals, wait_observations
from selfcheck_fixtures import ai_run, frame, manifest, mark_sample, streams


def measured(dictionary, budgets, rows, conditions=None, events=None):
    meta = manifest(dictionary)
    meta['desktop'] = dict(kind='isolated', isInputDesktop=False, windowFocused=False)
    data = streams(rows, dictionary)
    data['events'] += events or []
    run = dict(scene='judgement-selfcheck', conditions=conditions or {})
    return summary_for(Path('fixture'), meta, data, dictionary, budgets, run)


def check_judgement(root, dictionary, budgets):
    """Regression：绝对线冷窗可触发，严格边界、遮挡和两条核验开关不串口径。"""
    policy = budgets['perfAnomalies']
    rows = [frame(dictionary, 0)]
    rows[0]['frameSetupCpuMs'] = policy['heavyStutterMs'] + 1
    summary, items, _, _ = measured(dictionary, budgets, rows)
    assert any(v['type'] == 'costStutter' and v['level'] == 'heavy' for v in items)
    assert at(summary, 'all.experience.averageFps') is None, '隔离成本判据污染体验字段'
    rows[0]['frameSetupCpuMs'] = policy['heavyStutterMs']
    assert not any(v['type'] == 'costStutter' and v['level'] == 'heavy' for v in measured(dictionary, budgets, rows)[1])
    rows[0]['frameSetupCpuMs'] += 1
    rows[0]['contextFlags'] = 32
    covered = measured(dictionary, budgets, rows)[0]
    assert at(covered, 'judgement.pain.counts.costStutter') == 0
    rows = [frame(dictionary, i, gpu=policy['heavyStutterMs'] + 1) for i in range(5)]
    rows[-1]['contextFlags'] = 32
    candidates = [v for v in measured(dictionary, budgets, rows)[1] if v['type'] == 'costStutter' and v['metric'] == 'mainViewportGpuMs']
    assert any(v['visibility'] == 'candidate' for v in candidates), '遮挡边界被当成精确可见帧'
    rows = [frame(dictionary, 0, dt=policy['severeStutterMs'] + 1)]
    rows[0].update(physMs=policy['heavyStutterMs'] + 1, procMs=0, lateMs=0,
                   drawMs=0, tailMs=policy['severeStutterMs'] - policy['heavyStutterMs'])
    assert not any(v['type'] in ('logicPause', 'mainThreadLongStop') for v in measured(dictionary, budgets, rows)[1])
    enabled = dict(logicalPauseVerified=True, mainThreadLongStopVerified=True)
    assert {'logicPause', 'mainThreadLongStop'} <= {v['type'] for v in measured(dictionary, budgets, rows, enabled)[1]}
    required = measured(dictionary, budgets, rows, dict(suite='route'))[0]['judgement']['requiredChecks']
    assert any(v['key'] == 'logicPause' and v['status'] == 'rerun' for v in required)
    rows = [frame(dictionary, i) for i in range(100)]
    for row in rows:
        row.update(contextFlags=1, gcPauseMs=policy['gcWindowPauseMs'] * 1.5 / len(rows))
    assert any(v['type'] == 'gcWindow' for v in measured(dictionary, budgets, rows)[1]), '非稳态游玩段吞掉了绝对GC窗判据'


def check_raw_counts(root, dictionary, budgets):
    """Regression：同帧多指标不重复，合并相邻异常不吞原始帧数，越预算不算痛点。"""
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames'] + 1
    rows = [frame(dictionary, i) for i in range(warm + 2)]
    for row in rows[-2:]:
        row['frameSetupCpuMs'] = policy['lightCostStutterMs'] + 1
        row['viewportSamplesJson'][0]['cpuRenderMs'] = policy['lightCostStutterMs'] + 1
    summary, items, _, _ = measured(dictionary, budgets, rows, dict(suite='route'))
    assert at(summary, 'judgement.pain.lightFrames') == 2
    assert at(summary, 'judgement.pain.counts.costStutter') == 0
    assert any(v['occurrences'] == 2 for v in merge_anomalies([v for v in items if v['type'] == 'costStutter']))
    only_budget = measured(dictionary, budgets, [frame(dictionary, i, gpu=budgets['deviceTargets']['developer']['mainViewportGpuP95MsUpper'] + 1)
                                                for i in range(3)])[0]
    assert any(v['exceeded'] is True for v in only_budget['budgets'])
    assert at(only_budget, 'judgement.pain.heavyOrWorseFrames') == 0


def check_required_sets(root, dictionary, budgets):
    """Regression：直接传送零稳态不强求帧成本，轻级计率合批，缺显存容量照实重跑。"""
    enabled = dict(logicalPauseVerified=True, mainThreadLongStopVerified=True)
    direct_rows = [frame(dictionary, i) for i in range(5)]
    for row in direct_rows:
        row['contextFlags'] = 1
    direct = measured(dictionary, budgets, direct_rows, dict(suite='travel', **enabled))[0]
    assert at(direct, 'steady.cost.frameSetupCpuMs.n') == 0, '夹具未触发零稳态传送'
    checks = {check['key']: check for check in direct['judgement']['requiredChecks']}
    assert not set(COSTS) & checks.keys() and 'lightCostStutter' not in checks
    assert checks['costStutter']['status'] == 'measured' and checks['gc']['status'] == 'measured'
    assert checks['videoMemory']['status'] == 'rerun', '缺容量/硬件源冒充显存已判'
    assert all(check['exceeded'] is None for check in direct['budgets']), '传送套仍判帧成本预算'
    assert not distance_table([direct], direct), '传送套仍进入帧成本距离表'
    route = measured(dictionary, budgets, [frame(dictionary, i) for i in range(5)], dict(suite='route', **enabled))[0]
    assert set(COSTS) <= {check['key'] for check in route['judgement']['requiredChecks']}
    assert {check['metric'] for check in distance_table([route], route)} == set(COSTS)
    mixed = batch_pain([route, direct], budgets)
    assert mixed['steadyDurationMs'] == at(route, 'judgement.pain.steadyDurationMs') > 0
    assert mixed['lightFramesPerMinute'] == 0 and batch_pain([direct], budgets)['lightFramesPerMinute'] is None
    entries = []
    for scene, suite, rows in (('lake-default', 'fixed', [frame(dictionary, i) for i in range(3)]),
                               ('route', 'route', [frame(dictionary, i) for i in range(5)]),
                               ('standing-travel-direct', 'travel', direct_rows)):
        for repeat in (1, 2, 3):
            meta = manifest(dictionary, f'{scene}-{repeat}')
            meta.update(origin='ai')
            meta['build']['commit'] = 'selfcheck'
            meta['desktop'] = dict(kind='isolated', isInputDesktop=False, windowFocused=False)
            run = ai_run(scene=scene, repetition=repeat, scenes=['lake-default', 'route', 'standing-travel-direct'])
            run['conditions'] = dict(suite=suite, **enabled)
            data = mark_sample(streams(copy.deepcopy(rows), dictionary), dictionary)
            entries.append(summary_for(Path(meta['id']), meta, data, dictionary, budgets, run)[0])
    lake = entries[0]
    assert lake['comparisonKey']['scene'] == 'lake-default' and at(lake, 'steady.cost.measuredViewportGpuMs.p95') is None
    aggregate = next(check for check in lake['budgets'] if check['metric'] == 'measuredViewportGpuMs')
    assert aggregate['applicable'] is False and aggregate['exceeded'] is None and '未判' in aggregate['reason']
    assert {check['key'] for check in lake['judgement']['requiredChecks']} == set(COSTS)
    assert {check['metric'] for check in distance_table([lake], lake)} == set(COSTS)
    batch = batch_results(entries, budgets)[0][0]
    assert 'measuredViewportGpuMs' in batch['unjudged']
    assert not any('lightCostStutter' in reason for reason in batch['statusReasons'])
    assert any('/videoMemory:' in reason for reason in batch['statusReasons']), '批次掩盖硬件缺源'
    empty = [row for row in entries if at(row, 'run.conditions.suite') == 'travel']
    for row in empty:
        row['run']['protocol']['scenes'] = ['standing-travel-direct']
    batch = batch_results(empty, budgets)[0][0]
    assert any('a1/lightCostStutter:' in reason for reason in batch['statusReasons']), '批次零分母仍可判停'


def check_waits(root, dictionary, budgets):
    """Regression：真实进场端点可晚于完成、不串下次请求；缺终点和 AI 启动不误判。"""
    def event(name, when, **payload):
        return dict(schema=dictionary['schema'], sequence=int(when) + 1, event=name, source='selfcheck',
                    monotonicMs=when, collectionFrame=None, payload=payload)
    def enter(when, operation, phase, kind='newgame'):
        return event('game.enter', when, owner='IAppFlow', operation=operation, phase=phase, target=dict(kind=kind))
    def item(result, key):
        return next(v for v in result['items'] if v['key'] == key)
    events = [event('loading.state', 0, active=True, covered=True, progress=0),
              event('loading.state', 1000, active=True, covered=True, progress=.1),
              event('map.arrival', 15000, phase='progress', completed=99, total=100),
              event('loading.state', 23000, active=False, covered=False, progress=.1)]
    assert visible_intervals(events)[0]['longestNoProgressMs'] == 22000, 'arrival或通知代替了真实可见增长'
    meta = manifest(dictionary)
    meta['clock']['anchors'] = [dict(unixMs=946684800000, monotonicMs=0, readSpanMs=0)]
    run = dict(launcher=dict(scriptEnteredUtc='2000-01-01T00:00:00Z', processExitUtc='2000-01-01T00:00:07Z'))
    entry_events = [enter(0, 1, 'started'), enter(1000, 1, 'completed'), event('app.phase', 1005, phase='input-ready'),
                    enter(2000, 2, 'started', 'continue'), enter(3000, 2, 'completed', 'continue'),
                    event('app.phase', 3005, phase='input-ready'), enter(4000, 3, 'started', 'loadslot'),
                    enter(4500, 3, 'completed', 'loadslot'), event('app.phase', 4505, phase='input-ready'),
                    event('app.phase', 4900, phase='preload-ready'), enter(5000, 4, 'started'),
                    enter(5500, 4, 'completed'), event('app.phase', 5505, phase='input-ready'),
                    event('app.state', 6000, to='quitting')]
    observed = wait_observations(meta, entry_events, run, budgets)
    for key, seconds in (('immediateNewGameToWalkSecondsMax', 1.005), ('continueToWalkSecondsMax', 1.005),
                         ('loadSlotToWalkSecondsMax', .505), ('preloadedNewGameToWalkSecondsMax', .505), ('exitSecondsMax', 1)):
        value = item(observed, key)
        assert value['seconds'] == seconds and value['status'] == 'observed-pass' and value['reason'] is None
    missing_events = [enter(0, 1, 'started'), enter(1000, 1, 'completed')]
    missing = item(wait_observations(meta, missing_events, run, budgets), 'immediateNewGameToWalkSecondsMax')
    assert missing['seconds'] is None and missing['status'] == 'unjudged' and '终点缺测' in missing['reason']
    assert '超限' not in missing['reason']
    chained = missing_events + [enter(2000, 2, 'started', 'continue'), enter(3000, 2, 'completed', 'continue'),
                               event('app.phase', 3005, phase='input-ready')]
    result = wait_observations(meta, chained, run, budgets)
    assert item(result, 'immediateNewGameToWalkSecondsMax')['toMs'] is None, '后次进场端点补到了前次'
    assert item(result, 'continueToWalkSecondsMax')['seconds'] == 1.005
    mismatched = [enter(0, 1, 'started'), enter(1000, 2, 'completed'), event('app.phase', 1005, phase='input-ready')]
    assert item(wait_observations(meta, mismatched, run, budgets), 'immediateNewGameToWalkSecondsMax')['toMs'] is None
    meta.update(origin='ai', desktop=dict(kind='isolated', isInputDesktop=False))
    for desktop in (dict(kind='isolated', isInputDesktop=False), dict(kind='interactive', isInputDesktop=True)):
        meta['desktop'] = desktop
        for end in (1, 60000):
            phases = events + [event('app.phase', end, phase='title-input-ready'), event('app.phase', end + 5, phase='input-ready')]
            phases.sort(key=lambda e: e['monotonicMs'])
            result = wait_observations(meta, phases, run, budgets)
            for key in ('warmLauncherToTitleSecondsMax', 'coldLauncherToTitleSecondsMax', 'firstRunLauncherToRealtimeSecondsMax'):
                value = item(result, key)
                assert value['seconds'] is not None and value['status'] == 'unjudged' and value['pain'] is None and value['reason']
    meta['desktop'] = dict(kind='isolated', isInputDesktop=False)
    missing_exit = item(wait_observations(meta, [], {}, budgets), 'exitSecondsMax')
    assert '终点缺测' in missing_exit['reason'] and '超限' not in missing_exit['reason']
    limit_ms = budgets['waits']['continueToWalkSecondsMax'] * 1000
    for end, expected in ((limit_ms, 'observed-pass'), (limit_ms + 1, 'unjudged')):
        phases = [enter(0, 1, 'started', 'continue'), enter(end - 5, 1, 'completed', 'continue'),
                  event('app.phase', end, phase='input-ready')]
        value = item(wait_observations(meta, phases, run, budgets), 'continueToWalkSecondsMax')
        assert value['status'] == expected and value['pain'] in (False, None)
    data = mark_sample(streams([frame(dictionary, i) for i in range(5)], dictionary), dictionary, begin=20, end=50)
    data['events'] += [enter(0, 1, 'started', 'continue'), enter(5995, 1, 'completed', 'continue'),
                       event('app.phase', 6000, phase='input-ready')]
    result = summary_for(Path('fixture'), meta, data, dictionary, budgets, ai_run())[0]
    assert item(result['waits'], 'continueToWalkSecondsMax')['seconds'] == 6


def check_aba_ruler(root, dictionary, budgets):
    """Regression：改进必须严格越过两把散布尺，缺sigma0不默认为零。"""
    entries = []
    for batch, values in (('calibration', (5, 6, 5)), ('change', (6, 4.5, 6))):
        for phase_index, (phase, gpu) in enumerate(zip(('a1', 'b1', 'a2'), values)):
            for repeat in (1, 2, 3):
                meta = manifest(dictionary, f'{batch}-{phase}-{repeat}')
                second = phase_index * 10 + repeat * 2
                meta.update(origin='ai', startedAt=f'2000-01-01T00:00:{second:02d}Z', endedAt=f'2000-01-01T00:00:{second + 1:02d}Z')
                meta['build'].update(commit='baseline' if batch == 'calibration' or phase != 'b1' else 'candidate')
                run = ai_run(batch=batch, phase=phase, repetition=repeat, kind='aba')
                run['conditions']['noiseCalibrationBatchId'] = 'calibration'
                data = mark_sample(streams([frame(dictionary, i, gpu=gpu) for i in range(3)], dictionary), dictionary)
                entries.append(summary_for(Path(meta['id']), meta, data, dictionary, budgets, run)[0])
    batches, _ = batch_results(entries, budgets)
    result = next(b for b in batches if b['id'] == 'change')
    check = next(v for v in result['abaAssessment'] if v['metric'] == 'mainViewportGpuMs')
    assert check['sigma0'] == 1 and check['requiredImprovement'] == 1 and check['accepted'] is True
    unverified = copy.deepcopy(entries)
    for row in unverified:
        row['judgement']['requiredChecks'].append(dict(key='logicPause', observed=3, status='rerun', reason='第0轮未核'))
    partial, blocked = batch_results(unverified, budgets)
    calibration = next(b for b in partial if b['id'] == 'calibration')
    assert calibration['status'] == 'partial' and len(blocked) == len(unverified)
    assert at(calibration['groups'][0], 'phases.b1.steady.cost.mainViewportGpuMs.p95') == 6
    assert next(v for v in calibration['groups'][0]['noiseRanges'] if v['metric'] == 'mainViewportGpuMs')['sigma0'] == 1
    assert all(v['accepted'] is None for b in partial for v in b['abaAssessment']), '未核判据冒充合格基线'
    broken = batch_results(unverified[:-1], budgets)[0]
    incomplete = next(b for b in broken if b['id'] == 'change')
    assert all('steady' not in stage for group in incomplete['groups'] for stage in group['phases'].values()), '协议缺次仍提供资格成本'
    changed = copy.deepcopy(entries)
    for row in changed:
        if at(row, 'run.protocol.batchId') == 'change' and at(row, 'run.protocol.phase') == 'b1':
            row['steady']['cost']['mainViewportGpuMs']['p95'] = 5
    result = next(b for b in batch_results(changed, budgets)[0] if b['id'] == 'change')
    assert next(v for v in result['abaAssessment'] if v['metric'] == 'mainViewportGpuMs')['accepted'] is False
    for row in changed:
        row['run']['conditions']['noiseCalibrationBatchId'] = 'missing'
    result = next(b for b in batch_results(changed, budgets)[0] if b['id'] == 'change')
    assert next(v for v in result['abaAssessment'] if v['metric'] == 'mainViewportGpuMs')['accepted'] is None


def check_stopping(root, dictionary, budgets):
    for check in (check_judgement, check_raw_counts, check_required_sets, check_waits, check_aba_ruler):
        check(root, dictionary, budgets)
