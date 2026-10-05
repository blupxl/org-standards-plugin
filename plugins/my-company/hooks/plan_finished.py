"""Plan-finished hook for this plugin.

Claude Code runs it after a file is written (PostToolUse on Write, Edit and MultiEdit) and before
plan mode ends (PreToolUse on ExitPlanMode). It answers one question: is this a finished plan that
hasn't been checked against the standards? If so, it pauses once and asks Claude to run the plan
check. It makes no network calls and never judges the plan; the plan-check skill does that.

    python plan_finished.py                 hook mode: the event JSON on stdin
    python plan_finished.py --begin FILE    the plan check is working on FILE: stay quiet for it
    python plan_finished.py --stamp FILE    the plan check finished FILE: append its marker
    python plan_finished.py --end FILE      the plan check stopped without checking FILE: stop staying quiet
    python plan_finished.py --fingerprint   print the marker line for the plan text on stdin

Notes (which plans were paused, and files being checked) live in the system temp folder, or in
ACME_PLAN_CHECK_NOTES when set. Any error means "say nothing": the hook never blocks work.

The hook pauses each version of a plan once per session. As a guard against loops, it also stops
after MAX_PAUSES pauses in a row at one place (a plan file, or plan mode) without a checked plan in
between; a checked plan or a stamp starts the count again.
"""

import hashlib
import json
import os
import re
import sys
import tempfile
import time
from pathlib import Path, PurePosixPath

MARKER = re.compile(r"^<!-- standards-check: ([0-9a-f]{12}) -->[ \t\r]*$", re.MULTILINE)
QUIET_SECONDS = 30 * 60
MAX_PAUSES = 3
PLAN_MODE = "plan mode"
CHECKBOX = re.compile(r"^([ \t]*(?:[-*+]|\d+[.)])[ \t]+)\[[ xX]\]", re.MULTILINE)
FILE_TOOLS = {"Write", "Edit", "MultiEdit"}

FILE_REASON = (
    "This plan hasn't been checked against the company's standards: {path}. Before implementing it, "
    "run the plan check (this plugin's plan-check skill) on it. The check changes the plan only "
    "with the user's approval, then planning carries on as before."
)
PLAN_MODE_REASON = (
    "Before leaving plan mode, run the plan check on this plan: check it against the company's standards "
    "with this plugin's plan-check skill. Then present the adjusted plan with ExitPlanMode again, ending with the marker "
    "line the check gives you."
)


def notes_folder() -> Path:
    return Path(os.environ.get("ACME_PLAN_CHECK_NOTES") or Path(tempfile.gettempdir()) / "acme-plan-check")


def fingerprint(text: str) -> str:
    """Twelve hex characters for the plan's content, ignoring whitespace, a byte order mark, any
    marker line, and whether checkboxes are ticked (ticking off steps doesn't change the plan)."""
    text = MARKER.sub("", text.replace("\ufeff", ""))
    normalized = " ".join(CHECKBOX.sub(r"\1[ ]", text).split())
    return hashlib.sha256(normalized.encode("utf-8")).hexdigest()[:12]


def marker_line(text: str) -> str:
    return f"<!-- standards-check: {fingerprint(text)} -->"


def is_checked(text: str) -> bool:
    match = MARKER.search(text)
    return match is not None and match.group(1) == fingerprint(text)


def is_plan_file(path: str) -> bool:
    """Markdown under a folder named plans, except plan mode's own (.claude/plans)."""
    parts = [part.lower() for part in PurePosixPath(path.replace("\\", "/")).parts]
    if not parts or not parts[-1].endswith(".md") or "plans" not in parts[:-1]:
        return False
    folder = len(parts) - 2 - parts[:-1][::-1].index("plans")
    return not (folder > 0 and parts[folder - 1] == ".claude")


def file_key(path: str) -> str:
    return hashlib.sha1(str(Path(path).resolve()).lower().encode("utf-8")).hexdigest()


def is_being_checked(path: str, notes: Path, now: float) -> bool:
    note = notes / "active" / file_key(path)
    try:
        return 0 <= now - float(note.read_text(encoding="utf-8")) < QUIET_SECONDS
    except (OSError, ValueError):
        return False


