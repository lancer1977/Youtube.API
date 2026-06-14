# YouTube API Integration

YouTube Data API v3 helpers with an optional live chat/outbound surface for Polyhydra Games hosts.

## Tags

- api
- dotnet
- api-youtube
- youtube
- docs
- testing

## Project Structure

- `Src/`: Core library implementation.
- `Test/`: NUnit tests for the library.
- `docs/`: Detailed documentation and design decisions.

## Package Facts

- `Project`: `PolyhydraGames.APi.Youtube`
- `TargetFrameworks`: `net8.0;net10.0`
- `Language`: C#
- `Test project`: `test/Test.csproj`

## Getting Started

### Prerequisites
- .NET 10.0 SDK

## Testing
The project uses NUnit for testing. To run tests:
```bash
dotnet test APi.Youtube.sln
```

## Documentation
Additional documentation can be found in the [docs](docs/) directory.

## Docs

- [Docs Index](./docs/README.md)
- [Setup](./docs/setup.md)
- [POC](./docs/poc.md)
- [Feature Index](./docs/features/README.md)
- [Roadmap Index](./docs/roadmaps/README.md)

## Notes

- Keep read-only API helpers separate from the live chat surface.
- The live chat path is intended to stay optional and explicit about its OAuth requirements.

## Streamed surface quickstart

```csharp
services.AddYouTubeApi(configuration);          // data plane
services.AddYouTubeLiveStreaming(configuration); // inbound + outbound + poller
```

## Publishing

Use `./pub.sh` to restore, test, pack, and publish.

- GitHub Packages is the default publish target.
- Set `PUBLISH_NUGET_ORG=true` only when the package should also go to nuget.org.
- Set `DRY_RUN=true` to skip pushes.
- Use `PACKAGE_API_KEY`, `GHCR_TOKEN`, `GITHUB_PACKAGES_TOKEN`, `GITHUB_TOKEN`, or `GH_TOKEN` for GitHub Packages auth.

For private/internal consumption from GitHub Packages, add the GitHub NuGet
source for this organization and authenticate with a GitHub token or PAT:

```bash
dotnet nuget add source https://nuget.pkg.github.com/lancer1977/index.json \
  --name lancer1977 \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_TOKEN \
  --store-password-in-clear-text
```
