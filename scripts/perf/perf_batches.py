"""由唯一轮登记选择完整单次，生成确认、较差值与相邻轮比较。
用法：由 analyze.py 与 perf.py record 调用；输入 rounds.json 和会话摘要，产出 index.batches。
"""
import copy
import json
from collections import defaultdict
from pathlib import Path
from perf_model import COSTS, at, number, read_json
from perf_anomalies import budget_checks

ROUND_RULES = read_json(Path(__file__).with_name('derived-fields.json'))['roundComparison']


def group_key(entry):
    return json.dumps([entry['comparisonKey'], entry['calc'], entry['origin'], entry['scope']], sort_keys=True)


def validate_rounds(rounds):
    by_id, numbers = {}, set()
    for row in sorted(rounds, key=lambda row: row['number']):
        identifier, n, scenes = row['id'], row['number'], row['scenes']
        if not isinstance(identifier, str) or not identifier or identifier in by_id:
            raise ValueError('轮 ID 缺失或重复')
        if type(n) is not int or n < 0 or n in numbers:
            raise ValueError('轮序号缺失、重复或为负数')
        if (not isinstance(scenes, list) or not scenes or len(set(scenes)) != len(scenes)
                or any(not isinstance(scene, str) or not scene for scene in scenes)):
            raise ValueError('轮场景计划缺失或重复')
        if not isinstance(row.get('sourceRoot'), str) or not Path(row['sourceRoot']).is_absolute():
            raise ValueError('轮原始根必须是绝对路径')
        previous = row.get('previousId')
        if n == 0 and previous is not None or n > 0 and (previous not in by_id or by_id[previous]['number'] != n - 1):
            raise ValueError('前轮必须显式指向 N-1；第0轮无前轮')
        by_id[identifier], numbers = row, numbers | {n}
    return rounds


def read_rounds(out):
    path = Path(out) / 'rounds.json'
    return validate_rounds(read_json(path)['rounds']) if path.is_file() else []


def register_round(out, identifier, number_, source_root, scenes, previous_id=None):
    from perf_store import atomic_json, exclusive
    source = Path(source_root).resolve()
    if not source.is_dir():
        raise ValueError('轮原始根不存在')
    row = dict(id=identifier, number=number_, sourceRoot=str(source), scenes=list(scenes), previousId=previous_id)
    with exclusive(Path(out) / '.lock', timeout=30, create=True) as held:
        if not held:
            raise ValueError('分析锁被占用')
        rounds = read_rounds(out)
        prior = next((item for item in rounds if item['id'] == identifier), None)
        if prior is not None and prior != row:
            raise ValueError('已登记轮的身份、原始根或场景计划不能改写')
        if prior is None:
            rounds.append(row)
        validate_rounds(rounds)
        atomic_json(Path(out) / 'rounds.json', dict(rounds=sorted(rounds, key=lambda item: item['number'])))
    return row


def sample_window(manifest, streams, run):
    """边界只认成功入原始事件流的标记，不接受请求时长或入口猜测。"""
    if manifest.get('origin') != 'ai' or run.get('purpose') not in ('baseline', 'functional'):
        return None, []
    if not isinstance(run.get('sampleSegmentId'), str) or not run['sampleSegmentId'].strip():
        return None, ['正式采样段 ID 缺失']
    reasons = []
    marks = [e for e in streams['events'] if e['event'] == 'segment'
             and e['payload'].get('id') == run.get('sampleSegmentId')]
    ordered = sorted(marks, key=lambda e: e['sequence'])
    if len(ordered) != 2 or [e['payload'].get('phase') for e in ordered] != ['begin', 'end']:
        reasons.append('正式采样 Begin/End 缺失、重复、乱序或重叠')
    elif ordered[0]['monotonicMs'] >= ordered[1]['monotonicMs']:
        reasons.append('正式采样边界时间不递增')
    if reasons:
        return None, reasons
    begin, end = ordered
    conditions = manifest.get('samplingConditions') or {}
    if (manifest.get('conditionsMissingReason') is not None
            or conditions.get('segmentId') != run['sampleSegmentId']
            or conditions.get('collectionFrame') != begin.get('collectionFrame')
            or conditions.get('monotonicMs') != begin['monotonicMs']
            or type(conditions.get('renderDrawFrame')) is not int):
        return None, [manifest.get('conditionsMissingReason') or '正式采样条件未在对应 Begin 冻结']
    window = dict(segmentId=run['sampleSegmentId'], fromMs=begin['monotonicMs'], toMs=end['monotonicMs'],
                  beginSequence=begin['sequence'], endSequence=end['sequence'])
    return window, []


