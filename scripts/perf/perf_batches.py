"""确认 AI 正式采样边界与三次/A-B-A 批次完整性，集中产出场景成本中位。
用法：由 analyze.py 调用；输入 run.json、已有会话摘要与 segment 事实，产出 index.batches。
"""
import json
import statistics
from collections import defaultdict
from datetime import datetime
from perf_model import at, number

REPETITIONS = (1, 2, 3)
PHASES = dict(repeat3=('a1',), aba=('a1', 'b1', 'a2'))


def protocol_reasons(run):
    protocol = run.get('protocol')
    if not isinstance(protocol, dict):
        return ['AI 运行没有正式采样协议']
    scenes = protocol.get('scenes')
    kind = protocol.get('kind')
    reasons = []
    if not isinstance(kind, str) or kind not in PHASES or protocol.get('phase') not in PHASES[kind]:
        reasons.append('批次种类或阶段不符')
    if not isinstance(protocol.get('batchId'), str) or not protocol['batchId'].strip():
        reasons.append('批次 ID 缺失')
    if (not isinstance(scenes, list) or not scenes or any(not isinstance(s, str) or not s.strip() for s in scenes)
            or len(set(scenes)) != len(scenes) or run.get('scene') not in scenes):
        reasons.append('批次场景计划缺失、重复或未包含本场景')
    if type(protocol.get('repetition')) is not int or protocol['repetition'] not in REPETITIONS:
        reasons.append('场景重复序号不是 1、2、3')
    if run.get('purpose') not in ('baseline', 'functional', 'diagnostic'):
        reasons.append('运行用途未确认')
    if not isinstance(run.get('sampleSegmentId'), str) or not run['sampleSegmentId'].strip():
        reasons.append('正式采样段 ID 缺失')
    return reasons


def sample_window(manifest, streams, run):
    """边界只认成功入原始事件流的标记，不接受请求时长或入口猜测。"""
    if manifest.get('origin') != 'ai' or not isinstance(run.get('protocol'), dict):
        return None, []
    reasons = protocol_reasons(run)
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
    window = dict(segmentId=run['sampleSegmentId'], fromMs=begin['monotonicMs'], toMs=end['monotonicMs'],
                  beginSequence=begin['sequence'], endSequence=end['sequence'])
    return window, []


def sampled_streams(streams, window):
    """准备期可用于上下文归属；所有指标/异常/M/派生只消费本投影。"""
    start, end = window['fromMs'], window['toMs']
    selected = {name: [row for row in rows if start < row['monotonicMs'] <= end]
                for name, rows in streams.items()}
    # B1 行含上一帧区间，跨 Begin 的行无法拆成本，不能比例分摊或借准备期。
    selected['frames'] = [row for row in selected['frames']
                          if number(row.get('dtMs')) and row['monotonicMs'] - row['dtMs'] >= start]
    return selected


def group_key(entry):
    return json.dumps([entry['comparisonKey'], entry['calc'], entry['origin'], entry['scope']], sort_keys=True)


def median_tree(values):
    if all(isinstance(value, dict) for value in values):
        return {key: median_tree([value.get(key) for value in values]) for key in sorted(set().union(*values))}
    return statistics.median(values) if all(number(value) for value in values) else None


def phase_reasons(rows, kind):
    reasons, times = [], {}
    if any(not isinstance(row.get('environmentSignature'), dict) or any(
            row['environmentSignature'].get(key) is None for key in ('machine.driver', 'machine.os', 'build.godot')) for row in rows):
        reasons.append('同批驱动、系统或引擎环境缺失')
    if len({json.dumps(row.get('environmentSignature'), sort_keys=True) for row in rows}) != 1:
        reasons.append('同批驱动、系统或引擎环境不一致')
    for phase in PHASES.get(kind, ()):
        members = [row for row in rows if at(row, 'run.protocol.phase') == phase]
        commits = {at(row, 'build.commit') for row in members}
        if len(commits) != 1 or None in commits:
            reasons.append(f'{phase} 构建提交不一致或缺失')
        if kind != 'aba' or not members:
            continue
        try:
            starts = [datetime.fromisoformat(row['startedAt']) for row in members]
            ends = [datetime.fromisoformat(row['endedAt']) for row in members]
            if any(start.tzinfo is None or end.tzinfo is None or start > end for start, end in zip(starts, ends)):
                raise ValueError('缺少绝对时间或会话时间倒退')
            times[phase] = (min(starts), max(ends))
        except (KeyError, TypeError, ValueError):
            reasons.append(f'{phase} 缺少可确认顺序的会话起止事实')
    for before, after in (('a1', 'b1'), ('b1', 'a2')) if kind == 'aba' else ():
        if before in times and after in times and times[before][1] > times[after][0]:
            reasons.append(f'{before}/{after} 会话顺序不符或重叠')
    return reasons


