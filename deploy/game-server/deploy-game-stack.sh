#!/usr/bin/env sh
set -eu

ROOT="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
ALIENWARE_LIVE_ROOT="${ALIENWARE_LIVE_ROOT:-/home/lancer1977/game_servers}"
R620_LIVE_ROOT="${R620_LIVE_ROOT:-/home/lancer1977/servers}"
DEV_FORGE_ROOT="${DEV_FORGE_ROOT:-/home/lancer1977/code/dev-forge}"

usage() {
  cat >&2 <<'EOF'
Usage:
  deploy-game-stack.sh new <system> <stack>
  deploy-game-stack.sh apply <system> <stack>
  deploy-game-stack.sh attach <system> <stack> [service [command...]]
  deploy-game-stack.sh publish

Commands:
  new      Prepare a live runtime tree from desired state and seed missing env/config files.
  apply    Deploy the stack into the live runtime tree and start it.
  attach   Enter the primary container or run a command in a named service.
  publish  Copy the reusable deploy template into dev-forge.

Systems:
  alienware  Active game host at /home/lancer1977/game_servers
  r620       Legacy host at /home/lancer1977/servers
EOF
  exit 2
}

die() {
  echo "$*" >&2
  exit 1
}

compose() {
  if command -v docker-compose >/dev/null 2>&1; then
    docker-compose "$@"
  else
    docker compose "$@"
  fi
}

system_desired_dir() {
  system="$1"
  stack="$2"
  case "$system" in
    alienware) printf '%s/systems/alienware/stacks/%s\n' "$ROOT" "$stack" ;;
    r620) printf '%s/systems/r620/stacks/%s\n' "$ROOT" "$stack" ;;
    *) die "Unknown system: $system" ;;
  esac
}

system_live_root() {
  system="$1"
  case "$system" in
    alienware) printf '%s\n' "$ALIENWARE_LIVE_ROOT" ;;
    r620) printf '%s\n' "$R620_LIVE_ROOT" ;;
    *) die "Unknown system: $system" ;;
  esac
}

stack_live_dir() {
  system="$1"
  stack="$2"
  printf '%s/%s\n' "$(system_live_root "$system")" "$stack"
}

copy_desired_tree() {
  desired_dir="$1"
  live_dir="$2"

  mkdir -p "$live_dir"
  rsync -a \
    --exclude '.env' \
    --exclude 'wireguard/wg0.conf' \
    --exclude 'wireguard/wg_confs/' \
    "$desired_dir"/ \
    "$live_dir"/
}

render_or_seed_env() {
  desired_dir="$1"
  live_dir="$2"

  if [ -f "$live_dir/.env" ]; then
    return 0
  fi

  if [ -f "$desired_dir/infisical.env" ]; then
    # shellcheck disable=SC1090
    . "$desired_dir/infisical.env"
    if [ -f "${HOME:-/home/lancer1977}/.config/secrets/infisical.env" ]; then
      # shellcheck disable=SC1090
      . "${HOME:-/home/lancer1977}/.config/secrets/infisical.env"
    fi
    "$ROOT/scripts/render-env-from-infisical.py" \
      --project "${INFISICAL_PROJECT:?Missing INFISICAL_PROJECT for $desired_dir}" \
      --env "${INFISICAL_ENV:-prod}" \
      --path "${INFISICAL_PATH:?Missing INFISICAL_PATH for $desired_dir}" \
      --out "$live_dir/.env"
    return 0
  fi

  if [ -f "$live_dir/.env.example" ]; then
    cp "$live_dir/.env.example" "$live_dir/.env"
    return 0
  fi

  die "Missing live env file: $live_dir/.env"
}

seed_wireguard_placeholder() {
  live_dir="$1"

  if [ ! -d "$live_dir/wireguard" ]; then
    return 0
  fi

  if [ -f "$live_dir/wireguard/wg0.conf" ]; then
    return 0
  fi

  if [ -f "$live_dir/wireguard/wg0.conf.example" ]; then
    cp "$live_dir/wireguard/wg0.conf.example" "$live_dir/wireguard/wg0.conf"
  fi
}

