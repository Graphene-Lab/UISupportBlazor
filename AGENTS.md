# UISupportBlazor — notes for coding agents

> Read this file first. This library is the Blazor half of the auto-generated UI: components, the
> API middleware and the session — no executable of its own.

## What this is

The components a generated page is built from (`ObjectMember.razor` and the rest), the middleware
that exposes a back-end class over HTTP, the session model, and the Markdown parser used in the chat.

## Who consumes it

`AIOffice` — the only consumer today: its panels render through these components, and
`ObjectMember.razor` is the multi-file picker that feeds the attachment pipeline.

## Read before you edit

| If you touch… | Read first |
|---|---|
| the UI model, the generated pages | [docs/overview.md](docs/overview.md), [docs/developer-guide.md](docs/developer-guide.md) |
| a design decision | [docs/adr.md](docs/adr.md) |
| the HTTP surface | [docs/api-reference.md](docs/api-reference.md) and `ApiMiddleware.cs` |

## Non-negotiables

1. **The attachment picker contract.** `ObjectMember.razor` renders a `FileAttachment[]` parameter
   as `<InputFile Multiple>` (detected through `ClassInfo.IsAttachmentCollection`), appends,
   de-duplicates by name and enforces `Support.MaxAttachmentSizeBytes` (100 MB). The original bytes
   are uploaded — the conversion to Markdown happens server-side, never in the browser.
2. `ApiMiddleware` is the HTTP surface of a generated API: a change there changes every endpoint
   the UI exposes.
3. A component change is a UI change for every generated page, not just the one you are looking at.
4. The package id is implicit (the assembly name `UISupportBlazor`).

## Where things are

- `Components/` — the Razor components
- `ApiMiddleware.cs` — the HTTP surface
- `Session.cs`, `Support.cs` — session and shared support code
- `MarkdownParser.cs` — Markdown rendering used by the chat
- `wwwroot/` — static assets shipped with the package
- `docs/` — overview, api-reference, developer-guide, adr, security-policy, testing-strategy

## Library contract

Date version `1.yy.MM.dd`, README and LICENSE packed, published on a `v*` tag.
