"""按 ENG-PERF-DATA 判性能异常、关联阶段与候选原因，不推断责任函数。
用法：由 analyze.py 导入；输入已准备的帧、事件、清单和预算，产出异常、窗口和 deepen 条目。
"""
import math
import statistics
import bisect
from collections import Counter, defaultdict, deque
from perf_model import COSTS, LEVEL, PHASES, at, distribution, is_render_adapter, number, quantile, target_ms

MERGE_FRAME_MS = 250
MERGE_STATE_MS = 3000
MAX_PER_TYPE = 20
DEEPEN_REPEAT_COUNT = 3
LEVELS = {key: rank for rank, key in enumerate(LEVEL)}


def floor_of(metric, budgets, manifest):
    spec = budgets['perfAnomalies']['costFloors'].get(metric)
    if not spec:
        return None, None
    path = spec.get('ref') or spec['manifestRef']
    value = at(budgets if 'ref' in spec else manifest, path)
    return value if number(value) else None, ('budgets.json.' if 'ref' in spec else 'manifest.') + path


def frame_delta(frame, previous, key):
    if previous is None or frame['_gap'] or not number(frame.get(key)) or not number(previous.get(key)):
        return None
    value = frame[key] - previous[key]
    return value if value >= 0 else None


def annotate(frames, events, medians, policy):
    radius = policy['associationFrames']
    event_times = [event['monotonicMs'] for event in events]
    for i, frame in enumerate(frames):
        deltas = {key: frame_delta(frame, frames[i - 1] if i else None, key) for key in
                  ('pipelineCanvas', 'pipelineMesh', 'pipelineSurface', 'pipelineDraw', 'pipelineSpecialization', 'jitCompiledMethods')}
        frame['_compileDeltas'] = deltas
        prior = medians[i]
        extras = {key: max(0, frame[key] - prior[key]) for key in PHASES
                  if frame['_phaseValid'] and number(prior.get(key))}
        frame['_phase'] = max(extras, key=extras.get) if extras and max(extras.values()) > 0 else None
        neighborhood = frames[max(0, i - radius):min(len(frames), i + radius + 1)]
        start = neighborhood[0]['monotonicMs'] - (neighborhood[0].get('dtMs') or 0)
        end = neighborhood[-1]['monotonicMs']
        nearby_events = events[bisect.bisect_left(event_times, start):bisect.bisect_right(event_times, end)]
        frame['_nearEvents'] = [e for e in nearby_events
                                if e['event'] not in ('segment', 'clock.anchor', 'session.start', 'session.end')]


def candidates(frames, index, policy):
    radius = policy['associationFrames']
    nearby = frames[max(0, index - radius):min(len(frames), index + radius + 1)]
    causes = []
    if any(any((v or 0) > policy['pipelineIncrement'] for v in f['_compileDeltas'].values()) for f in nearby):
        causes.append('编译增量（关联窗口）')
    if any((f.get('gcPauseMs') or 0) > 0 or (f.get('gcGen2') or 0) > 0 for f in nearby):
        causes.append('全进程 GC（关联窗口，不归到函数）')
    if any((f.get('physicsSteps') or 0) > 1 for f in nearby):
        causes.append('多物理步')
    for a, b in zip(nearby, nearby[1:]):
        if number(a.get('observerYawDeg')) and number(b.get('observerYawDeg')) and b['_dtValid']:
            angle = abs((b['observerYawDeg'] - a['observerYawDeg'] + 180) % 360 - 180)
            if angle * 1000 / b['dtMs'] > policy['turnDegreesPerSecond']:
                causes.append('转身')
                break
    causes += [e['event'] for e in frames[index]['_nearEvents']]
    return sorted(set(causes))


