"""用有上限的中文表读取性能派生数据，不在展示层重算指标。
用法：python -I -X utf8 scripts/perf/perf.py {latest,show,batch,compare,slice,anomalies,selfcheck}；各子命令加 --help 查参数。
输入：output/perf 的分析器产出；产出：最多 100 行、16000 字符的中文表；selfcheck 只运行随附小样本，不写真实索引。
"""
import argparse
import csv
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.dont_write_bytecode = True
from perf_model import CALC, LEVEL, at, number, read_json

ROOT = Path(__file__).resolve().parents[2]
MAX_ROWS = 50
MAX_LINES = 100
MAX_CHARS = 16000
MAX_CELL = 300
STATUS = dict(current='当前基线', stale='过时', superseded='已替代', partial='不完整', invalid='作废', unsupported='格式不支持', evidence='证据')
TYPE = dict(dataValidity='数据有效性', costSpike='成本尖峰候选', budgetExceeded='预算越线', gcPause='GC 停顿',
            gen2='游玩时 gen2', compilation='管线或 JIT 编译', stutter='卡顿', freeze='冻结',
            loadingStutter='加载界面卡顿', sustainedDropped='持续掉帧', frameLimitFailure='限帧失效',
            gpuIdleCandidate='GPU 空等候选', sustainedIdleCandidate='持续空等候选', gcWindow='GC 窗合计',
            allocationStorm='全进程分配风暴')
TYPE.update(framePacing='帧节奏抖动', videoMemory='整卡显存逼近', clockThrottle='降频位背景',
            memoryGrowth='内存增长候选', utilizationSwing='占用读数波动', costStutter='成本卡顿',
            logicPause='逻辑段停顿', mainThreadLongStop='主线程长停', manualSaveThumbnail='手动存档缩略帧', hang='卡死')


def cell(value):
    if value is None:
        return '缺值'
    if isinstance(value, float):
        return f'{value:.4g}'
    if isinstance(value, (dict, list)):
        return json.dumps(value, ensure_ascii=False, separators=(',', ':'))[:MAX_CELL]
    return str(value).replace('\n', ' ')[:MAX_CELL]


def bounded(lines):
    output, used = [], 0
    for line in lines:
        line = str(line)
        if len(output) >= MAX_LINES - 1 or used + len(line) + 1 > MAX_CHARS - 40:
            output.append('…输出已达上限，请缩小范围或指定会话。')
            break
        output.append(line)
        used += len(line) + 1
    return '\n'.join(output)


def table(headers, rows):
    yield ' | '.join(headers)
    yield ' | '.join('---' for _ in headers)
    for row in rows:
        yield ' | '.join(cell(value) for value in row)


def load_index(out):
    path = out / 'index.json'
    return read_json(path) if path.exists() else dict(calc=CALC, sessions=[])


def find_summary(out, identifier):
    if not identifier or Path(identifier).name != identifier or identifier in ('.', '..'):
        raise ValueError('会话 ID 格式错误')
    summary = read_json(out / 'derived' / identifier / 'summary.json')
    errors = [error for error in load_index(out).get('errors', []) if error['source'] == summary['source']]
    if errors:
        raise ValueError('会话来源已出错：' + cell(summary['source']) + '；' +
                         '；'.join(cell(error['kind']) + '：' + cell(error['reason']) for error in errors))
    if summary['calc'] != CALC:
        raise ValueError('派生口径已过时；先运行 analyze.py 重算')
    return summary


