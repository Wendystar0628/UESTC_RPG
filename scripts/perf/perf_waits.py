"""从整场已有事件和启动/退出事实计算等待，不用采样段或成本代替墙钟。
用法：由 analyze.py 在裁剪正式样段前调用 wait_observations；返回有来源与未判原因的等待项。
"""
from datetime import datetime
from perf_model import at, number, scope_of


WAIT_KEYS = ('warmLauncherToTitleSecondsMax', 'coldLauncherToTitleSecondsMax',
             'firstRunLauncherToRealtimeSecondsMax', 'preloadedNewGameToWalkSecondsMax',
             'immediateNewGameToWalkSecondsMax', 'continueToWalkSecondsMax', 'loadSlotToWalkSecondsMax',
             'prefetchedTravelToReadySecondsMax', 'directTravelToReadySecondsMax',
             'returnToTitleSecondsMax', 'exitSecondsMax')


def wall_monotonic(value, manifest):
    if not isinstance(value, str):
        return None
    try:
        wall = datetime.fromisoformat(value.replace('Z', '+00:00'))
        if wall.tzinfo is None:
            return None
        unix_ms = wall.timestamp() * 1000
    except (ValueError, OverflowError):
        return None
    anchors = at(manifest, 'clock.anchors', []) or []
    anchors = [a for a in anchors if number(a.get('unixMs')) and number(a.get('monotonicMs'))]
    if not anchors:
        return None
    anchor = min(anchors, key=lambda a: abs(a['unixMs'] - unix_ms))
    return anchor['monotonicMs'] + unix_ms - anchor['unixMs']


def observation(key, start, end, manifest, budgets, source, reason=None, visible_progress=False, stalled_ms=None):
    policy = budgets['waits']
    limit = policy[key]
    seconds = (end - start) / 1000 if number(start) and number(end) and end >= start else None
    isolated = scope_of(manifest)[0] == 'cost'
    status = 'unjudged'
    if seconds is not None and reason is None:
        status = 'observed-pass' if seconds <= limit else 'unjudged' if isolated else 'exceeded'
    pain = None if seconds is None or reason else (seconds > limit * policy['visibleProgressPainMultiple'] if visible_progress else False)
    if number(stalled_ms):
        pain = pain or stalled_ms > policy['noVisibleProgressSecondsMax'] * 1000
    # Isolated wall time is only an upper bound; exceeding it cannot establish a player pain.
    if isolated and (status != 'observed-pass' or pain):
        pain = None
    return dict(key=key, fromMs=start, toMs=end, seconds=seconds, upperSeconds=limit,
                source=source, budgetSource='budgets.json.waits.' + key, status=status, pain=pain,
                visibleProgress=visible_progress, longestNoProgressMs=stalled_ms,
                reason=reason or ('隔离桌面超过等待上限，只记未判' if isolated and status == 'unjudged' else None))


def visible_intervals(events):
    result, opened, progress, last_advance, longest, advanced = [], None, None, None, 0, False
    for event in events:
        if event['event'] != 'loading.state':
            continue
        payload, now = event['payload'], event['monotonicMs']
        visible = payload.get('active') is True and payload.get('covered') is True
        if visible and opened is None:
            opened, last_advance, longest, advanced = now, now, 0, False
            progress = payload.get('progress')
        elif opened is not None:
            longest = max(longest, now - last_advance)
            value = payload.get('progress')
            if visible and number(value) and number(progress) and value > progress:
                last_advance, advanced = now, True
            if number(value):
                progress = value
            if not visible:
                result.append(dict(fromMs=opened, toMs=now, longestNoProgressMs=longest,
                                   advanced=advanced, progressKnown=number(progress), complete=True))
                opened = None
    if opened is not None and events:
        result.append(dict(fromMs=opened, toMs=events[-1]['monotonicMs'],
                           longestNoProgressMs=max(longest, events[-1]['monotonicMs'] - last_advance),
                           advanced=advanced, progressKnown=number(progress), complete=False))
    return result


def entry_observations(events, manifest, run, budgets):
    result, starts = [], {}
    phases = [e for e in events if e['event'] == 'app.phase']
    for event in events:
        if event['event'] != 'game.enter':
            continue
        payload, now = event['payload'], event['monotonicMs']
        identity = (payload.get('owner'), payload.get('operation'))
        if payload.get('phase') == 'started':
            starts[identity] = event
        elif payload.get('phase') == 'completed' and identity in starts:
            begin = starts.pop(identity)
            start = begin['monotonicMs']
            kind = str(at(begin, 'payload.target.kind', '')).lower()
            key = {'continue': 'continueToWalkSecondsMax', 'loadslot': 'loadSlotToWalkSecondsMax'}.get(kind)
            if kind == 'newgame':
                preloaded = any(p['monotonicMs'] <= start and p['payload'].get('phase') == 'preload-ready' for p in phases)
                key = 'preloadedNewGameToWalkSecondsMax' if preloaded else 'immediateNewGameToWalkSecondsMax'
            end = next((p['monotonicMs'] for p in phases if start <= p['monotonicMs'] <= now and p['payload'].get('phase') == 'input-ready'), None)
            if key:
                result.append(observation(key, start, end, manifest, budgets, 'game.enter started → app.phase input-ready'))
    return result


