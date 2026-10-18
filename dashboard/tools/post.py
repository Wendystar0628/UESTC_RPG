"""观察台发布工具：给作者发通知、挂审计项、结项、删过期（规则见 WF-COMMS 作者观察台一节）。
用法：
  python -I -X utf8 dashboard/tools/post.py notice "<一句话>" [--from 名] [--level info|attention|decision] [--link 路径] [--days N] [--pin]
  python -I -X utf8 dashboard/tools/post.py audit "<标题>" --why "<为什么要作者看>" [--link 路径] [--from 名]
  python -I -X utf8 dashboard/tools/post.py done <id>      审计项改 done，通知删除
  python -I -X utf8 dashboard/tools/post.py list           列出未结项
  python -I -X utf8 dashboard/tools/post.py prune          删过期通知、结项超过 7 天的审计项
--from 缺省读环境变量 DASH_FROM。同一句话重复发是更新同一条（id 由内容算出）。
"""
import argparse
import hashlib
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from datafile import read, write  # noqa: E402

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
MAX_TEXT, MAX_WHY = 120, 200
DONE_KEEP_DAYS = 7
FMT = '%Y-%m-%dT%H:%M:%S'
LOCK = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'data', 'curated', '.post.lock')


class Reject(Exception):
    pass


def now():
    return time.strftime(FMT)


def plus_days(days):
    return time.strftime(FMT, time.localtime(time.time() + days * 86400))


def age_days(iso):
    return (time.time() - time.mktime(time.strptime(iso, FMT))) / 86400


def item_id(prefix, text):
    return prefix + hashlib.sha1(text.encode('utf-8')).hexdigest()[:6]


def check_len(label, text, limit):
    if len(text) > limit:
        raise Reject(f'{label}太长（{len(text)} 字，上限 {limit}）：改成一句话再发')


def check_link(link):
    if not link:
        return None
    rel = link.replace('\\', '/')
    full = os.path.normpath(os.path.join(REPO, rel))
    if os.path.isabs(rel) or rel.startswith('..') or not full.startswith(os.path.normpath(REPO)) or not os.path.exists(full):
        raise Reject(f'link 必须是存在的仓库相对路径：{link}')
    return rel


def sender(args):
    name = args.sender or os.environ.get('DASH_FROM')
    if not name:
        raise Reject('缺 --from（或环境变量 DASH_FROM）：写主会话、Codex 主线或子代理名')
    return name


class locked:
    """两个会话同时发时排队：独占创建锁文件，最多等 5 秒，超过 30 秒的旧锁直接清掉。"""

    def __enter__(self):
        os.makedirs(os.path.dirname(LOCK), exist_ok=True)
        for _ in range(100):
            try:
                os.close(os.open(LOCK, os.O_CREAT | os.O_EXCL))
                return self
            except FileExistsError:
                try:
                    if time.time() - os.path.getmtime(LOCK) > 30:
                        os.remove(LOCK)
                except OSError:
                    pass
                time.sleep(0.05)
        raise Reject('数据文件被占用，稍后再试')

    def __exit__(self, *_):
        try:
            os.remove(LOCK)
        except OSError:
            pass


def load(name):
    return (read('curated', name) or {}).get('items', [])


def save(name, items, who):
    write('curated', name, {'items': items}, source='post.py:' + who)


def cmd_notice(args):
    who, text = sender(args), args.text.strip()
    check_len('通知', text, MAX_TEXT)
    link = check_link(args.link)
    nid = item_id('n', text)
    note = {'id': nid, 'at': now(), 'from': who, 'level': args.level, 'text': text, 'link': link,
            'expires': None if args.pin else plus_days(args.days), 'pinned': bool(args.pin)}
    with locked():
        items = [i for i in load('notices') if i['id'] != nid]
        save('notices', [note] + items, who)
    return f'通知 {nid} 已发（{args.level}，' + ('置顶' if args.pin else f'{args.days} 天后过期') + '）'


