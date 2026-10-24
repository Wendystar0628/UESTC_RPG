"""按 ENG-PERF-DATA 判性能异常、关联阶段与候选原因，不推断责任函数。
用法：由 analyze.py 导入；输入已准备的帧、事件、清单和预算，产出异常、窗口和 deepen 条目。
"""
import math
import statistics
import bisect
from collections import Counter, defaultdict, deque
from perf_model import COSTS, PHASES, at, distribution, number, quantile, target_ms

MERGE_FRAME_MS = 250
MERGE_STATE_MS = 3000
MAX_PER_TYPE = 20
DEEPEN_REPEAT_COUNT = 3
LEVELS = {'info': 0, 'light': 1, 'heavy': 2, 'severe': 3}


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


def frame_anomalies(frames, manifest, budgets, medians, scope):
    policy, target = budgets['perfAnomalies'], target_ms(manifest)
    result = []
    for i, frame in enumerate(frames):
        row = []
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
                if metric in ('pipelineCanvas', 'pipelineSurface') and frame['_steady']:
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
        if any(a['type'] in ('stutter', 'costSpike') and
               not (a['metric'] in COSTS[:2] and frame['_mainViewportApproximate']) for a in row):
            for item in row:
                if item['type'] == 'compilation' and item['metric'] in ('pipelineCanvas', 'pipelineSurface'):
                    item['level'] = 'heavy'
        result.extend(row)
    return result


def time_windows(frames):
    rows, elapsed = [], 0
    for frame in frames:
        if not frame['_dtValid'] or not frame['_steady'] or (rows and frame['_scene'] != rows[-1]['_scene']):
            rows, elapsed = [], 0
        if not frame['_dtValid'] or not frame['_steady']:
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
    drop_streak = idle_streak = alloc_streak = 0
    previous_end = None
    for rows in time_windows(frames):
        duration = sum(f['dtMs'] for f in rows)
        dt = statistics.median(f['dtMs'] for f in rows)
        gpu = quantile([f.get('mainViewportGpuMs') for f in rows], .5)
        first = rows[0]
        if previous_end is not None and first['sequence'] != previous_end + 1:
            candidate, streak, established = None, 0, '不定'
            drop_streak = idle_streak = alloc_streak = 0
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
        gc = sum(f.get('gcPauseMs') or 0 for f in rows)
        if gc > policy['gcWindowPauseMs']:
            result.append(anomaly('gcWindow', 'heavy', first, gc, policy['gcWindowPauseMs'], 'budgets.json.perfAnomalies.gcWindowPauseMs', 'gcPauseMs', True))
        allocation = sum(f.get('allocatedBytes') or 0 for f in rows) / 1048576 * 1000 / duration
        alloc_streak = alloc_streak + 1 if allocation > policy['allocationMiBPerSecond'] else 0
        if alloc_streak >= policy['allocationSustainedWindows']:
            result.append(anomaly('allocationStorm', 'severe' if allocation > policy['allocationSevereMiBPerSecond'] else 'heavy', first,
                                  allocation, policy['allocationMiBPerSecond'], 'budgets.json.perfAnomalies', 'allocatedMiBPerSecond', True))
        for item in result:
            if item['firstSequence'] == first['sequence'] and item['state']:
                item.update(toMs=rows[-1]['monotonicMs'], durationMs=duration, lastFrame=rows[-1]['collectionFrame'], lastSequence=rows[-1]['sequence'])
        windows.append(window)
    for item in result:
        item['bottleneck'] = next((w['bottleneck'] for w in windows if w['fromMs'] <= item['fromMs'] <= w['toMs']), '不定')
    return windows, result


def budget_checks(summary, manifest, budgets):
    # Conditions identify the existing developer target; cost spike floors remain usable elsewhere.
    conditions = budgets['perfAnomalies']['budgetConditions']
    graphics = manifest.get('graphics') or {}
    applicable = (conditions['gpuNameContains'].lower() in str(at(manifest, 'machine.gpu', '')).lower()
                  and graphics.get('preset') == conditions['preset']
                  and at(manifest, 'display.windowWidthPx') == conditions['widthPx']
                  and at(manifest, 'display.windowHeightPx') == conditions['heightPx']
                  and graphics.get('internalWidthPx') == conditions['widthPx']
                  and graphics.get('internalHeightPx') == conditions['heightPx'])
    checks = []
    for metric in COSTS:
        floor, source = floor_of(metric, budgets, manifest)
        measured = at(summary, f'steady.cost.{metric}.p95')
        budget_applies = applicable and metric != 'frameSetupCpuMs'
        checks.append(dict(metric=metric, value=measured, budget=floor, budgetSource=source,
                           applicable=budget_applies, ratio=measured / floor if number(measured) and floor else None,
                           exceeded=measured > floor if budget_applies and number(measured) and floor else None,
                           reason=None if budget_applies else '会话条件与现有预算不一致；帧准备仅借用 CPU 底线'))
    return checks


def slow_anomalies(frames, streams, manifest, budgets, scope):
    if not frames:
        return []
    policy, result = budgets['perfAnomalies'], []
    for sample in streams['hw']:
        frame = nearest_frame(frames, sample['monotonicMs'])
        used, capacity = sample.get('wholeGpuMemoryMiB'), at(manifest, 'machine.vramMiB')
        if number(used) and number(capacity) and capacity > 0 and used >= policy['videoMemoryNearFraction'] * capacity:
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
            for key in ('privateMiB', 'managedHeapMiB'):
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
        utilization = [s for s in streams['hw'] if number(s.get('gpuUtilFraction'))]
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
        tools = ['EventPipe', 'Nsight'] if phase != 'drawMs' else ['gpu-profile', 'Nsight', 'EventPipe']
        args = [str(a) for a in at(manifest, 'run.args', []) if not str(a).startswith('--perf-out=')]
        result.append(dict(type=item['type'], metric=item.get('metric'), phase=phase, tools=tools,
                           firstFrame=item['firstFrame'], lastFrame=item['lastFrame'],
                           scene=manifest.get('scene'), poseArgs=args,
                           reason='严重级' if item['level'] == 'severe' else '同会话反复且无事件原因',
                           command='pwsh -NoProfile -File scripts/perf/run.ps1 -DeepenSession ' + "'" + str(manifest['id']).replace("'", "''") + "'",
                           commandState='requires TOOL-428/429 runner; preserve poseArgs and collection conditions'))
    return result