def anomaly(kind, level, frame, value, threshold, source, metric=None, state=False):
    return dict(type=kind, level=level, metric=metric, fromMs=frame['monotonicMs'],
                toMs=frame['monotonicMs'], durationMs=0, firstFrame=frame['collectionFrame'],
                lastFrame=frame['collectionFrame'], firstSequence=frame['sequence'], lastSequence=frame['sequence'],
                occurrences=1, value=value, threshold=threshold, thresholdSource=source,
                excessMs=max(0, value - threshold) if number(value) and number(threshold) else 0,
                phase=None if metric in COSTS[:2] and frame.get('_mainViewportApproximate') else frame.get('_phase'),
                candidates=[], bottleneck='不定', state=state,
                attributionAvailable=not (metric in COSTS[:2] and frame.get('_mainViewportApproximate')),
                sourceDrawFrames={key: view['sourceDrawFrame'] for key, view in frame.get('_viewports', {}).items()},
                note=('主视口近似对齐（±2 帧）的成本样本；不作逐帧尖峰归因'
                      if metric in COSTS[:2] and frame.get('_mainViewportApproximate') else
                      '关联阶段是墙钟区间；渲染回读仅在关联窗口内定位'))


def cost_stutters(frames, index, median, policy, run, manifest):
    """成本绝对线不等暖窗；渲染回读只按关联窗口的遮挡事实分类。"""
    frame, result = frames[index], []
    for metric in COSTS:
        value = frame.get(metric)
        if not number(value):
            continue
        radius = policy['associationFrames'] if metric in COSTS[:2] else 0
        nearby = frames[max(0, index - radius):index + radius + 1]
        states = {f['_covered'] for f in nearby}
        visibility = 'visible' if states == {False} else 'covered' if states == {True} else 'candidate'
        if radius and len(nearby) != 2 * radius + 1:
            visibility = 'candidate'
        manual = (metric == 'frameSetupCpuMs' and frame['_capture'] and at(run, 'observed.save.reason') == 'Manual'
                  and any(e['event'] == 'sync.readback' and e['payload'].get('purpose') == 'saveThumbnail'
                          and e.get('collectionFrame') == frame['collectionFrame'] for e in frame['_nearEvents']))
        level, threshold = None, None
        if manual:
            threshold = policy['manualSaveThumbnailFrameMsMax']
            level = 'severe' if value > threshold else None
        else:
            for key, proposed in (('freezeMs', 'severe'), ('severeStutterMs', 'severe'), ('heavyStutterMs', 'heavy')):
                if value > policy[key]:
                    level, threshold = proposed, policy[key]
                    break
            baseline = median.get(metric)
            if level is None and frame['_steady'] and number(baseline):
                threshold = max(policy['lightCostStutterMs'], policy['lightStutterMedianMultiple'] * baseline)
                if value > threshold:
                    level = 'light'
        if level is not None:
            kind = 'manualSaveThumbnail' if manual else 'costStutter'
            item = anomaly(kind, level if visibility != 'candidate' else 'info', frame, value, threshold,
                           'budgets.json.perfAnomalies', metric)
            item.update(visibility=visibility, severity=level, frozen=value > policy['freezeMs'],
                        associationFromFrame=nearby[0]['collectionFrame'], associationToFrame=nearby[-1]['collectionFrame'])
            result.append(item)
    if frame['_phaseValid'] and at(run, 'conditions.logicalPauseVerified') is True:
        value = sum(frame[k] for k in PHASES[:3])
        for key, level in (('freezeMs', 'severe'), ('severeStutterMs', 'severe'), ('heavyStutterMs', 'heavy')):
            if value > policy[key]:
                result.append(anomaly('logicPause', level, frame, value, policy[key], 'budgets.json.perfAnomalies', 'logicWallMs'))
                break
    if (at(manifest, 'desktop.kind') == 'isolated' and frame['_dtValid']
            and at(run, 'conditions.mainThreadLongStopVerified') is True
            and frame['dtMs'] > policy['severeStutterMs']):
        result.append(anomaly('mainThreadLongStop', 'severe', frame, frame['dtMs'], policy['severeStutterMs'],
                              'budgets.json.perfAnomalies.severeStutterMs', 'dtMs'))
    return result


