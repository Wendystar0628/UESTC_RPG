"""实时采集：谁在跑（会话、子代理）、后台脚本、构建锁，写 data/auto/live.js，作者观察台的模型页读它。
用法：python -I -X utf8 dashboard/tools/live.py          写一次
      python -I -X utf8 dashboard/tools/live.py --loop   每 15 s 写一次；已有一个在跑就直接退出
只读：Codex 会话表与会话记录末尾、Claude 会话记录与子代理记录、进程表、构建锁、子代理登记（钩子写的）；不联网。
会话表、记录读取、进程表、构建锁、施工队列直接用巡逻（.collab/tools/watch/patrol.py）的函数；每一块独立取，失败的那块留空，不拖垮其余。
形状见 dashboard/README.md §2.1。
"""
import glob
import json
import os
import re
import sqlite3
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "..", "..", ".collab", "tools", "watch"))  # 巡逻脚本在仓库根的 .collab 下
import patrol  # noqa: E402
from datafile import write  # noqa: E402

REPO = patrol.REPO
CLAUDE_PROJ = patrol.CLAUDE_PROJ
EVERY_S = 15            # --loop 的间隔
RECENT_MIN = patrol.RECENT_MIN   # 子代理在这么多分钟内有动静才列出来
STALL_MIN = 20          # 记录最后一条还没收尾、却这么久没写，算不在跑
LOCK_FILE = REPO + "/output/.locks/live.lock"
AGENT_LOG = patrol.C + "/tools/state/agents/claude.jsonl"   # 钩子登记的 Claude 子代理
PATROL_LAST = patrol.C + "/tools/state/patrol.last"         # 巡逻每次运行 touch 它（C-155）
PATROL_EVERY_MIN = 10
TASK_MAX = 40


def iso(ts):
    return time.strftime("%Y-%m-%dT%H:%M:%S", time.localtime(ts))


def clip(text, n=TASK_MAX):
    text = re.sub(r"[*`]", "", text or "")
    text = patrol.short(text)
    return text if len(text) <= n else text[:n - 1] + "…"


def tail_lines(path, size):
    """文件末尾 size 字节里的整行（丢掉可能被截断的第一行）。"""
    n = os.path.getsize(path)
    with open(path, "rb") as f:
        f.seek(max(0, n - size))
        lines = f.read().decode("utf-8", "ignore").splitlines()
    return lines[1:] if n > size else lines


def row_of(text):
    m = re.search(r"(?<![A-Za-z])([A-Z]-\d+[a-z]?)(?![A-Za-z0-9])", text or "")
    return m.group(1) if m and not m.group(1).startswith("D-") else None


# ---------- Codex ----------

def codex_block(queue):
    """Astra、Sol 两个主会话与它们名下（含子代理再开的）子代理；型号与推理强度在会话表的 model、reasoning_effort 列。"""
    db = sqlite3.connect(f"file:{patrol.CODEX_DB}?mode=ro", uri=True)
    cut = time.time() - RECENT_MIN * 60
    marks = ",".join("?" * len(patrol.APP))
    rows = db.execute(
        "select id, name, rollout_path, source, updated_at, created_at, agent_nickname, agent_path, model, reasoning_effort "
        f"from threads where archived = 0 and (name in ({marks}) or updated_at > ?)",
        [n for _, n in patrol.APP] + [cut]).fetchall()
    sessions, roots = [], {}
    for label, name in patrol.APP:
        r = next((x for x in rows if x[1] == name), None)
        if not r:
            continue
        running, _mtime, last = patrol.turn(r[2])   # 记录文件的修改时间被占用时不更新，最后动静一律取会话表的 updated_at
        sessions.append({"id": r[0], "name": label, "side": "codex", "model": r[8] or None, "effort": r[9] or None,
                         "state": "running" if running else "idle", "since": iso(r[4]), "task": clip(last) or None})
        roots[r[0]] = label
    parent_of = {}
    for r in rows:
        try:
            parent_of[r[0]] = json.loads(r[3])["subagent"]["thread_spawn"]["parent_thread_id"]
        except (ValueError, KeyError, TypeError):
            pass
    known, agents = set(roots), []
    for _ in range(3):  # 子代理再开子代理，最多按三层往下认
        for r in sorted(rows, key=lambda x: -(x[4] or 0)):
            if r[0] in known or parent_of.get(r[0]) not in known or (r[4] or 0) < cut:
                continue
            known.add(r[0])
            running = patrol.turn(r[2])[0]
            seg = (r[7] or "").split("/")[2:3] or [""]   # /root/<任务名>[/子任务]：队列行号在任务名开头
            m = re.match(r"([a-z])(\d+[a-z]?)_", seg[0])
            row = f"{m.group(1).upper()}-{m.group(2)}" if m else None
            task = clip(queue.get(row, "")) if row else ""
            agents.append({"id": r[0], "parent": parent_of[r[0]], "name": r[6] or "?", "side": "codex", "model": r[8] or None,
                           "effort": r[9] or None, "state": "running" if running else "done", "started": iso(r[5] or r[4]),
                           "last": iso(r[4]), "task": task or clip(seg[0].replace("_", " ")), "row": row})
    return sessions, agents


