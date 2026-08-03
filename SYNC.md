# Keeping local Unity and GitHub in sync

The Cloud Agent edits **GitHub** (`stmitchell0428-cloud/BoC4x`). Your Unity project is in sync only when it matches what is **committed and pushed** there.

## Why they diverged

Features that exist only on your PC (salvation-history intro cards, hexameron choice, etc.) were never pushed to GitHub. The agent branch (`cursor/tightening-batch-b1b6`) had the tightening batch but not your intro work — so Play looked different.

## One-time: push your local game to GitHub

In your local project folder (`BoC4x`):

```bash
git status
git add -A
git commit -m "Salvation history intro, local playtest fixes"
git push origin master
```

If you work on a feature branch, push that branch instead and open a PR.

## Day to day

| You want… | Do this |
|-----------|---------|
| **Local ← cloud/agent changes** | `git pull origin master` (or merge PR #1) |
| **Cloud ← your local changes** | commit + `git push` on your machine |
| **Check if you're aligned** | `git fetch origin && git status` — should say *up to date* |

## After pulling

1. Open Unity → wait for recompile  
2. Run EditMode tests once  
3. Play a quick smoke test  

If something only works locally, it is **not synced** until pushed.
