"""验证判停必判缺测、绝对成本线、原始帧计次、等待单边判与散布边界。
用法：由 perf.py selfcheck 编排；只用小样本，不启动游戏、不产生性能基线。
"""
import copy
from pathlib import Path
from analyze import summary_for
from perf_anomalies import merge_anomalies
from perf_batches import batch_results
from perf_model import at
from perf_waits import visible_intervals, wait_observations
from selfcheck_fixtures import ai_run, frame, manifest, mark_sample, streams


def measured(dictionary, budgets, rows, conditions=None, events=None):
    meta = manifest(dictionary)
    meta['desktop'] = dict(kind='isolated', isInputDesktop=False, windowFocused=False)
    meta['machine']['vramMiB'] = 16000
    data = streams(rows, dictionary)
    data['events'] += events or []
    run = dict(conditions=conditions or {})
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


def check_waits(root, dictionary, budgets):
    """Regression：整场事件不随样段裁剪，真实展示增长才重置停滞，隔离超限只未判。"""
    def event(name, when, **payload):
        return dict(schema=dictionary['schema'], sequence=int(when) + 1, event=name, source='selfcheck',
                    monotonicMs=when, collectionFrame=None, payload=payload)
    events = [event('loading.state', 0, active=True, covered=True, progress=0),
              event('loading.state', 1000, active=True, covered=True, progress=.1),
              event('map.arrival', 15000, phase='progress', completed=99, total=100),
              event('loading.state', 23000, active=False, covered=False, progress=.1)]
    intervals = visible_intervals(events)
    assert intervals[0]['longestNoProgressMs'] == 22000, 'arrival或通知代替了真实可见增长'
    meta = manifest(dictionary)
    meta['clock']['anchors'] = [dict(unixMs=946684800000, monotonicMs=0, readSpanMs=0)]
    entry = event('app.phase', 6000, phase='title-input-ready')
    run = dict(conditions=dict(startupCache='warm'), launcher=dict(scriptEnteredUtc='2000-01-01T00:00:00Z', basis='launcher-script-entry'))
    play = wait_observations(meta, events + [entry], run, budgets)
    assert next(v for v in play['items'] if v['key'] == 'warmLauncherToTitleSecondsMax')['status'] == 'exceeded'
    meta['desktop'] = dict(kind='isolated', isInputDesktop=False)
    isolated = wait_observations(meta, events + [entry], run, budgets)
    value = next(v for v in isolated['items'] if v['key'] == 'warmLauncherToTitleSecondsMax')
    assert value['seconds'] == 6 and value['status'] == 'unjudged' and value['pain'] is None
    data = mark_sample(streams([frame(dictionary, i) for i in range(5)], dictionary), dictionary, begin=20, end=50)
    data['events'] += [entry]
    ai = ai_run()
    ai.update(run)
    meta['origin'] = 'ai'
    result = summary_for(Path('fixture'), meta, data, dictionary, budgets, ai)[0]
    assert next(v for v in result['waits']['items'] if v['key'] == 'warmLauncherToTitleSecondsMax')['seconds'] == 6


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
    for check in (check_judgement, check_raw_counts, check_waits, check_aba_ruler):
        check(root, dictionary, budgets)
