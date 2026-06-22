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
- config contract: `.env` plus `.env.example` in each runtime tree
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
