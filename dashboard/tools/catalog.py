"""观察台每份公开与内部结果的唯一登记；不导入生产方或执行 IO。
用法：datafile 派生公开间隔；collect 绑定已导入生产方；live 按本表调度。
产出：无（静态 metadata，不发现模块或类型）。
"""
from dataclasses import dataclass


@dataclass(frozen=True)
class Dataset:
    producer: str
    inputs: frozenset[str]
    interval: int | None = None
    lane: str = 'fast'
    batch_s: float = .1
    rows: bool = False
    sample: bool = False
    priority: int = 1
    refresh_s: int | None = None
    expiry: bool = False


# inputs 是原生通知来源或 @结果；interval=None 的结果不公开为数据文件。
DATASETS = {
    'live': Dataset('live_data', frozenset({'sessions', 'queue'}), 90,
                    rows=True, sample=True, priority=0),
    'attention': Dataset('attention.refresh', frozenset({'attention', 'shots', 'worktrees'}), 300),
    'feedback': Dataset('attention.refresh', frozenset({'letters'}), 300),
    'progress': Dataset('progress.build', frozenset({'queue', 'history:queue'}), 300,
                        rows=True, priority=0),
    'flow': Dataset('flow.build', frozenset({'queue', 'history:queue', 'history:pushes', 'history:audits',
                                         'history:checkpoint', '@progress', '@_git'}), 600,
                    lane='slow', batch_s=10, rows=True),
    'collab': Dataset('collab.build', frozenset({'queue', 'history:letters', 'history:audits', 'history:pushes',
                                             'history:agents', 'history:leads', 'history:queue', 'history:checkpoint'}),
                      600, lane='slow', batch_s=10, rows=True),
    'team': Dataset('team.build', frozenset({'sessions', 'queue', 'history:agents', '@progress'}), 300,
                    rows=True, sample=True),
    'health': Dataset('health.build', frozenset({'history:patrol', 'history:checkpoint', 'gates', 'code',
                                             'worktrees', 'hooks', 'patrol:interval'}), 1800, expiry=True),
    'system': Dataset('system.build', frozenset({'scripts', 'settings', 'hooks', 'local', '@docs'}), 1800,
                      lane='slow', refresh_s=600),
    'hookfeed': Dataset('hookfeed_data', frozenset({'hooks', 'local'}), 300),
    'feed': Dataset('feed.build', frozenset({'queue', 'history:queue', 'history:pushes', 'history:audits',
                                         'history:checkpoint', 'docs', 'notices', '@_git'}), 600, rows=True, expiry=True),
    'docs': Dataset('docs.build', frozenset({'docs'}), 3600, lane='slow'),
    'now': Dataset('now.build', frozenset({'queue', '@progress', '@flow', '@collab'}), 600),
    '_git': Dataset('git_data', frozenset({'git', 'history:pushes'}), lane='slow'),
    '_app': Dataset('app_data', frozenset({'app'}), lane='slow'),
}
