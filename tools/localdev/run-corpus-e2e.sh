#!/usr/bin/env bash
# The standard's own pages, end to end, in a browser.
#
#   tools/localdev/run-corpus-e2e.sh              reuse the local workspace
#   tools/localdev/run-corpus-e2e.sh --fresh      empty imprint and the scratch registry first
#   tools/localdev/run-corpus-e2e.sh --keep       leave the hosts running for a hand look
#
# WHAT IT PROVES. Not that the unit tests pass — that the pages a reader gets are real. It boots a
# LOCAL imprint (authoring API + the file-system projection), makes a site, seeds a SCRATCH CAI
# registry with published subjects, boots Cai.Web with its Syndication section pointed at that
# imprint so the publish loop's startup sweep publishes, waits for the projection to write the
# static files, serves them with Imprint.Site, and screenshots the result.
#
# ★★ CARRIED OVER FROM THE PRODUCER, WHOSE HARNESS THIS WAS. The pages moved to the standard and this
#    moved with them (kennel b9858c7c4). What did not change is the reason: the corpus pages are
#    served by imprint, so "green" says nothing about what a reader gets.
#
# ★★ NOTHING IS PUBLISHED PUBLICLY. codeassuranceindex.info is a real website and the SAME sweep code
#    publishes to it in production; the only thing separating the two is which host the Syndication
#    section names. So the loopback check below is a REFUSAL, not a warning, and it is the first
#    thing this script does after resolving its configuration.
#
# ★ THE SCRATCH REGISTRY IS ITS OWN FILE and is never the deployed one: a harness that seeded
#   fixtures into the real store would publish invented codebases under the standard's own name.
#
# ★ node_modules DOES NOT EXIST INSIDE A GIT WORKTREE, so the Playwright driver is resolved from the
#   main kennel checkout by absolute path (CLAUDE.md, "Verification and the UI").
#
# ★ Scratch state lives under ~/Hentet/cai/corpus-e2e/, never /tmp (owner decision 2026-08-18).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
IMPRINT_ROOT="${IMPRINT_ROOT:-/home/jimmy/RiderProjects/imprint.canine.dev}"
KENNEL_ROOT="${KENNEL_ROOT:-/home/jimmy/RiderProjects/kennel.canine.dev}"
WORKSPACE="${CORPUS_E2E_WORKSPACE:-$HOME/Hentet/cai/corpus-e2e}"

EDITOR_PORT="${EDITOR_PORT:-5099}"   # imprint authoring API + editor
SITE_PORT="${SITE_PORT:-5098}"       # imprint's published static site
CAI_PORT="${CAI_PORT:-8190}"         # the CAI host that runs the sweep
AUTHORING_TOKEN="${CORPUS_E2E_TOKEN:-corpus-e2e-local-token}"
SITE_NAME="CAI E2E"
SUBJECTS="${CORPUS_E2E_SUBJECTS:-40}"

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
CAI_BASE="http://$HOST:$CAI_PORT"

LOGS="$WORKSPACE/logs"; SHOTS="$WORKSPACE/shots"; DATA="$WORKSPACE/imprint-data"
REGISTRY="$WORKSPACE/registry"
mkdir -p "$LOGS" "$SHOTS" "$REGISTRY"
REGISTRY_DB="$REGISTRY/cai-registry.db"

CAI_DLL="$ROOT/src/Cai.Web/bin/Debug/net10.0/Cai.Web.dll"
EDITOR_DLL="$IMPRINT_ROOT/src/Imprint.Editor/bin/Debug/net10.0/Imprint.Editor.dll"
SITE_DLL="$IMPRINT_ROOT/src/Imprint.Site/bin/Debug/net10.0/Imprint.Site.dll"
for dll in "$CAI_DLL" "$EDITOR_DLL" "$SITE_DLL"; do
  [ -f "$dll" ] || { echo "missing build output: $dll" >&2
                     echo "build it first (never while a host is running)." >&2; exit 1; }
done

# ── process custody ──────────────────────────────────────────────────────────────────────────
# NEVER `pkill -f` a bare dll name: the production CAI host on this box runs the SAME Cai.Web.dll,
# and an unanchored match SIGKILLs it. Kill the recorded pid and nothing else.
PIDS=()
record() { PIDS+=("$1"); echo "$1" > "$LOGS/$2.pid"; }
reap_recorded() {
  for name in cai imprint-editor imprint-site; do
    f="$LOGS/$name.pid"
    [ -f "$f" ] && kill -9 "$(cat "$f" 2>/dev/null)" 2>/dev/null || true
    rm -f "$f"
  done
}
cleanup() {
  [ "$KEEP" = 1 ] && { echo; echo "--keep: hosts left up — editor $EDITOR_BASE, site $SITE_BASE, cai $CAI_BASE"; return; }
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
      echo "  $label EXITED before listening — last log lines:" >&2; tail -25 "$LOGS/$label.log" >&2; return 1
    fi
    sleep 2
  done
  echo "  $label did not answer $url in time; see $LOGS/$label.log" >&2; return 1
}

api() { curl -fsS -m20 -H "Authorization: Bearer $AUTHORING_TOKEN" "$@"; }

boot_cai() { # label
  (
    cd "$ROOT/src/Cai.Web"
    ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$CAI_BASE" DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    Registry__DbPath="$REGISTRY_DB" \
    Syndication__SiteBaseUrl="$EDITOR_BASE" \
    Syndication__SiteId="$1" \
    Syndication__Token="$AUTHORING_TOKEN" \
    Syndication__TickMinutes=5 \
    nohup dotnet "$CAI_DLL" > "$LOGS/cai.log" 2>&1 &
    echo $! > "$LOGS/cai.pid.tmp"
  )
  CAI_PID="$(cat "$LOGS/cai.pid.tmp")"; rm -f "$LOGS/cai.pid.tmp"
  record "$CAI_PID" cai
  wait_for "$CAI_BASE/health" cai 60 "$CAI_PID"
}

