# Deployment Template

This folder captures reusable default-deploy baselines for new project repos.

The original template is tuned to the .NET/Traefik pattern used by the dev-forge workbench example, but the shape is meant to be copied and adapted per product repo.

Use it when you need a repeatable starting point for:

- a Portainer stack
- a browser-facing service behind Traefik
- a browser-facing service behind Nginx
- health checks and rollback-friendly updates
- a clearly documented secret contract
- optional host mounts without making them required

## Source-of-truth split

- `~/code/gitops` owns live homelab desired state, promoted stacks, and host-specific inventory.
- `~/code/dev-forge` owns reusable deployment shapes, templates, and operator notes.
- Product repos own their app code, image build, and app-specific deployment details.

## Files

- `portainer-stack.yml` — reusable Portainer/Traefik stack skeleton
- `docker-compose.local.yml` — reusable local smoke compose for build-and-run validation
- `nginx/` — Nginx front-door variant for static-first frontends plus app backends
- `game-server/` — reusable game-stack lifecycle helper for fresh runtime trees, apply, attach, and publish

## Placeholder conventions

Replace these names in the copied template:

- `APP_NAME` — service and router name
- `APP_IMAGE` — published image reference
- `APP_HOST` — external host name
- `APP_CONTAINER_PORT` — container port exposed by the app
- `APP_ALLOWED_SOURCE_RANGES` — LAN/VPN whitelist for internal-only exposure
- `TRAEFIK_PUBLIC_NETWORK` — external Traefik network name
- `APP_SECRET_ENV_FILE` — optional secret env file name when the app needs one

## What the template covers

- one replica
- start-first updates
- rollback on update failure
- a `/health` healthcheck
- Traefik router labels
- an internal IP whitelist for LAN/VPN-only exposure
- an external Traefik network reference

## What stays in the product repo

- the actual Dockerfile or build pipeline
- app-specific config and secrets
- any persistence paths or bind mounts the app needs
- any non-default routes, ports, or auth gates

## Recommended next step

Copy the template into the owning repo, replace the placeholders, and then adapt the local smoke compose and add the runbook there.