def frame_anomalies(frames, manifest, budgets, medians, scope, run):
    policy, target = budgets['perfAnomalies'], target_ms(manifest)
    result = []
    for i, frame in enumerate(frames):
        row = cost_stutters(frames, i, medians[i], policy, run, manifest)
        for metric in COSTS + tuple(frame.get('_viewportCpuFields', [])):
            floor, source = floor_of('mainViewportCpuMs' if metric.startswith('viewportCpuMs:') else metric, budgets, manifest)
            value, median = frame.get(metric), medians[i].get(metric)
            if frame['_steady'] and floor is not None and number(value) and number(median) and value > policy['costSpikeMedianMultiple'] * median and value >= floor:
                item = anomaly('costSpike', 'info', frame, value,
                               max(policy['costSpikeMedianMultiple'] * median, floor), source, metric)
                if metric in COSTS[:2] and frame['_mainViewportApproximate']:
                    radius = policy['associationFrames']
                    nearby = frames[max(0, i - radius):i + radius + 1]
                    item.update(fromMs=nearby[0]['monotonicMs'], toMs=nearby[-1]['monotonicMs'],
                                durationMs=nearby[-1]['monotonicMs'] - nearby[0]['monotonicMs'],
                                firstFrame=nearby[0]['collectionFrame'], lastFrame=nearby[-1]['collectionFrame'],
                                firstSequence=nearby[0]['sequence'], lastSequence=nearby[-1]['sequence'])
                row.append(item)
        pause = frame.get('gcPauseMs')
        if number(pause) and pause > policy['gcPauseMs']:
            row.append(anomaly('gcPause', 'light', frame, pause, policy['gcPauseMs'], 'budgets.json.perfAnomalies.gcPauseMs', 'gcPauseMs'))
        if frame.get('appState', '').lower() == 'playing':
            if (frame.get('gcGen2') or 0) > 0:
                row.append(anomaly('gen2', 'light', frame, frame['gcGen2'], 0, 'ENG-PERF-DATA', 'gcGen2'))
            for metric, delta in frame['_compileDeltas'].items():
                if delta is None or delta <= policy['pipelineIncrement']:
                    continue
                level = 'heavy' if metric == 'pipelineDraw' else 'info'
                if metric in ('pipelineCanvas', 'pipelineSurface') and frame['_covered'] is False:
                    level = 'light'
                row.append(anomaly('compilation', level, frame, delta, policy['pipelineIncrement'],
                                   'budgets.json.perfAnomalies.pipelineIncrement', metric))
        if scope == 'full' and frame['_dtValid']:
            value, median = frame['dtMs'], medians[i].get('dtMs')
            level = None
            for key, proposed in (('freezeMs', 'severe'), ('severeStutterMs', 'severe'), ('heavyStutterMs', 'heavy')):
                if value > policy[key]:
                    level, threshold = proposed, policy[key]
                    break
            if level is None and median is not None and target is not None:
                threshold = max(policy['lightStutterMedianMultiple'] * median, policy['lightStutterTargetMultiple'] * target)
                if value > threshold:
                    level = 'light'
            if level:
                kind = 'freeze' if value > policy['freezeMs'] else 'stutter'
                row.append(anomaly(kind, level, frame, value, threshold, 'budgets.json.perfAnomalies', 'dtMs'))
            if (frame.get('contextFlags') or 0) & 32 and value > policy['severeStutterMs']:
                row.append(anomaly('loadingStutter', 'heavy' if value > policy['freezeMs'] else 'light', frame,
                                   value, policy['severeStutterMs'], 'budgets.json.perfAnomalies', 'dtMs'))
            affected = level or (target is not None and value > policy['droppedFrameTargetMultiple'] * target)
            near = frames[max(0, i - policy['associationFrames']):i + policy['associationFrames'] + 1]
            gpu = [f['_measuredGpuMs'] for f in near]
            if affected and frame['_phaseValid'] and gpu and all(number(v) for v in gpu) and max(gpu) < policy['idleGpuFraction'] * value:
                row.append(anomaly('gpuIdleCandidate', 'info', frame, value, policy['idleGpuFraction'] * value,
                                   'budgets.json.perfAnomalies.idleGpuFraction', 'dtMs'))
        for item in row:
            if item['metric'] not in COSTS[:2] or not frame['_mainViewportApproximate']:
                item['candidates'] = candidates(frames, i, policy)
        if any(a['type'] in ('stutter', 'costSpike', 'costStutter', 'logicPause', 'mainThreadLongStop') and
               not (a['metric'] in COSTS[:2] and frame['_mainViewportApproximate']) for a in row):
            for item in row:
                if item['type'] == 'compilation' and item['metric'] in ('pipelineCanvas', 'pipelineSurface'):
                    item['level'] = 'heavy'
        result.extend(row)
    return result


