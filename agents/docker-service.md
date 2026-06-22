# Docker service overlay

## Stack clues
- Check for `Dockerfile`, `docker-compose.yml`, `compose.yml`, and deployment docs.

## Common commands
- Build image: `docker build .`
- Compose up: `docker compose up` or `docker-compose up`
- Compose down: `docker compose down`
- Validate config: `docker compose config`

## Safety boundaries
- Do not change exposed ports, host networking, or runtime volumes without explicit approval.
- Do not edit production `.env` values or secret files from templates.
- Keep image tags and deployment entrypoints aligned to existing release flow unless explicitly requested.

## Deployment notes
- Read deployment docs before changing service wiring.
- Treat container/network changes as high-impact changes requiring explicit authorization.