def batch_results(entries, budgets, failed_sources=()):
    """缺一个正式单次就无批次基线；失败来源仍参与完整性，但不贡献指标。"""
    batches = defaultdict(list)
    for entry in entries:
        identifier = at(entry, 'run.protocol.batchId')
        if entry.get('origin') == 'ai' and isinstance(identifier, str) and identifier:
            batches[identifier].append(entry)
    results, blocked = [], {}
    for identifier, rows in sorted(batches.items()):
        first = rows[0]['run']['protocol']
        kind, scenes = first.get('kind'), first.get('scenes')
        kind = kind if isinstance(kind, str) else None
        reasons, source_reasons, judgement_reasons = phase_reasons(rows, kind), [], []
        groups = defaultdict(list)
        for row in rows:
            run, protocol = row['run'], row['run']['protocol']
            reasons.extend(protocol_reasons(run))
            if protocol.get('kind') != kind or protocol.get('scenes') != scenes:
                reasons.append('同批场景计划或运行种类不一致')
            if row['source'] in failed_sources:
                source_reasons.append('批次会话来源分析失败: ' + row['id'])
            if row['baseStatus'] != 'candidate':
                reasons.append('批次含无效、未完成或证据运行: ' + row['id'])
            if not isinstance(row.get('judgement'), dict):
                judgement_reasons.append('必判摘要缺失: ' + row['id'])
            for check in at(row, 'judgement.requiredChecks', []) or []:
                if check['status'] != 'measured':
                    judgement_reasons.append(row['id'] + '/' + check['key'] + ': ' + check['reason'])
            groups[group_key(row)].append(row)
        frozen_keys = ('logicalPauseVerified', 'mainThreadLongStopVerified', 'noiseCalibrationBatchId', 'tuningSelectedMetrics')
        if len({json.dumps({key: at(row, 'run.conditions.' + key) for key in frozen_keys}, sort_keys=True) for row in rows}) != 1:
            reasons.append('同批核验开关/散布基线/入选项条件不一致')
        plan = scenes if isinstance(scenes, list) and all(isinstance(s, str) for s in scenes) else []
        by_scene = defaultdict(list)
        projected = []
        for grouped in groups.values():
            scene = grouped[0]['comparisonKey'].get('scene')
            if not isinstance(scene, str):
                reasons.append('场景 ID 不是字符串')
                scene = None
            by_scene[scene].append(grouped)
            phases = {}
            for phase in PHASES.get(kind, ()):
                runs = [row for row in grouped if at(row, 'run.protocol.phase') == phase]
                repeats = [at(row, 'run.protocol.repetition') for row in runs]
                if sorted(repeats, key=str) != list(REPETITIONS):
                    reasons.append(f'{scene}/{phase} 未恰好完成三次')
                runs.sort(key=lambda row: str(at(row, 'run.protocol.repetition')))
                if len({json.dumps({key: at(row, 'run.conditions.' + key) for key in ('suite', 'travelPrefetch')}, sort_keys=True)
                        for row in grouped}) != 1:
                    reasons.append(f'{scene} 场景套或旅行变体不一致')
                phases[phase] = dict(sessionIds=[row['id'] for row in runs])
            if kind == 'aba':
                commits = {at(row, 'build.commit') for row in grouped if at(row, 'run.protocol.phase') in ('a1', 'a2')}
                if len(commits) != 1 or None in commits:
                    reasons.append(f'{scene} 的 A1/A2 构建提交不同或缺失')
            projected.append(dict(comparisonKey=grouped[0]['comparisonKey'], calc=grouped[0]['calc'],
                                  origin=grouped[0]['origin'], scope=grouped[0]['scope'], phases=phases))
        if set(by_scene) != set(plan) or any(len(groups) != 1 for groups in by_scene.values()):
            reasons.append('场景覆盖不足或同场景比较键/calc/origin/scope 不一致')
        pain = {phase: batch_pain([row for row in rows if at(row, 'run.protocol.phase') == phase], budgets)
                for phase in PHASES.get(kind, ())}
        for phase, value in pain.items():
            if any(at(row, 'run.protocol.phase') == phase and at(row, 'run.conditions.suite') in ('route', 'travel') for row in rows) and not number(value['lightFramesPerMinute']):
                judgement_reasons.append(phase + '/lightCostStutter: 路线与传送批次合计稳态计率时长不足，重跑')
        measurement_reasons = sorted(set(reasons + source_reasons))
        blocking_reasons = sorted(set(reasons + judgement_reasons))
        reasons = sorted(set(measurement_reasons + judgement_reasons))
        status = ('invalid' if any(row['baseStatus'] == 'invalid' for row in rows) else
                  'evidence' if all(row['baseStatus'] == 'evidence' for row in rows) else 'partial') if reasons else 'candidate'
        if reasons:
            # IO 故障不改既有会话基线身份与保留规则；批次仍 partial，读口不比较。
            if blocking_reasons:
                blocked.update({row['id']: blocking_reasons for row in rows if row['baseStatus'] == 'candidate'})
        if not measurement_reasons:
            populate_phase_costs(projected, groups)
        result = dict(id=identifier, kind=kind, scenes=plan, status=status, statusReasons=reasons, groups=projected)
        result['pain'] = pain
        result['pain']['total'] = batch_pain(rows, budgets)
        if measurement_reasons:
            for pain in result['pain'].values():
                pain['status'] = 'rerun'
        result['requiredChecks'] = [dict(sessionId=row['id'], **check) for row in rows
                                    for check in at(row, 'judgement.requiredChecks', []) or []]
        result['unjudged'] = sorted({item['key'] for row in rows for item in at(row, 'waits.items', []) or [] if item['status'] == 'unjudged'})
        result['unjudged'] += sorted({kind for row in rows for detector in row.get('unavailableDetectors', []) for kind in detector['types']})
        result['unjudged'] += sorted({check['metric'] for row in rows for check in row.get('budgets', [])
                                      if check['metric'] == 'measuredViewportGpuMs' and not check['applicable']})
        results.append(result)
    assess_aba(results, entries)
    return results, blocked


