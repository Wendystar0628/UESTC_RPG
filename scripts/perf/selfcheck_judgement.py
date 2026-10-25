"""验证判停必判缺测、绝对成本线、原始帧计次、等待单边判与相邻轮比较。
用法：由 perf.py selfcheck 编排；只用小样本，不启动游戏、不产生性能基线。
"""
import copy
import json
import tempfile
from argparse import Namespace
from pathlib import Path
from analyze import summary_for
from perf_anomalies import budget_checks, merge_anomalies
from perf_batches import batch_pain, batch_results, distance_table, ROUND_RULES
from perf_model import COSTS, at
from perf_curves import bucket_curves
from perf_store import batch_states, git, rebuild_states
from perf_waits import visible_intervals, wait_observations
from selfcheck_fixtures import ai_run, frame, manifest, mark_sample, streams, viewport, round_registration


def measured(dictionary, budgets, rows, conditions=None, events=None):
    meta = manifest(dictionary)
    meta['origin'] = 'ai'
    meta['desktop'] = dict(kind='isolated', isInputDesktop=False, windowFocused=False)
    data = streams(rows, dictionary)
    data['events'] += events or []
    run = dict(scene='judgement-selfcheck-' + str((conditions or {}).get('suite', 'unknown')), conditions=conditions or {})
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
                                                for i in range(3)], dict(suite='fixed'))[0]
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
    for scene, suite, rows in (('fixed-with-reflection', 'fixed', [frame(dictionary, i) for i in range(3)]),
                               ('route', 'route', [frame(dictionary, i) for i in range(5)]),
                               ('standing-travel-direct', 'travel', direct_rows)):
        if suite in ('fixed', 'route'):
            for row in rows:
                row['viewportSamplesJson'].append(viewport(2, 'reflection', None, None))
        meta = manifest(dictionary, scene)
        meta.update(origin='ai')
        meta['build']['commit'] = 'selfcheck'
        meta['machine']['vramMiB'] = 1000
        meta['desktop'] = dict(kind='isolated', isInputDesktop=False, windowFocused=False)
        run = ai_run(scene=scene)
        run['conditions'] = dict(suite=suite, **enabled)
        data = mark_sample(streams(copy.deepcopy(rows), dictionary), dictionary, meta)
        data['hw'] = [dict(rowKind='nvml', isRenderAdapter=True, monotonicMs=10, wholeGpuMemoryMiB=100)]
        entries.append(summary_for(Path(meta['id']), meta, data, dictionary, budgets, run)[0])
    reflected = entries[0]
    assert at(reflected, 'steady.cost.measuredViewportGpuMs.p95') is None
    aggregate = next(check for check in reflected['budgets'] if check['metric'] == 'measuredViewportGpuMs')
    assert aggregate['applicable'] is False and aggregate['exceeded'] is None and '未判' in aggregate['reason']
    assert {check['key'] for check in reflected['judgement']['requiredChecks']} == set(COSTS)
    assert {check['metric'] for check in distance_table([reflected], reflected)} == set(COSTS)
    registration = round_registration(Path.cwd(), [row['comparisonKey']['scene'] for row in entries])
    batch = batch_results(entries, budgets, rounds=[registration])[0]
    assert 'measuredViewportGpuMs' in batch['unjudged']
    assert batch['status'] == 'candidate', '完整路线加零稳态传送仍被要求重跑'
    routes = [row for row in entries if at(row, 'run.conditions.suite') != 'fixed']
    route_plan = round_registration(Path.cwd(), [row['comparisonKey']['scene'] for row in routes])
    assert 'measuredViewportGpuMs' in batch_results(routes, budgets, rounds=[route_plan])[0]['unjudged']
    empty = [row for row in entries if at(row, 'run.conditions.suite') == 'travel']
    empty_plan = round_registration(Path.cwd(), [row['comparisonKey']['scene'] for row in empty])
    batch = batch_results(empty, budgets, rounds=[empty_plan])[0]
    assert batch['pain']['lightFramesPerMinute'] is None and batch['pain']['status'] == 'rerun'


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
    for origin in ('ai', 'play'):
        meta['origin'] = origin
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
    data = mark_sample(streams([frame(dictionary, i) for i in range(5)], dictionary), dictionary, meta, begin=20, end=50)
    data['events'] += [enter(0, 1, 'started', 'continue'), enter(5995, 1, 'completed', 'continue'),
                       event('app.phase', 6000, phase='input-ready')]
    result = summary_for(Path('fixture'), meta, data, dictionary, budgets, ai_run())[0]
    assert item(result['waits'], 'continueToWalkSecondsMax')['seconds'] == 6


