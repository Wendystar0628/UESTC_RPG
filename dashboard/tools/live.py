"""实时采集：谁在跑（会话、子代理）、在做的工作、后台脚本、构建锁、临时工作树、钩子，写 data/auto/live.js，作者观察台的总览、施工、模型页读它；
另每 60 s 写一次脚本目录 data/auto/catalog.js（所有脚本与开头说明，脚本页读它）；协作文件、文档或 git 一变就立即重跑 collect.py。
用法：python -I -X utf8 dashboard/tools/live.py          写一次
      python -I -X utf8 dashboard/tools/live.py --loop   每 15 s 写一次；已有一个在跑就直接退出；输入一变就跑一次 collect.py，无变化时每 2 分钟周期刷新
入口只组装：数据都经监控库（.collab/tools/monitor/，与巡逻共用，只读）的 view/ 取，不在这里另写读法；本机协作数据（.collab）经它读，
这是仓库代码引用 .collab 的唯一例外。每一块独立取，失败的那块留空并把原因记进 errors，不拖垮其余。形状见 dashboard/README.md §2.1。
改了 live.py 或 monitor 以后要重启 --loop（它把代码读进内存，只有项目采集那部分会自动重新加载）。
"""
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "..", "..", ".collab", "tools"))  # 监控库 monitor 在仓库根的 .collab 下
from datafile import write  # noqa: E402
from monitor import util as U  # noqa: E402
from monitor.read import collab, repo, scriptfiles, wpnames  # noqa: E402
from monitor.check import build as gate_check  # noqa: E402
from monitor.view import catalog, hooks, people, running, trees, work  # noqa: E402

EVERY_S = 15            # --loop 的间隔
COLLECT_INTERVAL_S = 120   # 项目采集（collect.py）的周期刷新间隔；输入一变就立即采（见 inputs_stamp）
CATALOG_EVERY_S = 60    # 脚本目录（catalog.js）的间隔，扫描很轻
LOCK_FILE = U.REPO + "/output/.locks/live.lock"


def build_block(items, n_engine, n_build, editors):
    held = repo.lock_held()
    holder = None
    if held:   # 锁文件是空的，不记谁占着：取在跑的编译、引擎、门禁、截图里最久的那个
        busy = [s for s in items if s["state"] == "running" and (s["kind"] in ("build", "engine", "gate") or s["name"] == "fixed-shots.ps1")]
        if busy:
            b = min(busy, key=lambda s: s["since"])
            holder = f"{b['name']}（{b['where']}）" if b.get("where") else b["name"]
    return {"lock": "held" if held else "free", "holder": holder, "engines": n_engine, "builds": n_build, "editors": editors}


def worktree_block(procs):
    """临时工作树：登记 × 进程 × 最近一次门禁（门禁的一句话只有 check/build 一份实现）。"""
    found = repo.worktrees(procs)
    return trees.build(found, {t["path"]: gate_check.gate_brief(t["path"]) for t in found if not t["prunable"]})


def part(errors, name, fn, default):
    """一块失败不拖垮其余：失败原因（异常类型与原文，哪个文件读不到会写在里面）记进 errors，页面据此说明。"""
    try:
        return fn()
    except Exception as e:
        errors.append(f"{name}：{type(e).__name__} {e}")
        return default


def collect():
    errors = []
    rows = part(errors, "施工队列", collab.queue_rows, [])
    queue = {r["id"]: r["what"] for r in rows}
    ids = list(queue)
    c_part = part(errors, "Codex", lambda: people.codex(queue, ids), ([], []))
    a_part = part(errors, "Claude", lambda: people.claude(ids), ([], []))
    sessions, agents = people.merge(a_part, c_part)
    procs = part(errors, "进程表", repo.processes, [])
    items, n_engine, n_build, editors = part(errors, "脚本", lambda: running.build(procs), ([], 0, 0, []))
    out_scripts = running.with_status(items)
    build = part(errors, "构建锁", lambda: build_block(items, n_engine, n_build, editors), None)
    hook_cards = part(errors, "钩子", hooks.cards, None)
    changes = part(errors, "工作区改动", repo.changes, [])
    worktrees = part(errors, "临时工作树", lambda: worktree_block(procs), None)
    done = part(errors, "在做的工作", lambda: work.build(rows, agents, sessions, changes, worktrees, out_scripts), None)
    live = {"sessions": sessions, "agents": agents, "scripts": out_scripts, "build": build, "work": done, "worktrees": worktrees, "hooks": hook_cards, "errors": errors}
    return live, procs


