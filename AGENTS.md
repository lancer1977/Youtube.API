# AGENTS.md

## Purpose

This file is the repo-local guide for coding agents working in this repository.
Follow the closest `AGENTS.md` first, then the workspace-root `AGENTS.md` when this repo lives under `~/code`.

## Default Workflow

- Start with understanding first, then planning, then implementation.
- Read the repository `README.md` and any local docs before changing code.
- Check `docs/features/` and `docs/roadmaps/` when they exist.
- Prefer small, traceable changes over broad rewrites.
- Ask a direct question if the work is ambiguous or would require a risky assumption.
- Preserve existing patterns unless there is a strong reason to change them.

## Validation

- Prefer creating or updating unit tests when they are the right validation surface.
- Use repo-native build, test, and lint commands instead of guessing.
- Verify the smallest relevant surface that proves the change.
- If a change affects docs, update the docs and checklists in the same pass.

## Code Standards

- Keep edits focused and reviewable.
- Prefer strongly typed code and explicit boundaries where practical.
- Avoid destructive commands unless explicitly requested.
- Do not revert user changes you did not make.
- Keep secrets and tokens out of source control.

## Docs Standards

- Treat documentation as part of the product.
- Prefer checklist-heavy docs for progress, validation, and follow-up work.
- Record implementation notes when the repo has ongoing feature work.
- When behavior changes, update the relevant feature or roadmap doc before finishing.

## Git Hygiene

- Do not rewrite shared history unless explicitly asked.
- Do not force-push by default.
- Keep the working tree clean enough that future diffs are easy to inspect.

## Repo Notes

- Add repo-specific commands, constraints, and validation steps below this line.
