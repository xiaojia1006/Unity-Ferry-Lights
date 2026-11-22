import http.client
import json
import sqlite3
import tempfile
import threading
import unittest
from contextlib import closing
from pathlib import Path
from auth_server import AccountServer


class AccountsTest(unittest.TestCase):
    def setUp(self):
        temp_root = Path(__file__).resolve().parents[1] / "Temp"
        temp_root.mkdir(exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=temp_root)
        self.database = Path(self.temp.name) / "accounts.sqlite3"
        self.start()

    def start(self):
        self.server = AccountServer(("127.0.0.1", 0), self.database)
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()

    def stop(self):
        self.server.shutdown()
        self.server.server_close()
        self.thread.join()

    def tearDown(self):
        self.stop()
        self.temp.cleanup()

    def call(self, path, account="tester", password="Example-123", headers=None, raw=None):
        connection = http.client.HTTPConnection("127.0.0.1", self.server.server_port, timeout=5)
        data = raw if raw is not None else json.dumps(dict(account=account, password=password))
        connection.request("POST", path, data, headers or {"Content-Type": "application/json"})
        response = connection.getresponse()
        result = response.status, json.loads(response.read())
        connection.close()
        return result

    def test_registration_login_and_session(self):
        self.assertEqual(self.call("/register")[0], 201)
        status, result = self.call("/login")
        self.assertEqual(status, 200)
        self.assertTrue(result["success"])
        self.assertTrue(result["token"])
        connection = http.client.HTTPConnection("127.0.0.1", self.server.server_port)
        connection.request("GET", "/me", headers={"Authorization": "Bearer " + result["token"]})
        response = connection.getresponse()
        self.assertEqual(response.status, 200)
        self.assertEqual(json.loads(response.read())["account"], "tester")
        connection.close()

    def test_persistence_hash_and_no_duplicate(self):
        self.assertEqual(self.call("/register")[0], 201)
        self.assertEqual(self.call("/register", "TESTER")[0], 409)
        with closing(sqlite3.connect(self.database)) as db:
            salt, digest = db.execute("SELECT salt, digest FROM accounts").fetchone()
            self.assertEqual(len(salt), 16)
            self.assertEqual(len(digest), 32)
            self.assertNotEqual(digest, b"Example-123")
        self.stop()
        self.start()
        self.assertEqual(self.call("/login", "TESTER")[0], 200)

    def test_wrong_password_and_missing_account(self):
        self.call("/register")
        wrong = self.call("/login", password="incorrect-pass")
        missing = self.call("/login", account="missing")
        self.assertEqual(wrong, missing)
        self.assertEqual(wrong[0], 401)
        self.assertNotIn("token", wrong[1])

    def test_validation_and_browser_origin(self):
        for account, password in [("a", "Example-123"), ("a' OR 1=1", "Example-123"), ("tester", "short"), ("tester", " " * 8), ("tester", "x" * 129)]:
            self.assertEqual(self.call("/register", account, password)[0], 400)
        self.assertEqual(self.call("/login", raw="[]")[0], 400)
        self.assertEqual(self.call("/login", raw="{bad")[0], 400)
        self.assertEqual(self.call("/login", raw=" " * 4097)[0], 413)
        self.assertEqual(self.call("/register", headers={"Content-Type": "application/json", "Origin": "https://example.com"})[0], 403)

    def test_rate_limit(self):
        for _ in range(20):
            self.assertEqual(self.call("/login", account="a")[0], 400)
        self.assertEqual(self.call("/login")[0], 429)


if __name__ == "__main__":
    unittest.main(verbosity=2)
