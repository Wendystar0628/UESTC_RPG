"""观察台每份公开与内部结果的唯一登记；不导入生产方或执行 IO。
用法：datafile 派生公开间隔；collect 绑定已导入生产方；live 按本表调度。
产出：无（静态 metadata，不发现模块或类型）。
"""
from dataclasses import dataclass


@dataclass(frozen=True)
class Dataset:
    inputs: frozenset[str]
    interval: int | None = None
    lane: str = 'fast'
    batch_s: float = .1
    rows: bool = False
    sample: bool = False
    priority: int = 1
    refresh_s: int | None = None
    expiry: bool = False
    kind: str = 'data'   # data：公开结果；raw：共享原始事实；app：界面发布


# inputs 是原生通知来源或 @结果；interval=None 的结果不公开为数据文件。
DATASETS = {
    'live': Dataset(frozenset({'sessions', 'queue'}), 90,
                    rows=True, sample=True, priority=0),
    'attention': Dataset(frozenset({'attention', 'shots', 'worktrees'}), 300),
    'feedback': Dataset(frozenset({'letters'}), 300),
    'progress': Dataset(frozenset({'queue', 'history:queue'}), 300,
                        rows=True, priority=0),
    'flow': Dataset(frozenset({'queue', 'history:queue', 'history:pushes', 'history:audits',
                                         'history:checkpoint', '@progress', '@_git'}), 600,
                    lane='slow', batch_s=10, rows=True),
    'collab': Dataset(frozenset({'queue', 'history:letters', 'history:audits', 'history:pushes',
                                             'history:agents', 'history:leads', 'history:queue', 'history:checkpoint'}),
                      600, lane='slow', batch_s=10, rows=True),
    'team': Dataset(frozenset({'sessions', 'queue', 'history:agents', '@progress'}), 300,
                    rows=True, sample=True),
    'health': Dataset(frozenset({'history:patrol', 'history:checkpoint', 'gates', 'code',
                                             'worktrees', 'hooks', 'patrol:interval'}), 1800, expiry=True),
    'failures': Dataset(frozenset({'history:failures', 'git', 'failure-types'}), 1800),
    'perf': Dataset(frozenset({'perf', 'git'}), 3600, lane='slow', batch_s=10),
    'system': Dataset(frozenset({'scripts', 'settings', 'hooks', 'local', '@docs'}), 1800,
                      lane='slow', refresh_s=600),
    'hookfeed': Dataset(frozenset({'hooks', 'local', 'sessions', 'queue'}), 300,
                        rows=True, sample=True, expiry=True),
    'feed': Dataset(frozenset({'queue', 'history:queue', 'history:pushes', 'history:audits',
                                         'history:checkpoint', 'docs', 'notices', '@_git'}), 600, rows=True, expiry=True),
    'docs': Dataset(frozenset({'docs'}), 3600, lane='slow'),
    'now': Dataset(frozenset({'queue', '@progress', '@flow', '@collab'}), 600),
    '_git': Dataset(frozenset({'git', 'history:pushes'}), lane='slow', kind='raw'),
    '_app': Dataset(frozenset({'app'}), lane='slow', kind='app'),
}

# 策展输入只登记在这里；不参与自动采集调度。
CURATED_NAMES = ('audit', 'notices')