def overview(summary):
    yield f"会话 {summary['id']} | {STATUS.get(summary['status'], summary['status'])} | calc={summary['calc']} | scope={summary['scope']}"
    yield '状态原因：' + cell(summary.get('statusReasons'))
    yield '采集运行状态：' + cell(at(summary, 'run.status')) + '；原因：' + cell(at(summary, 'run.reasons'))
    recording = summary.get('recording') or {}
    yield '记录完整性：' + cell({key: recording.get(key) for key in ('status', 'complete', 'fault', 'endReason')})
    yield from table(['原始流', '状态', '写出行', '丢行'],
                     ((row.get('file'), row.get('state'), row.get('writtenRows'), row.get('droppedRows'))
                      for row in recording.get('streams') or []))
    delivery = summary.get('eventDelivery') or {}
    counts = list((delivery.get('counts') or {}).items())
    for offset in range(0, len(counts), 4):
        yield '全会话事件次数：' + '；'.join(name + '=' + str(count) for name, count in counts[offset:offset + 4])
    for gap in delivery.get('gaps', [])[:10]:
        payload = gap['payload']
        yield '事件缺口：' + cell({key: payload.get(key) for key in ('stream', 'rows', 'firstSequence', 'lastSequence')})
        yield '缺口源区间：' + cell({key: payload.get(key) for key in ('firstCollectionFrame', 'lastCollectionFrame', 'fromMs', 'toMs')})
        yield from table(['邻近事件序号', '采集帧', '单调ms', '事件', '来源'],
                         ((row.get(key) for key in ('sequence', 'collectionFrame', 'monotonicMs', 'event', 'source'))
                          for row in gap['neighbours']))
    yield '比较键：' + cell(summary['comparisonKey'])
    yield '构建提交：' + cell(at(summary, 'build.commit')) + '；原始 schema=' + cell(summary.get('schema'))
    yield '构建配置：' + cell(at(summary, 'build.configuration')) + '；含未提交改动：' + cell(at(summary, 'build.dirty'))
    metrics = [('主视口 GPU p95 / ms', at(summary, 'steady.cost.mainViewportGpuMs.p95')),
               ('视口 CPU 渲染 p95 / ms', at(summary, 'steady.cost.mainViewportCpuMs.p95')),
               ('帧准备 CPU p95 / ms', at(summary, 'steady.cost.frameSetupCpuMs.p95')),
               ('整卡显存峰值 / MiB', at(summary, 'all.cost.hardware.wholeGpuMemoryMiB.maximum')),
               ('平均 FPS', at(summary, 'all.experience.averageFps')),
               ('1% low / FPS', at(summary, 'all.experience.low1Fps')),
               ('0.1% low / FPS', at(summary, 'all.experience.low01Fps')),
               ('有效帧 N；1% / 0.1% 尾样本数', [at(summary, 'all.experience.' + k) for k in ('n', 'low1TailN', 'low01TailN')]),
               ('帧时 p99 / ms', at(summary, 'all.experience.frameMs.p99')),
               ('掉帧比例 / 0–1', at(summary, 'all.experience.droppedFrameFraction')),
               ('GPU 占用均值 / 0–1', at(summary, 'all.experience.gpuUtilFraction'))]
    yield from table(['指标', '值'], metrics)
    if summary.get('scopeReason'):
        yield '体验缺值原因：' + summary['scopeReason']
    for check in summary.get('budgets', []):
        state = '越线' if check['exceeded'] is True else '预算内' if check['exceeded'] is False else '不适用或缺测'
        yield f"预算 {check['metric']}：{state}；比值={cell(check['ratio'])}；{check['budgetSource']}"
    yield '异常：' + cell(summary.get('anomalyCounts'))
    judgement = summary.get('judgement') or {}
    yield '必判：' + cell(judgement.get('requiredChecks'))
    yield '成本痛点（固定套只作对照）：' + cell(judgement.get('pain'))
    yield from table(['等待', '观测秒', '上限秒', '状态', '原因'],
                     ((v['key'], v['seconds'], v['upperSeconds'], v['status'], v['reason'])
                      for v in at(summary, 'waits.items', []) or []))
    conclusion = summary.get('conclusion')
    if not conclusion:
        unsupported = summary.get('baseStatus', summary['status']) == 'unsupported'
        reasons = summary.get('sampleReasons') or summary.get('baseReasons') or summary.get('statusReasons')
        conclusion = ('当前 schema 不支持，未计算指标。' if unsupported else '采集或分析未完成，未计算指标。') + '原因：' + cell(reasons)
    yield '结论：' + conclusion


def latest(out, args):
    index = load_index(out)
    errors = index.get('errors', [])
    failed_sources = {error['source'] for error in errors}
    entries = [s for s in index['sessions'] if s['calc'] == CALC and s['source'] not in failed_sources]
    if args.origin:
        entries = [s for s in entries if s['origin'] == args.origin]
    if args.scope:
        entries = [s for s in entries if s['scope'] == args.scope]
    if not entries:
        yield '暂无已分析会话；启动入口、AI 采集或观察台将触发 analyze.py。'
    entries.sort(key=lambda s: s.get('endedAt') or '', reverse=True)
    # Keep the newest source for each comparison group; partial/evidence remain visible with explicit state.
    groups = set()
    shown = []
    for entry in entries:
        key = json.dumps([entry['comparisonKey'], entry['scope'], entry['origin']], sort_keys=True)
        if key not in groups:
            groups.add(key)
            shown.append(entry)
        if len(shown) >= args.limit:
            break
    for entry in shown:
        yield from overview(find_summary(out, entry['id']))
    yield f'会话总数 {len(entries)}；本次列出 {len(shown)} 个比较组。'
    if errors:
        yield '出错来源：'
        yield from table(['来源', '种类', '原因'], ((error['source'], error['kind'], error['reason']) for error in errors))


