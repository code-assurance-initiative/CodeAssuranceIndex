#!/usr/bin/env python3
"""Every internal link on every published page, checked against what was actually published.

★★ WHY A SEPARATE SWEEP. The screenshot runner visits ONE page per shape — fifteen of sixty-seven —
   so a link that points at a path nobody built survives on the forty-two it never opens. The builder
   is careful about this (§6 links "only to pages this build produced", and a group row's href is
   derived from its own Path so a link cannot outrun the page set), but "careful by construction" is
   a claim about the code, and this is the same claim taken over the OUTPUT.

★ It reads the published files rather than crawling: no browser, no server, and it sees every page
  rather than every page a crawl happens to reach from the root.
"""
import re
import sys
from pathlib import Path

HREF = re.compile(r'(?:href|src)="(/[^"#?]*)', re.IGNORECASE)

# ★★ ADDRESSES THE SWEEP DOES NOT BUILD AND MUST NOT: they are AUTHORED pages on the standard's own
#    site, which a scratch harness site does not have. Naming them here rather than filtering them
#    silently is the point — every survey portrait carries a "Verify this score yourself" card, so if
#    /verify/ ever stops existing, forty pages here and six thousand in production point at nothing,
#    and no sweep of the published set can tell. They are reported, separately, every run.
AUTHORED = {"/", "/verify/"}


def published_paths(root: Path) -> set[str]:
    """Every address the site serves: a directory holding index.html, plus every literal file."""
    served = set()
    for path in root.rglob("*"):
        if path.is_dir():
            continue
        if path.suffix in {".gz", ".br"}:
            continue
        relative = path.relative_to(root).as_posix()
        served.add("/" + relative)
        if path.name == "index.html":
            parent = path.parent.relative_to(root).as_posix()
            served.add("/" if parent == "." else f"/{parent}/")
    return served


def main() -> int:
    root = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
    if not root.is_dir():
        print(f"error: {root} is not a directory", file=sys.stderr)
        return 2

    served = published_paths(root)
    pages = sorted(root.rglob("*.html"))
    dangling: dict[str, set[str]] = {}
    links = 0

    for page in pages:
        where = "/" + page.relative_to(root).as_posix()
        for target in HREF.findall(page.read_text(encoding="utf-8", errors="replace")):
            links += 1
            # A directory address is served by its index.html; both spellings count as published.
            if target in served or f"{target}/" in served or f"{target}index.html" in served:
                continue
            dangling.setdefault(target, set()).add(where)

    authored = {t: w for t, w in dangling.items() if t in AUTHORED or f"{t}/" in AUTHORED}
    broken = {t: w for t, w in dangling.items() if t not in authored}

    print(f"{len(pages)} pages, {links} internal links, {len(served)} published addresses")
    for target, wheres in sorted(authored.items()):
        print(f"▲ AUTHORED {target} — linked from {len(wheres)} page(s); the sweep does not build it, "
              f"so it must exist on the site as an authored page")

    if not broken:
        print("no dangling links")
        return 0

    for target, wheres in sorted(broken.items()):
        first = sorted(wheres)[0]
        more = f" (+{len(wheres) - 1} more pages)" if len(wheres) > 1 else ""
        print(f"★ DANGLING {target} — linked from {first}{more}")
    return 1


if __name__ == "__main__":
    sys.exit(main())