def time_windows(frames, include_nonsteady=False):
    rows, elapsed = [], 0
    for frame in frames:
        valid = frame['_dtValid'] and (frame['_steady'] or include_nonsteady and frame.get('appState', '').lower() == 'playing')
        if not valid or (rows and frame['_scene'] != rows[-1]['_scene']):
            rows, elapsed = [], 0
        if not valid:
            continue
        rows.append(frame)
        elapsed += frame['dtMs']
        if elapsed >= 1000:
            yield rows
            rows, elapsed = [], 0


def window_analysis(frames, manifest, budgets, scope):
    policy, target = budgets['perfAnomalies'], target_ms(manifest)
    windows, result = [], []
    candidate, streak, established = None, 0, '不定'
    drop_streak = idle_streak = 0
    previous_end = None
    for rows in time_windows(frames):
        duration = sum(f['dtMs'] for f in rows)
        dt = statistics.median(f['dtMs'] for f in rows)
        gpu = quantile([f.get('mainViewportGpuMs') for f in rows], .5)
        first = rows[0]
        if previous_end is not None and first['sequence'] != previous_end + 1:
            candidate, streak, established = None, 0, '不定'
            drop_streak = idle_streak = 0
        previous_end = rows[-1]['sequence']
        current = '不定'
        if scope == 'full' and number(gpu):
            if target and abs(dt - target) <= policy['bottleneckToleranceFraction'] * target and gpu < policy['bottleneckGpuFraction'] * dt:
                current = '限帧'
            elif gpu >= policy['bottleneckGpuFraction'] * dt:
                current = 'GPU'
        streak = streak + 1 if candidate == current else 1
        candidate = current
        if streak >= policy['stateHysteresisWindows']:
            established = current
        window = dict(fromMs=first['monotonicMs'] - first['dtMs'], toMs=rows[-1]['monotonicMs'],
                      n=len(rows), durationMs=duration, averageFps=1000 * len(rows) / duration if scope == 'full' else None,
                      frameP50Ms=dt if scope == 'full' else None, mainGpuP50Ms=gpu,
                      bottleneck=established if scope == 'full' else None)
        if scope == 'full' and target:
            dropped = dt > policy['sustainedDroppedTargetMultiple'] * target
            drop_streak = drop_streak + 1 if dropped else 0
            if drop_streak >= policy['stateHysteresisWindows']:
                result.append(anomaly('sustainedDropped', 'heavy' if dt > policy['droppedFrameTargetMultiple'] * target else 'light',
                                      first, dt, policy['sustainedDroppedTargetMultiple'] * target, 'budgets.json.perfAnomalies', 'dtMs', True))
            if window['averageFps'] > policy['frameLimitFailureMultiple'] * 1000 / target:
                result.append(anomaly('frameLimitFailure', 'light', first, window['averageFps'],
                                      policy['frameLimitFailureMultiple'] * 1000 / target, 'budgets.json.perfAnomalies', 'averageFps', True))
            all_gpu = [f['_measuredGpuMs'] for f in rows]
            busy = sum(all_gpu) / duration if all(number(v) for v in all_gpu) else None
            idle = busy is not None and busy < policy['idleGpuFraction'] and window['averageFps'] < policy['idleFpsFraction'] * 1000 / target and established != '限帧'
            idle_streak = idle_streak + 1 if idle else 0
            if idle_streak >= policy['stateHysteresisWindows']:
                result.append(anomaly('sustainedIdleCandidate', 'info', first, busy, policy['idleGpuFraction'],
                                      'budgets.json.perfAnomalies', 'measuredGpuBusyFraction', True))
        for item in result:
            if item['firstSequence'] == first['sequence'] and item['state']:
                item.update(toMs=rows[-1]['monotonicMs'], durationMs=duration, lastFrame=rows[-1]['collectionFrame'], lastSequence=rows[-1]['sequence'])
        windows.append(window)
    for item in result:
        item['bottleneck'] = next((w['bottleneck'] for w in windows if w['fromMs'] <= item['fromMs'] <= w['toMs']), '不定')
    return windows, result + gc_windows(frames, policy)


