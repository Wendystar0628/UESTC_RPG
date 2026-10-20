"""作者观察台本地静态服务、审美确认与作者留言入口，仅绑定回环地址。
用法：python -I -X utf8 dashboard/tools/serve.py [--port 8765]；打开观察台.cmd自动启动。
产出：dashboard-ack.json、Claude inbox 作者信、attention.js、feedback.js、index.js（用户操作时写入）。
"""
import argparse
import functools
import json
import os
import posixpath
import sys
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import unquote, urlsplit

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, '..', '..', '.collab', 'tools'))
import datafile
from post import locked
from monitor.view import attention


class Handler(SimpleHTTPRequestHandler):
    def translate_path(self, path):
        # 页内仍使用仓库相对链接；这些权威资料根按仓库路径公开给本机作者。
        rel = posixpath.normpath(unquote(urlsplit(path).path)).lstrip('/')
        if rel.startswith('data/'):
            parts = rel.split('/')
            allowed = len(parts) == 3 and ((parts[1] == 'auto' and parts[2] in {n + '.js' for n in (*datafile.AUTO_NAMES, 'index')}) or (parts[1] == 'curated' and parts[2] == 'notices.js') or parts[1] == 'demo')
            if not allowed:
                return os.path.join(datafile.ROOT, '.not-published')
        if rel.split('/', 1)[0] in ('docs', 'output', '.collab', 'game', 'scripts', 'pipeline'):
            root = os.path.abspath(os.path.join(datafile.ROOT, '..'))
            full = os.path.abspath(os.path.join(root, rel))
            if os.path.commonpath([root, full]) == root:
                return full
        return super().translate_path(path)

    def end_headers(self):
        self.send_header('Cache-Control', 'no-cache')
        super().end_headers()

    def answer(self, code, payload):
        body = json.dumps(payload, ensure_ascii=False).encode('utf-8')
        self.send_response(code)
        self.send_header('Content-Type', 'application/json; charset=utf-8')
        self.send_header('Content-Length', str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_POST(self):
        if self.path not in ('/api/attention/ack', '/api/feedback'):
            self.answer(404, {'ok': False, 'error': '未知操作'})
            return
        origin = self.headers.get('Origin')
        if origin and origin != 'http://' + self.headers.get('Host', ''):
            self.answer(403, {'ok': False, 'error': '只接受观察台同源操作'})
            return
        try:
            length = int(self.headers.get('Content-Length', '0'))
            if not 0 < length <= 32768:
                raise ValueError('请求体长度无效')
            request = json.loads(self.rfile.read(length))
            if not isinstance(request, dict):
                raise ValueError('请求体应是对象')
            with locked():
                if self.path == '/api/feedback':
                    at = attention.send_feedback(request.get('text'))
                    datafile.write('auto', 'feedback', attention.feedback(), source='serve.py')
                    datafile.refresh_index()
                    self.answer(200, {'ok': True, 'at': at})
                    return
                data = attention.build()
                item = next((i for i in data['items'] if i['id'] == request.get('id')), None)
                if item is None:
                    self.answer(404, {'ok': False, 'error': '过目项已不存在'})
                    return
                if request.get('revision') and request['revision'] != attention.revision(item):
                    self.answer(409, {'ok': False, 'error': '内容已变化，请刷新'})
                    return
                attention.acknowledge(item)
                item['acked'] = True
                datafile.write('auto', 'attention', data, source='serve.py')
                datafile.refresh_index()
            self.answer(200, {'ok': True})
        except (ValueError, TypeError) as error:
            self.answer(400, {'ok': False, 'error': str(error)})
        except Exception as error:
            self.answer(500, {'ok': False, 'error': str(error)})


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=8765)
    args = parser.parse_args()
    server = ThreadingHTTPServer(('127.0.0.1', args.port), functools.partial(Handler, directory=datafile.ROOT))
    print(f'观察台：http://127.0.0.1:{args.port}/', flush=True)
    server.serve_forever()


if __name__ == '__main__':
    main()
