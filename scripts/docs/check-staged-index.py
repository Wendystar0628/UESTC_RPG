"""只在本次提交含 docs/**/*.md 变更时，用提交索引中的文档核对两份生成索引。

用法：由 scripts/git-hooks/pre-commit 调用，或 python -I scripts/docs/check-staged-index.py。
产出：只读 Git 对象和当前 GIT_INDEX_FILE；临时文档快照退出即清理，不修改工作区或索引。
依赖：生成算法只调用 gen-index.py（PyYAML）；不运行全量文档校验。
"""
import os
from pathlib import Path
import runpy
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]


def git(*args, input=None):
    # 保留 Git 在 commit -- <paths> 时传来的临时 GIT_INDEX_FILE，不猜 .git/index。
    return subprocess.run(['git', '-C', str(ROOT), *args], input=input,
                          capture_output=True, check=True,
                          env={**os.environ, 'GIT_OPTIONAL_LOCKS': '0'}).stdout


def index_docs():
    """当前提交索引中的 Markdown 与 ROUTES.json 原始 blob；不读工作区文档。"""
    entries = []
    for record in git('ls-files', '--stage', '-z', '--', 'docs/').split(b'\0'):
        if not record:
            continue
        info, raw_path = record.split(b'\t', 1)
        name = raw_path.decode('utf-8')
        if name.startswith('docs/godot-api/'):
            continue  # 生成器也不读取生成的 API 参考。
        if not name.endswith('.md') and name != 'docs/ROUTES.json':
            continue
        mode, oid, stage = info.split()
        if stage != b'0' or mode not in (b'100644', b'100755'):
            raise ValueError('文档索引项必须是已解决的普通文件：' + name)
        entries.append((name, oid))
    raw = git('cat-file', '--batch', input=b''.join(oid + b'\n' for _, oid in entries))
    docs, offset = {}, 0
    for name, oid in entries:
        end = raw.index(b'\n', offset)
        actual, kind, size = raw[offset:end].split()
        if actual != oid or kind != b'blob':
            raise ValueError('无法读取提交中的文档：' + name)
        start = end + 1
        offset = start + int(size)
        docs[name] = raw[start:offset]
        offset += 1
    return docs


def main():
    changed = git('diff', '--cached', '--name-only', '--no-renames', '-z', '--', 'docs/')
    if not any(name.endswith(b'.md') for name in changed.split(b'\0')):
        return 0
    staged = index_docs()
    generator = runpy.run_path(str(ROOT / 'scripts/docs/gen-index.py'))
    with tempfile.TemporaryDirectory(prefix='uestc-doc-index-') as temp:
        docs_dir = Path(temp) / 'docs'
        docs_dir.mkdir()
        for name, content in staged.items():
            target = Path(temp) / name
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(content)
        expected = {
            'docs/SECTIONS.md': generator['build_sections'](str(docs_dir)),
            'docs/ROUTES.json': generator['build_routes'](generator['load'](str(docs_dir)), str(docs_dir)),
        }
    stale = [name for name, text in expected.items() if staged.get(name) != text.encode('utf-8')]
    if not stale:
        return 0
    print('拒绝提交：本次提交中的文档与生成索引不一致：' + '、'.join(stale), file=sys.stderr)
    print('运行 python -I scripts/docs/gen-index.py 后，把 docs/SECTIONS.md、docs/ROUTES.json '
          '与对应文档带进同一提交；git commit -- <paths> 也要列入这两份索引。', file=sys.stderr)
    print('只核对本次提交索引；未暂存或其他人的工作区文档不参与，检查不会 git add。', file=sys.stderr)
    return 1


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        print('提交文档索引检查失败：' + str(error), file=sys.stderr)
        if isinstance(error, subprocess.CalledProcessError):
            print(error.stderr.decode('utf-8', errors='replace').strip(), file=sys.stderr)
        sys.exit(1)
