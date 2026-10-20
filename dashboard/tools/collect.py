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
from monitor import util as U
from monitor.read import collab as read_collab, repo
from monitor.view import attention, progress, flow, collab, team, health, system, hookfeed, people, feed, docs, now
SOURCE = 'monitor.view'
_LOCK = threading.RLock()
_VALUES = {}
_RAW = None
_SAMPLE = None
_ROWS = None

# inputs 是原生通知来源名或 @公开结果；内部 _git 只供共享完成证据。
PRODUCERS = {
    'live': lambda context: live_data(context['sample']),
    'attention': lambda context: attention.refresh(datafile.write, names=('attention',))['attention'],
    'feedback': lambda context: attention.refresh(datafile.write, names=('feedback',))['feedback'],
    'progress': progress.build, 'flow': flow.build, 'collab': collab.build,
    'team': team.build, 'health': health.build, 'system': system.build,
    'hookfeed': lambda context: hookfeed.build(),
    'feed': feed.build, 'docs': docs.build, 'now': now.build,
}
INPUTS = {
    'live': {'sessions', 'queue'},
    'attention': {'attention', 'shots', 'worktrees'}, 'feedback': {'letters'},
    'progress': {'queue', 'history:queue'},
    'flow': {'queue', 'history:queue', 'history:pushes', 'history:audits', 'history:checkpoint', '@progress', '@_git'},
    'collab': {'queue', 'history:letters', 'history:audits', 'history:pushes', 'history:agents', 'history:leads', 'history:queue', 'history:checkpoint'},
    'team': {'sessions', 'queue', 'history:agents'},
    'health': {'history:patrol', 'history:checkpoint', 'gates', 'code', 'worktrees', 'hooks'},
    'system': {'scripts', 'settings', 'hooks', 'local', '@docs'},
    'hookfeed': {'hooks', 'local'}, 'docs': {'docs'},
    'feed': {'queue', 'history:queue', 'history:pushes', 'history:audits', 'history:checkpoint', 'docs', 'notices', '@_git'},
    'now': {'queue', '@progress', '@flow', '@collab'},
    '_git': {'git', 'history:pushes'}, '_app': {'app'},
}
SLOW_NAMES = {'docs', 'system', '_git', '_app', 'flow', 'collab'}
ROW_NAMES = {'live', 'team', 'progress', 'flow', 'collab', 'feed'}


def live_data(sample):
    leads = []
    for side, who in (('claude', 'Claude'), ('codex', 'Codex')):
        session = next((item for item in sample['sessions'] if item['side'] == side), {})
        state = session.get('state')
        leads.append({'who': who, 'running': True if state == 'running' else False if state == 'idle' else None,
                      'since': session.get('since')})
    return {'leads': leads}


def context(names, recorder=None):
    global _ROWS
    with _LOCK:
        rows, sampled = _ROWS, _SAMPLE
        values = dict(_VALUES)
    rows_error, sample_error = None, None
    if names & ROW_NAMES:
        try:
            rows = read_collab.queue_rows()
            with _LOCK:
                _ROWS = rows
        except Exception as error:
            rows_error = error
    if names & {'live', 'team'}:
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
    done, failed = set(), set()
    shared = context(names, recorder)
    if names & {'live', 'team'} and shared['sample'] is not None:
        with _LOCK:
            _SAMPLE = shared['sample']
        if recorder is not None:
            recorder.observe_sample(shared['sample'])
    if '_git' in names:
        try:
            raw = repo.progress_facts()
            with _LOCK:
                _RAW = raw
            shared['raw'] = raw
            done.add('_git')
        except Exception as error:
            failed.add('_git')
            with _LOCK:
                _RAW = {'errors': [f'{type(error).__name__}: {error}']}
                shared['raw'] = _RAW
            print(f'collect：Git来源采集失败（{type(error).__name__}: {error}）', file=sys.stderr)
    if '_app' in names:
        datafile.publish_app(app_paths)
        done.add('_app')
    pending = names - {'_git', '_app', 'attention', 'feedback'}
    while pending:
        ready = sorted((name for name in pending if not any(
            source[1:] in pending for source in INPUTS.get(name, ()) if source.startswith('@'))),
            key=lambda item: (item not in ('live', 'progress'), item))
        if not ready:
            raise ValueError('采集生产方存在循环依赖：' + ', '.join(sorted(pending)))
        name = ready[0]
        pending.remove(name)
        try:
            if name not in PRODUCERS:
                raise ValueError('未接入生产方：' + name)
            if name in ROW_NAMES and shared['rows_error'] is not None:
                raise shared['rows_error']
            if name in ('live', 'team') and shared['sample_error'] is not None:
                raise shared['sample_error']
            data = PRODUCERS[name](shared)
        except Exception as error:
            message = f'{type(error).__name__}: {error}'
            print(f'collect：{name} 采集失败（{message}）', file=sys.stderr)
            data = {'errors': [message]}
        datafile.write('auto', name, data, source=SOURCE + '.' + name)
        with _LOCK:
            _VALUES[name] = data
        shared['values'][name] = data
        (failed if data.get('errors') else done).add(name)
    selected = names & {'attention', 'feedback'}
    if selected:
        result = attention.refresh(datafile.write, names=tuple(sorted(selected)))
        for name, data in result.items():
            with _LOCK:
                _VALUES[name] = data
            shared['values'][name] = data
            (failed if data.get('errors') else done).add(name)
    return done, failed


def next_expiry():
    """同一会话事实的时间阈值：活跃静默分钟、过时与交回一小时到期。"""
    now = time.time()
    with _LOCK:
        sample = _SAMPLE
    if sample is None:
        return None
    deadlines = []
    for agent in sample['agents']:
        last = U.epoch(agent['last']) if agent.get('last') else None
        if last and agent.get('state') in ('running', 'stalled', 'unknown'):
            deadlines.append(last + (int(max(0, now - last) / 60) + 1) * 60)
            deadlines.extend(last + minutes * 60 for minutes in (U.STALL_MIN, U.ABANDON_MIN) if last + minutes * 60 > now)
        end = agent.get('completedAt') or agent.get('endedAt')
        ended = U.epoch(end) if end else None
        if ended and ended + 3600 > now:
            deadlines.append(ended + 3600)
    return min(deadlines) if deadlines else None


def main():
    names = set(sys.argv[1:] or INPUTS)
    unknown = names - set(INPUTS)
    if unknown:
        raise ValueError('未接入数据：' + ', '.join(sorted(unknown)))
    while True:
        expanded = names | {source[1:] for name in names for source in INPUTS[name] if source.startswith('@')}
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
