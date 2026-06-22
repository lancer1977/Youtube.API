# Homelab ops repo overlay

## Stack clues
- Check for host manifests, service inventories, compose files, and operator docs.
- Common patterns: `compose.yml`, `docker-compose.*`, deployment playbooks, host scripts.

## Common commands
- Inspect health/status: `unknown`
- Apply safe dry-run or docs-first updates first.
- Run host/container checks only when explicitly requested by issue context.

## Safety boundaries
- Do not change credentials, host keys, ports, or network routing without explicit approval.
- Do not restart or remove services during docs-first cleanup tasks.
- Require rollback plan when touching service definitions or publish paths.

## Operational notes
- Treat runtime operations as destructive by default.
- Keep issue packet explicit about pre/post host state, commands run, and observed impact.