def cmd_audit(args):
    who, title, why = sender(args), args.title.strip(), (args.why or '').strip()
    if not why:
        raise Reject('审计项必须写 --why：为什么要作者看')
    check_len('标题', title, MAX_TEXT)
    check_len('why', why, MAX_WHY)
    link = check_link(args.link)
    aid = item_id('a', title)
    item = {'id': aid, 'at': now(), 'from': who, 'title': title, 'why': why, 'link': link, 'status': 'open'}
    with locked():
        items = [i for i in load('audit') if i['id'] != aid]
        save('audit', [item] + items, who)
    return f'审计项 {aid} 已挂'


def cmd_done(args):
    who = os.environ.get('DASH_FROM') or 'post.py'
    with locked():
        audits, notes = load('audit'), load('notices')
        hit_a = [i for i in audits if i['id'] == args.id]
        hit_n = [i for i in notes if i['id'] == args.id]
        if not hit_a and not hit_n:
            raise Reject(f'找不到 {args.id}：用 list 看现有的 id')
        if hit_a:
            for i in hit_a:
                i['status'], i['done_at'] = 'done', now()
            save('audit', audits, who)
            return f'审计项 {args.id} 已结项'
        save('notices', [i for i in notes if i['id'] != args.id], who)
        return f'通知 {args.id} 已删除'


def cmd_list(_args):
    lines = []
    for i in load('audit'):
        if i['status'] == 'open':
            lines.append(f"审计 {i['id']} [{i['from']}] {i['title']}（{i['at'][:10]}）")
    for i in load('notices'):
        lines.append(f"通知 {i['id']} [{i['from']}/{i['level']}] {i['text']}（{i['at'][:10]}" + ('，置顶' if i.get('pinned') else '') + '）')
    return '\n'.join(lines) if lines else '没有未结项'


def prune_all():
    """删过期通知与结项超过 7 天的审计项；返回 (删通知数, 删审计项数)。collect.py 也可调用。"""
    with locked():
        notes, audits = load('notices'), load('audit')
        keep_n = [i for i in notes if i.get('pinned') or not i.get('expires') or i['expires'] > now()]
        keep_a = [i for i in audits if not (i['status'] == 'done' and age_days(i.get('done_at') or i['at']) > DONE_KEEP_DAYS)]
        if len(keep_n) != len(notes):
            save('notices', keep_n, 'post.py')
        if len(keep_a) != len(audits):
            save('audit', keep_a, 'post.py')
    return len(notes) - len(keep_n), len(audits) - len(keep_a)


def cmd_prune(_args):
    n, a = prune_all()
    return f'删过期通知 {n} 条、结项超过 {DONE_KEEP_DAYS} 天的审计项 {a} 条'


def main(argv=None):
    ap = argparse.ArgumentParser(description='观察台发布工具')
    sub = ap.add_subparsers(dest='cmd', required=True)
    n = sub.add_parser('notice')
    n.add_argument('text')
    n.add_argument('--level', choices=('info', 'attention', 'decision'), default='info')
    n.add_argument('--days', type=int, default=3)
    n.add_argument('--pin', action='store_true')
    a = sub.add_parser('audit')
    a.add_argument('title')
    a.add_argument('--why')
    for p in (n, a):
        p.add_argument('--from', dest='sender')
        p.add_argument('--link')
    d = sub.add_parser('done')
    d.add_argument('id')
    sub.add_parser('list')
    sub.add_parser('prune')
    args = ap.parse_args(argv)
    try:
        print({'notice': cmd_notice, 'audit': cmd_audit, 'done': cmd_done, 'list': cmd_list, 'prune': cmd_prune}[args.cmd](args))
    except Reject as e:
        print('拒绝：' + str(e), file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    sys.stderr.reconfigure(encoding='utf-8')
    sys.exit(main())