def gc_windows(frames, policy):
    result, streak, previous_end = [], 0, None
    for rows in time_windows(frames, include_nonsteady=True):
        duration, first = sum(f['dtMs'] for f in rows), rows[0]
        if previous_end is not None and first['sequence'] != previous_end + 1:
            streak = 0
        previous_end = rows[-1]['sequence']
        pause = sum(f.get('gcPauseMs') or 0 for f in rows)
        allocation = sum(f.get('allocatedBytes') or 0 for f in rows) / 1048576 * 1000 / duration
        streak = streak + 1 if allocation > policy['allocationMiBPerSecond'] else 0
        specs = []
        if pause > policy['gcWindowPauseMs']:
            specs.append(('gcWindow', 'heavy', pause, policy['gcWindowPauseMs'], 'gcPauseMs'))
        if streak >= policy['allocationSustainedWindows']:
            specs.append(('allocationStorm', 'severe' if allocation > policy['allocationSevereMiBPerSecond'] else 'heavy',
                          allocation, policy['allocationMiBPerSecond'], 'allocatedMiBPerSecond'))
        for kind, level, value, threshold, metric in specs:
            item = anomaly(kind, level, first, value, threshold, 'budgets.json.perfAnomalies', metric, True)
            item.update(toMs=rows[-1]['monotonicMs'], durationMs=duration,
                        lastFrame=rows[-1]['collectionFrame'], lastSequence=rows[-1]['sequence'])
            result.append(item)
    return result


def budget_checks(summary, manifest=None, budgets=None, runs=None):
    """会话与阶段中位共用判据；消费者只筛 required/unjudged 并读取 tier。"""
    run = runs[0]['run'] if runs else summary.get('run', {})
    suite = at(run, 'conditions.suite')
    scene = run.get('scene')
    selected = at(run, 'conditions.tuningSelectedMetrics', []) or []
    if runs:
        candidates = [dict(check) for check in runs[0].get('budgets', [])]
        for check in candidates:
            members = [next((v for v in row.get('budgets', []) if v['metric'] == check['metric']), {}) for row in runs]
            matching = all(all(v.get(key) == check.get(key) for key in
                              ('budget', 'stop', 'budgetSource', 'stopSource', 'required', 'unjudged')) for v in members)
            check['applicable'] = matching and all(v.get('applicable') is True and number(v.get('value')) for v in members)
            if check['required'] and not check['applicable']:
                check['reason'] = ('阶段内预算或判据条件不一致，重跑' if not matching else
                                   '；'.join(sorted({v.get('reason') or '必判成本缺测，重跑' for v in members})))
    else:
        target = budgets['deviceTargets']['developer']
        graphics = manifest.get('graphics') or {}
        applicable = (target['gpuNameContains'].lower() in str(at(manifest, 'machine.gpu', '')).lower()
                      and graphics.get('preset') == target['preset']
                      and at(manifest, 'display.windowWidthPx') == target['widthPx']
                      and at(manifest, 'display.windowHeightPx') == target['heightPx']
                      and graphics.get('internalWidthPx') == target['widthPx'] * target['renderScale']
                      and graphics.get('internalHeightPx') == target['heightPx'] * target['renderScale'])
        keys = dict(mainViewportGpuMs=('mainViewportGpuP95MsUpper', 'mainViewportGpuP95MsStop'),
                    mainViewportCpuMs=('viewportCpuP95MsUpper', 'viewportCpuP95MsStop'),
                    frameSetupCpuMs=('frameSetupCpuP95MsUpper', None))
        # ENG-PERF-DATA: defer an incomplete aggregate when the session includes additional viewports.
        if (any(not key.startswith('main:') for key in at(summary, 'all.cost.viewports', {}))
                and at(summary, 'all.cost.measuredViewportGpuMs.p95') is None):
            keys['measuredViewportGpuMs'] = ('mainViewportGpuP95MsUpper', None)
        candidates = []
        for metric, (upper_key, stop_key) in keys.items():
            unjudged = metric == 'measuredViewportGpuMs'
            author = manifest.get('origin') == 'play'
            required = not unjudged and not author and suite in ('fixed', 'route')
            applies = required and applicable
            stop = target[stop_key] if stop_key and suite == 'fixed' else None
            reason = ('受测视口 GPU 合计来源尚未确认，未判' if unjudged else
                      '作者会话不判帧成本预算' if author else
                      '传送套不判帧成本' if suite == 'travel' else '场景套未确认，不适用帧成本预算' if not required else
                      None if applies else '会话条件与开发机预算不一致')
            candidates.append(dict(metric=metric, budget=target[upper_key], stop=stop, required=required,
                                   unjudged=unjudged, applicable=applies, reason=reason,
                                   budgetSource='budgets.json.deviceTargets.developer.' + upper_key,
                                   stopSource='budgets.json.deviceTargets.developer.' + stop_key if stop is not None else None))
    checks = []
    for check in candidates:
        metric, upper, stop = check['metric'], check['budget'], check['stop']
        measured = at(summary, f'steady.cost.{metric}.p95')
        picked = str(scene) + '.' + metric in selected
        judged = check['applicable'] and number(measured)
        if check['required'] and not number(measured) and not check['reason']:
            check['reason'] = '必判成本缺测，重跑'
        tier = ('unjudged' if check['unjudged'] else 'not-applicable' if not check['required'] else
                'rerun' if not judged else 'above-upper' if measured > upper else
                'above-stop' if picked and stop is not None and measured > stop else 'within')
        checks.append(dict(check, value=measured, observed=int(number(measured)), selected=picked, tier=tier,
                           ratio=measured / upper if number(measured) and upper else None,
                           exceeded=tier == 'above-upper' if judged else None))
    return checks