stop_cai() {
  [ -f "$LOGS/cai.pid" ] || return 0
  kill "$(cat "$LOGS/cai.pid")" 2>/dev/null || true
  sleep 2
  kill -9 "$(cat "$LOGS/cai.pid")" 2>/dev/null || true
  rm -f "$LOGS/cai.pid"
}

if [ "$FRESH" = 1 ]; then
  echo "--fresh: emptying $DATA and $REGISTRY_DB"
  rm -rf "$DATA" "$LOGS/site-id"; rm -f "$REGISTRY_DB"*
fi
mkdir -p "$DATA"

# ── 1. imprint editor (authoring API + the projection that publishes) ────────────────────────
echo "[1/7] imprint editor on $EDITOR_BASE (data: $DATA)"
(
  cd "$IMPRINT_ROOT/src/Imprint.Editor"
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$EDITOR_BASE" \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  Imprint__Authoring__Token="$AUTHORING_TOKEN" \
  Imprint__Authoring__Actor="service:cai-e2e" \
  nohup dotnet "$EDITOR_DLL" --ImprintData="$DATA" > "$LOGS/imprint-editor.log" 2>&1 &
  echo $! > "$LOGS/imprint-editor.pid.tmp"
)
EDITOR_PID="$(cat "$LOGS/imprint-editor.pid.tmp")"; rm -f "$LOGS/imprint-editor.pid.tmp"
record "$EDITOR_PID" imprint-editor
# /api/authoring/* is FAIL-CLOSED on the token, so probe the AUTHENTICATED read: a 200 proves the
# token this run publishes with is the one the editor accepts, which is the half that actually fails.
for _ in $(seq 1 45); do
  if api "$EDITOR_BASE/api/authoring/sites" -o /dev/null 2>/dev/null; then echo "  imprint-editor UP"; break; fi
  kill -0 "$EDITOR_PID" 2>/dev/null || { echo "  imprint editor died:" >&2; tail -25 "$LOGS/imprint-editor.log" >&2; exit 1; }
  sleep 2
done

# ── 2. the site ──────────────────────────────────────────────────────────────────────────────
echo "[2/7] site '$SITE_NAME'"
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

# ── 3. let the registry create its own schema ────────────────────────────────────────────────
# ★ THE SEEDER REFUSES TO CREATE TABLES, deliberately: a schema invented by a harness drifts from
#   the registry's own, and then this would prove a render over a store the real one does not have.
#   So the host boots once against an empty file and makes it.
echo "[3/7] booting CAI once so the registry creates its schema"
boot_cai "$SITE_ID"
stop_cai

# ── 4. seed published subjects ───────────────────────────────────────────────────────────────
echo "[4/7] seeding $SUBJECTS published subjects"
python3 "$ROOT/tools/localdev/seed-local-registry.py" --db "$REGISTRY_DB" --subjects "$SUBJECTS"

# ── 5. the sweep ─────────────────────────────────────────────────────────────────────────────
# The publish loop runs its first sweep AT STARTUP (SitePublishHostedService's do-while), so booting
# the host with Syndication configured IS "run one sweep".
echo "[5/7] booting CAI on $CAI_BASE, publishing to $EDITOR_BASE"
boot_cai "$SITE_ID"

CORPUS_ROOT="state-of-the-corpus"
SWEPT=0
for _ in $(seq 1 150); do
  if api "$EDITOR_BASE/api/authoring/sites/$SITE_ID/syndicated" 2>/dev/null \
     | python3 -c "import json,sys;p=[x['path'] for x in json.load(sys.stdin)['pages']];sys.exit(0 if '$CORPUS_ROOT' in p else 1)"; then
    SWEPT=1; break
  fi
  sleep 2
done
[ "$SWEPT" = 1 ] || { echo "  no page under '$CORPUS_ROOT' was ever pushed — see $LOGS/cai.log" >&2; exit 1; }

echo "  what the publisher says it did:"
curl -fsS -m10 "$CAI_BASE/api/health/detail" \
  | python3 -c "import json,sys;d=json.load(sys.stdin);print('   ', [c['data'] for c in d['checks'] if c['name']=='registry'])"
echo "  what the site says it is serving:"
api "$EDITOR_BASE/api/authoring/sites/$SITE_ID/syndicated" \
  | python3 -c "
import json,sys
p = sorted(x['path'] for x in json.load(sys.stdin)['pages'])
print(f'    {len(p)} pages')
[print('    ', x) for x in p[:12]]
json.dump(p, open('$LOGS/published-paths.json', 'w'))
"

# ── 6. the static output, and a host for it ──────────────────────────────────────────────────
# imprint's publisher is a debounced projection: the push returns before the files exist. Wait for
# the FILE, not for a guess at the debounce.
echo "[6/7] waiting for the published files, then serving them on $SITE_BASE"
for _ in $(seq 1 90); do
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

# ── 7. what a reader gets ────────────────────────────────────────────────────────────────────
echo "[7/7] screenshotting the published pages"
STAMP="$(date -u +%Y%m%d-%H%M%S)"
set +e
node "$ROOT/tools/localdev/ui/shoot.mjs" \
  --base "$SITE_BASE" --out "$SHOTS/$STAMP" --paths "$LOGS/published-paths.json"
RESULT=$?
set -e

echo
echo "screenshots: $SHOTS/$STAMP   (LOOK at them — a green report over an unviewed screenshot is worthless)"
exit "$RESULT"
