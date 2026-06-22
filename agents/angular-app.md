# Angular app overlay

## Stack clues
- Check for `package.json`, `angular.json`, and `src/app`.

## Common commands
- Restore/install: `npm ci` or `npm install`
- Build: `npm run build`
- Test: `npm test`
- Lint: `npm run lint`
- Run locally: `npm run start`

## Safety boundaries
- Do not change production environment URLs or OAuth/client IDs without explicit approval.
- Do not rework unrelated component scopes from a narrow issue.
- Keep environment and secret handling constrained to requested config paths.

## Review notes
- Confirm whether the repository uses strict mode or workspace-specific scripts from `package.json` before making claims.
