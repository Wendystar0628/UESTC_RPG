"""读取分析器生成的场景指标、预算线与上一次采集差值。
用法：python -I -X utf8 scripts/perf/perf.py {latest,show,compare}；latest 可加 --scene，show/compare 使用会话 ID。
"""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.dont_write_bytecode = True
from perf_model import CALC, number, read_json

ROOT = Path(__file__).resolve().parents[2]


def cell(value):
    if value is None:
        return '缺测'
    return f'{value:.4g}' if number(value) else str(value)


def load_index(out):
    index = read_json(out / 'index.json')
    if index.get('calc') != CALC:
        raise ValueError('请先运行 analyze.py 更新指标')
    return index


def find_summary(out, identifier):
    from analyze import session_id
    entry = next((e for e in load_index(out)['sessions'] if e['id'] == identifier), None)
    if entry is None:
        raise ValueError('没有该会话的当前指标：' + identifier)
    return read_json(out / 'derived' / session_id(identifier) / 'summary.json')


def overview(summary, delta=()):
    print(f"场景 {summary.get('scene')} | 会话 {summary['id']} | {summary.get('origin')} | {summary.get('scope')}")
    print('指标（时间为 p95/ms，内存为 p95/MiB） | 本次 | 上次 | 差值 | 变化比例')
    differences = {row['metric']: row for row in delta}
    for metric, value in summary.get('metrics', {}).items():
        row = differences.get(metric, {})
        print(' | '.join(map(cell, (metric, value, row.get('previous'), row.get('delta'), row.get('changeFraction')))))
    for check in summary.get('budgets', []):
        print(f"预算 {check['metric']}: {cell(check['value'])} / {cell(check['budget'])}; 差 {cell(check['deltaToBudget'])}; {check['budgetSource']}")
    if summary.get('waits'):
        for item in summary['waits']['items']:
            if item['seconds'] is not None:
                print(f"等待 {item['key']}: {cell(item['seconds'])} s / {cell(item['upperSeconds'])} s")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=ROOT)
    parser.add_argument('--output', type=Path, help='共享 output/perf 目录')
    commands = parser.add_subparsers(dest='command', required=True)
    latest = commands.add_parser('latest', help='各场景最新指标与上次差值')
    latest.add_argument('--scene')
    latest.add_argument('--origin', choices=('play', 'ai'), default='ai')
    latest.add_argument('--scope', choices=('cost', 'full'))
    latest.add_argument('--limit', type=int, default=100)
    show = commands.add_parser('show', help='单会话指标')
    show.add_argument('id')
    compare = commands.add_parser('compare', help='同场景两次会话的指标差值')
    compare.add_argument('left')
    compare.add_argument('right')
    args = parser.parse_args()
    out = args.output.resolve() if args.output else args.root.resolve() / 'output/perf'
    try:
        index = load_index(out)
        if args.command == 'latest':
            from analyze import scene_results
            entries = [e for e in index['sessions'] if e.get('origin') == args.origin
                       and (not args.scope or e.get('scope') == args.scope)
                       and (not args.scene or e.get('scene') == args.scene)]
            if args.origin == 'ai':
                scenes = scene_results(entries)
                for row in scenes[:max(1, args.limit)]:
                    overview(find_summary(out, row['id']), row['delta'])
            else:
                entries.sort(key=lambda e: (e.get('endedAt') or '', e['id']), reverse=True)
                for row in entries[:max(1, args.limit)]:
                    overview(find_summary(out, row['id']))
            if not entries:
                print('暂无已分析会话；请运行 analyze.py。')
            for error in index.get('errors', []):
                print(f"分析错误 {error['source']}: {error['reason']}")
        elif args.command == 'show':
            current = find_summary(out, args.id)
            row = next((s for s in index['scenes'] if s['id'] == args.id), {})
            overview(current, row.get('delta', []))
        else:
            before, current = find_summary(out, args.left), find_summary(out, args.right)
            if before.get('scene') != current.get('scene') or before.get('origin') != current.get('origin'):
                raise ValueError('比较需要同一场景、同一来源')
            from analyze import metric_delta
            delta = metric_delta(current['metrics'], before['metrics'])
            overview(current, delta)
        return 0
    except (OSError, ValueError, KeyError, TypeError) as error:
        print('无法读取性能指标：' + str(error), file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
