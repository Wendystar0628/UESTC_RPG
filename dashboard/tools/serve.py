"""作者观察台本地静态服务、审美确认与作者留言入口，仅绑定回环地址。
用法：python -I -X utf8 dashboard/tools/serve.py [--port 8765]；打开观察台.cmd自动启动。
产出：通过 attention 事务发布 dashboard-ack.json、显式作者信及 attention.js、feedback.js。
"""
import argparse
import functools
import json
import os
import sys
import threading
import time
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, unquote, urlsplit
from watchdog.events import FileSystemEventHandler
from watchdog.observers.read_directory_changes import WindowsApiObserver

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, '..', '..', '.collab', 'tools'))
import datafile
from monitor.view import attention, docs, health
from monitor.read import repo
from monitor import util as U


def checked_payload(publication):
    """HTTP 只包装唯一 view 判鲜结果；内部 publication 保持时间戳表。"""
    stamps = {name: publication['checked'].get(name) for name in datafile.AUTO_NAMES}
    fresh = health.freshness(stamps, datafile.AUTO_NAMES, time.time())
    return {name: {'at': stamps[name], 'stale': fresh['stale'][name], 'ratio': fresh['ratio'][name]}
            for name in datafile.AUTO_NAMES}


class Publications(FileSystemEventHandler):
    """只通知 metadata 已变化；不采集、不保存事件历史、不持发布锁等客户端。"""
    def __init__(self):
        self.condition = threading.Condition()
        self.generation = 0
        self.stopped = False
        self.path = os.path.normcase(os.path.abspath(os.path.join(U.STATE, 'dashboard-publication.json')))

    def on_any_event(self, event):
        if event.is_directory or event.event_type not in ('created', 'modified', 'deleted', 'moved'):
            return
        if any(os.path.normcase(os.path.abspath(path)) == self.path
               for path in (event.src_path, getattr(event, 'dest_path', '')) if path):
            with self.condition:
                self.generation += 1
                self.condition.notify_all()

    def mark(self):
        with self.condition:
            return self.generation

    def wait(self, generation, timeout):
        with self.condition:
            self.condition.wait_for(lambda: self.stopped or self.generation != generation, timeout)
            return self.generation, self.stopped

    def stop(self):
        with self.condition:
            self.stopped = True
            self.condition.notify_all()


class Handler(SimpleHTTPRequestHandler):
    def translate_path(self, path):
        rel = unquote(urlsplit(path).path).lstrip('/') or 'index.html'
        denied = os.path.join(datafile.ROOT, '.not-published')
        if rel.startswith('data/'):
            parts = rel.split('/')
            allowed = len(parts) == 3 and parts[1] == 'auto' and parts[2] in {n + '.js' for n in datafile.AUTO_NAMES}
            return datafile.path('auto', parts[2][:-3]) if allowed else denied
        # 文档、明确的总览别名、观察台公共资源共用 post 的文件解析；不映射整棵仓库目录。
        return repo.dashboard_file(rel) or repo.dashboard_file('dashboard/' + rel) or denied

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

    def do_GET(self):
        url = urlsplit(self.path)
        if url.path == '/api/data-names.js':
            body = ('window.DashDataNames = ' + json.dumps(list(datafile.AUTO_NAMES)) + ';\n').encode('utf-8')
            self.send_response(200)
            self.send_header('Content-Type', 'text/javascript; charset=utf-8')
            self.send_header('Cache-Control', 'no-store')
            self.send_header('Content-Length', str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        if url.path in ('/api/doc', '/api/doc/search'):
            key = 'code' if url.path == '/api/doc' else 'q'
            query = parse_qs(url.query, keep_blank_values=True)
            if len(query.get(key, [])) != 1:
                self.answer(400, {'ok': False, 'error': '请提供一个文档编码' if key == 'code' else '请提供一个搜索词'})
                return
            try:
                payload = docs.document(query[key][0]) if key == 'code' else docs.search(query[key][0])
                self.answer(200, payload)
            except docs.UnknownDocument:
                self.answer(404, {'ok': False, 'error': '没有这份文档'})
            except Exception as error:
                self.answer(500, {'ok': False, 'error': '读取文档失败：' + ' '.join(str(error).split())})
            return
        if urlsplit(self.path).path != '/api/events':
            return super().do_GET()
        notifications = self.server.publications
        generation = notifications.mark()
        self.send_response(200)
        self.send_header('Content-Type', 'text/event-stream; charset=utf-8')
        self.send_header('Connection', 'keep-alive')
        self.end_headers()
        self.connection.settimeout(10)
        self.close_connection = True
        def emit(event, payload):
            self.wfile.write(('event: ' + event + '\ndata: ' + json.dumps(payload, ensure_ascii=False) + '\n\n').encode('utf-8'))
            self.wfile.flush()
        try:
            self.wfile.write(b'retry: 3000\n\n')
            self.wfile.flush()
            publication = datafile.publication()
            while 'app' not in publication['versions']:
                self.wfile.write(': 等待观察台首次发布\n\n'.encode('utf-8'))
                self.wfile.flush()
                generation, stopped = notifications.wait(generation, 15)
                if stopped:
                    return
                publication = datafile.publication()
            emit('versions', {'at': publication['at'], 'versions': publication['versions'],
                              'checked': checked_payload(publication), 'full': True})
            versions = publication['versions']
            heartbeat = time.monotonic() + 15
            while True:
                generation, stopped = notifications.wait(generation, max(0, heartbeat - time.monotonic()))
                if stopped:
                    return
                publication = datafile.publication()
                if 'app' not in publication['versions']:
                    raise ValueError('观察台发布状态缺少界面版本')
                changed = {name: value for name, value in publication['versions'].items() if versions.get(name) != value}
                if changed:
                    emit('versions', {'at': publication['at'], 'full': False, 'versions': changed,
                                      'checked': checked_payload(publication)})
                versions = publication['versions']
                if time.monotonic() >= heartbeat:
                    emit('checked', {'at': U.iso(time.time()), 'checked': checked_payload(publication)})
                    heartbeat = time.monotonic() + 15
        except (BrokenPipeError, ConnectionResetError, TimeoutError):
            return
        except Exception as error:
            self.log_error('推送失败：%s: %s', type(error).__name__, error)

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
            if self.path == '/api/feedback':
                at = attention.submit_feedback(request.get('text'), datafile.write)
                self.answer(200, {'ok': True, 'at': at})
                return
            attention.acknowledge(request.get('id'), request.get('revision'), datafile.write)
            self.answer(200, {'ok': True})
        except attention.RequestError as error:
            self.answer(error.status, {'ok': False, 'error': str(error)})
        except (ValueError, TypeError) as error:
            self.answer(400, {'ok': False, 'error': str(error)})
        except Exception as error:
            self.answer(500, {'ok': False, 'error': str(error)})


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=8765)
    args = parser.parse_args()
    server = ThreadingHTTPServer(('127.0.0.1', args.port), functools.partial(Handler, directory=datafile.ROOT))
    notifications = Publications()
    observer = WindowsApiObserver()
    observer.schedule(notifications, U.STATE, recursive=False)
    server.publications = notifications
    try:
        observer.start()
        print(f'观察台：http://127.0.0.1:{args.port}/', flush=True)
        server.serve_forever()
    finally:
        notifications.stop()
        observer.stop()
        if observer.is_alive():
            observer.join()
        server.server_close()


if __name__ == '__main__':
    main()
