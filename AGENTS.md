# Project instructions

## Start here

- Read `README.md`, `CODEX_HANDOFF.md`, and `LEGAL_NOTICE.md` before proposing changes.
- This repository contains both animation assets and a Windows WPF desktop application.
- Treat `pet/pet.manifest.json` as the single source of truth for animation rows, timing, and aliases.

## Asset safety

- Treat `source/` as immutable source material unless the user explicitly asks to revise artwork.
- Do not overwrite `assets/spritesheet-v2.png`; place rebuilt artifacts under `build/` first.
- Preserve transparent backgrounds and the documented 192x208 cell registration.
- Never silently mirror a directional row when asymmetric hair, bows, markings, lighting, or props would reverse.
- Do not remove QA files merely because the application does not consume them.

## Integration expectations

- Keep animation-state mapping data-driven; do not scatter frame coordinates through the renderer.
- Map application events to the semantic states documented in `CODEX_HANDOFF.md`.
- Keep the character name `深深` and an ASCII identifier such as `shenshen`.
- Rebuild desktop/CLI and web assets with `python scripts/build_codex_package.py`, then verify them with `python scripts/verify_sprite.py` after copying or re-encoding the atlas.
- For application changes, run the Release build and `ShenshenPet.Core.Tests` console tests.

## Repository hygiene

- Do not add generated build folders, caches, credentials, or machine-specific absolute paths to Git.
- Preserve the artwork's CC BY-NC-SA 4.0 and upstream non-commercial restrictions in `ASSET_LICENSE.md`; the MIT grant covers code only.
- Keep this project clearly labeled as unofficial and not affiliated with OpenAI.