def check_round_ruler(root, dictionary, budgets):
    """Regression：相邻单次按规则值判收益，未核与确认缺测保留成本且禁止合入。"""
    from perf_batches import scene_pain
    entries, registrations = [], []
    fraction = ROUND_RULES['minimumImprovementFraction']
    for n, value in enumerate((4, 4 * (1 - fraction / 2))):
        identifier = 'round-' + str(n)
        source = Path.cwd() / identifier
        meta = manifest(dictionary, identifier)
        meta.update(origin='ai', desktop=dict(kind='isolated'))
        meta['build']['commit'] = 'selfcheck'
        run = ai_run(round_id=identifier)
        sample_rows = [frame(dictionary, i, gpu=value) for i in range(3)]
        for row in sample_rows:
            row['frameSetupCpuMs'] = budgets['deviceTargets']['developer']['frameSetupCpuP95MsUpper'] / 2
        data = mark_sample(streams(sample_rows, dictionary), dictionary, meta)
        entries.append(summary_for(source / identifier, meta, data, dictionary, budgets, run)[0])
        registrations.append(round_registration(source, ['baseline-selfcheck'], identifier, n, 'round-0' if n else None))
    def result(rows):
        return batch_results(rows, budgets, rounds=registrations)[1]
    def check(round_):
        return next(row for row in round_['comparison'] if row['metric'] == 'mainViewportGpuMs')
    assert check(result(entries))['change'] == 'unchanged' and check(result(entries))['accepted'] is False
    travel = copy.deepcopy(entries)
    for row in travel:
        row['run']['conditions']['suite'] = 'travel'
        for budget in row['budgets']:
            budget.update(required=False, applicable=False, exceeded=None)
    assert not result(travel)['groups'][0]['distanceTable']
    assert len(result(travel)['comparison']) == len(COSTS), '传送套不判帧预算，但已测成本变化仍须保留'

    improved = copy.deepcopy(entries)
    improved[1]['steady']['cost']['mainViewportGpuMs']['p95'] = 4 * (1 - fraction * 2)
    assert check(result(improved))['accepted'] is True
    for key in ('logicPause', 'mainThreadLongStop'):
        improved[0]['judgement']['requiredChecks'].append(dict(key=key, observed=3, status='rerun', reason='第0轮未核'))
    unverified = batch_results(improved, budgets, rounds=registrations)
    assert unverified[0]['status'] == 'candidate' and unverified[0]['gateStatus'] == 'Incomplete'
    assert at(unverified[0]['groups'][0], 'steady.cost.mainViewportGpuMs.p95') == 4
    assert all(row['accepted'] is None for row in unverified[1]['comparison'])
    assert next(row for row in unverified[1]['comparison'] if row['metric'] == 'mainViewportGpuMs')['change'] == 'improved'
    assert {'logicPause', 'mainThreadLongStop'} <= set(unverified[0]['unjudged'])
    # Confirmation pain is the worse observed count/rate, never their sum.
    before, after = copy.deepcopy(entries[1]), copy.deepcopy(entries[1])
    for row, count, duration in ((before, 1, 60000), (after, 2, 120000)):
        row['judgement']['pain'].update(counts=dict(costStutter=count), lightFrames=count, steadyDurationMs=duration)
    pain = scene_pain([before, after])
    assert pain['counts']['costStutter'] == 2 and pain['lightFramesPerMinute'] == 1
    assert 'lightExceeded' not in pain and 'lightLimit' not in pain
    crossing = copy.deepcopy(entries)
    crossing[1]['judgement']['pain']['counts']['gc'] = 1
    assert check(result(crossing))['accepted'] is None
    mismatch = copy.deepcopy(entries)
    mismatch[1]['comparisonKey']['machine'] = 'different'
    assert not result(mismatch)['comparison']