# ---------- Claude ----------

def claude_turn(lines):
    """记录末尾判断一轮收没收尾，并取型号：（在跑, 型号）。
    最后一条助手消息 stop_reason 是 end_turn 就是收尾了；其余（调用工具中、等工具结果）算在跑，由调用方按多久没写再判。"""
    for l in reversed(lines):
        if '"type":"assistant"' not in l and '"type":"user"' not in l:
            continue
        try:
            d = json.loads(l)
        except ValueError:
            continue
        if d.get("type") == "assistant":
            msg = d.get("message") or {}
            return msg.get("stop_reason") != "end_turn", msg.get("model")
        return True, None
    return False, None


def claude_model(lines):
    for l in reversed(lines):
        if '"type":"assistant"' in l:
            try:
                d = json.loads(l)
            except ValueError:
                continue
            return (d.get("message") or {}).get("model"), d.get("effort")
    return None, None


def registry():
    """钩子登记的 Claude 子代理，按 agent_id 找；没取到 agent_id 的忽略。"""
    out = {}
    try:
        for l in open(AGENT_LOG, encoding="utf-8"):
            try:
                d = json.loads(l)
            except ValueError:
                continue
            if d.get("agent_id"):
                out[d["agent_id"]] = d
    except OSError:
        pass
    return out


def claude_block():
    """主会话是项目目录里最近更新的会话记录；子代理在 <会话号>/subagents/agent-<号>.jsonl，旁边的 .meta.json 有任务、型号、推理强度。"""
    files = glob.glob(CLAUDE_PROJ + "/*.jsonl")
    if not files:
        return [], []
    now = time.time()
    main = max(files, key=os.path.getmtime)
    sid = os.path.basename(main)[:-6]
    reg = registry()
    agents = []
    cut = now - RECENT_MIN * 60
    for p in glob.glob(CLAUDE_PROJ + "/*/subagents/agent-*.jsonl"):
        mtime = os.path.getmtime(p)
        if mtime < cut:
            continue
        meta = {}
        try:
            meta = json.load(open(p[:-6] + ".meta.json", encoding="utf-8"))
        except (OSError, ValueError):
            pass
        aid = os.path.basename(p)[len("agent-"):-6]
        running, model = claude_turn(tail_lines(p, 300_000))
        running = running and now - mtime < STALL_MIN * 60
        r = reg.get(aid, {})
        desc = meta.get("description") or r.get("description") or ""
        started = r.get("at") or iso(os.path.getctime(p[:-6] + ".meta.json") if os.path.exists(p[:-6] + ".meta.json") else mtime)
        alias = meta.get("model") or r.get("model")
        agents.append({"id": aid, "parent": os.path.basename(os.path.dirname(os.path.dirname(p))), "name": meta.get("agentType") or "子代理",
                       "side": "claude", "model": r.get("resolved_model") or model, "alias": alias,
                       "effort": meta.get("effort") or r.get("effort"), "state": "running" if running else "done",
                       "started": started, "last": iso(mtime), "task": clip(desc), "row": row_of(desc)})
    # 刚开始还没有助手消息的子代理只有别名（sonnet）：按已知的全名补成同一种写法
    names = {a["alias"]: a["model"] for a in agents if a["alias"] and a["model"]}
    for a in agents:
        alias = a.pop("alias")
        a["model"] = a["model"] or names.get(alias) or alias
    lines = tail_lines(main, 3_000_000)
    running, _model = claude_turn(lines)
    model, effort = claude_model(lines)
    busy = running and now - os.path.getmtime(main) < STALL_MIN * 60
    busy = busy or any(a["state"] == "running" and a["parent"] == sid for a in agents)   # 等着子代理交回也算在忙
    session = {"id": sid, "name": "主会话", "side": "claude", "model": model, "effort": effort,
               "state": "running" if busy else "idle", "since": iso(os.path.getmtime(main)), "task": None}
    return [session], agents


