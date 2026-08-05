# 🔒 Security Audit — GuideCraft v1.1.0

This document describes the security posture of GuideCraft and the audit performed
by a simulated attacker (the author). It is intended for transparency and to help
users make informed decisions about trusting the application with their API keys.

> **Scope:** Local Windows WPF desktop application. Single-user, no network server,
> no telemetry.

## Threat Model

The application holds a **third-party API Key** (Qwen DashScope or DeepSeek) which
gives the bearer the ability to invoke paid or rate-limited inference on the
provider's platform. The key concern is:

- Can an attacker obtain a user's API Key (or a useful substitute) by interacting
  with the application, its files, or its network traffic?

We are NOT concerned with:

- Code execution on the user's machine (single-user trust boundary).
- Server-side compromise (we do not run a server; all state is local).

## Storage & Transport

| Path | What | Protection |
|------|------|------------|
| `%AppData%\GuideCraft\guidecraft.db` | SQLite database | Per-user ACL (default). **API Key is stored as DPAPI-encrypted ciphertext** under `settings.api_key`. |
| `settings` table — `preferred_model`, `theme`, `language`, `sidebar_position`, `welcome_shown` | UI preferences | Plain text (non-sensitive). |
| `messages`, `projects`, `conversations` tables | Conversation history & generated code | Plain text. Contains user prompt content but never the API Key. |
| Network calls | HTTPS only (`https://dashscope.aliyuncs.com/...`, `https://api.deepseek.com/...`) | TLS 1.2+. |
| Exports (ZIP) | `main.py`, `requirements.txt`, `README.md`, `REQUIREMENT.md` | Never include the API Key. |

## DPAPI Encryption (API Key at rest)

```csharp
// Services/SettingsService.cs
var bytes = ProtectedData.Protect(
    Encoding.UTF8.GetBytes(plain),
    null,
    DataProtectionScope.CurrentUser);
```

`DataProtectionScope.CurrentUser` means only the same Windows user account can
decrypt. The ciphertext is stored as Base64 in the `settings` table.

**Failure mode:** if the user's profile is corrupted, or a backup is restored to
another user account, decryption fails with `CryptographicException`. The
application catches this and silently treats the Key as empty, prompting the
user to re-enter it. No key material is leaked.

## Memory

The decrypted API Key is held in `SettingsService.Settings.ApiKey` (a managed
`string`) for the process lifetime so the runtime can make inference calls.
A process-memory dump by another process running as the same user could expose
it. This is a fundamental limitation of any in-process credential handling and
is acknowledged. Mitigations:

- The Key is only used inside the `LlmApiClient` request pipeline and never
  serialized, logged, copied to disk, or echoed in error messages.
- The setting dialog uses a `PasswordBox` (masked input).
- No console/stdout logging is enabled in any build configuration.

## Error Messages

`LlmApiException` messages contain only the HTTP status code and a generic
description — never the API Key, request headers, or response body. Example:

```
HTTP 401
```

UI maps status codes to localized messages (e.g. `Str.Chat.ApiKeyInvalid`),
which do not embed the Key. Verified by grep over the source tree:

```
$ grep -rn "ApiKey" --include="*.cs" | grep -v "_settings.Settings"
  (only FallbackZh/Resources strings reference the literal phrase "API Key")
```

## Input Validation

- All SQL goes through `Microsoft.Data.Sqlite` parameters — no string
  concatenation → no SQL injection vector.
- User prompts and generated code are written verbatim to the `messages` /
  `projects` tables. They are displayed via `MarkdownRenderer` which converts
  only a known-safe Markdown subset (`MarkdownView` does NOT execute HTML or
  scripts).
- The ZIP exporter validates the file path via `SaveFileDialog`; the title is
  sanitized through `Path.GetInvalidFileNameChars`.

## Network

- All outbound HTTP(S) uses a single, long-lived `HttpClient` (singleton) with
  `Timeout = 5 minutes` (required for streaming SSE).
- HTTPS endpoints are hard-coded in `LlmCatalog`. There is **no user-controlled
  URL input** → no SSRF.
- Authorization headers (`Bearer <key>`) are constructed per request and never
  cached beyond a single call.

## Auto-Update

`UpdateChecker` queries `https://api.github.com/repos/YYRMMAYO/GuideCraft/releases/latest`.
The response body is parsed with strict `System.Text.Json`; `tag_name` is parsed
as `Version` with `TryParse` (no exceptions on bad input). The release URL is
opened via `Process.Start` which respects the user's default browser. There is
no automatic download/install of binaries.

## Known Limitations (acknowledged, not fixed)

1. **Same-user process memory** — a process dump by another admin-context
   process can read the decrypted Key.
2. **Unsigned executable** — Windows SmartScreen may show a warning on first
   launch. Users must click "More info → Run anyway".
3. **DPAPI does not protect against the user themselves** — a user can read
   their own Key by re-running the application. This is by design.
4. **No code-signing certificate** — releases are unsigned. Consider adding
   this for production distribution.

## Audit Checklist

| Item | Status |
|------|--------|
| API Key not in plain-text on disk | ✅ DPAPI-encrypted |
| API Key not in exports / logs / error messages | ✅ Verified by grep |
| HTTPS only | ✅ Hard-coded endpoints |
| Parameterized SQL | ✅ Microsoft.Data.Sqlite parameters |
| No user-controlled URL / SSRF | ✅ |
| Password input masked | ✅ PasswordBox |
| Markdown rendering safe (no HTML exec) | ✅ FlowDocument subset |
| Update check safe | ✅ Strict parse + user-driven download |
| Cryptographic failure handled gracefully | ✅ TryDecrypt returns empty on `CryptographicException` |
| Trim normalization | ✅ SaveApiKey trims input |

## Reporting Vulnerabilities

If you discover a security issue, please open a GitHub Issue or contact the
author (YYRMMAYO) directly. Do not disclose publicly before a fix is released.