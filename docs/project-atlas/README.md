# Project Atlas

## Purpose

`Youtube.API` provides the `PolyhydraGames.APi.Youtube` .NET package for
YouTube Data API helpers and optional live-chat inbound/outbound integration for
Polyhydra Games hosts.

## Primary Workflow

1. Update read-only API helpers and configuration under `src/`.
2. Keep live-chat behavior isolated behind `AddYouTubeLiveStreaming`.
3. Add unit tests under `test/` for registration, routing, option validation,
   poller behavior, and checkpoint behavior.
4. Keep OAuth/live API tests explicit so default CI does not require YouTube
   credentials.

## Validation

Run:

```bash
dotnet restore APi.Youtube.sln
dotnet build APi.Youtube.sln --configuration Release --no-restore
dotnet test APi.Youtube.sln --configuration Release --no-restore --verbosity minimal
dotnet list APi.Youtube.sln package --outdated
devstudio validate --repo /mnt/data/lancer1977/code/Api.Youtube
```

Live validation requires YouTube API and OAuth credentials plus a target channel
or broadcast. Keep credentials in user secrets, environment variables, or a
secret store.

## Boundaries

- This repo owns the YouTube package, package tests, package publishing, and
  package-level setup docs.
- Consuming hosts own runtime secret storage, OAuth refresh-token management,
  channel selection, and user-facing behavior.
- Generated runtime output under `.devstudio/runtime/` is local evidence, not
  source.
