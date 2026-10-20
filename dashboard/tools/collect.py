"""观察台按份采集；live 的快慢通道调用同一生产表。
用法：python -I -X utf8 dashboard/tools/collect.py [数据名…]。
产出：data/auto 的 v3 数据；不写 index，不另开采集守护。
"""
import os
import sys
import time
import threading
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(ROOT, '.collab', 'tools'))
import datafile
from catalog import DATASETS
from monitor import util as U
from monitor.read import collab as read_collab, repo
from monitor.view import attention, progress, flow, collab, team, health, system, hookfeed, people, feed, docs, now
SOURCE = 'monitor.view'
_LOCK = threading.RLock()
_VALUES = {}
_RAW = None
_SAMPLE = None
_ROWS = None
_EXPIRIES = {}


def live_data(context):
    sample = context['sample']
    leads = []
    for side, who in (('claude', 'Claude'), ('codex', 'Codex')):
        session = next((item for item in sample['sessions'] if item['side'] == side), {})
        state = session.get('state')
        leads.append({'who': who, 'running': True if state == 'running' else False if state == 'idle' else None,
                      'since': session.get('since')})
    return {'leads': leads}


def hookfeed_data(context):
    return hookfeed.build()


def git_data(context):
    global _RAW
    raw = repo.progress_facts()
    with _LOCK:
        _RAW = raw
    context['raw'] = raw
    return raw


def app_data(context):
    datafile.publish_app(context['app_paths'])


def producer(spec):
    """只绑定本文件显式导入的模块或本地适配函数；缺生产方直接报错。"""
    module, dot, function = spec.producer.partition('.')
    try:
        result = getattr(globals()[module], function) if dot else globals()[module]
    except (KeyError, AttributeError) as error:
        raise ValueError('未接入生产方：' + spec.producer) from error
    if not callable(result):
        raise ValueError('生产方不可调用：' + spec.producer)
    return result


# 只检查登记接合，入口启动时即发现公开名单或实际生产方遗漏。
assert tuple(datafile.AUTO_NAMES.items()) == tuple((name, spec.interval) for name, spec in DATASETS.items()
                                                 if spec.interval is not None), \
    '公开采集登记与 AUTO_NAMES 不一致'
for _spec in DATASETS.values():
    producer(_spec)


def context(names):
    global _ROWS
    with _LOCK:
        rows, sampled = _ROWS, _SAMPLE
        values = dict(_VALUES)
    rows_error, sample_error = None, None
    if any(DATASETS[name].rows for name in names):
        try:
            rows = read_collab.queue_rows()
            with _LOCK:
                _ROWS = rows
        except Exception as error:
            rows_error = error
    if any(DATASETS[name].sample for name in names):
        sampled = None
        try:
            if rows_error is not None:
                raise rows_error
            sampled = people.sample(rows)
        except Exception as error:
            sample_error = error
    with _LOCK:
        raw = _RAW
    return {'rows': rows, 'sample': sampled, 'values': values, 'rows_error': rows_error, 'sample_error': sample_error,
            'raw': raw if raw is not None else {'errors': ['Git 来源尚未完成首次采集']}}


def run(names, recorder=None, app_paths=()):
    """仅重算点名项，完成一份即发布；返回成功/失败，不吞真实错误体。"""
    global _RAW, _SAMPLE
    names = set(names)
    unknown = names - DATASETS.keys()
    if unknown:
        raise ValueError('未接入数据：' + ', '.join(sorted(unknown)))
    done, failed = set(), set()
    shared = context(names)
    shared['app_paths'] = app_paths
    if any(DATASETS[name].sample for name in names) and shared['sample'] is not None:
        with _LOCK:
            _SAMPLE = shared['sample']
        if recorder is not None:
            recorder.observe_sample(shared['sample'])
    selected = {name for name in names if producer(DATASETS[name]) is attention.refresh}
    pending = names - selected
    while pending:
        ready = sorted((name for name in pending if not any(
            source[1:] in pending for source in DATASETS[name].inputs if source.startswith('@'))),
            key=lambda item: (DATASETS[item].priority, item))
        if not ready:
            raise ValueError('采集生产方存在循环依赖：' + ', '.join(sorted(pending)))
        name = ready[0]
        pending.remove(name)
        spec = DATASETS[name]
        expiry = None
        try:
            if spec.rows and shared['rows_error'] is not None:
                raise shared['rows_error']
            if spec.sample and shared['sample_error'] is not None:
                raise shared['sample_error']
            result = producer(spec)(shared)
            data, expiry = result if spec.expiry else (result, None)
        except Exception as error:
            message = f'{type(error).__name__}: {error}'
            print(f'collect：{name} 采集失败（{message}）', file=sys.stderr)
            data = {'errors': [message]}
        if spec.interval is None:
            if name == '_git' and data.get('errors'):
                with _LOCK:
                    _RAW = data
                shared['raw'] = data
            (failed if data and data.get('errors') else done).add(name)
            continue
        datafile.write('auto', name, data, source=SOURCE + '.' + name)
        with _LOCK:
            _VALUES[name] = data
            if spec.expiry:
                _EXPIRIES[name] = expiry if not data.get('errors') else None
        shared['values'][name] = data
        (failed if data.get('errors') else done).add(name)
    if selected:
        result = attention.refresh(datafile.write, names=tuple(sorted(selected)))
        for name, data in result.items():
            with _LOCK:
                _VALUES[name] = data
            shared['values'][name] = data
            (failed if data.get('errors') else done).add(name)
    return done, failed


def next_expiry():
    """现有会话到期点与 view 给出的巡逻到期点；返回来源名到 epoch。"""
    now = time.time()
    with _LOCK:
        sample = _SAMPLE
        expiries = {name: due for name, due in _EXPIRIES.items() if due is not None}
    deadlines = []
    for agent in sample['agents'] if sample is not None else ():
        last = U.epoch(agent['last']) if agent.get('last') else None
        if last and agent.get('state') in ('running', 'stalled', 'unknown'):
            deadlines.append(last + (int(max(0, now - last) / 60) + 1) * 60)
            deadlines.extend(last + minutes * 60 for minutes in (U.STALL_MIN, U.ABANDON_MIN) if last + minutes * 60 > now)
        end = agent.get('completedAt') or agent.get('endedAt')
        ended = U.epoch(end) if end else None
        if ended and ended + 3600 > now:
            deadlines.append(ended + 3600)
    if deadlines:
        expiries['sessions'] = min(deadlines)
    return expiries


def main():
    names = set(sys.argv[1:] or DATASETS)
    unknown = names - DATASETS.keys()
    if unknown:
        raise ValueError('未接入数据：' + ', '.join(sorted(unknown)))
    while True:
        expanded = names | {source[1:] for name in names for source in DATASETS[name].inputs if source.startswith('@')}
        if expanded == names:
            break
        names = expanded
    if '_app' in names:
        datafile.publish_app()
        names.remove('_app')
    # 人工入口按同一依赖顺序计算；快慢并发只归 live 调度。
    _, failed = run(names)
    return 1 if failed else 0


if __name__ == '__main__':
    sys.exit(main())