def slow_anomalies(frames, streams, manifest, budgets, scope):
    if not frames:
        return []
    policy, result = budgets['perfAnomalies'], []
    for sample in streams['hw']:
        if not is_render_adapter(sample):
            continue
        frame = nearest_frame(frames, sample['monotonicMs'])
        used, capacity = sample.get('wholeGpuMemoryMiB'), at(manifest, 'machine.vramMiB')
        if number(used) and number(capacity) and capacity > 0 and used > policy['videoMemoryNearFraction'] * capacity:
            result.append(anomaly('videoMemory', 'severe' if used > capacity else 'heavy', frame, used,
                                  policy['videoMemoryNearFraction'] * capacity, 'budgets.json.perfAnomalies.videoMemoryNearFraction', 'wholeGpuMemoryMiB', True))
        if (sample.get('throttleReasonBits') or 0) != 0:
            item = anomaly('clockThrottle', 'info', frame, sample['throttleReasonBits'], None, 'NVML raw reason mask', 'throttleReasonBits', True)
            item['note'] = '降频原始位仅作背景；不由低频或低占用判 CPU/GPU 责任'
            result.append(item)
    samples = streams['samples']
    if len(samples) >= 2:
        duration = (samples[-1]['monotonicMs'] - samples[0]['monotonicMs']) / 1000
        if duration >= policy['memoryGrowthMinimumSeconds']:
            for key in ('workingSetMiB', 'managedHeapMiB'):
                values = [s for s in samples if number(s.get(key))]
                if len(values) < 2:
                    continue
                elapsed = (values[-1]['monotonicMs'] - values[0]['monotonicMs']) / 60000
                slope = (values[-1][key] - values[0][key]) / elapsed if elapsed else None
                if slope is not None and slope > policy['memoryGrowthMiBPerMinute']:
                    item = anomaly('memoryGrowth', 'info', nearest_frame(frames, values[0]['monotonicMs']), slope,
                                   policy['memoryGrowthMiBPerMinute'], 'budgets.json.perfAnomalies.memoryGrowthMiBPerMinute', key, True)
                    item.update(toMs=values[-1]['monotonicMs'], durationMs=duration * 1000,
                                lastFrame=nearest_frame(frames, values[-1]['monotonicMs'])['collectionFrame'])
                    item['note'] = '首末增长候选，不声称内存泄漏；需诊断核查生命周期与驻留量'
                    result.append(item)
    if scope == 'full':
        utilization = [s for s in streams['hw'] if is_render_adapter(s) and number(s.get('gpuUtilFraction'))]
        for a, b in zip(utilization, utilization[1:]):
            delta = abs(b['gpuUtilFraction'] - a['gpuUtilFraction'])
            if delta > policy['utilizationSwingFraction']:
                result.append(anomaly('utilizationSwing', 'info', nearest_frame(frames, b['monotonicMs']), delta,
                                      policy['utilizationSwingFraction'], 'budgets.json.perfAnomalies.utilizationSwingFraction', 'gpuUtilFraction', True))
    return result


def nearest_frame(frames, milliseconds):
    times = [frame['monotonicMs'] for frame in frames]
    index = bisect.bisect_right(times, milliseconds) - 1
    return frames[max(0, index)]


