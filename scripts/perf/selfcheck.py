"""小样本与收口回归自检，只验证性能分析行为，不代表真实录制或性能达标。
用法：python -I -X utf8 scripts/perf/perf.py selfcheck；输入当前字典和预算，产出自检结果，临时六件与派生自动清除。
"""
from perf_model import read_json
from selfcheck_analysis import (check_snapshot, check_median, check_deepen, check_manifest, check_event_order,
                                check_cost_windows, check_cold_gap, check_viewport_sources)
from selfcheck_batches import check_sampling_batches, check_batch_freshness, check_failed_baseline
from selfcheck_storage import check_pending, check_processed_error, check_retention_authority, check_roundtrip
from selfcheck_projection import check_derived_curves, check_monitor_projection, check_missing_projection
from selfcheck_context import check_context, check_window_changes, check_hardware_facts


def run(root):
    dictionary, budgets = read_json(root / 'game/data/perf-fields.json'), read_json(root / 'budgets.json')
    result = []
    for check in (check_snapshot, check_median, check_pending, check_deepen, check_manifest, check_context, check_window_changes, check_hardware_facts):
        check(root, dictionary, budgets)
    result.append('通过 TOOL-447/457：缩略图与命令上下文只认帧位、窗口变化共享稳定时长，当前 schema 硬件聚合事实与原生时间保真。')
    result.append('通过 TOOL-435：同 schema 重算与结构校验、M 保窗与换口径、pending 锁/索引/过期、deepen、清单枚举与上下文权威回归。')
    check_sampling_batches(root, dictionary, budgets)
    check_batch_freshness(root, dictionary, budgets)
    result.append('通过 TOOL-457：趋势只投影 index.batches 完整阶段中位；功能、partial 与无批次会话不入趋势，stale 保留说明。')
    result.append('通过 TOOL-448：真实样段裁剪、跨界帧排除与 M 重建，作者/非协议证据保留；三次/A-B-A 完整性与中位由分析器产出。')
    check_processed_error(root, dictionary, budgets)
    check_failed_baseline(root, dictionary, budgets)
    check_derived_curves(root, dictionary, budgets)
    check_monitor_projection(root, dictionary, budgets)
    check_retention_authority(root, dictionary, budgets)
    check_missing_projection(root, dictionary, budgets)
    result.append('通过 TOOL-437：错误输入只处理一次，输入变化重算；曲线、帧率与归一化负载由分析器派生。')
    result.append('通过 TOOL-441：内容错误缓存、IO 重试与恢复、冲突重查、来源删除与派生缺失判鲜；好坏会话隔离，重复故障不自激通知。')
    result.append('通过 TOOL-445/446：超保留窗口基线一次 IO 失败不晋升旧会话、不删原始；派生内容错误缓存，规则计数与缺日志原因通过。')
    result.append('通过 TOOL-451：CLI 拒绝失败来源的旧派生且好会话可读；普通坏批按预算删除、三类错误按真实 removed 清除，非 IO 派生异常缓存。')
    check_event_order(root, dictionary, budgets)
    result.append('通过 TOOL-438：事件 sequence 先校验再按时刻稳定排序；重复序号与逐帧时刻倒退仍拒绝。')
    check_cost_windows(root, dictionary, budgets)
    result.append('通过 1/4：近似主视口成本进入分布与 M；尖峰仅报±2帧窗口，无逐帧阶段/原因归因。')
    check_cold_gap(root, dictionary, budgets)
    result.append('通过 2/4：冷启动无相对判据、绝对预算照报；缺口不跨算，low 缺样为 null。')
    check_viewport_sources(root, dictionary, budgets)
    result.append('通过 3/4：近似主视口不进合计；跳过行不复用主成本，未确认倒影留 null，已确认来源不重加。')
    check_roundtrip(root, dictionary, budgets)
    result.append('通过 4/4：预算独立报、cost 体验全 null；六件往返、写入锁跳过、历史去重、按条数修剪和输出上限通过。')
    return result