def find_batch(out, identifier):
    batch = next((row for row in load_index(out).get('batches', []) if row['id'] == identifier), None)
    if batch is None:
        raise ValueError('轮记录不存在')
    return batch


def compare(out, args):
    previous, current = find_batch(out, args.left), find_batch(out, args.right)
    if current['previousId'] != previous['id'] or current['number'] != previous['number'] + 1:
        raise ValueError('只比较显式相邻轮 N-1 与 N')
    yield f"第{previous['number']}轮 {previous['id']} → 第{current['number']}轮 {current['id']}"
    yield from comparison_lines(current.get('comparison', []))


def comparison_lines(rows):
    yield from table(['场景/指标', '前轮', '本轮', '改善比例', '变化', '可合入', '原因'],
                     ((row['scene'] + '/' + row['metric'], row['previous'], row['value'], row['improvementFraction'],
                       row['change'], row['accepted'], row['reasons']) for row in rows))
    if not rows:
        yield '没有完整相邻轮比较；成本记录仍可查。'


def batch_overview(out, args):
    batch = find_batch(out, args.id)
    yield f"第{batch['number']}轮 {batch['id']} | {STATUS.get(batch['status'], batch['status'])} | 前轮={batch['previousId']}"
    yield '状态原因：' + cell(batch['statusReasons'])
    yield '原始根：' + cell(batch['sourceRoot'])
    yield f"登记场景 {len(batch['scenes'])}；已选主采 {len(batch['groups'])}；确认 {sum(bool(group['confirmationSessionId']) for group in batch['groups'])}"
    missing = {(check['key'], check.get('reason')) for check in batch.get('requiredChecks', []) if check['status'] != 'measured'}
    yield from table(['未核必判', '原因'], sorted(missing))
    yield '未核项目：' + cell(batch.get('unjudged'))
    yield '本轮痛点：' + cell(batch.get('pain'))
    yield '门禁结论：' + cell(batch.get('gateStatus'))
    for group in batch['groups']:
        main = find_summary(out, group['mainSessionId'])
        key = group['comparisonKey']
        yield '场景 ' + cell(key['scene']) + '；构建=' + cell(at(main, 'build.commit')) + '；原始schema=' + cell(main['schema'])
        yield '输出/内部尺寸：' + cell(key['outputResolution']) + '/' + cell(key['internalResolution']) + f"；calc={group['calc']}；scope={group['scope']}"
        yield from table(['主采', '确认', '主 GPU p95/ms', '帧准备 CPU p95/ms', '待确认', '原因'],
                         [(group['mainSessionId'], group['confirmationSessionId'], at(group, 'steady.cost.mainViewportGpuMs.p95'),
                           at(group, 'steady.cost.frameSetupCpuMs.p95'), group['confirmationPending'], group['confirmationReasons'])])


def distance(out, args):
    batch = find_batch(out, args.id)
    yield f"第{batch['number']}轮 {batch['id']} | {STATUS.get(batch['status'], batch['status'])}"
    groups = [group for group in batch['groups'] if not args.scene or group['comparisonKey']['scene'] == args.scene]
    rows = [row for group in groups for row in group['distanceTable']]
    yield from table(['场景', '指标', '测值ms', '上沿ms', '停线ms', '比值', '档位'],
                     ((r['scene'], r['metric'], r['value'], r['upper'], r['stop'], r['ratio'], r['tier']) for r in rows))
    yield '痛点：' + cell(batch.get('pain'))
    yield '未核项目：' + cell(batch.get('unjudged'))
    yield '待确认场景：' + cell([dict(scene=g['comparisonKey']['scene'], reasons=g['confirmationReasons']) for g in groups if g['confirmationPending']])
    yield from comparison_lines([row for group in groups for row in group.get('comparison', [])])
    if not rows:
        yield '没有完整场景距离表；不能据空表判达标。'


def record(out, args):
    from perf_batches import register_round
    from analyze import analyze_all
    register_round(out, args.id, args.number, args.source_root, args.scene, args.previous_id)
    result = analyze_all(args.root.resolve(), author=args.author_root)
    if result['errors']:
        raise ValueError('分析来源出错: ' + cell(result['errors']))
    return batch_overview(out, args)


