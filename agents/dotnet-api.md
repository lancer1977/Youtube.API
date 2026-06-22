# .NET API overlay

## Stack clues
- Check for `*.sln`, `*.csproj`, `Program.cs`, `appsettings*.json`, and `Dockerfile`/compose files.

## API-specific notes
- Verify route and contract changes with adjacent docs or integration notes.
- Prefer incremental, issue-scoped edits and targeted tests.
- Keep auth, tokens, and migration behavior change scope explicit in the issue packet.

## Common commands
- Restore: `dotnet restore <solution_or_project>`
- Build: `dotnet build <solution_or_project> --no-restore`
- Test: `dotnet test <solution_or_project_or_test_project> --no-restore`
- Format: `dotnet format <solution_or_project>`
- Run locally: `unknown`

## Safety boundaries
- Do not edit `appsettings*.json` production values without explicit approval.
- Do not change authentication, authorization, callback URLs, or connection strings unless requested in the ticket.
- Do not update package feeds or signing/certification settings without explicit approval.