def check_stopping(root, dictionary, budgets):
    for check in (check_judgement, check_raw_counts, check_required_sets, check_waits, check_round_ruler,
                  check_batch_light, check_render_memory, check_budget_consumers):
        check(root, dictionary, budgets)


def playing_sample(dictionary, budgets, rows, scene='route', suite='route', hardware=None):
    meta = manifest(dictionary, scene)
    meta['origin'] = 'ai'
    meta['desktop'] = dict(kind='isolated', isInputDesktop=False, windowFocused=False)
    meta['machine']['vramMiB'] = 1000
    data = streams(rows, dictionary)
    data['hw'] = hardware if hardware is not None else [dict(rowKind='nvml', isRenderAdapter=True, monotonicMs=10, wholeGpuMemoryMiB=100)]
    run = dict(scene=scene, conditions=dict(suite=suite, logicalPauseVerified=True, mainThreadLongStopVerified=True))
    return summary_for(Path('fixture'), meta, data, dictionary, budgets, run), meta, data


def check_batch_light(root, dictionary, budgets):
    """Regression：单次高轻级率不改状态，路线稀释后只在合计判，零稳态传送不单独阻断。"""
    policy = budgets['perfAnomalies']
    rows = [frame(dictionary, i) for i in range(policy['medianMinimumFrames'] + 2)]
    rows[-1]['frameSetupCpuMs'] = policy['lightCostStutterMs'] + 1
    noisy = playing_sample(dictionary, budgets, rows)[0][0]
    raw = noisy['judgement']['pain']
    assert raw['lightFrames'] == 1 and raw['lightFrames'] * 60000 / raw['steadyDurationMs'] > policy['lightCostStutterFramesPerMinuteMax']
    assert all(v['status'] == 'measured' for v in noisy['judgement']['requiredChecks'])
    assert raw['status'] == 'clear'
    steady_ms = 60000 / policy['lightCostStutterFramesPerMinuteMax'] + raw['steadyDurationMs']
    quiet = playing_sample(dictionary, budgets, [frame(dictionary, i) for i in range(int(steady_ms / 10) + 1)], scene='quiet-route')[0][0]
    direct_rows = [frame(dictionary, i) for i in range(3)]
    for row in direct_rows:
        row['contextFlags'] = 1
    direct = playing_sample(dictionary, budgets, direct_rows, scene='direct', suite='travel')[0][0]
    assert direct['judgement']['pain']['steadyDurationMs'] == 0
    assert direct['judgement']['pain']['status'] == 'clear' and all(v['status'] == 'measured' for v in direct['judgement']['requiredChecks'])
    combined = batch_pain([noisy, quiet, direct], budgets)
    assert combined['lightFrames'] == 1 and combined['lightExceeded'] is False and combined['status'] == 'clear'
    assert batch_pain([noisy, direct], budgets)['lightExceeded'] is True


def check_render_memory(root, dictionary, budgets):
    """Regression：第二卡高显存不污染渲染卡异常/必判/峰值/曲线；身份或容量缺测照实重跑。"""
    hardware = [dict(rowKind='nvml', isRenderAdapter=True, monotonicMs=10, wholeGpuMemoryMiB=100, processVideoMemoryMiB=50),
                dict(rowKind='nvml', isRenderAdapter=False, monotonicMs=10, wholeGpuMemoryMiB=2000, processVideoMemoryMiB=1500)]
    result, meta, data = playing_sample(dictionary, budgets, [frame(dictionary, i) for i in range(3)], hardware=hardware)
    summary, items, _, _ = result
    assert not any(v['type'] == 'videoMemory' for v in items) and at(summary, 'judgement.pain.status') == 'clear'
    assert at(summary, 'all.cost.hardware.wholeGpuMemoryMiB.maximum') == 100
    assert at(summary, 'all.cost.hardware.processVideoMemoryMiB.maximum') == 50
    curve = bucket_curves(summary, meta, data, 900)
    column = curve['columns'].index('wholeGpuMemoryMiBMax')
    assert max(v[column] for v in curve['buckets'] if v[column] is not None) == 100
    hardware[0]['isRenderAdapter'] = None
    unknown = playing_sample(dictionary, budgets, [frame(dictionary, i) for i in range(3)], hardware=hardware)[0][0]
    assert at(unknown, 'all.cost.hardware.wholeGpuMemoryMiB.maximum') is None
    assert next(v for v in unknown['judgement']['requiredChecks'] if v['key'] == 'videoMemory')['status'] == 'rerun'
    unknown_curve = bucket_curves(unknown, meta, data, 900)
    assert all(v[column] is None for v in unknown_curve['buckets'])
    hardware[0].update(isRenderAdapter=True, wholeGpuMemoryMiB=1000)
    triggered = playing_sample(dictionary, budgets, [frame(dictionary, i) for i in range(3)], hardware=hardware)[0]
    assert any(v['type'] == 'videoMemory' for v in triggered[1]) and at(triggered[0], 'judgement.pain.counts.videoMemory') > 0
    meta['machine']['vramMiB'] = None
    missing = summary_for(Path('fixture'), meta, data, dictionary, budgets, summary['run'])[0]
    assert next(v for v in missing['judgement']['requiredChecks'] if v['key'] == 'videoMemory')['status'] == 'rerun'


