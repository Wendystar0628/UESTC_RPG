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


def batch_results(entries, failed_sources=()):
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
        reasons, source_reasons = phase_reasons(rows, kind), []
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
            groups[group_key(row)].append(row)
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
                phases[phase] = dict(sessionIds=[row['id'] for row in runs])
            if kind == 'aba':
                commits = {at(row, 'build.commit') for row in grouped if at(row, 'run.protocol.phase') in ('a1', 'a2')}
                if len(commits) != 1 or None in commits:
                    reasons.append(f'{scene} 的 A1/A2 构建提交不同或缺失')
            projected.append(dict(comparisonKey=grouped[0]['comparisonKey'], calc=grouped[0]['calc'],
                                  origin=grouped[0]['origin'], scope=grouped[0]['scope'], phases=phases))
        if set(by_scene) != set(plan) or any(len(groups) != 1 for groups in by_scene.values()):
            reasons.append('场景覆盖不足或同场景比较键/calc/origin/scope 不一致')
        blocking_reasons = sorted(set(reasons))
        reasons = sorted(set(reasons + source_reasons))
        status = ('invalid' if any(row['baseStatus'] == 'invalid' for row in rows) else
                  'evidence' if all(row['baseStatus'] == 'evidence' for row in rows) else 'partial') if reasons else 'candidate'
        if reasons:
            # IO 故障不改既有会话基线身份与保留规则；批次仍 partial，读口不比较。
            if blocking_reasons:
                blocked.update({row['id']: blocking_reasons for row in rows if row['baseStatus'] == 'candidate'})
        else:
            for group in projected:
                members = groups[group_key(group)]
                for phase, value in group['phases'].items():
                    runs = [row for row in members if at(row, 'run.protocol.phase') == phase]
                    for title in ('all', 'steady'):
                        value[title] = dict(cost=median_tree([at(row, title + '.cost') for row in runs]))
        results.append(dict(id=identifier, kind=kind, scenes=plan, status=status, statusReasons=reasons, groups=projected))
    return results, blocked
