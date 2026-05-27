# UISupportBlazor – Security Policy

## Input Handling

All user input displayed in Razor components is HTML-encoded by Blazor by default, preventing XSS. Do not use `@((MarkupString)userInput)` unless the source is fully trusted and sanitised.

The `MarkdownParser.cs` component converts Markdown to HTML. Ensure the Markdown source is from a trusted origin. If user-supplied Markdown is rendered, apply HTML sanitisation before passing it to `MarkupString`.

## Session Management

`Session.cs` manages per-user server-side state in Blazor Server. Sessions are tied to the SignalR circuit lifetime:

- Sessions are automatically invalidated when the circuit disconnects.
- Do not store unencrypted sensitive data (passwords, tokens) in the session object.
- Use `SecureStorage` for persistent sensitive data.

## JavaScript Interop

`ExampleJsInterop.cs` and `exampleJsInterop.js` demonstrate JS interop patterns. When passing data between .NET and JavaScript:

- Validate all data received from JavaScript on the .NET side.
- Avoid passing raw HTML strings from JavaScript to .NET for server-side rendering.

## API Middleware

`ApiMiddleware.cs` handles API requests within the Blazor Server pipeline. Ensure:

- All API endpoints validate the caller's identity before processing.
- Rate limiting is applied at the reverse proxy level.

## Dependency Security

UISupportBlazor depends on `UISupportGeneric`, `EncryptedMessaging`, and `SecureStorage`. Keep all dependencies updated to incorporate security patches.

## Reporting Vulnerabilities

Open a private GitHub Security Advisory in the repository. Do not disclose vulnerabilities publicly before a fix is available.