def sampled_streams(streams, window):
    """准备期可用于上下文归属；所有指标/异常/M/派生只消费本投影。"""
    start, end = window['fromMs'], window['toMs']
    selected = {name: [row for row in rows if start < row['monotonicMs'] <= end]
                for name, rows in streams.items()}
    # 帧行含上一帧区间，跨 Begin 的行无法拆成本，不能比例分摊或借准备期。
    selected['frames'] = [row for row in selected['frames']
                          if number(row.get('dtMs')) and row['monotonicMs'] - row['dtMs'] >= start]
    return selected



def latest_key(row):
    return tuple(str(row.get(key) or '') for key in ('endedAt', 'startedAt', 'id'))


def base_valid(row):
    return row.get('origin') == 'ai' and row.get('baseStatus') == 'candidate'


def apply_confirmation(group, main, confirm):
    """Keep each chosen distribution intact, including its actual n, and name its source session."""
    sources = {}
    for section in ('all', 'steady'):
        for metric in COSTS:
            path = section + '.cost.' + metric
            a, b = at(main, path + '.p95'), at(confirm, path + '.p95')
            chosen = confirm if number(a) and number(b) and b > a else main
            group[section]['cost'][metric] = copy.deepcopy(at(chosen, path))
            sources[path] = dict(sessionId=chosen['id'], statistic='p95', complete=number(a) and number(b))
    # Whole-card memory is judged by its observed peak; retain that session's complete distribution.
    path = 'all.cost.hardware.wholeGpuMemoryMiB'
    a, b = at(main, path + '.maximum'), at(confirm, path + '.maximum')
    chosen = confirm if number(a) and number(b) and b > a else main
    group['all']['cost']['hardware']['wholeGpuMemoryMiB'] = copy.deepcopy(at(chosen, path))
    sources[path] = dict(sessionId=chosen['id'], statistic='maximum', complete=number(a) and number(b))
    group['metricSources'] = sources


def distance_table(runs, value):
    return [dict(scene=runs[0]['comparisonKey']['scene'], metric=check['metric'], value=check['value'],
                 upper=check['budget'], stop=check['stop'], ratio=check['ratio'], tier=check['tier'],
                 selected=check['selected'], exceeded=check['exceeded'], source=check['budgetSource'], stopSource=check['stopSource'], reason=check['reason'])
            for check in budget_checks(value, runs=runs) if check['required']]


def scene_pain(rows):
    """同场景主采与确认逐项取较差；次数和轻级率不相加。"""
    facts = [at(row, 'judgement.pain', {}) or {} for row in rows]
    counts = {key: max(fact.get('counts', {}).get(key, 0) for fact in facts)
              for key in set().union(*(fact.get('counts', {}) for fact in facts))}
    rated = [(row, fact.get('lightFrames', 0) * 60000 / fact['steadyDurationMs'])
             for row, fact in zip(rows, facts) if fact.get('steadyDurationMs', 0) > 0]
    chosen, rate = max(rated, key=lambda item: item[1]) if rated and len(rated) == len(facts) else (None, None)
    source = (dict(sessionId=chosen['id'], lightFrames=at(chosen, 'judgement.pain.lightFrames'),
                   steadyDurationMs=at(chosen, 'judgement.pain.steadyDurationMs')) if chosen else None)
    return dict(counts=counts, lightFrames=max((fact.get('lightFrames', 0) for fact in facts), default=0),
                lightFramesPerMinute=rate, lightSource=source,
                severityCounts={key: max(fact.get('severityCounts', {}).get(key, 0) for fact in facts) for key in ('heavy', 'severe')})


def confirmation_reasons(group):
    fraction = ROUND_RULES['minimumImprovementFraction']
    reasons = []
    for check in group['distanceTable']:
        for line in ('upper', 'stop'):
            value, boundary = check['value'], check[line]
            if number(value) and number(boundary) and boundary > 0 and abs(value - boundary) / boundary < fraction:
                reasons.append(check['metric'] + ' 接近' + line)
    pain = group['pain']
    reasons += ['痛点仅一次: ' + key for key, count in pain['counts'].items() if count == 1]
    if pain['lightFrames'] == 1:
        reasons.append('轻级痛点仅一次')
    return reasons


