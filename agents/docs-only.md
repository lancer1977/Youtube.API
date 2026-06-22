# Docs-only repo overlay

## Stack clues
- Check for markdown-heavy repos and absence of a runtime app stack.
- Typical files: `README.md`, `docs/`, `templates/`, `workflows/`.

## Common commands
- Validate docs links or markdown formatting with repo-native tooling only.
- Run `unknown` for repo-wide checks until confirmed.

## Safety boundaries
- Prefer non-functional updates unless the issue explicitly requests content changes.
- Do not alter examples or operational procedures without matching issue context.
- Keep repository conventions (filename conventions, front matter style, link targets) intact.
- Do not edit secrets, credentials, API keys, or auth material.
- Do not apply unsafe broad edits without scoped ownership in the ticket.
