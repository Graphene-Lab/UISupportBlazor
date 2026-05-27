# UISupportBlazor – API Reference

## ApiMiddleware

```csharp
public class ApiMiddleware
```

### Constructor

```csharp
ApiMiddleware(
	RequestDelegate next,
	Type apiCommandSet,
	string? apiPubblicKey = null,
	string? apiPath = "/api")
```

| Parameter | Description |
|---|---|
| `next` | Next ASP.NET Core middleware |
| `apiCommandSet` | Type whose public static methods are exposed as API commands |
| `apiPubblicKey` | Optional RSA public key for request encryption |
| `apiPath` | URL path prefix (default `/api`) |

### HTTP Contract

- **Method**: `POST`
- **Content-Type**: `application/json`
- **Body**: `{ "command": "<method name>", "args": [ ... ] }`
- **Response**: `{ "result": <value>, "error": "<message or null>" }`

---

## Razor Components

### `<ObjectEditor>`

Renders all public editable members of the target object as form fields.

| Parameter | Type | Description |
|---|---|---|
| `Target` | `object` | The back-end object to edit |

### `<ObjectMember>`

Renders a single member (property or field).

| Parameter | Type | Description |
|---|---|---|
| `Member` | `BaseUISupport` | The UI model element to render |

### `<ObjectArray>`

Renders an `IEnumerable` property as a table or list.

### `<Menu>`

Auto-generates a navigation menu from the top-level sections of `Target`.

### `<Nav>`

Handles navigation state between sections/panels.

### `<Message>`

Displays status/toast messages.

| Parameter | Type | Description |
|---|---|---|
| `Text` | `string` | Message content |
| `IsError` | `bool` | Renders as error if `true` |

### `<Tooltip>`

Shows contextual help text, sourced from XML doc-comments via `MarkdownParser`.

### `<Warning>`

Displays validation or system warnings.

---

## Support (static helpers)

```csharp
public static class Support
```

| Method | Description |
|---|---|
| `GetSession(...)` | Retrieves or creates the `Session` for the current user |

---

## Session

```csharp
public class Session
```

Holds per-user navigation and object state. Registered as a Blazor scoped service.

| Property | Description |
|---|---|
| `CurrentObject` | Currently displayed back-end object |
| `NavigationStack` | Breadcrumb navigation history |