def check_budget_consumers(root, dictionary, budgets):
    """Regression：同个完整夹具批次，距离表与 CLI 消费同一分组档位。"""
    from perf import distance
    seed = measured(dictionary, budgets, [frame(dictionary, 0)], dict(suite='fixed'))[0]
    ruler = next(v for v in seed['budgets'] if v['stop'] is not None)
    metric, upper, stop = ruler['metric'], ruler['budget'], ruler['stop']
    from perf_batches import confirmation_reasons
    for value, picked, expected in ((stop, False, False), (stop, True, True), (upper, False, True)):
        grouped = dict(distanceTable=[dict(metric=metric, value=value, upper=upper, stop=stop, selected=picked)],
                       pain=dict(counts={}, lightFrames=0))
        assert bool(confirmation_reasons(grouped)) is expected, (value, picked)
    for suite in (None, 'unknown', 'travel'):
        other = measured(dictionary, budgets, [frame(dictionary, 0)], dict(suite=suite))[0]
        assert not any(v['applicable'] for v in other['budgets'])
    assert not any(v['unjudged'] for v in seed['budgets']), '无非主视口的会话凭空产生合计未判'
    for value, mismatch, expected in ((stop, False, 'within'), (upper + 1, False, 'above-upper'),
                                      (upper, False, 'above-stop'), (None, False, 'rerun'),
                                      (upper, True, 'rerun')):
        with tempfile.TemporaryDirectory(prefix='uestc-perf-budget-selfcheck-') as temporary:
            fixture = Path(temporary)
            out, batch_id, scene = fixture / 'output/perf', 'selfcheck-only', 'lake-default'
            out.mkdir(parents=True)
            entries = []
            identifier = 'fixture-single'
            meta = manifest(dictionary, identifier)
            meta.update(origin='ai', desktop=dict(kind='isolated', isInputDesktop=False, windowFocused=False))
            meta['build']['commit'] = git(root, 'rev-parse', 'HEAD')[1]
            if mismatch:
                meta['graphics']['preset'] = 'selfcheck-wrong-preset'
            run = ai_run(scene=scene, round_id=batch_id)
            run['conditions']['tuningSelectedMetrics'] = [scene + '.' + metric]
            data = mark_sample(streams([frame(dictionary, i, gpu=value) for i in range(3)], dictionary), dictionary, meta)
            entries.append(summary_for(fixture / identifier, meta, data, dictionary, budgets, run)[0])
            batches = batch_results(entries, budgets, rounds=[round_registration(fixture, [scene], batch_id)])
            rebuild_states(entries, root)
            batch_states(batches, entries, root)
            batch = batches[0]
            table = batch['groups'][0]['distanceTable']
            selected = next(v for v in table if v['metric'] == metric)
            assert selected['tier'] == expected, selected
            (out / 'index.json').write_text(json.dumps(dict(calc=seed['calc'], batches=batches, sessions=entries, errors=[])), encoding='utf-8')
            cli = list(distance(out, Namespace(id=batch_id, scene=None)))
            assert any(line.split(' | ')[1:2] == [metric] and line.endswith(' | ' + expected) for line in cli)