def batch_pain(rows, budgets):
    """路线/旅行按场景较差结果汇总；确认不增加样本总时长或痛点数。"""
    grouped = defaultdict(list)
    for row in rows:
        if at(row, 'run.conditions.suite') in ('route', 'travel'):
            grouped[row['comparisonKey']['scene']].append(row)
    values = {scene: scene_pain(members) for scene, members in grouped.items()}
    counts = {key: sum(value['counts'].get(key, 0) for value in values.values())
              for key in set().union(*(value['counts'] for value in values.values()))}
    # Choose one real light-rate sample per scene, preserving its count and duration as a pair.
    light_sources = [dict(scene=scene, **value['lightSource']) for scene, value in values.items() if value['lightSource']]
    duration = sum(source['steadyDurationMs'] for source in light_sources)
    light_frames = sum(source['lightFrames'] for source in light_sources)
    missing_rate = any(value['lightSource'] is None and any(at(row, 'judgement.pain.steadyDurationMs', 0) > 0
                       for row in grouped[scene]) for scene, value in values.items())
    rate = light_frames * 60000 / duration if duration and not missing_rate else None
    limit = budgets['perfAnomalies']['lightCostStutterFramesPerMinuteMax']
    light_exceeded = rate > limit if rate is not None else None
    complete = bool(rows) and all(at(row, 'judgement.requiredChecks') and all(check['status'] == 'measured' for check in row['judgement']['requiredChecks']) for row in rows)
    return dict(counts=counts, lightFrames=light_frames, steadyDurationMs=duration, lightSources=light_sources, severityCounts={key: sum(value['severityCounts'][key] for value in values.values()) for key in ('heavy', 'severe')},
                lightFramesPerMinute=rate, lightLimit=limit, lightExceeded=light_exceeded,
                zeroTolerance=[dict(key=key, count=count, allowed=0) for key, count in counts.items()],
                worstScenes={key: max(values, key=lambda scene: values[scene]['counts'].get(key, 0)) for key in counts},
                fixedControl=[dict(sessionId=row['id'], pain=at(row, 'judgement.pain')) for row in rows if at(row, 'run.conditions.suite') == 'fixed'],
                status='rerun' if not complete or grouped and rate is None else 'pain' if any(counts.values()) or light_exceeded is True else 'clear')


def batch_results(entries, budgets, failed_sources=(), rounds=()):
    results = []
    for registration in validate_rounds(rounds):
        reasons, groups, selected = [], [], []
        source = Path(registration['sourceRoot']).resolve()
        available = [row for row in entries if Path(row['source']).resolve().is_relative_to(source)]
        for scene in registration['scenes']:
            candidates = [row for row in available if row['comparisonKey'].get('scene') == scene
                          and base_valid(row) and not at(row, 'run.confirmationOf')]
            if not candidates:
                reasons.append(scene + ': 无完整有效单次')
                continue
            main = max(candidates, key=latest_key)
            if main['source'] in failed_sources:
                reasons.append(scene + ': 最新完整单次来源分析失败，不晋升旧单次')
                continue
            confirmations = [row for row in available if at(row, 'run.confirmationOf') == main['id'] and base_valid(row)]
            group = {key: copy.deepcopy(main[key]) for key in ('comparisonKey', 'calc', 'origin', 'scope', 'environmentSignature', 'all', 'steady')}
            group.update(sessionIds=[main['id']], mainSessionId=main['id'], confirmationSessionId=None)
            group['distanceTable'] = distance_table([main], group)
            group['pain'] = scene_pain([main])
            group['confirmationReasons'] = confirmation_reasons(group)
            group['confirmationRequired'] = bool(group['confirmationReasons'])
            members = [main]
            if confirmations:
                confirm = max(confirmations, key=latest_key)
                frozen = ('suite', 'travelPrefetch', 'logicalPauseVerified', 'mainThreadLongStopVerified', 'tuningSelectedMetrics')
                if (len(confirmations) != 1 or confirm['source'] in failed_sources or group_key(confirm) != group_key(main) or confirm['build'] != main['build']
                        or confirm['environmentSignature'] != main['environmentSignature']
                        or any(at(confirm, 'run.conditions.' + key) != at(main, 'run.conditions.' + key) for key in frozen)):
                    reasons.append(scene + ': 确认次数或冻结条件不符')
                else:
                    members.append(confirm)
                    group['sessionIds'].append(confirm['id'])
                    group['confirmationSessionId'] = confirm['id']
                    apply_confirmation(group, main, confirm)
                    group['distanceTable'] = distance_table(members, group)
                    group['pain'] = scene_pain(members)
            group['confirmationPending'] = group['confirmationRequired'] and len(members) == 1
            group['requiredChecks'] = [dict(sessionId=row['id'], **check) for row in members for check in at(row, 'judgement.requiredChecks', [])]
            selected.extend(members)
            groups.append(group)
        if any(not isinstance(row.get('environmentSignature'), dict) or any(row['environmentSignature'].get(key) is None for key in ('machine.driver', 'machine.os', 'build.godot')) for row in selected):
            reasons.append('本轮驱动/系统/引擎环境缺失')
        if len({at(row, 'build.commit') for row in selected}) > 1 or len({json.dumps(row.get('environmentSignature'), sort_keys=True) for row in selected}) > 1:
            reasons.append('本轮构建或驱动/系统/引擎环境不一致')
        missing_checks = [check for group in groups for check in group['requiredChecks'] if check['status'] != 'measured']
        unjudged = sorted({check['key'] for check in missing_checks} |
                          {check['metric'] for row in selected for check in row.get('budgets', []) if check['unjudged']} |
                          {item['key'] for row in selected for item in at(row, 'waits.items', []) or [] if item['status'] == 'unjudged'} |
                          {kind for row in selected for detector in row.get('unavailableDetectors', []) for kind in detector['types']})
        result = dict(**registration, groups=groups, status='partial' if reasons else 'candidate', statusReasons=reasons,
                      requiredChecks=[check for group in groups for check in group['requiredChecks']],
                      unjudged=unjudged, pain=batch_pain(selected, budgets) if selected else None,
                      confirmationPending=any(group['confirmationPending'] for group in groups))
        result['gateStatus'] = ('Incomplete' if reasons or missing_checks or result['confirmationPending'] or at(result, 'pain.status') == 'rerun' else
                                'Failed' if any(check['tier'] == 'above-upper' for group in groups for check in group['distanceTable']) else 'Passed')
        results.append(result)
    assess_rounds(results)
    return results