# ---------- 脚本与构建 ----------

HEAVY_ENGINE = {"headless": "无头运行", "import": "资源导入", "bake": "烘焙", "gate": "门禁", "shots": "截图", "bench": "基准", "export": "导出"}


def script_defs():
    """（id, 中文名, 种类, 进程名要有的字, 命令行要匹配的正则）。拿脚本名认是哪个脚本；bash -c 之类的外壳也带着命令文字，所以限定进程名。"""
    return [
        ("inbox", "来信监视", "watch", "bash", r"inbox_wait\.sh\"?\s*$"),
        ("live", "观察台实时采集", "watch", "python", r"dashboard[/\\]tools[/\\]live\.py.*--loop"),
        ("gate", "门禁", "gate", "powershell|pwsh", r"gate\.ps1"),
        ("shots", "固定机位截图", "other", "powershell|pwsh", r"fixed_shots\.ps1"),
        ("codex_run", "向 Codex 派活", "other", "python", r"codex_app_run\.py"),
        ("codex_steer", "向 Codex 发引导", "other", "python", r"codex_app_steer\.py"),
        ("clean", "清理脚本", "other", "python|powershell|pwsh", r"clean_artifacts\.py|clean[^ ]*\.ps1"),
    ]


def runtime_text(start):
    m = patrol.minutes(start)
    return "刚开始" if m < 1 else f"跑了 {m} 分钟" if m < 120 else f"跑了 {m // 60} 小时"


def scripts_block(procs):
    out, watchers = [], {"inbox", "live"}
    for sid, name, kind, pname, cmd_re in script_defs():
        hit = [s for n, c, s in procs if re.match(pname, n.lower()) and re.search(cmd_re, c)]
        if hit:
            start = min(hit)
            out.append({"id": sid, "name": name, "kind": kind, "state": "running", "since": iso(start),
                        "note": runtime_text(start) + (f"（{len(hit)} 个进程）" if len(hit) > 1 else "")})
        elif sid in watchers:
            out.append({"id": sid, "name": name, "kind": kind, "state": "stopped", "since": None, "note": "没在跑"})
    # 巡逻是 Claude 会话里的定时任务，进程里看不到：看它每次运行 touch 的状态文件
    if os.path.exists(PATROL_LAST):
        last = os.path.getmtime(PATROL_LAST)
        alive = patrol.minutes(last) < PATROL_EVERY_MIN * 2.5
        out.append({"id": "patrol", "name": "巡逻", "kind": "cron", "state": "running" if alive else "stopped", "since": iso(last),
                    "note": f"上次 {patrol.minutes(last)} 分钟前" if alive else f"已 {patrol.minutes(last)} 分钟没运行"})
    else:
        out.append({"id": "patrol", "name": "巡逻", "kind": "cron", "state": "stopped", "since": None, "note": "没有运行痕迹"})
    # 引擎（命令行跑的）与 dotnet 编译测试：同一种合成一行
    engines, builds = {}, {}
    for n, c, s in procs:
        if n.lower().startswith("godot"):
            m = re.search(r"--(headless|import|bake|gate|shots|bench|export)\b", c)
            if m:
                engines.setdefault(HEAVY_ENGINE[m.group(1)], []).append(s)
        elif n.lower() == "dotnet.exe":
            m = re.search(r"\b(build|test)\b", c)
            if m:
                builds.setdefault("编译" if m.group(1) == "build" else "测试", []).append(s)
    key = {v: k for k, v in HEAVY_ENGINE.items()} | {"编译": "build", "测试": "test"}   # id 用英文
    for label, group, kind in (("Godot ", engines, "engine"), ("dotnet ", builds, "build")):
        for what, starts in group.items():
            out.append({"id": f"{kind}-{key[what]}", "name": label + what, "kind": kind, "state": "running", "since": iso(min(starts)),
                        "note": runtime_text(min(starts)) + (f"（{len(starts)} 个）" if len(starts) > 1 else "")})
    return out, sum(map(len, engines.values())), sum(map(len, builds.values()))


