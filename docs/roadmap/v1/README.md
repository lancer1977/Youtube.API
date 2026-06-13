# Api.Youtube Roadmap (v1)

## Vision
Keep the YouTube package small, testable, and explicit about the split between read-only Data API access and optional live chat/outbound support.

## Current Status
Stable for the core Data API surface. Live chat is implemented as an optional net10.0-only surface and still depends on live account validation.

## Goals
- Keep the core API surface documented and easy to wire into hosts.
- Preserve the clear split between read-only API calls and OAuth-backed live chat.
- Keep the live chat path gated behind explicit config and net10.0 builds.

## Known Gaps
- Live broadcast discovery still depends on network-accessible YouTube state.
- Live chat validation cannot be fully replaced by offline tests.
- The roadmap template should stay concrete rather than drifting back into placeholder state.
