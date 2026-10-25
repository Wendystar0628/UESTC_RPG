"""从整场已有事件和启动/退出事实计算等待，不用采样段或成本代替墙钟。
用法：由 analyze.py 在裁剪正式样段前调用 wait_observations；返回实测秒数、数值线及缺测原因。
"""
from datetime import datetime
from perf_model import at, number


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


def observation(key, start, end, budgets, source, reason=None):
    policy = budgets['waits']
    limit = policy[key]
    seconds = (end - start) / 1000 if number(start) and number(end) and end >= start else None
    if reason is None and seconds is None:
        reason = '等待起点/终点缺测' if not number(start) or not number(end) else '等待端点次序无效'
    return dict(key=key, fromMs=start, toMs=end, seconds=seconds, upperSeconds=limit,
                source=source, budgetSource='budgets.json.waits.' + key, reason=reason)



def entry_observations(events, budgets):
    result = []
    starts = [(index, event) for index, event in enumerate(events)
              if event['event'] == 'game.enter' and event['payload'].get('phase') == 'started']
    phases = [e for e in events if e['event'] == 'app.phase']
    for position, (index, begin) in enumerate(starts):
        stop = starts[position + 1][0] if position + 1 < len(starts) else len(events)
        following = events[index + 1:stop]
        identity = (begin['payload'].get('owner'), begin['payload'].get('operation'))
        completed = any(e['event'] == 'game.enter' and e['payload'].get('phase') == 'completed'
                        and (e['payload'].get('owner'), e['payload'].get('operation')) == identity for e in following)
        start = begin['monotonicMs']
        kind = str(at(begin, 'payload.target.kind', '')).lower()
        key = {'continue': 'continueToWalkSecondsMax', 'loadslot': 'loadSlotToWalkSecondsMax'}.get(kind)
        if kind == 'newgame':
            preloaded = any(p['monotonicMs'] <= start and p['payload'].get('phase') == 'preload-ready' for p in phases)
            key = 'preloadedNewGameToWalkSecondsMax' if preloaded else 'immediateNewGameToWalkSecondsMax'
        end = next((e['monotonicMs'] for e in following if e['event'] == 'app.phase'
                    and e['payload'].get('phase') == 'input-ready'), None) if completed else None
        if key:
            reason = None if end is not None else '本次进场命令的真实就绪终点缺测'
            result.append(observation(key, start, end, budgets, 'game.enter started → app.phase input-ready', reason))
    return result


def wait_observations(manifest, events, run, budgets):
    result = entry_observations(events, budgets)
    phases = {name: next((e['monotonicMs'] for e in events if e['event'] == 'app.phase' and e['payload'].get('phase') == name), None)
              for name in ('title-input-ready', 'input-ready')}
    launch = wall_monotonic(at(run, 'launcher.scriptEnteredUtc'), manifest)
    exit_time = wall_monotonic(at(run, 'launcher.processExitUtc'), manifest)
    # ENG-PERF-DATA: no session records the actual double-click start yet.
    reason = '未记录实际双击起点；这里只显示脚本入口到就绪的墙钟'
    for key, endpoint in ((WAIT_KEYS[0], 'title-input-ready'), (WAIT_KEYS[1], 'title-input-ready'),
                          (WAIT_KEYS[2], 'input-ready')):
        result.append(observation(key, launch, phases[endpoint], budgets, 'run.launcher → app.phase', reason))
    quitting = next((e['monotonicMs'] for e in events if e['event'] == 'app.state' and str(e['payload'].get('to')).lower() == 'quitting'), None)
    result.append(observation('exitSecondsMax', quitting, exit_time, budgets, 'app.state Quitting → native GetProcessTimes'))
    leaving = None
    for event in events:
        if event['event'] != 'app.state':
            continue
        state = str(event['payload'].get('to')).lower()
        if state == 'leaving':
            leaving = event['monotonicMs']
        elif state == 'title' and leaving is not None:
            result.append(observation('returnToTitleSecondsMax', leaving, event['monotonicMs'], budgets,
                                      'app.state Leaving → Title'))
            leaving = None
    for segment in at(run, 'observed.travelSegments', []) or []:
        key = 'prefetchedTravelToReadySecondsMax' if at(run, 'conditions.travelPrefetch') is True else 'directTravelToReadySecondsMax'
        reason = None if segment.get('completed') is True and isinstance(at(run, 'conditions.travelPrefetch'), bool) else '真实旅行请求/就绪或预取变体缺测'
        result.append({**observation(key, segment.get('fromMs'), segment.get('toMs'), budgets,
                                     'PerfCollectionDriver actual Travel request → arrival/walk/input ready', reason),
                       'segment': segment.get('index'), 'markerId': segment.get('markerId')})
    present = {item['key'] for item in result}
    for key in WAIT_KEYS:
        if key not in present:
            result.append(observation(key, None, None, budgets, None, '对应请求/真实就绪端点尚未采到'))
    return dict(items=result)
