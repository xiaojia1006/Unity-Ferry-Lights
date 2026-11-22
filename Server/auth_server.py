"""Loopback-only development account service; Python standard library only."""
import argparse
import hashlib
import hmac
import json
import re
import secrets
import sqlite3
import threading
import time
from collections import defaultdict, deque
from contextlib import contextmanager
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

ITERATIONS = 310_000
ACCOUNT = re.compile(r"[A-Za-z0-9_]{3,32}\Z")


class AccountServer(ThreadingHTTPServer):
    daemon_threads = True

    def __init__(self, address, database):
        self.database = str(database)
        Path(database).parent.mkdir(parents=True, exist_ok=True)
        with self.connect() as db:
            db.execute("CREATE TABLE IF NOT EXISTS accounts (name TEXT PRIMARY KEY COLLATE NOCASE, salt BLOB NOT NULL, digest BLOB NOT NULL, iterations INTEGER NOT NULL)")
        self.sessions = {}
        self.attempts = defaultdict(deque)
        self.lock = threading.Lock()
        super().__init__(address, Handler)

    @contextmanager
    def connect(self):
        connection = sqlite3.connect(self.database, timeout=10)
        try:
            with connection:
                yield connection
        finally:
            connection.close()

    def limited(self, address):
        now = time.monotonic()
        with self.lock:
            attempts = self.attempts[address]
            while attempts and attempts[0] < now - 60:
                attempts.popleft()
            if len(attempts) >= 20:
                return True
            attempts.append(now)
            return False

    def session(self, name):
        token = secrets.token_urlsafe(32)
        with self.lock:
            now = time.time()
            self.sessions = {key: value for key, value in self.sessions.items() if value[1] > now}
            self.sessions[token] = (name, now + 3600)
        return token


class Handler(BaseHTTPRequestHandler):
    def setup(self):
        super().setup()
        self.connection.settimeout(10)

    def log_message(self, *unused):
        pass  # Never log request bodies or credentials.

    def reply(self, status, message, **extra):
        payload = json.dumps(dict(success=status < 300, message=message, **extra), ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Cache-Control", "no-store")
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)

    def allowed(self):
        # Native Unity client uses no Origin. Do not expose this service to websites.
        host = self.headers.get("Host", "")
        return host in (f"127.0.0.1:{self.server.server_port}", f"localhost:{self.server.server_port}") and not self.headers.get("Origin")

    def do_GET(self):
        if not self.allowed():
            return self.reply(403, "请求来源不受支持")
        if self.path == "/health":
            return self.reply(200, "账号服务已启动", service="willowreach-local-auth", version=1)
        if self.path == "/me":
            token = self.headers.get("Authorization", "").removeprefix("Bearer ")
            with self.server.lock:
                session = self.server.sessions.get(token)
            if session and session[1] > time.time():
                return self.reply(200, "登录有效", account=session[0])
            return self.reply(401, "登录已失效")
        self.reply(404, "接口不存在")

    def do_POST(self):
        if not self.allowed():
            return self.reply(403, "请求来源不受支持")
        if self.path not in ("/register", "/login"):
            return self.reply(404, "接口不存在")
        if self.server.limited(self.client_address[0]):
            return self.reply(429, "操作太频繁，请一分钟后再试")
        if self.headers.get_content_type() != "application/json":
            return self.reply(415, "请求格式错误")
        try:
            length = int(self.headers.get("Content-Length", "0"))
            if not 0 < length <= 4096:
                return self.reply(413, "请求大小不受支持")
            data = json.loads(self.rfile.read(length).decode("utf-8"))
            if not isinstance(data, dict):
                raise ValueError()
            name, password = data.get("account"), data.get("password")
            if not isinstance(name, str) or not isinstance(password, str):
                raise ValueError()
            name = name.strip()
        except (ValueError, UnicodeError):
            return self.reply(400, "请求格式错误")
        if not ACCOUNT.fullmatch(name):
            return self.reply(400, "账号需为3至32位字母、数字或下划线")
        if not 8 <= len(password) <= 128 or not password.strip():
            return self.reply(400, "密码需为8至128位，不能全为空格")
        try:
            if self.path == "/register":
                salt = secrets.token_bytes(16)
                digest = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"), salt, ITERATIONS)
                try:
                    with self.server.connect() as db:
                        db.execute("INSERT INTO accounts VALUES (?, ?, ?, ?)", (name, salt, digest, ITERATIONS))
                except sqlite3.IntegrityError:
                    return self.reply(409, "该账号已存在，请直接登录")
                return self.reply(201, "注册成功，请输入密码登录", account=name)
            with self.server.connect() as db:
                row = db.execute("SELECT name, salt, digest, iterations FROM accounts WHERE name = ?", (name,)).fetchone()
            # Perform equivalent password work for unknown accounts.
            salt, expected, iterations = (row[1], row[2], row[3]) if row else (bytes(16), bytes(32), ITERATIONS)
            actual = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"), salt, iterations)
            if not hmac.compare_digest(actual, expected) or row is None:
                return self.reply(401, "账号或密码错误")
            self.reply(200, "登录成功", account=row[0], token=self.server.session(row[0]))
        except sqlite3.Error:
            self.reply(500, "账号数据库暂时不可用，请稍后重试")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=18765)
    parser.add_argument("--database", type=Path, default=Path(__file__).parent / "data" / "accounts.sqlite3")
    args = parser.parse_args()
    with AccountServer(("127.0.0.1", args.port), args.database) as server:
        print(f"Willowreach account service: http://127.0.0.1:{server.server_port}", flush=True)
        server.serve_forever()


if __name__ == "__main__":
    main()
