# Keeping local Unity and GitHub in sync

The Cloud Agent edits **GitHub** (`stmitchell0428-cloud/BoC4x`). Your Unity project is in sync only when it matches what is **committed and pushed** there.

## Why they diverged

Features that exist only on your PC (salvation-history intro cards, hexameron choice, etc.) were never pushed to GitHub. The agent branch (`cursor/tightening-batch-b1b6`) had the tightening batch but not your intro work — so Play looked different.

## One-time: push your local game to GitHub

In your local project folder (`BoC4x` on your PC):

```bash
cd path/to/BoC4x
git status
git remote -v
```

If `origin` is not set:

```bash
git remote add origin https://github.com/stmitchell0428-cloud/BoC4x.git
```

Push everything on your current branch:

```bash
git add -A
git commit -m "Full local tree: salvation history intro, playtest fixes"
git push -u origin HEAD
```

If `master` on GitHub is behind and push is rejected:

```bash
git fetch origin
git pull origin master --rebase
git push -u origin HEAD
```

Or push to a dedicated branch (safer when agent PRs are open):

```bash
git checkout -b local/full-tree-aug3
git add -A
git commit -m "Full local tree snapshot"
git push -u origin local/full-tree-aug3
```

Then open a PR on GitHub: **local/full-tree-aug3 → master**.

## Day to day

| You want… | Do this |
|-----------|---------|
| **Local ← cloud/agent changes** | `git pull origin master` (or merge PR #1) |
| **Cloud ← your local changes** | commit + `git push` on your machine |
| **Check if you're aligned** | `git fetch origin && git status` — should say *up to date* |

**Session bookmark:** [`PROGRESS.md`](PROGRESS.md) · next smoke list: [`PLAYTEST-AUDIT-BATCH.md`](PLAYTEST-AUDIT-BATCH.md)

## After pulling

1. Open Unity → wait for recompile  
2. Run EditMode tests once  
3. Play from lobby using the playtest checklist  

If something only works locally, it is **not synced** until pushed.
