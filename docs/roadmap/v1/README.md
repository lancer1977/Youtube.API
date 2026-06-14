# Api.Youtube Roadmap (v1)

## Vision
Keep the YouTube package small, testable, and explicit about the split between read-only Data API access and optional live chat/outbound support.

## Current Status
Production-ready for the core Data API surface and the optional net10.0 live stack.

## Goals
- Keep the core API surface documented and easy to wire into hosts.
- Preserve the clear split between read-only API calls and OAuth-backed live chat.
- Keep the live chat path gated behind explicit config and net10.0 builds.
- Standardize YouTube live stream lifecycle: discover -> poll -> publish -> route outbound.
- Keep startup and shutdown deterministic with backoff and rate-limit handling.

## Known Gaps
- Live broadcast discovery still depends on network-accessible YouTube state.
- Live chat validation cannot be fully replaced by offline tests.

## Release Notes

### 2026-06-13
- Added the streaming-ready integration stack:
  - net10-only live surface
  - validated options lifecycle
  - `IYouTubeApiClientFactory`
  - `IYouTubeAuthStateStore`
  - `IYouTubeLiveChatPoller`
  - `IYouTubeLiveClient`
  - checkpoint-backed live-chat resume
  - id rebind recovery for rotated or expired live chat ids
  - explicit streamer-keyed auth-state resolution

## Hermes Kanban
- Hermes Kanban: t_streaming-ready-v1