def assess_rounds(rounds):
    by_id = {row['id']: row for row in rounds}
    fraction = ROUND_RULES['minimumImprovementFraction']
    complete_statuses = ('candidate', 'current', 'stale', 'superseded')
    for current in rounds:
        previous = by_id.get(current['previousId'])
        current['comparison'] = []
        unknown, regressions = [], []
        if previous:
            if any(check['status'] != 'measured' for item in (previous, current) for check in item['requiredChecks']):
                unknown.append('必判项目仍未核；不影响已测成本变化事实')
            if previous['confirmationPending'] or current['confirmationPending']:
                unknown.append('一次确认尚未完成')
            if current['status'] == 'stale':
                unknown.append('当前轮已过时，合入资格未判')
            before_pain, after_pain = previous.get('pain') or {}, current.get('pain') or {}
            if before_pain.get('status') == 'rerun' or after_pain.get('status') == 'rerun':
                unknown.append('整轮痛点完整性未核')
            for key, count in after_pain.get('counts', {}).items():
                if count and before_pain.get('counts', {}).get(key) == 0:
                    regressions.append('本轮新出痛点: ' + key)
            if after_pain.get('lightExceeded') is True and before_pain.get('lightExceeded') is False:
                regressions.append('本轮新出轻级痛点')
            for scene in current['groups']:
                before_scene = next((item for item in previous['groups'] if group_key(item) == group_key(scene)), None)
                if before_scene is None:
                    unknown.append(scene['comparisonKey']['scene'] + ': 相邻轮比较组缺失')
                    continue
                old = {row['metric']: row for row in before_scene['distanceTable']}
                for row in scene['distanceTable']:
                    prior = old.get(row['metric'], {})
                    if row['tier'] == 'rerun' or prior.get('tier') == 'rerun':
                        unknown.append(row['metric'] + ': 预算判据未核')
                    for line in ('upper', 'stop'):
                        if line == 'stop' and not row['selected']:
                            continue
                        if all(number(v) for v in (row['value'], row[line], prior.get('value'), prior.get(line))) and row['value'] > row[line] and prior['value'] <= prior[line]:
                            regressions.append(row['metric'] + ' 被推出' + line)
        for group in current['groups']:
            before = next((item for item in previous['groups'] if group_key(item) == group_key(group)), None) if previous else None
            comparisons, facts = [], []
            acceptance = list(unknown)
            guards = list(regressions)
            if previous and (previous['status'] not in complete_statuses or current['status'] not in complete_statuses or before is None):
                facts.append('相邻轮不完整或比较键/calc/scope/origin 不一致')
            if before:
                if before.get('environmentSignature') != group.get('environmentSignature'):
                    facts.append('相邻轮驱动/系统/引擎环境不一致')
                for metric in COSTS:
                    value = at(group, 'steady.cost.' + metric + '.p95')
                    old_value = at(before, 'steady.cost.' + metric + '.p95')
                    improvement = (old_value - value) / old_value if number(value) and number(old_value) and old_value > 0 else None
                    missing = [] if improvement is not None else ['前轮/本轮指标缺测或前轮分母非正']
                    measured = not facts and improvement is not None
                    change = ('improved' if improvement >= fraction else 'unchanged' if improvement > -fraction else 'regressed') if measured else 'unjudged'
                    metric_acceptance = list(acceptance)
                    if any(item.get('metricSources', {}).get('steady.cost.' + metric, {}).get('complete') is False for item in (before, group)):
                        metric_acceptance.append('该指标确认成本缺测，较差值未核')
                    comparisons.append(dict(scene=group['comparisonKey']['scene'], metric=metric, previous=old_value,
                        value=value, improvementFraction=improvement, change=change,
                        accepted=(improvement >= fraction and not guards) if measured and not metric_acceptance else None,
                        reasons=facts + missing + metric_acceptance + guards))
            group['comparison'] = comparisons
            current['comparison'].extend(comparisons)
