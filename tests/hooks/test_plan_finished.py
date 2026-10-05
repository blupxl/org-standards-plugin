import importlib.util
import json
import os
import time
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

HOOK = Path(__file__).resolve().parents[2] / "plugins" / "my-company" / "hooks" / "plan_finished.py"
spec = importlib.util.spec_from_file_location("plan_finished", HOOK)
plan_finished = importlib.util.module_from_spec(spec)
spec.loader.exec_module(plan_finished)

PLAN = "# Customer cache\n\n1. Add StackExchange.Redis.\n2. Cache key cust_{id}.\n"


class PlanFinishedTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.folder = Path(temporary.name)
        self.notes = self.folder / "notes"
        self.plan = self.folder / "repo" / "docs" / "superpowers" / "plans" / "2026-10-05-customer-cache.md"
        self.plan.parent.mkdir(parents=True)
        self.plan.write_text(PLAN, encoding="utf-8")

    def written(self, path=None, session="session-1"):
        return {"session_id": session, "tool_name": "Write", "tool_input": {"file_path": str(path or self.plan)}}

    def decide(self, event, now=1000.0):
        return plan_finished.decide(event, self.notes, now)

    def test_a_new_plan_file_pauses_with_a_reason(self):
        decision = self.decide(self.written())
        self.assertEqual("block", decision["decision"])
        self.assertIn("plan check", decision["reason"])
        self.assertIn(self.plan.name, decision["reason"])

    def test_the_same_plan_pauses_only_once(self):
        self.decide(self.written())
        self.assertIsNone(self.decide(self.written()))

    def test_a_changed_plan_pauses_again(self):
        self.decide(self.written())
        self.plan.write_text(PLAN + "3. Add an endpoint.\n", encoding="utf-8")
        self.assertIsNotNone(self.decide(self.written()))

    def test_a_stamped_plan_passes_until_it_is_edited(self):
        subprocess.run([sys.executable, str(HOOK), "--stamp", str(self.plan)], check=True,
                       env={**os.environ, "ACME_PLAN_CHECK_NOTES": str(self.notes)})
        self.assertIsNone(self.decide(self.written()))
        self.plan.write_text(self.plan.read_text(encoding="utf-8").replace("cust_{id}", "orders:customer:{id}:v1"), encoding="utf-8")
        self.assertIsNotNone(self.decide(self.written(session="session-2")))

    def test_it_is_quiet_while_the_check_is_working_on_the_file(self):
        subprocess.run([sys.executable, str(HOOK), "--begin", str(self.plan)], check=True,
                       env={**__import__("os").environ, "ACME_PLAN_CHECK_NOTES": str(self.notes)})
        self.assertIsNone(self.decide(self.written(), now=time.time()))

    def test_the_quiet_period_expires(self):
        subprocess.run([sys.executable, str(HOOK), "--begin", str(self.plan)], check=True,
                       env={**os.environ, "ACME_PLAN_CHECK_NOTES": str(self.notes)})
        later = time.time() + plan_finished.QUIET_SECONDS + 60
        self.assertIsNotNone(self.decide(self.written(), now=later))

    def test_stamping_keeps_lf_line_endings(self):
        self.plan.write_bytes(PLAN.encode("utf-8"))
        subprocess.run([sys.executable, str(HOOK), "--stamp", str(self.plan)], check=True,
                       env={**os.environ, "ACME_PLAN_CHECK_NOTES": str(self.notes)})
        self.assertNotIn(b"\r", self.plan.read_bytes())

    def test_a_crlf_plan_mode_plan_ending_with_its_marker_passes(self):
        stamped = (PLAN + "\n" + plan_finished.marker_line(PLAN) + "\n").replace("\n", "\r\n")
        event = {"session_id": "session-1", "tool_name": "ExitPlanMode", "tool_input": {"plan": stamped}}
        self.assertIsNone(self.decide(event))

    def test_hook_mode_reads_utf8_paths(self):
        plan = self.folder / "Josée" / "docs" / "plans" / "p.md"
        plan.parent.mkdir(parents=True)
        plan.write_text(PLAN, encoding="utf-8")
        payload = json.dumps(self.written(plan), ensure_ascii=False).encode("utf-8")
        result = subprocess.run([sys.executable, str(HOOK)], input=payload, capture_output=True,
                                env={**os.environ, "ACME_PLAN_CHECK_NOTES": str(self.notes)})
        self.assertEqual("block", json.loads(result.stdout.decode("utf-8"))["decision"])

    def test_files_outside_a_plans_folder_are_ignored(self):
        other = self.folder / "repo" / "README.md"
        other.write_text("hello", encoding="utf-8")
        self.assertIsNone(self.decide(self.written(other)))

    def test_a_spec_that_holds_the_plan_pauses(self):
        spec = self.folder / "repo" / "docs" / "superpowers" / "specs" / "2026-10-05-customer-cache-design.md"
        spec.parent.mkdir(parents=True)
        spec.write_text(PLAN, encoding="utf-8")
        self.assertEqual("block", self.decide(self.written(spec))["decision"])

    def test_a_stamped_spec_passes(self):
        spec = self.folder / "repo" / "docs" / "superpowers" / "specs" / "2026-10-05-customer-cache-design.md"
        spec.parent.mkdir(parents=True)
        spec.write_text(PLAN, encoding="utf-8")
        subprocess.run([sys.executable, str(HOOK), "--stamp", str(spec)], check=True,
                       env={**os.environ, "ACME_PLAN_CHECK_NOTES": str(self.notes)})
        self.assertIsNone(self.decide(self.written(spec)))

    def test_plan_mode_files_are_left_to_exit_plan_mode(self):
        self.assertFalse(plan_finished.is_plan_file(str(Path.home() / ".claude" / "plans" / "brave-otter.md")))
        self.assertFalse(plan_finished.is_plan_file(str(Path.home() / ".claude" / "specs" / "x.md")))

    def test_only_markdown_under_specs_counts(self):
        self.assertTrue(plan_finished.is_plan_file(r"C:\repo\docs\superpowers\specs\x.md"))
        self.assertFalse(plan_finished.is_plan_file(r"C:\repo\docs\superpowers\specs\x.txt"))

    def test_windows_paths_are_plans_too(self):
        self.assertTrue(plan_finished.is_plan_file(r"C:\repo\docs\superpowers\plans\x.md"))
        self.assertFalse(plan_finished.is_plan_file(r"C:\repo\docs\superpowers\plans\x.txt"))

    def test_leaving_plan_mode_is_denied_once_with_a_reason(self):
        event = {"session_id": "session-1", "tool_name": "ExitPlanMode", "tool_input": {"plan": PLAN}}
        decision = self.decide(event)
        output = decision["hookSpecificOutput"]
        self.assertEqual("deny", output["permissionDecision"])
        self.assertIn("plan check", output["permissionDecisionReason"])
        self.assertIsNone(self.decide(event))

    def test_a_plan_mode_plan_ending_with_its_marker_passes(self):
        stamped = PLAN + "\n" + plan_finished.marker_line(PLAN) + "\n"
        event = {"session_id": "session-1", "tool_name": "ExitPlanMode", "tool_input": {"plan": stamped}}
        self.assertIsNone(self.decide(event))

    def test_it_stops_pausing_after_three_tries_on_one_plan(self):
        for attempt in range(3):
            self.plan.write_text(PLAN + f"{attempt}\n", encoding="utf-8")
            self.assertIsNotNone(self.decide(self.written()))
        self.plan.write_text(PLAN + "3\n", encoding="utf-8")
        self.assertIsNone(self.decide(self.written()))

    def run_hook(self, *arguments, stdin=None):
        return subprocess.run([sys.executable, str(HOOK), *arguments], input=stdin, capture_output=True, check=True,
                              env={**os.environ, "ACME_PLAN_CHECK_NOTES": str(self.notes)})

    def test_a_stamp_resets_the_pause_limit_for_that_plan(self):
        for attempt in range(4):
            self.plan.write_text(PLAN + f"{attempt}\n", encoding="utf-8")
            self.assertIsNotNone(self.decide(self.written()), f"edit {attempt} should pause")
            self.run_hook("--stamp", str(self.plan))
            self.assertIsNone(self.decide(self.written()))

    def test_a_checked_plan_resets_the_pause_limit(self):
        for attempt in range(4):
            text = PLAN + f"{attempt}\n"
            self.plan.write_text(text, encoding="utf-8")
            self.assertIsNotNone(self.decide(self.written()), f"edit {attempt} should pause")
            self.plan.write_text(text + "\n" + plan_finished.marker_line(text) + "\n", encoding="utf-8")
            self.assertIsNone(self.decide(self.written()))

    def test_a_checked_plan_mode_plan_resets_its_pause_limit(self):
        for attempt in range(4):
            text = PLAN + f"{attempt}\n"
            unchecked = {"session_id": "session-1", "tool_name": "ExitPlanMode", "tool_input": {"plan": text}}
            checked = {"session_id": "session-1", "tool_name": "ExitPlanMode",
                       "tool_input": {"plan": text + "\n" + plan_finished.marker_line(text) + "\n"}}
            self.assertIsNotNone(self.decide(unchecked), f"plan {attempt} should pause")
            self.assertIsNone(self.decide(checked))

    def test_the_limit_counts_each_plan_file_separately(self):
        other = self.plan.parent / "other.md"
        for attempt in range(3):
            self.plan.write_text(PLAN + f"{attempt}\n", encoding="utf-8")
            self.decide(self.written())
        other.write_text(PLAN + "other\n", encoding="utf-8")
        self.assertIsNotNone(self.decide(self.written(other)))

    def test_end_clears_the_quiet_note_without_stamping(self):
        self.run_hook("--begin", str(self.plan))
        self.run_hook("--end", str(self.plan))
        self.assertEqual(PLAN, self.plan.read_text(encoding="utf-8"))
        self.assertFalse(plan_finished.is_being_checked(str(self.plan), self.notes, time.time()))

    def test_ticking_checkboxes_keeps_the_fingerprint(self):
        plan = "# Plan\n\n- [ ] Add the cache.\n  - [ ] Write the test.\n* [ ] Ship it.\n"
        ticked = plan.replace("- [ ] Add", "- [x] Add").replace("- [ ] Write", "- [X] Write").replace("* [ ]", "* [x]")
        self.assertEqual(plan_finished.fingerprint(plan), plan_finished.fingerprint(ticked))
        self.assertNotEqual(plan_finished.fingerprint(plan), plan_finished.fingerprint(plan.replace("cache", "queue")))
        self.assertNotEqual(plan_finished.fingerprint("- [ ] a"), plan_finished.fingerprint("* [ ] a"))

    def test_the_fingerprint_ignores_a_byte_order_mark(self):
        self.assertEqual(plan_finished.fingerprint(PLAN), plan_finished.fingerprint("﻿" + PLAN))
        printed = self.run_hook("--fingerprint", stdin=b"\xef\xbb\xbf" + PLAN.encode("utf-8")).stdout.decode("utf-8").strip()
        self.assertEqual(plan_finished.marker_line(PLAN), printed)

    def test_a_stamped_plan_with_a_byte_order_mark_passes(self):
        self.plan.write_bytes(b"\xef\xbb\xbf" + PLAN.encode("utf-8"))
        self.run_hook("--stamp", str(self.plan))
        self.assertIsNone(self.decide(self.written()))

    def test_messages_name_this_plugin(self):
        self.assertNotIn("acme", plan_finished.FILE_REASON)
        self.assertNotIn("acme", plan_finished.PLAN_MODE_REASON)
        self.assertIn("this plugin's plan-check skill", plan_finished.FILE_REASON)

    def test_a_missing_file_is_ignored(self):
        self.assertIsNone(self.decide(self.written(self.plan.parent / "gone.md")))

    def test_bad_input_prints_nothing_and_exits_cleanly(self):
        for stdin in ["", "not json", json.dumps({"tool_name": "Write"}), json.dumps([1, 2])]:
            result = subprocess.run([sys.executable, str(HOOK)], input=stdin, capture_output=True, text=True)
            self.assertEqual(0, result.returncode)
            self.assertEqual("", result.stdout)

    def test_hook_mode_prints_the_decision_as_json(self):
        result = subprocess.run([sys.executable, str(HOOK)], input=json.dumps(self.written()), capture_output=True, text=True,
                                env={**__import__("os").environ, "ACME_PLAN_CHECK_NOTES": str(self.notes)})
        self.assertEqual(0, result.returncode)
        self.assertEqual("block", json.loads(result.stdout)["decision"])

    def test_the_fingerprint_ignores_whitespace_and_the_marker(self):
        self.assertEqual(plan_finished.fingerprint("a  b\r\nc"), plan_finished.fingerprint("a b\nc\n" + plan_finished.marker_line("a b c")))


if __name__ == "__main__":
    unittest.main()
