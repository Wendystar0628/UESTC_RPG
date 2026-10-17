"""统计 docs/ 里每份文档的字数，列出超体量上限的（上限见 WF-DOC §5，这里按它实现）。

用法：
  python -I -X utf8 scripts/docs/doc_size.py            每份文档一行，从大到小，超限的标 ✗
  python -I -X utf8 scripts/docs/doc_size.py --over     只列超限项；有超限时退出码为 1
  python -I -X utf8 scripts/docs/doc_size.py --json --files docs/x.md ...   只量给定文件，输出超限项的 JSON（提交后钩子用）

字数 = 去掉文件头后的字符数；一节 = 从一个标题到下一个标题（不认代码块里的 #）。
"""
import glob
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
D = os.path.join(ROOT, 'docs')
LIMIT = {'doc': 12000, 'map': 16000, 's0': 1000, 'hooks': 900, 'section': 2000, 'core0': 1800, 'core1': 3000, 'entry': 1200}
REGISTRY = {'CORE-DECISIONS', 'CORE-GLOSSARY', 'ENG-WP'}  # 登记类（连同它们的子文档）：每条一行，不量全文与单节


def registry(code):
    return any(code == r or code.startswith(r + '-') for r in REGISTRY)


def docs_in(paths=None):
    if paths:
        files = [os.path.join(ROOT, p) if not os.path.isabs(p) else p for p in paths]
    else:
        files = glob.glob(os.path.join(D, '**', '*.md'), recursive=True)
    out = []
    for p in files:
        rel = os.path.relpath(p, D).replace(chr(92), '/')
        if rel.startswith(('godot-api/', '..')) or rel == 'SECTIONS.md' or not os.path.isfile(p):
            continue
        out.append(p)
    return sorted(out)


def measure(path):
    text = open(path, encoding='utf-8').read().replace('\r\n', '\n')
    m = re.match(r'^---\n(.*?)\n---\n', text, re.S)
    code = re.search(r'^id:\s*(\S+)', m.group(1), re.M).group(1) if m else os.path.basename(path)
    body = text[m.end():] if m else text
    sections, cur, fence = [], ['', []], False
    for line in body.split('\n'):
        if line.lstrip().startswith('```'):
            fence = not fence
        h = None if fence else re.match(r'^(#{2,4}) (.*)$', line)
        if h:
            sections.append(cur)
            cur = [h.group(2).strip(), []]
        else:
            cur[1].append(line)
    sections.append(cur)
    secs = [(title, len('\n'.join(lines))) for title, lines in sections if title]
    return code, len(body), secs


def check(path):
    """返回 (编码, 正文字数, [(哪一处, 字数, 上限)])"""
    code, total, secs = measure(path)
    over = []
    if not registry(code):
        cap = LIMIT['map'] if code.startswith('MAP-') else LIMIT['doc']
        if total > cap:
            over.append(('全文', total, cap))
    for title, n in secs:
        if title.startswith('0 '):
            cap = LIMIT['core0'] if code == 'CORE-INDEX' else LIMIT['s0']
        elif '接入要点' in title:
            cap = LIMIT['hooks']
        elif code == 'CORE-INDEX' and title.startswith('1 '):
            cap = LIMIT['core1']
        elif code == 'WF-ENTRY' and re.match(r'^[1-9] ', title):
            cap = LIMIT['entry']
        elif registry(code):
            continue
        else:
            cap = LIMIT['section']
        if n > cap:
            over.append((title, n, cap))
    return code, total, over


def main(argv):
    files = argv[argv.index('--files') + 1:] if '--files' in argv else None
    results = [check(p) + (os.path.relpath(p, ROOT).replace(chr(92), '/'),) for p in docs_in(files)]
    bad = [r for r in results if r[2]]
    if '--json' in argv:
        print(json.dumps([{'path': p, 'code': c, 'total': t, 'over': [list(o) for o in ov]} for c, t, ov, p in bad], ensure_ascii=False))
        return 0
    if '--over' not in argv:
        for c, t, ov, p in sorted(results, key=lambda r: -r[1]):
            print('%s %-20s %7d 字  %s' % ('✗' if ov else ' ', c, t, p))
        print('共 %d 份，%d 字；超限 %d 份' % (len(results), sum(r[1] for r in results), len(bad)))
    for c, t, ov, p in bad:
        for where, n, cap in ov:
            print('超限  %-20s %s：%d 字（上限 %d）' % (c, where, n, cap))
    return 1 if bad and '--over' in argv else 0


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    sys.exit(main(sys.argv[1:]))
