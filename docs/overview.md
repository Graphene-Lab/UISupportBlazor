# UISupportBlazor – System Overview

## Purpose

**UISupportBlazor** is a Blazor component library that implements **deterministic, assembly-driven automatic front-end generation**. Given a compiled .NET back-end assembly, the library analyzes its types and members at runtime and renders a complete, interactive Blazor user interface — without any manual front-end code.

The technology aims to reduce overall development effort by up to **70 %** by eliminating the traditional front-end development phase entirely.

## How It Works

```
Back-end Assembly (compiled)
		│
		▼
UISupportGeneric (assembly analyzer)
		│  builds ClassInfo / UISupport object graph
		▼
UISupportBlazor Razor components
		│  render HTML + handle user interactions
		▼
Browser
```

1. The developer writes only back-end logic (plain C# classes, properties, methods).
2. At runtime, `UISupportGeneric` reflects over the compiled assembly and builds an in-memory UI model.
3. `UISupportBlazor` razor components consume that model and render panels, editors, tables, tooltips, warnings, and navigation menus automatically.
4. API calls between browser and back-end are handled by `ApiMiddleware`, which exposes back-end methods as an encrypted POST endpoint.

## Key Components

| File | Role |
|---|---|
| `ApiMiddleware.cs` | ASP.NET Core middleware – exposes back-end methods via POST `/api` |
| `Session.cs` | Per-user session state |
| `Support.cs` | Blazor-specific helpers and extension methods |
| `MarkdownParser.cs` | Renders Markdown descriptions from XML doc-comments |
| `ObjectEditor.razor` | Auto-generated form for editing an object's properties |
| `ObjectMember.razor` | Renders a single property/field |
| `ObjectArray.razor` | Renders collections |
| `Menu.razor` | Auto-generated navigation menu |
| `Nav.razor` | Navigation logic |
| `Message.razor` | Toast / status messages |
| `Tooltip.razor` | Contextual help derived from XML doc-comments |
| `Warning.razor` | Validation / error display |

## Deterministic vs. Probabilistic AI

Unlike LLM-based code generators (which produce probabilistically variable output), UISupportBlazor operates **deterministically**: the same back-end assembly always produces exactly the same UI, making the approach suitable for certified, regulated, and safety-critical systems.

## Target Framework

**.NET Standard 2.1** (compatible with .NET 5+, Blazor Server and Blazor WebAssembly).

## Dependencies

- `UISupportGeneric` – core assembly analyzer
- `Microsoft.AspNetCore.Components.Web` – Blazor primitives
