#!/usr/bin/env python3
"""Create blobs + commit + branch + PR on bhs1983/overland from staged exact bytes. No clone."""
from __future__ import annotations

import base64
import hashlib
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(r"C:\Users\bhsmi\AppData\Local\Temp\overland-v3")
REPO = "bhs1983/overland"
BRANCH = "cursor/v3-atlas-parallax"
PARENT = "a1eae89e9b55ffebc557543eaca589eae189f5e8"

# expected sha256 of binaries we care about (assert before upload)
EXPECT = {
    "assets/v3/characters/hero/hero_atlas.png": "000f355f243f3039a91507735e3a4ae11a311960557f232c8254264aa9919a14",
    "assets/v3/vfx/impacts.png": "fb6c2a9cf5add8f2f0a9a1df8d56537ca8a2e1b36a8d5d39302df0200a8449de",
    "assets/v3/vfx/spark.png": "6c531c2844031e788e6b25b39314d39a08b04a99a248d03b3c0de49a87157ed5",
    "assets/v3/characters/enemies/clinker.png": "7940f75ecc5e2d329e0d446018a83e8eeff1f7961d7b83e770219ba5a424ea97",
    "assets/v3/characters/enemies/claywalker.png": "a4ae3e56cbd0a0c8f35e5dea2101a08a26501f4ef32236bf181d70612ff753ae",
    "assets/v3/characters/enemies/brickleech.png": "c3d99ddbb4d4fec984809b63f44eabe297da4f9d24483d55b3fa6e6946c345f2",
    "assets/v3/environment/parallax/cold_stack/far_bg.png": "9bf706dbb7c21074848ebc90649be9fd07b44407ba67a954caccb908c51f6192",
    "assets/v3/environment/parallax/cold_stack/mid_bg.png": "82d3b6d7f7e56d68830d4e3f9f3763017113f86a6fb8c697fefa08d336c4d0aa",
    "assets/v3/environment/parallax/kilnwalk/far_bg.png": "ba388d00fc727b33ba644ac3c256778f142882d9d12dac6eed06f96f0b0247fb",
    "assets/v3/environment/parallax/kilnwalk/mid_bg.png": "902b8848920170f5993ce408caad6f049c3b54aa9546458e624d868b20404f36",
}


def gh_api(method: str, path: str, payload: dict | None = None) -> dict:
    cmd = ["gh", "api", "-X", method, path]
    if payload is not None:
        cmd += ["--input", "-"]
        raw = json.dumps(payload).encode("utf-8")
        r = subprocess.run(cmd, input=raw, capture_output=True)
    else:
        r = subprocess.run(cmd, capture_output=True)
    if r.returncode != 0:
        sys.stderr.write(r.stderr.decode("utf-8", "replace"))
        raise SystemExit(f"gh api failed: {method} {path} rc={r.returncode}")
    return json.loads(r.stdout.decode("utf-8"))


def main() -> None:
    files = []
    for p in sorted(ROOT.rglob("*")):
        if not p.is_file():
            continue
        rel = p.relative_to(ROOT).as_posix()
        data = p.read_bytes()
        digest = hashlib.sha256(data).hexdigest()
        if rel in EXPECT:
            assert digest == EXPECT[rel], (rel, digest, EXPECT[rel])
        files.append((rel, data, digest))
        print(f"stage {rel} {len(data)} {digest[:12]}")

    if not files:
        raise SystemExit("no staged files")

    parent = gh_api("GET", f"repos/{REPO}/commits/{PARENT}")
    base_tree = parent["commit"]["tree"]["sha"]
    print("base_tree", base_tree)

    tree_items = []
    for rel, data, digest in files:
        blob = gh_api(
            "POST",
            f"repos/{REPO}/git/blobs",
            {"content": base64.b64encode(data).decode("ascii"), "encoding": "base64"},
        )
        print(f"blob {rel} {blob['sha'][:12]}")
        tree_items.append({"path": rel, "mode": "100644", "type": "blob", "sha": blob["sha"]})

    tree = gh_api(
        "POST",
        f"repos/{REPO}/git/trees",
        {"base_tree": base_tree, "tree": tree_items},
    )
    print("tree", tree["sha"])

    commit = gh_api(
        "POST",
        f"repos/{REPO}/git/commits",
        {
            "message": "Import v3 hero atlas, VFX, parallax, and locked docs.\n\nExact bytes from Art disk. Nearest, no mipmaps. No Whimble.",
            "tree": tree["sha"],
            "parents": [PARENT],
        },
    )
    sha = commit["sha"]
    print("COMMIT", sha)

    # create or update branch ref
    ref = f"refs/heads/{BRANCH}"
    try:
        gh_api("GET", f"repos/{REPO}/git/ref/heads/{BRANCH}")
        gh_api("PATCH", f"repos/{REPO}/git/refs/heads/{BRANCH}", {"sha": sha, "force": True})
        print("updated ref", BRANCH)
    except SystemExit:
        gh_api("POST", f"repos/{REPO}/git/refs", {"ref": ref, "sha": sha})
        print("created ref", BRANCH)

    # PR if missing
    existing = subprocess.run(
        ["gh", "pr", "list", "--repo", REPO, "--head", BRANCH, "--json", "number,url"],
        capture_output=True,
        text=True,
    )
    prs = json.loads(existing.stdout or "[]")
    if prs:
        print("PR", prs[0]["url"])
    else:
        created = subprocess.run(
            [
                "gh",
                "pr",
                "create",
                "--repo",
                REPO,
                "--base",
                "main",
                "--head",
                BRANCH,
                "--title",
                "v3 atlas + parallax + Checkpoint 3",
                "--body",
                "## Summary\n- Exact-byte v3 hero atlas, VFX, Clinker/Claywalker/Brickleech, parallax layers, ART.md\n- DESIGN.md / SLICE-0.md copied if ahead (environment talks)\n- Wiring + CP3 rooms 4–8 follow on this PR\n\n## Test plan\n- [ ] PNGs decode, Filter Nearest, no mipmaps\n- [ ] Far/mid Repeat on, FG Repeat off\n- [ ] Hero pivot 16,47, fluewalker_* names\n",
            ],
            capture_output=True,
            text=True,
        )
        print(created.stdout)
        print(created.stderr)
        if created.returncode != 0:
            raise SystemExit(created.returncode)
    print("DONE", sha)


if __name__ == "__main__":
    main()
