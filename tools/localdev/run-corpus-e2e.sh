#!/usr/bin/env bash
# W11 — the corpus publishing pipeline, end to end, in a browser.
#
#   tools/localdev/run-corpus-e2e.sh              reuse the local workspace (exercises the no-op push path)
#   tools/localdev/run-corpus-e2e.sh --fresh      empty imprint first, so every push is a first push
#   tools/localdev/run-corpus-e2e.sh --keep       leave the three hosts running for a hand look
#
# WHAT IT PROVES. Not that the unit tests pass — that the pages a reader gets are real. It boots a
# LOCAL imprint (editor + authoring API), makes a site, points a Kennel Watchdog host's
# Kennel:Registry at it, lets the publisher run ONE sweep, waits for imprint's file-system
# projection to write the static pages, serves them with Imprint.Site, and drives the result with
# Playwright (ui/corpus-e2e.mjs), which asserts that every share on every page carries its
# denominator and its basis.
#
# ★★ NOTHING IS PUBLISHED PUBLICLY. codeassuranceindex.info is a real website and the same sweep
#    code publishes to it in production; the only thing separating the two is which host
#    Kennel:Registry names. So the loopback check below is a REFUSAL, not a warning, and it is the
#    first thing this script does after resolving its configuration.
#
# ★ node_modules DOES NOT EXIST INSIDE A GIT WORKTREE (CLAUDE.md, "Verification and the UI"). The
#   driver resolves Playwright from the MAIN checkout by absolute path; this script derives that
#   path from `git rev-parse --git-common-dir` and hands it over, so neither file hardcodes a
#   /home/... path the way the legacy e2e runners do.
#
# ★ Scratch state lives under ~/Hentet/kennel/corpus-publishing/, never /tmp (owner decision
#   2026-08-18). Only the pidfiles follow run-app.sh into /tmp, so the two scripts reap alike.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
MAIN_CHECKOUT="$(dirname "$(git -C "$ROOT" rev-parse --path-format=absolute --git-common-dir)")"
IMPRINT_ROOT="${IMPRINT_ROOT:-/home/jimmy/RiderProjects/imprint.canine.dev}"
WORKSPACE="${CORPUS_E2E_WORKSPACE:-$HOME/Hentet/kennel/corpus-publishing/e2e}"

EDITOR_PORT="${EDITOR_PORT:-5099}"      # imprint authoring API + editor
SITE_PORT="${SITE_PORT:-5098}"          # imprint's published static site
WATCHDOG_PORT="${WATCHDOG_PORT:-8175}"  # the Kennel host that runs the sweep
# The token is a LOCAL shared secret for a loopback-only instance. It is not a credential for
# anything that exists elsewhere, and it is written into the workspace so a re-run reuses it.
AUTHORING_TOKEN="${CORPUS_E2E_TOKEN:-corpus-e2e-local-token}"
SITE_NAME="Corpus E2E"

FRESH=0; KEEP=0
while [ $# -gt 0 ]; do
  case "$1" in
    --fresh) FRESH=1; shift ;;
    --keep)  KEEP=1; shift ;;
    *) echo "usage: $0 [--fresh] [--keep]" >&2; exit 2 ;;
  esac
done

# ── the refusal ──────────────────────────────────────────────────────────────────────────────
# Whole-host match, never a substring: "localhost.example.com" contains "localhost", and a
# substring test is how a check like this ends up passing something it was written to stop.
case "${CORPUS_E2E_HOST:-127.0.0.1}" in
  127.0.0.1|localhost|::1) ;;
  *) echo "REFUSED: this harness only ever publishes to a loopback imprint (got '${CORPUS_E2E_HOST}')." >&2; exit 3 ;;
esac
HOST="${CORPUS_E2E_HOST:-127.0.0.1}"
EDITOR_BASE="http://$HOST:$EDITOR_PORT"
SITE_BASE="http://$HOST:$SITE_PORT"

LOGS="$WORKSPACE/logs"; SHOTS="$WORKSPACE/shots"; DATA="$WORKSPACE/imprint-data"
mkdir -p "$LOGS" "$SHOTS"

WATCHDOG_DLL="$ROOT/src/Kennel.Watchdog.App/bin/Debug/net10.0/Kennel.Watchdog.App.dll"
EDITOR_DLL="$IMPRINT_ROOT/src/Imprint.Editor/bin/Debug/net10.0/Imprint.Editor.dll"
SITE_DLL="$IMPRINT_ROOT/src/Imprint.Site/bin/Debug/net10.0/Imprint.Site.dll"
for dll in "$WATCHDOG_DLL" "$EDITOR_DLL" "$SITE_DLL"; do
  [ -f "$dll" ] || { echo "missing build output: $dll" >&2
                     echo "build it first (never while a host is running)." >&2; exit 1; }
done