def populate_phase_costs(projected, groups):
    for group in projected:
        members = groups[group_key(group)]
        for phase, value in group['phases'].items():
            runs = [row for row in members if at(row, 'run.protocol.phase') == phase]
            for title in ('all', 'steady'):
                value[title] = dict(cost=median_tree([at(row, title + '.cost') for row in runs]))
            value['distanceTable'] = distance_table(runs, value)


def distance_table(runs, phase):
    result = []
    first = runs[0]
    if at(first, 'run.conditions.suite') not in ('fixed', 'route'):
        return result
    scene = first['comparisonKey']['scene']
    selected = at(first, 'run.conditions.tuningSelectedMetrics', []) or []
    for check in first.get('budgets', []):
        if check['metric'] == 'measuredViewportGpuMs':
            continue
        metric = check['metric']
        value = at(phase, 'steady.cost.' + metric + '.p95')
        members = [next((v for v in row.get('budgets', []) if v['metric'] == metric), {}) for row in runs]
        applicable = all(v.get('applicable') is True and number(v.get('value')) for v in members)
        upper, stop = check['budget'], check.get('stop')
        picked = scene + '.' + metric in selected
        tier = ('rerun' if not applicable or not number(value) else 'above-upper' if value > upper else
                'above-stop' if picked and stop is not None and value > stop else 'within')
        result.append(dict(scene=scene, metric=metric, value=value, upper=upper, stop=stop,
                           ratio=value / upper if number(value) and upper else None, tier=tier, selected=picked,
                           source=check['budgetSource'], stopSource=check.get('stopSource')))
    return result


def batch_pain(rows, budgets):
    """路线和旅行的所有原始运行合计；固定机位单列对照。"""
    playing = [row for row in rows if at(row, 'run.conditions.suite') in ('route', 'travel')]
    controls = [row for row in rows if at(row, 'run.conditions.suite') == 'fixed']
    result = dict(counts={}, severityCounts=dict(heavy=0, severe=0), lightFrames=0, steadyDurationMs=0, worstScenes={},
                  fixedControl=[dict(sessionId=row['id'], pain=at(row, 'judgement.pain')) for row in controls])
    for row in playing:
        pain = at(row, 'judgement.pain', {}) or {}
        result['lightFrames'] += pain.get('lightFrames', 0)
        result['steadyDurationMs'] += pain.get('steadyDurationMs', 0)
        for key in result['severityCounts']:
            result['severityCounts'][key] += pain.get('severityCounts', {}).get(key, 0)
        for key, count in pain.get('counts', {}).items():
            result['counts'][key] = result['counts'].get(key, 0) + count
    for key in result['counts']:
        scenes = defaultdict(int)
        for row in playing:
            scenes[row['comparisonKey']['scene']] += at(row, 'judgement.pain.counts.' + key, 0)
        result['worstScenes'][key] = max(scenes, key=scenes.get) if scenes else None
    light_scenes = defaultdict(lambda: [0, 0])
    for row in playing:
        scene = light_scenes[row['comparisonKey']['scene']]
        scene[0] += at(row, 'judgement.pain.lightFrames', 0)
        scene[1] += at(row, 'judgement.pain.steadyDurationMs', 0)
    measured_rates = {scene: count * 60000 / duration for scene, (count, duration) in light_scenes.items() if duration}
    result['worstScenes']['lightCostStutter'] = max(measured_rates, key=measured_rates.get) if measured_rates else None
    duration = result['steadyDurationMs']
    result['lightFramesPerMinute'] = result['lightFrames'] * 60000 / duration if duration else None
    result['lightLimit'] = budgets['perfAnomalies']['lightCostStutterFramesPerMinuteMax']
    result['zeroTolerance'] = [dict(key=key, count=count, allowed=0) for key, count in result['counts'].items()]
    complete = bool(playing) and all(at(row, 'judgement.requiredChecks') and all(
        check['status'] == 'measured' for check in row['judgement']['requiredChecks']) for row in playing)
    result['status'] = ('rerun' if not complete or not duration else 'pain' if any(result['counts'].values()) or
                        result['lightFramesPerMinute'] > result['lightLimit'] else 'clear')
    return result


