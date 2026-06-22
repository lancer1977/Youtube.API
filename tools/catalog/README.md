# MCP Tool Catalog Template

This folder contains reusable starter files for adding a new MCP-backed tool to a Dev-Forge-style repo.

Use these templates when you want a new tool to follow the same manifest, credential, and deployment shape as the first-wave catalog.

## Files

- `mcp-tool.yaml` - starter catalog entry for a new MCP-backed tool

## How to use

1. Copy `mcp-tool.yaml` into `tools/catalog/tools/<tool-id>.yaml` in the target repo.
2. Replace the placeholder IDs, commands, and secret names with the real tool contract.
3. Add the matching wrapper, health command, and deployment notes in the target repo.
4. Run the catalog validator and wrapper generator before publishing the change.

## What the template expects

- one stable `id`
- a single executable entrypoint
- explicit secret requirements
- explicit health behavior
- explicit deployment targets
- read/write policy defaults that keep the tool read-only unless the tool truly needs writes

## Related templates

- `templates/repo/docs/features/feature-template.md`
- `templates/repo/deploy/README.md`
- `templates/repo/scripts/README.md`