# ── process custody ──────────────────────────────────────────────────────────────────────────
# Same rule as run-app.sh, and for the same reason: NEVER `pkill -f` a bare dll name. The
# production watchdog and scan worker on this box run the SAME Kennel.Watchdog.App.dll, and an
# unanchored match SIGKILLs them. Kill the recorded pid, and fall back only to the anchored
# absolute path under $ROOT, which no deployed service can match.
PIDS=()
record() { PIDS+=("$1"); echo "$1" > "$LOGS/$2.pid"; }
reap_recorded() {
  for name in watchdog imprint-editor imprint-site; do
    f="$LOGS/$name.pid"
    [ -f "$f" ] && kill -9 "$(cat "$f" 2>/dev/null)" 2>/dev/null || true
    rm -f "$f"
  done
}
cleanup() {
  [ "$KEEP" = 1 ] && { echo; echo "--keep: leaving hosts up — editor $EDITOR_BASE, site $SITE_BASE, watchdog http://$HOST:$WATCHDOG_PORT"; return; }
  for pid in "${PIDS[@]:-}"; do [ -n "$pid" ] && kill "$pid" 2>/dev/null || true; done
  sleep 1
  for pid in "${PIDS[@]:-}"; do [ -n "$pid" ] && kill -9 "$pid" 2>/dev/null || true; done
  rm -f "$LOGS"/*.pid
}
trap cleanup EXIT
reap_recorded

wait_for() { # url, label, attempts (2s apart), pid to watch
  local url="$1" label="$2" attempts="$3" pid="${4:-}"
  for _ in $(seq 1 "$attempts"); do
    curl -fsS -m5 -o /dev/null "$url" 2>/dev/null && { echo "  $label UP"; return 0; }
    if [ -n "$pid" ] && ! kill -0 "$pid" 2>/dev/null; then
      echo "  $label EXITED before listening — last log lines:" >&2; tail -20 "$LOGS/$label.log" >&2; return 1
    fi
    sleep 2
  done
  echo "  $label did not answer $url in time; see $LOGS/$label.log" >&2; return 1
}

api() { curl -fsS -m20 -H "Authorization: Bearer $AUTHORING_TOKEN" "$@"; }

# ── 1. imprint editor (authoring API + the file-system projection that publishes) ────────────
if [ "$FRESH" = 1 ]; then
  echo "--fresh: emptying $DATA"
  rm -rf "$DATA" "$LOGS/site-id"
fi
mkdir -p "$DATA"

echo "[1/6] imprint editor on $EDITOR_BASE (data: $DATA)"
(
  cd "$IMPRINT_ROOT/src/Imprint.Editor"
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$EDITOR_BASE" \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  Imprint__Authoring__Token="$AUTHORING_TOKEN" \
  Imprint__Authoring__Actor="service:corpus-e2e" \
  nohup dotnet "$EDITOR_DLL" --ImprintData="$DATA" > "$LOGS/imprint-editor.log" 2>&1 &
  echo $! > "$LOGS/imprint-editor.pid.tmp"
)
EDITOR_PID="$(cat "$LOGS/imprint-editor.pid.tmp")"; rm -f "$LOGS/imprint-editor.pid.tmp"
record "$EDITOR_PID" imprint-editor
# /api/authoring/* is FAIL-CLOSED on the token: an unauthenticated 401 here still proves it is up,
# so probe the authenticated read — a 200 proves the token this run will publish with is the one
# the editor accepts, which is the half that actually fails.
for _ in $(seq 1 45); do
  if api "$EDITOR_BASE/api/authoring/sites" -o /dev/null 2>/dev/null; then echo "  imprint-editor UP"; break; fi
  kill -0 "$EDITOR_PID" 2>/dev/null || { echo "  imprint editor died:" >&2; tail -20 "$LOGS/imprint-editor.log" >&2; exit 1; }
  sleep 2
done

# ── 2. the site ──────────────────────────────────────────────────────────────────────────────
echo "[2/6] site '$SITE_NAME'"
SITE_ID="$(api "$EDITOR_BASE/api/authoring/sites" \
  | python3 -c "import json,sys;s=[x for x in json.load(sys.stdin) if x['name']=='$SITE_NAME'];print(s[0]['id'] if s else '')")"
if [ -z "$SITE_ID" ]; then
  SITE_ID="$(api -X POST -H 'Content-Type: application/json' \
    -d "{\"name\":\"$SITE_NAME\",\"defaultLocale\":\"en\"}" "$EDITOR_BASE/api/authoring/sites" \
    | python3 -c "import json,sys;print(json.load(sys.stdin)['siteId'])")"
  echo "  created $SITE_ID"
else
  echo "  reusing $SITE_ID"
fi
echo "$SITE_ID" > "$LOGS/site-id"

# ── 3. the Kennel host, pointed at it ────────────────────────────────────────────────────────
# The publish loop runs its first sweep AT STARTUP (RegistryPublishHostedService's do-while), so
# booting the host with Kennel:Registry configured IS "run one sweep". TickMinutes is floored at 5
# by the service, so a second sweep cannot surprise a run that finishes inside five minutes.
#
# ★ The dev seed's rows land in an IN-MEMORY store, so seeding and sweeping must happen in ONE
#   boot. DevSeedRunner is an IHostedService whose StartAsync runs to completion before the module
#   hosted services start (AddKennelUi is composed before AddModules), so the seed is always done
#   before the sweep. The corpus SNAPSHOT is not: CorpusSnapshotDailyService and the publish loop
#   are both BackgroundServices started back to back, and the sweep only reaches the corpus
#   reading after pushing every survey page — which is why this waits for the corpus root to
#   appear rather than assuming the first sweep carried it.
echo "[3/6] watchdog on http://$HOST:$WATCHDOG_PORT, publishing to $EDITOR_BASE"
(
  cd "$ROOT/src/Kennel.Watchdog.App"
  unset Kennel__Postgres__ConnectionString KENNEL_PG_TEST 2>/dev/null || true
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://$HOST:$WATCHDOG_PORT" \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 KENNEL_DEV_PROVIDER_STUB=1 Kennel__Auth__Dev__IsAdmin=true \
  Kennel__Registry__SiteBaseUrl="$EDITOR_BASE" \
  Kennel__Registry__SiteId="$SITE_ID" \
  Kennel__Registry__Token="$AUTHORING_TOKEN" \
  Kennel__Registry__ReportBaseUrl="http://$HOST:$WATCHDOG_PORT" \
  Kennel__Registry__TickMinutes=5 \
  nohup dotnet "$WATCHDOG_DLL" > "$LOGS/watchdog.log" 2>&1 &
  echo $! > "$LOGS/watchdog.pid.tmp"
)
WATCHDOG_PID="$(cat "$LOGS/watchdog.pid.tmp")"; rm -f "$LOGS/watchdog.pid.tmp"
record "$WATCHDOG_PID" watchdog
wait_for "http://$HOST:$WATCHDOG_PORT/health" watchdog 60 "$WATCHDOG_PID"

# ── 4. the sweep ─────────────────────────────────────────────────────────────────────────────
echo "[4/6] waiting for the sweep to reach the corpus root"
CORPUS_ROOT="state-of-the-corpus"
SWEPT=0
for _ in $(seq 1 200); do   # up to ~6m40s: past the 5-minute floor, so a second tick is covered
  if api "$EDITOR_BASE/api/authoring/sites/$SITE_ID/syndicated" 2>/dev/null \
     | python3 -c "import json,sys;p=[x['path'] for x in json.load(sys.stdin)['pages']];sys.exit(0 if '$CORPUS_ROOT' in p else 1)"; then
    SWEPT=1; break
  fi
  sleep 2
done
[ "$SWEPT" = 1 ] || { echo "  no page under '$CORPUS_ROOT' was ever pushed — see $LOGS/watchdog.log" >&2; exit 1; }

echo "  what the publisher says it did:"
curl -fsS -m10 "http://$HOST:$WATCHDOG_PORT/api/system/info" \
  | python3 -c "import json,sys;print('   ',json.dumps(json.load(sys.stdin)['registrySweep']))"
echo "  what the site says it is serving:"
api "$EDITOR_BASE/api/authoring/sites/$SITE_ID/syndicated" \
  | python3 -c "import json,sys;[print('    ',x['path']) for x in json.load(sys.stdin)['pages']]"

# ── 5. the static output, and a host for it ──────────────────────────────────────────────────
# imprint's publisher is a debounced projection: the push returns before the files exist. Wait
# for the file, not for a guess at the debounce.
echo "[5/6] waiting for the published files, then serving them on $SITE_BASE"
for _ in $(seq 1 60); do
  [ -f "$DATA/publish/$CORPUS_ROOT/index.html" ] && break
  sleep 2
done
[ -f "$DATA/publish/$CORPUS_ROOT/index.html" ] || {
  echo "  the pages were accepted but never published to disk; see $LOGS/imprint-editor.log" >&2; exit 1; }

(
  cd "$IMPRINT_ROOT/src/Imprint.Site"
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$SITE_BASE" DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  nohup dotnet "$SITE_DLL" --ImprintPublish="$DATA/publish" > "$LOGS/imprint-site.log" 2>&1 &
  echo $! > "$LOGS/imprint-site.pid.tmp"
)
SITE_PID="$(cat "$LOGS/imprint-site.pid.tmp")"; rm -f "$LOGS/imprint-site.pid.tmp"
record "$SITE_PID" imprint-site
wait_for "$SITE_BASE/$CORPUS_ROOT/" imprint-site 30 "$SITE_PID"

# ── 6. what a reader gets ────────────────────────────────────────────────────────────────────
echo "[6/6] driving the published pages"
STAMP="$(date -u +%Y%m%d-%H%M%S)"
set +e
# --api is the WATCHDOG host, not the site: the JSON and CSV mirrors are served by the same
# process that publishes the pages, and the point of checking them here is that a figure quoted
# from a mirror and the same figure quoted from a page come from the one snapshot. Checking them
# in a separate step would not have that guarantee.
node "$ROOT/tools/localdev/ui/corpus-e2e.mjs" \
  --base "$SITE_BASE" --api "http://$HOST:$WATCHDOG_PORT" \
  --out "$SHOTS/$STAMP" --tooling "$MAIN_CHECKOUT/tools/localdev/ui"
RESULT=$?
set -e

echo
echo "screenshots: $SHOTS/$STAMP   (LOOK at them — a green report over an unviewed screenshot is worthless)"
exit "$RESULT"
