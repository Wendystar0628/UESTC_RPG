"""用有上限的中文表读取性能派生数据，不在展示层重算指标。
用法：python -I -X utf8 scripts/perf/perf.py {latest,show,compare,slice,anomalies,selfcheck}；各子命令加 --help 查参数。
输入：output/perf 的分析器产出；产出：最多 100 行、16000 字符的中文表；selfcheck 只运行随附四例小样本，不写真实索引。
"""
import argparse
import csv
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.dont_write_bytecode = True
from perf_model import CALC, at, number, read_json

ROOT = Path(__file__).resolve().parents[2]
MAX_ROWS = 50
MAX_LINES = 100
MAX_CHARS = 16000
MAX_CELL = 300
STATUS = dict(current='当前基线', stale='过时', superseded='已替代', partial='不完整', invalid='作废', unsupported='格式不支持', evidence='证据')
LEVEL = dict(info='提示', light='轻', heavy='重', severe='严重')
TYPE = dict(dataValidity='数据有效性', costSpike='成本尖峰候选', budgetExceeded='预算越线', gcPause='GC 停顿',
            gen2='游玩时 gen2', compilation='管线或 JIT 编译', stutter='卡顿', freeze='冻结',
            loadingStutter='加载界面卡顿', sustainedDropped='持续掉帧', frameLimitFailure='限帧失效',
            gpuIdleCandidate='GPU 空等候选', sustainedIdleCandidate='持续空等候选', gcWindow='GC 窗合计',
            allocationStorm='全进程分配风暴')
TYPE.update(framePacing='帧节奏抖动', videoMemory='整卡显存逼近', clockThrottle='降频位背景',
            memoryGrowth='内存增长候选', utilizationSwing='占用读数波动')


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
    if summary['calc'] != CALC:
        raise ValueError('派生口径已过时；先运行 analyze.py 重算')
    return summary


def overview(summary):
    yield f"会话 {summary['id']} | {STATUS.get(summary['status'], summary['status'])} | calc={summary['calc']} | scope={summary['scope']}"
    yield '状态原因：' + cell(summary.get('statusReasons'))
    yield '比较键：' + cell(summary['comparisonKey'])
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
    yield '结论：' + summary.get('conclusion', '当前 schema 不支持，未计算指标。')


def latest(out, args):
    entries = [s for s in load_index(out)['sessions'] if s['calc'] == CALC]
    if args.origin:
        entries = [s for s in entries if s['origin'] == args.origin]
    if args.scope:
        entries = [s for s in entries if s['scope'] == args.scope]
    if not entries:
        yield '暂无已分析会话；启动入口、AI 采集或观察台将触发 analyze.py。'
        return
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


def compare(out, args):
    left, right = find_summary(out, args.left), find_summary(out, args.right)
    eligible = (left['comparisonKey'] == right['comparisonKey'] and left['scope'] == right['scope']
                and left['calc'] == right['calc'] and not left.get('comparisonMissing') and not right.get('comparisonMissing'))
    clean = all(s['origin'] == 'ai' and s['status'] in ('current', 'superseded') and at(s, 'build.dirty') is False for s in (left, right))
    eligible &= clean
    yield f"{left['id']} → {right['id']}"
    yield '可比：' + ('是；同键、同口径、同配置、有效常规运行。' if eligible else '否；比较键、口径、状态或来源不符合比较条件，差异不下结论。')
    metrics = ('steady.cost.mainViewportGpuMs.p95', 'steady.cost.mainViewportCpuMs.p95',
               'steady.cost.frameSetupCpuMs.p95', 'all.experience.averageFps', 'all.experience.low1Fps',
               'all.experience.frameMs.p99')
    yield from table(['指标', '左', '右', '差值（仅可比时）'],
                     ((key, at(left, key), at(right, key), at(right, key) - at(left, key)
                       if eligible and number(at(left, key)) and number(at(right, key)) else None) for key in metrics))


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
    compare_parser = commands.add_parser('compare', help='检查可比性并列成本差值')
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
    commands.add_parser('selfcheck', help='运行四例小样本')
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
