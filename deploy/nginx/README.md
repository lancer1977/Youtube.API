# Nginx Front-Door Deployment Template

This folder captures the reusable deployment baseline for projects that want:

- a static-first or app-rendered frontend
- an ASP.NET Core backend behind a reverse proxy
- SignalR/WebSocket-safe proxying
- durable app state on a named volume
- a small, teachable compose shape

## Shape

- `app` is the backend service
- `nginx` is the public front door
- `app-data` is the durable state volume
- `default.conf` owns the Nginx proxy rules

The template is intentionally simple:

- Nginx publishes the only public port
- the app stays on the internal network
- `/health` is proxied to the app
- `/hubs/` is websocket-aware for SignalR
- optional static assets can be mounted into Nginx later without changing the proxy contract

## Files

- `docker-compose.local.yml` — local build-and-smoke compose
- `docker-compose.portainer.yml` — Portainer-ready stack shape
- `default.conf` — Nginx front-door config
- `.env.example` — copyable variable template

## Default assumptions

- the app listens on container port `8080`
- the app exposes a `/health` endpoint
- the app can run with `ASPNETCORE_URLS=http://+:8080`
- the app image can answer the healthcheck command used in the compose file, or the project will replace that probe

## Typical use case

This is a good fit when you want the public surface to stay simple while the backend owns the durable behavior:

- static site assets
- ASP.NET Core APIs
- SignalR realtime updates
- auth/session state
- SQLite or another simple app-owned store

## What stays in the product repo

- the actual app code
- the real Dockerfile
- any frontend build pipeline
- app-specific environment variables and secrets
- any custom nginx paths, headers, or caching policy

## Validation

Use the local compose file first:

```bash
docker compose -f templates/repo/deploy/nginx/docker-compose.local.yml up -d --build
curl -fsS http://localhost:8080/health
docker compose -f templates/repo/deploy/nginx/docker-compose.local.yml down
```

If the app image does not already contain the healthcheck tool, add one in the product repo or replace the probe before using the template as-is.