def wait_observations(manifest, events, run, budgets):
    result = entry_observations(events, manifest, run, budgets)
    phases = {name: next((e['monotonicMs'] for e in events if e['event'] == 'app.phase' and e['payload'].get('phase') == name), None)
              for name in ('title-input-ready', 'input-ready')}
    launch = wall_monotonic(at(run, 'launcher.scriptEnteredUtc'), manifest)
    exit_time = wall_monotonic(at(run, 'launcher.processExitUtc'), manifest)
    condition = at(run, 'conditions.startupCache')
    for state, key, endpoint in (('warm', WAIT_KEYS[0], 'title-input-ready'), ('cold', WAIT_KEYS[1], 'title-input-ready'),
                                 ('first-run', WAIT_KEYS[2], 'input-ready')):
        reason = None if condition == state else '启动缓存条件未确认或不属本次条件'
        if at(run, 'launcher.basis') != 'launcher-script-entry':
            reason = 'prepared-attempt起点不等于完整启动脚本入口，该启动目标未判'
        if state != 'warm' and scope_of(manifest)[0] != 'full':
            reason = '冷启动与驱动管线缓存为空只看作者会话'
        result.append(observation(key, launch, phases[endpoint], manifest, budgets, 'run.launcher → app.phase', reason))
    quitting = next((e['monotonicMs'] for e in events if e['event'] == 'app.state' and str(e['payload'].get('to')).lower() == 'quitting'), None)
    result.append(observation('exitSecondsMax', quitting, exit_time, manifest, budgets, 'app.state Quitting → native GetProcessTimes'))
    leaving = None
    for event in events:
        if event['event'] != 'app.state':
            continue
        state = str(event['payload'].get('to')).lower()
        if state == 'leaving':
            leaving = event['monotonicMs']
        elif state == 'title' and leaving is not None:
            result.append(observation('returnToTitleSecondsMax', leaving, event['monotonicMs'], manifest, budgets,
                                      'app.state Leaving → Title'))
            leaving = None
    for segment in at(run, 'observed.travelSegments', []) or []:
        key = 'prefetchedTravelToReadySecondsMax' if at(run, 'conditions.travelPrefetch') is True else 'directTravelToReadySecondsMax'
        reason = None if segment.get('completed') is True and isinstance(at(run, 'conditions.travelPrefetch'), bool) else '真实旅行请求/就绪或预取变体缺测'
        result.append({**observation(key, segment.get('fromMs'), segment.get('toMs'), manifest, budgets,
                                     'PerfCollectionDriver actual Travel request → arrival/walk/input ready', reason),
                       'segment': segment.get('index'), 'markerId': segment.get('markerId')})
    intervals = visible_intervals(events)
    for interval in intervals:
        interval['status'] = 'measured' if interval['complete'] and interval['progressKnown'] else 'unjudged'
        interval['pain'] = (interval['longestNoProgressMs'] > budgets['waits']['noVisibleProgressSecondsMax'] * 1000
                            if interval['status'] == 'measured' and scope_of(manifest)[0] == 'full' else None)
    for index, item in enumerate(result):
        if not number(item['fromMs']) or not number(item['toMs']):
            continue
        related = [v for v in intervals if item['fromMs'] <= v['fromMs'] and v['toMs'] <= item['toMs'] and v['status'] == 'measured']
        if related:
            result[index] = {**observation(item['key'], item['fromMs'], item['toMs'], manifest, budgets, item['source'], item['reason'],
                                           any(v['advanced'] for v in related), max(v['longestNoProgressMs'] for v in related)),
                             **{k: item[k] for k in ('segment', 'markerId') if k in item}}
    present = {item['key'] for item in result}
    for key in WAIT_KEYS:
        if key not in present:
            result.append(observation(key, None, None, manifest, budgets, None, '对应请求/真实就绪端点尚未采到'))
    return dict(items=result, visibleIntervals=intervals,
                noProgressSource='budgets.json.waits.noVisibleProgressSecondsMax',
                painMultipleSource='budgets.json.waits.visibleProgressPainMultiple')