ensure_bind_mount_dirs() {
  compose_file="$1"
  live_dir="$2"

  [ -f "$compose_file" ] || return 0

  awk -v live_dir="$live_dir" '
    /^[[:space:]]*-[[:space:]]/ {
      line=$0
      sub(/^[[:space:]]*-[[:space:]]*/, "", line)
      split(line, parts, ":")
      src=parts[1]
      if (src ~ /^\.\//) {
        sub(/^\.\//, "", src)
        print live_dir "/" src
      } else if (src ~ /^\//) {
        print src
      }
    }
  ' "$compose_file" | while IFS= read -r path; do
    [ -n "$path" ] || continue
    mkdir -p "$path"
  done
}

prepare_stack() {
  system="$1"
  stack="$2"

  desired_dir="$(system_desired_dir "$system" "$stack")"
  live_dir="$(stack_live_dir "$system" "$stack")"

  [ -d "$desired_dir" ] || die "Missing desired stack directory: $desired_dir"
  [ -f "$desired_dir/compose.yml" ] || die "Missing desired compose: $desired_dir/compose.yml"

  copy_desired_tree "$desired_dir" "$live_dir"
  render_or_seed_env "$desired_dir" "$live_dir"
  seed_wireguard_placeholder "$live_dir"
  ensure_bind_mount_dirs "$live_dir/compose.yml" "$live_dir"

  printf '%s\n' "$live_dir"
}

choose_primary_service() {
  services="$1"
  printf '%s\n' "$services" | awk '
    $0 !~ /(^|-)wg$/ &&
    $0 !~ /state-web/ &&
    $0 !~ /sidecar/ &&
    $0 !~ /^wireguard$/ {
      print
      exit
    }
  '
}

deploy_stack() {
  system="$1"
  stack="$2"

  live_dir="$(prepare_stack "$system" "$stack")"
  cd "$live_dir"
  compose -f compose.yml config >/dev/null
  compose -f compose.yml up -d --remove-orphans
  echo "Deployed $system/$stack from $(system_desired_dir "$system" "$stack") to $live_dir."
}

attach_stack() {
  system="$1"
  stack="$2"
  shift 2

  live_dir="$(prepare_stack "$system" "$stack")"
  cd "$live_dir"

  services="$(compose -f compose.yml config --services)"
  primary_service="$(choose_primary_service "$services")"
  [ -n "$primary_service" ] || die "Could not determine primary service for $system/$stack"

  if [ "${1:-}" = "logs" ]; then
    shift
    compose -f compose.yml logs -f --tail="${1:-100}"
    exit 0
  fi

  service="${1:-$primary_service}"
  if [ "$#" -gt 0 ]; then
    shift
  fi
  if [ "$#" -eq 0 ]; then
    set -- sh
  fi

  if command -v docker-compose >/dev/null 2>&1; then
    exec docker-compose -f compose.yml exec "$service" "$@"
  else
    exec docker compose -f compose.yml exec "$service" "$@"
  fi
}

publish_template() {
  dest_dir="$DEV_FORGE_ROOT/templates/repo/deploy/game-server"
  mkdir -p "$dest_dir"
  cp "$0" "$dest_dir/deploy-game-stack.sh"
  chmod +x "$dest_dir/deploy-game-stack.sh"
  cat > "$dest_dir/README.md" <<'EOF'
# Game Server Deploy Template

This folder captures the reusable deploy baseline for game-server repos.

Use the script in this template when you need to:

- prepare a fresh runtime tree from desired state
- apply a stack to an existing host directory
- attach to the primary container or run a command in a live service
- promote the stable script shape into a repo-local deploy helper

The reference pattern is the Windrose pair:

- canonical stack: `windrose`
- dev sibling: `windrose-dev`
- runtime roots: `/home/lancer1977/game_servers/windrose` and `/home/lancer1977/game_servers/windrose-dev`
- deploy flow: `new`, `apply`, `attach`, with no Portainer step

This baseline is intentionally generic. Repo-specific stack names, live roots,
and secret contracts should be provided by the owning repo.

Recommended local commands:

```sh
./scripts/deploy-game-stack.sh new <system> <stack>
./scripts/deploy-game-stack.sh apply <system> <stack>
./scripts/deploy-game-stack.sh attach <system> <stack>
```

The GitOps repo keeps the active Alienware game-server desired state. Dev-forge
keeps the reusable baseline so future repos can copy one stable shape.
EOF
  echo "Published game-server deploy template to $dest_dir."
}

if [ "$#" -lt 1 ]; then
  usage
fi

command="$1"
shift

case "$command" in
  new|apply|attach)
    [ "$#" -ge 2 ] || usage
    system="$1"
    stack="$2"
    shift 2
    case "$command" in
      new)
        prepare_stack "$system" "$stack" >/dev/null
        echo "Prepared $system/$stack."
        ;;
      apply)
        deploy_stack "$system" "$stack"
        ;;
      attach)
        attach_stack "$system" "$stack" "$@"
        ;;
    esac
    ;;
  publish)
    publish_template
    ;;
  *)
    usage
    ;;
esac
