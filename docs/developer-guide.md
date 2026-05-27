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