def hang_anomalies(frames, events):
    if not frames:
        return []
    result = []
    for event in events:
        if event['event'] != 'hang.begin':
            continue
        payload = event['payload']
        duration = event['monotonicMs'] - payload['stalledSinceMs']
        item = anomaly('hang', 'severe', nearest_frame(frames, event['monotonicMs']), duration, None,
                       'manifest.kernelBudgets; hang.begin watchdog event', state=True)
        item.update(fromMs=payload['stalledSinceMs'], toMs=event['monotonicMs'], durationMs=duration,
                    phase=payload['boundary'], candidates=['hang.begin'])
        result.append(item)
    return result


def session_judgement(frames, streams, manifest, budgets, run, raw_items, checks):
    """合并前记录命中帧与测量完整性；预算越线不进入痛点计数。"""
    suite = at(run, 'conditions.suite')
    play = suite in ('route', 'travel')
    playing = [f for f in frames if f.get('appState', '').lower() == 'playing']
    visible = [f for f in playing if f['_covered'] is False]
    steady = [f for f in frames if f['_steady'] and f['_dtValid']]
    duration = sum(f['dtMs'] for f in steady)
    required = []

    def record(key, n, enabled=True, reason=None):
        complete = n > 0 and enabled
        required.append(dict(key=key, observed=n, status='measured' if complete else 'rerun',
                             reason=None if complete else reason or '必判字段缺测，重跑'))

    for check in checks:
        if not check['required']:
            continue
        record(check['metric'], check['observed'], check['tier'] != 'rerun', check.get('reason'))
    if manifest.get('origin') == 'ai' and isinstance(run.get('protocol'), dict) and suite not in ('fixed', 'route', 'travel'):
        record('suite', 0, False, '场景套未确认，重跑')
    if play:
        record('costStutter', len(visible), all(number(f.get(k)) for f in visible for k in COSTS))
        record('logicPause', sum(f['_phaseValid'] for f in playing),
               at(run, 'conditions.logicalPauseVerified') is True and all(f['_phaseValid'] for f in playing),
               '第0轮尚未核实逻辑三段或分段缺测，重跑')
        record('mainThreadLongStop', sum(f['_dtValid'] for f in playing),
               at(manifest, 'desktop.kind') == 'isolated' and at(run, 'conditions.mainThreadLongStopVerified') is True,
               '第0轮尚未核实静置长停或不是隔离桌面，重跑')
        record('drawCompilation', len(visible), all(number(f.get('pipelineDraw')) for f in visible))
        record('gc', len(playing), all(number(f.get(k)) for f in playing for k in ('gcPauseMs', 'gcGen2', 'allocatedBytes')))
        hardware = [h for h in streams['hw'] if is_render_adapter(h) and number(h.get('wholeGpuMemoryMiB'))]
        record('videoMemory', len(hardware), number(at(manifest, 'machine.vramMiB')) and at(manifest, 'machine.vramMiB') > 0,
               '真实渲染卡显存行或容量缺测，重跑')
        event_state = next((s for s in manifest.get('streams', []) if s['file'] == 'events.jsonl'), {})
        record('hang', int(event_state.get('state') == 'recorded'), manifest.get('complete') is True)
    categories = dict(costStutter='costStutter', logicPause='logicPause', mainThreadLongStop='mainThreadLongStop',
                      gcWindow='gc', allocationStorm='gc', videoMemory='videoMemory', hang='hang')
    hits, levels, light, candidates_count = defaultdict(set), defaultdict(set), set(), 0
    for item in raw_items:
        if item['type'] == 'costStutter' and item.get('visibility') == 'candidate':
            candidates_count += 1
        if item['type'] == 'costStutter' and item.get('visibility') != 'visible':
            continue
        if item['type'] == 'hang' and not any(f['sequence'] == item['firstSequence'] and
                                            f.get('appState', '').lower() == 'playing' for f in frames):
            continue
        if item['type'] == 'costStutter' and item['level'] == 'light':
            light.add(item['firstSequence'])
        category = ('drawCompilation' if item.get('metric') == 'pipelineDraw' else 'otherCompilation') if item['type'] == 'compilation' else categories.get(item['type'])
        if category and LEVELS[item['level']] >= LEVELS['heavy']:
            hits[category].add(item['firstSequence'])
            levels[item['level']].add(item['firstSequence'])
    counts = {key: len(hits[key]) for key in ('costStutter', 'logicPause', 'mainThreadLongStop', 'drawCompilation', 'otherCompilation', 'gc', 'videoMemory', 'hang')}
    policy = budgets['perfAnomalies']
    return dict(requiredChecks=required,
                pain=dict(suite=suite, scene=run.get('scene'), counts=counts, lightFrames=len(light),
                          steadyDurationMs=duration, visibilityCandidates=candidates_count,
                          severityCounts=dict(heavy=len(levels['heavy'] - levels['severe']), severe=len(levels['severe'])),
                          heavyOrWorseFrames=len(set().union(*hits.values())) if hits else 0,
                          status='rerun' if any(v['status'] == 'rerun' for v in required) else 'pain' if any(counts.values()) else 'clear'),
                lines=[dict(key='targetFrameMs', value=target_ms(manifest) if at(manifest, 'desktop.kind') == 'interactive' else None,
                            unit='ms', source='manifest.pacing/display', reason='体验目标仅交互桌面适用')] +
                      [dict(key=key, value=policy[key], unit='ms', source='budgets.json.perfAnomalies.' + key)
                       for key in ('heavyStutterMs', 'severeStutterMs', 'freezeMs', 'lightCostStutterMs', 'manualSaveThumbnailFrameMsMax')])


