# Blazor app overlay

## Stack clues
- Check for `*.csproj`, `Program.cs`, and `Pages` or `Components` directories.

## Common commands
- Restore: `dotnet restore`
- Build: `dotnet build`
- Test: `dotnet test`
- Publish: `dotnet publish`
- Run locally: `dotnet run`

## Safety boundaries
- Do not change deployment mode (`Server`/`WebAssembly`) or auth setup without explicit approval.
- Do not update environment URLs or token-related config without explicit approval.
- Keep component and service changes scoped to requested behavior only.

## Validation notes
- Confirm route and interaction changes with feature notes before reporting completion.