def build_block(procs, scripts, n_engine, n_build):
    held = patrol.lock_held()
    holder = None
    if held:   # 锁文件是空的，不记谁占着：取在跑的编译、引擎、门禁、截图里最久的那个
        busy = [s for s in scripts if s["kind"] in ("build", "engine", "gate") or s["id"] == "shots"]
        busy = [s for s in busy if s["state"] == "running"]
        holder = min(busy, key=lambda s: s["since"])["name"] if busy else None
    editors = sum(1 for n, _, _ in procs if n.lower().startswith("godot")) - n_engine
    return {"lock": "held" if held else "free", "holder": holder, "engines": n_engine, "builds": n_build, "editors": max(editors, 0)}


# ---------- 汇总 ----------

def part(errors, name, fn, default):
    try:
        return fn()
    except Exception as e:   # 一块失败不拖垮其余
        errors.append(f"{name}：{type(e).__name__}")
        return default


def collect():
    errors = []
    queue = part(errors, "施工队列", lambda: {r["id"]: r["what"] for r in patrol.queue_rows()}, {})
    c_sessions, c_agents = part(errors, "Codex", lambda: codex_block(queue), ([], []))
    a_sessions, a_agents = part(errors, "Claude", claude_block, ([], []))
    procs = part(errors, "进程表", patrol.processes, [])
    scripts, n_engine, n_build = part(errors, "脚本", lambda: scripts_block(procs), ([], 0, 0))
    build = part(errors, "构建锁", lambda: build_block(procs, scripts, n_engine, n_build), None)
    agents = sorted(c_agents + a_agents, key=lambda a: a["last"], reverse=True)
    agents.sort(key=lambda a: a["state"] != "running")   # 在跑的排前面，各自按最后动静新旧
    return {"sessions": a_sessions + c_sessions, "agents": agents, "scripts": scripts, "build": build, "errors": errors}


COLLECT_EVERY_S = 600  # 项目采集（collect.py）的间隔


def write_once():
    return write("auto", "live", collect(), source="live.py")


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


def main():
    if "--loop" not in sys.argv:
        write_once()
        return
    lock = single_instance()
    if lock is None:
        print("live.py --loop 已经在跑，退出")
        return
    last_collect = 0.0
    while True:
        t = time.time()
        try:
            write_once()
        except Exception as e:   # 写失败（磁盘、占用）下一轮再试
            print(f"{iso(t)} 写 live.js 失败：{type(e).__name__} {e}", file=sys.stderr)
        # 项目数据也由这个循环定时刷新：没有 Claude 会话（巡逻停了）时观察台照样更新
        if t - last_collect >= COLLECT_EVERY_S:
            last_collect = t
            try:
                import importlib
                import collect
                importlib.reload(collect)  # 采集脚本改过也立即生效，不用重启循环
                collect.main()
            except BaseException as e:  # 采集出错不影响实时数据
                if isinstance(e, KeyboardInterrupt):
                    raise
                print(f"{iso(t)} 项目采集失败：{type(e).__name__} {e}", file=sys.stderr)
        time.sleep(max(1.0, EVERY_S - (time.time() - t)))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