def merge_anomalies(items):
    grouped = defaultdict(list)
    for item in items:
        grouped[(item['type'], item.get('metric'), item['level'], item.get('phase'), item['state'], item['attributionAvailable'])].append(item)
    merged = []
    for group in grouped.values():
        group.sort(key=lambda a: a['fromMs'])
        current = None
        for item in group:
            gap = MERGE_STATE_MS if item['state'] else MERGE_FRAME_MS
            if current and item['fromMs'] - current['toMs'] < gap:
                current['toMs'] = max(current['toMs'], item['toMs'])
                current['durationMs'] = current['toMs'] - current['fromMs']
                current['lastFrame'] = item['lastFrame']
                current['lastSequence'] = item['lastSequence']
                current['occurrences'] += item['occurrences']
                current['excessMs'] += item['excessMs']
                current['value'] = max(current['value'], item['value']) if number(current['value']) and number(item['value']) else current['value']
                current['candidates'] = sorted(set(current['candidates'] + item['candidates']))
            else:
                current = dict(item)
                merged.append(current)
    return sorted(merged, key=lambda a: (a['type'] != 'dataValidity', -LEVELS[a['level']], -a['excessMs'], a['fromMs']))


def deepen(items, manifest):
    result = []
    unexplained = Counter((a['type'], a.get('metric'), a.get('phase')) for a in items
                          if a['attributionAvailable'] and not a['candidates']
                          and (LEVELS[a['level']] >= 2 or a['type'] == 'costSpike'))
    for item in items:
        key = (item['type'], item.get('metric'), item.get('phase'))
        if item['level'] != 'severe' and (not item['attributionAvailable'] or unexplained[key] < DEEPEN_REPEAT_COUNT):
            continue
        if any(r['type'] == item['type'] and r['metric'] == item.get('metric') and r['phase'] == item.get('phase') for r in result):
            continue
        phase = item.get('phase')
        tools = (['gpu-profile', 'Nsight'] if item.get('metric') == 'mainViewportGpuMs' else
                 ['gpu-profile'] if phase == 'drawMs' else ['EventPipe'])
        args = [str(a) for a in at(manifest, 'run.args', []) if not str(a).startswith('--perf-out=')]
        result.append(dict(type=item['type'], metric=item.get('metric'), phase=phase, tools=tools,
                           firstFrame=item['firstFrame'], lastFrame=item['lastFrame'],
                           scene=manifest.get('scene'), poseArgs=args,
                           reason='严重级' if item['level'] == 'severe' else '同会话反复且无事件原因',
                           command="powershell.exe -NoProfile -Command \"& 'scripts/perf/run.ps1' -DeepenSession '" + str(manifest['id']).replace("'", "''") +
                           "' -DeepenEntry " + str(len(result)) + " -DiagnosticTool " + ','.join("'" + tool + "'" for tool in tools) + '\"',
                           commandState='ready'))
    return result