def anomalies(out, args):
    summary = find_summary(out, args.id)
    value = read_json(out / 'derived' / args.id / 'anomalies.json')
    rows = value['items']
    if args.type:
        rows = [row for row in rows if row['type'] == args.type]
    yield f"会话 {args.id}；{summary['scope']}；异常总数 {value['total']}；各类摘要最多 20 条。"
    yield from table(['类型', '级别', '帧区间', '关联阶段', '值 / 阈值', '候选原因'],
                     ((TYPE.get(row['type'], row['type']), LEVEL[row['level']],
                       f"{row['firstFrame']}–{row['lastFrame']}", row['phase'],
                       f"{cell(row['value'])} / {cell(row['threshold'])}", row['candidates']) for row in rows[:args.limit]))
    for item in summary.get('deepen', [])[:args.limit]:
        yield '深挖：' + item['command'] + '；' + item['commandState']


def slice_rows(out, args):
    find_summary(out, args.id)
    path = out / 'derived' / args.id / 'hotframes.csv'
    yield f'会话 {args.id}；仅展示重级以上异常切片；原始逐帧流不由 CLI 打开。'
    rows = []
    with path.open(encoding='utf-8', newline='') as stream:
        for row in csv.DictReader(stream):
            frame = int(row['collectionFrame'])
            if args.start is not None and frame < args.start:
                continue
            if args.end is not None and frame > args.end:
                continue
            rows.append([row.get(key) for key in ('collectionFrame', 'dtMs', 'mainViewportGpuMs', 'mainViewportCpuMs', 'associationPhase', 'steady', 'gapBefore')])
            if len(rows) >= args.limit:
                break
    yield from table(['采集帧', '间隔ms', '主GPU ms', '视口CPU ms', '关联阶段', '稳态', '缺口前缀'], rows)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=ROOT)
    commands = parser.add_subparsers(dest='command', required=True)
    latest_parser = commands.add_parser('latest', help='按比较组列最新已分析数据')
    latest_parser.add_argument('--origin', choices=('play', 'ai'))
    latest_parser.add_argument('--scope', choices=('cost', 'full'))
    latest_parser.add_argument('--limit', type=int, default=2)
    show_parser = commands.add_parser('show', help='显示一个会话')
    show_parser.add_argument('id')
    batch_parser = commands.add_parser('batch', help='读取分析器生成的轮完整性与单次/确认较差值')
    batch_parser.add_argument('id')
    distance_parser = commands.add_parser('distance', help='只读分析器距离表、痛点与相邻轮比较')
    distance_parser.add_argument('id')
    distance_parser.add_argument('--scene')
    compare_parser = commands.add_parser('compare', help='读取显式相邻两轮的分析器结论')
    compare_parser.add_argument('left')
    compare_parser.add_argument('right')
    for name in ('anomalies', 'slice'):
        command = commands.add_parser(name)
        command.add_argument('id')
        command.add_argument('--limit', type=int, default=20)
        if name == 'anomalies':
            command.add_argument('--type')
        else:
            command.add_argument('--start', type=int)
            command.add_argument('--end', type=int)
    record_parser = commands.add_parser('record', help='登记唯一轮并经现有分析器离线重算')
    record_parser.add_argument('id')
    record_parser.add_argument('--number', type=int, required=True)
    record_parser.add_argument('--source-root', type=Path, required=True)
    record_parser.add_argument('--scene', action='append', required=True)
    record_parser.add_argument('--previous-id')
    record_parser.add_argument('--author-root', type=Path)
    commands.add_parser('selfcheck', help='运行小样本与收口回归')
    args = parser.parse_args()
    if hasattr(args, 'limit'):
        args.limit = max(1, min(args.limit, MAX_ROWS))
    out = args.root.resolve() / 'output/perf'
    try:
        if args.command == 'selfcheck':
            from selfcheck import run
            lines = run(args.root.resolve())
        elif args.command == 'latest':
            lines = latest(out, args)
        elif args.command == 'show':
            lines = overview(find_summary(out, args.id))
        elif args.command == 'compare':
            lines = compare(out, args)
        elif args.command == 'record':
            lines = record(out, args)
        elif args.command == 'batch':
            lines = batch_overview(out, args)
        elif args.command == 'distance':
            lines = distance(out, args)
        elif args.command == 'anomalies':
            lines = anomalies(out, args)
        else:
            lines = slice_rows(out, args)
        print(bounded(lines))
        return 0
    except (OSError, ValueError, KeyError) as error:
        print(bounded(['无法读取性能派生：' + str(error)]), file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