_last_catalog = 0.0
# 项目采集读的输入：协作文件、文档、git 的提交与暂存；任何一个的最新修改时间变了就重采
WATCH_DIRS = [U.COLLAB + d for d in ("/status", "/notes", "/requests", "/tasks", "/research", "/side")] + [U.REPO + "/docs"]
WATCH_FILES = [U.COLLAB + "/HANDOVER.md", U.REPO + "/ownership.json", U.REPO + "/.git/HEAD", U.REPO + "/.git/index", U.REPO + "/.git/refs/heads/main"]


def write_catalog(procs):
    """脚本目录：失败只在 stderr 说一句（旧的 catalog.js 留着，页面按它的 updated 显示数据旧了）。"""
    try:
        write("auto", "catalog", catalog.build(scriptfiles.scan(), procs), source="live.py")
    except Exception as e:
        print(f"{U.iso(time.time())} 写 catalog.js 失败：{type(e).__name__} {e}", file=sys.stderr)


def inputs_stamp():
    """项目采集各输入的最新修改时间与文件数（目录里删了文件也算变）；docs/godot-api 是生成参考，不看。"""
    newest, count = 0.0, 0
    for d in WATCH_DIRS:
        for root, dirs, files in os.walk(d):
            dirs[:] = [x for x in dirs if x != "godot-api"]
            for f in files:
                count += 1
                try:
                    newest = max(newest, os.path.getmtime(os.path.join(root, f)))
                except OSError:
                    pass
    for f in WATCH_FILES:
        try:
            newest = max(newest, os.path.getmtime(f))
        except OSError:
            pass
    return newest, count


def write_once():
    global _last_catalog
    live, procs = collect()
    path = write("auto", "live", wpnames.annotate_tree(live), source="live.py")   # 工作包号在写文件处配中文包名
    try:
        gate_check.shot_rounds(True)   # 截图轮次即时记账：工作树拍完就删，也不丢这一轮
    except Exception as e:
        print(f"{U.iso(time.time())} 记截图轮次失败：{type(e).__name__} {e}", file=sys.stderr)
    if time.time() - _last_catalog >= CATALOG_EVERY_S:
        _last_catalog = time.time()
        write_catalog(procs)
    return path


def single_instance():
    """独占打开锁文件的第一个字节：已有一个在跑就拿不到。进程退出（含崩溃）锁自动释放。返回要一直持有的文件对象，拿不到返回 None。"""
    import msvcrt
    os.makedirs(os.path.dirname(LOCK_FILE), exist_ok=True)
    f = open(LOCK_FILE, "a+b")
    try:
        f.seek(0)
        msvcrt.locking(f.fileno(), msvcrt.LK_NBLCK, 1)
    except OSError:
        f.close()
        return None
    return f


def reload_monitor():
    """监控库与 collect 的脚本改过也立即生效（项目采集用）：按依赖从下往上重新加载。"""
    import importlib
    order = ("monitor.util", "monitor.record", "monitor.read", "monitor.check", "monitor.view")
    names = sorted((n for n in sys.modules if n.startswith("monitor.") and n != "monitor"), key=lambda n: next((i for i, p in enumerate(order) if n.startswith(p)), 99))
    for n in names:
        importlib.reload(sys.modules[n])
    if "collect" in sys.modules:
        importlib.reload(sys.modules["collect"])


def main():
    if "--loop" not in sys.argv:
        write_once()
        return
    lock = single_instance()
    if lock is None:
        print("live.py --loop 已经在跑，退出")
        return
    last_collect, last_stamp = 0.0, None
    while True:
        t = time.time()
        try:
            write_once()
        except Exception as e:   # 写失败（磁盘、占用）下一轮再试
            print(f"{U.iso(t)} 写 live.js 失败：{type(e).__name__} {e}", file=sys.stderr)
        # 项目数据也由这个循环定时刷新：没有 Claude 会话（巡逻停了）时观察台照样更新
        stamp = inputs_stamp()
        if stamp != last_stamp or t - last_collect >= COLLECT_INTERVAL_S:
            last_collect, last_stamp = t, stamp
            try:
                reload_monitor()
                import collect
                collect.main()
            except BaseException as e:  # 采集出错不影响实时数据
                if isinstance(e, KeyboardInterrupt):
                    raise
                print(f"{U.iso(t)} 项目采集失败：{type(e).__name__} {e}", file=sys.stderr)
        time.sleep(max(1.0, EVERY_S - (time.time() - t)))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