def assess_aba(batches, entries):
    by_id = {batch['id']: batch for batch in batches}
    by_session = {row['id']: row for row in entries}
    for batch in batches:
        batch['abaAssessment'] = []
        if batch['kind'] != 'aba':
            continue
        for group in batch['groups']:
            phases = group['phases']
            if any(not isinstance(stage.get('steady'), dict) for stage in phases.values()):
                continue
            session = by_session[phases['a1']['sessionIds'][0]]
            calibration_id = at(session, 'run.conditions.noiseCalibrationBatchId')
            calibration = by_id.get(calibration_id, {})
            reference = next((g for g in calibration.get('groups', []) if g['comparisonKey'] == group['comparisonKey']
                              and g['calc'] == group['calc'] and g['scope'] == group['scope']), None)
            reference_sessions = ([by_session[identifier] for p in reference['phases'].values() for identifier in p['sessionIds']]
                                  if reference else [])
            unmodified_calibration = bool(reference_sessions) and len({at(row, 'build.commit') for row in reference_sessions}) == 1
            group['noiseRanges'] = []
            for distance in phases['b1']['distanceTable']:
                metric = distance['metric']
                values = [at(phases[p], 'steady.cost.' + metric + '.p95') for p in ('a1', 'b1', 'a2')]
                if not all(number(v) for v in values):
                    continue
                group['noiseRanges'].append(dict(metric=metric, sigma0=max(values) - min(values)))
                reference_values = ([at(reference, 'phases.' + p + '.steady.cost.' + metric + '.p95')
                                     for p in ('a1', 'b1', 'a2')] if reference and calibration.get('kind') == 'aba'
                                    and all(isinstance(stage.get('steady'), dict) for stage in reference['phases'].values())
                                    and unmodified_calibration else [])
                sigma = max(reference_values) - min(reference_values) if reference_values and all(number(v) for v in reference_values) else None
                spread = abs(values[0] - values[2])
                improvement = (values[0] + values[2]) / 2 - values[1]
                ruler = max(spread, sigma) if sigma is not None else None
                guards = regression_guards(phases, batch['pain'])
                eligible = batch['status'] == 'candidate'
                batch['abaAssessment'].append(dict(scene=group['comparisonKey']['scene'], metric=metric,
                    improvement=improvement, aDifference=spread, sigma0=sigma, noiseCalibrationBatchId=calibration_id,
                    requiredImprovement=ruler, accepted=improvement > ruler and not guards if ruler is not None and eligible else None,
                    reasons=guards + ([] if sigma is not None else ['同场景同口径第0轮散布缺测，未判']) +
                    ([] if eligible else ['必判未核或批次不完整，只提供成本校准事实'])))


def regression_guards(phases, pain):
    reasons = []
    reference = {row['metric']: row for row in phases['a1'].get('distanceTable', [])}
    other_a = {row['metric']: row for row in phases['a2'].get('distanceTable', [])}
    for row in phases['b1'].get('distanceTable', []):
        before, after = reference.get(row['metric'], {}), other_a.get(row['metric'], {})
        for line in ('upper', 'stop'):
            limit = row[line]
            if line == 'stop' and not row['selected']:
                continue
            if number(limit) and all(number(a.get('value')) and a['value'] <= limit for a in (before, after)) and row['value'] > limit:
                reasons.append(row['metric'] + ' 被推出' + line)
    b, a1, a2 = (pain.get(phase, {}) for phase in ('b1', 'a1', 'a2'))
    for key, count in b.get('counts', {}).items():
        if count > 0 and a1.get('counts', {}).get(key) == 0 and a2.get('counts', {}).get(key) == 0:
            reasons.append('B新出痛点: ' + key)
    if (number(b.get('lightFramesPerMinute')) and b['lightFramesPerMinute'] > b['lightLimit'] and
            all(number(a.get('lightFramesPerMinute')) and a['lightFramesPerMinute'] <= a['lightLimit'] for a in (a1, a2))):
        reasons.append('B新出轻级痛点')
    return reasons