def plan_from(event: dict, notes: Path, now: float):
    """(where, text) for a finished plan in this event, or None."""
    tool = event.get("tool_name")
    tool_input = event.get("tool_input")
    if not isinstance(tool_input, dict):
        return None

    if tool == "ExitPlanMode":
        if isinstance(tool_input.get("plan"), str):
            return PLAN_MODE, tool_input["plan"]
        path = tool_input.get("planFilePath")
        if isinstance(path, str) and Path(path).is_file():
            return PLAN_MODE, Path(path).read_text(encoding="utf-8")
        return None

    path = tool_input.get("file_path")
    if tool not in FILE_TOOLS or not isinstance(path, str) or not is_plan_file(path):
        return None
    if not Path(path).is_file() or is_being_checked(path, notes, now):
        return None
    return path, Path(path).read_text(encoding="utf-8")


def decide(event, notes: Path, now: float):
    """The hook's answer for one event: a decision to print, or None to say nothing."""
    if not isinstance(event, dict):
        return None
    found = plan_from(event, notes, now)
    if found is None:
        return None
    where, text = found
    location = PLAN_MODE if where == PLAN_MODE else file_key(where)

    session = re.sub(r"[^A-Za-z0-9_-]", "", str(event.get("session_id") or "unknown")) or "unknown"
    session_notes = notes / f"{session}.json"
    state = read_state(session_notes)
    paused = state.setdefault("paused", {})
    counts = state.setdefault("counts", {})

    if is_checked(text):
        if counts.pop(location, None) is not None:
            write_state(session_notes, state)
        return None

    current = fingerprint(text)
    if current in paused or counts.get(location, 0) >= MAX_PAUSES:
        return None
    paused[current] = now
    counts[location] = counts.get(location, 0) + 1
    write_state(session_notes, state)

    if event.get("tool_name") == "ExitPlanMode":
        return {"hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": PLAN_MODE_REASON,
        }}
    return {"decision": "block", "reason": FILE_REASON.format(path=where)}


def read_state(session_notes: Path) -> dict:
    try:
        state = json.loads(session_notes.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {}
    return state if isinstance(state, dict) else {}


def write_state(session_notes: Path, state: dict) -> None:
    session_notes.parent.mkdir(parents=True, exist_ok=True)
    session_notes.write_text(json.dumps(state), encoding="utf-8")


def begin(path: str, notes: Path) -> None:
    (notes / "active").mkdir(parents=True, exist_ok=True)
    (notes / "active" / file_key(path)).write_text(str(time.time()), encoding="utf-8")


def stamp(path: str, notes: Path) -> None:
    plan = Path(path)
    with open(plan, encoding="utf-8", newline="") as source:
        original = source.read()
    newline = "\r\n" if "\r\n" in original else "\n"
    text = MARKER.sub("", original).rstrip()
    with open(plan, "w", encoding="utf-8", newline="") as target:
        target.write(f"{text}{newline}{newline}{marker_line(text)}{newline}")
    end(path, notes)
    key = file_key(path)
    for session_notes in notes.glob("*.json"):
        state = read_state(session_notes)
        counts = state.get("counts")
        if isinstance(counts, dict) and counts.pop(key, None) is not None:
            write_state(session_notes, state)


def end(path: str, notes: Path) -> None:
    try:
        (notes / "active" / file_key(path)).unlink()
    except OSError:
        pass


def main(arguments) -> None:
    if len(arguments) == 2 and arguments[0] == "--begin":
        begin(arguments[1], notes_folder())
    elif len(arguments) == 2 and arguments[0] == "--stamp":
        stamp(arguments[1], notes_folder())
    elif len(arguments) == 2 and arguments[0] == "--end":
        end(arguments[1], notes_folder())
    elif arguments == ["--fingerprint"]:
        print(marker_line(sys.stdin.buffer.read().decode("utf-8-sig")))
    elif not arguments:
        try:
            raw = sys.stdin.buffer.read().decode("utf-8")
            decision = decide(json.loads(raw or "null"), notes_folder(), time.time())
        except Exception:
            decision = None
        if decision is not None:
            print(json.dumps(decision))


if __name__ == "__main__":
    main(sys.argv[1:])
