# UISupportBlazor – Developer Guide

## Installation

Add the NuGet package to your Blazor Server project:

```bash
dotnet add package UISupportBlazor
```

Or add a project reference when working in the same solution.

## Quick Start

### 1. Register the middleware in `Program.cs`

```csharp
// Register the ApiMiddleware to expose your back-end type
app.UseMiddleware<UISupportBlazor.ApiMiddleware>(typeof(MyBackEndClass));
```

### 2. Add the auto-GUI to a page

```razor
@using UISupportBlazor

<ObjectEditor Target="@myInstance" />
```

`ObjectEditor` reflects `myInstance` and renders all public properties as editable form fields, complete with validation driven by attributes defined in `UISupportGeneric`.

### 3. Add navigation

```razor
<Menu Target="@myInstance" />
```

The `Menu` component generates navigation entries for each logical section detected in the back-end object.

## Controlling the Generated UI

Use attributes from `UISupportGeneric` to refine the generated interface:

| Attribute | Effect |
|---|---|
| `[HiddenFromGUI]` | Hides the member from the UI |
| `[HiddenBind]` | Hides but still binds the value |
| `[IsPublicApi]` | Marks a method as callable from the API endpoint |
| `[Range]` | Adds min/max validation |
| `[Regex]` | Adds regex-based validation |
| XML `<summary>` | Used as label / tooltip in the UI |

## File Attachments (multi-file picker)

A method parameter of type `FileAttachment[]` (or `List<FileAttachment>` / `IEnumerable<FileAttachment>`)
is rendered by `ObjectMember` as a **standard multi-file input** (`<InputFile Multiple>`).
The names of the selected files are listed under the picker, each with a ✕ button to remove it.

```csharp
public void Run(string prompt, FileAttachment[]? attachments = null) { ... }
```

Rules of the feature:

- **Add several files at once or one at a time.** Each new selection is APPENDED to the
  already-selected files (Ctrl/Shift-click selects several in one dialog; picking a single file
  in a second dialog adds it to the previous ones). Files with the same name are not duplicated.
- **Original binary, no client-side conversion.** `ObjectMember.OnFilesSelected` reads each file
  with `OpenReadStream` and stores it in a `FileAttachment` (name + bytes). The conversion to
  Markdown is performed server-side by the consumer (e.g. `AllToMarkdown.Converter`, or
  Z.ai GLM-OCR for images), not in the browser.
- **Size limit.** A single file cannot exceed `Support.MaxAttachmentSizeBytes` (default 100 MB);
  the value is a mutable static so applications can raise/lower it.
- **Optional.** If nothing is selected the method receives `null` (declare the parameter nullable).
- **Context budget.** The consumer may truncate the injected content to fit the model context
  (see `AgentOrchestrator.MaxAttachmentContextChars` and `TruncateMarkdown` in AIOrchestrator).
- **Works everywhere a `FileAttachment[]` member is rendered** — method parameters and properties
  alike: the Voice panel exposes a static `FileAttachment[]` property so attachments also flow
  into the streaming chat path.

## API Middleware

`ApiMiddleware` intercepts HTTP POST requests to `/api` (configurable):

```http
POST /api
Content-Type: application/json

{ "command": "MethodName", "args": [arg1, arg2] }
```

Response:
```json
{ "result": <return value>, "error": null }
```

Only methods decorated with `[IsPublicApi]` are accessible.

### Optional API Encryption

Pass a public key to `ApiMiddleware` to enable request encryption:

```csharp
app.UseMiddleware<UISupportBlazor.ApiMiddleware>(typeof(MyApi), myPublicKey);
```

## Session Management

`Session` holds per-user state (current object, navigation history). Inject it as a Blazor scoped service or manage it manually via `Support.GetSession`.

## Extending Components

All razor components are `partial` classes. You can subclass or wrap them to customise rendering while keeping the automated binding logic intact.

## Building

```powershell
dotnet build ..\..\UISupportBlazor\UISupportBlazor.csproj
```
