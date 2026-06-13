# Api.Youtube

**Location:** `~/code/APIs/Api.Youtube`

**Purpose:** YouTube Data API and live chat integration.

**Assembly:** `PolyhydraGames.APi.Youtube`

## Dependencies

- `Google.Apis.YouTube.v3`

## Config

Has config (`hasConfig: true`). Expect:
- API key for read-only calls
- OAuth client credentials and refresh token for live chat / outbound chat
- live channel id or broadcast id for chat discovery

## Related

- [Setup](./setup.md)

## Status

✅ **Working** (small library + tests)
