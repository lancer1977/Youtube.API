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
