# UISupportBlazor – Testing Strategy

## Overview

Because `UISupportBlazor` is a rendering library driven by runtime reflection, the testing strategy focuses on three areas:

1. **Unit tests** – UI model generation logic (via `UISupportGeneric`).
2. **Component tests** – Blazor component rendering (using bUnit).
3. **Integration tests** – full round-trip: back-end object → UI model → rendered HTML + API call.

---

## 1. Unit Tests (UISupportGeneric Layer)

Test that the assembly analyzer produces the correct `ClassInfo` for various back-end types.

```csharp
// Example using xUnit
public class ClassInfoTests
{
	[Fact]
	public void HiddenFromGUI_Attribute_ExcludesMember()
	{
		var info = GetClassInfo.Build(new MyBackEnd());
		Assert.DoesNotContain(info.Elements, e => e.Name == "InternalState");
	}

	[Fact]
	public void Range_Attribute_SetsValidationBounds()
	{
		var info = GetClassInfo.Build(new MyBackEnd());
		var element = info.Elements.OfType<EditableUIMember>()
						 .First(e => e.Name == "Progress");
		Assert.Equal(0, element.Min);
		Assert.Equal(100, element.Max);
	}
}
```

---

## 2. Component Tests (bUnit)

Use **bUnit** to render Blazor components in isolation and assert rendered output.

```csharp
using Bunit;

public class ObjectEditorTests : TestContext
{
	[Fact]
	public void ObjectEditor_RendersAllPublicProperties()
	{
		var instance = new SimpleModel { Name = "Test", Value = 42 };
		var cut = RenderComponent<ObjectEditor>(p =>
			p.Add(c => c.Target, instance));

		Assert.Contains("Name", cut.Markup);
		Assert.Contains("Value", cut.Markup);
	}
}
```

---

## 3. Integration Tests (API Round-Trip)

Test the full pipeline using `WebApplicationFactory<Program>` or a test server.

```csharp
public class ApiMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
	[Fact]
	public async Task Post_Api_InvokesMethod_ReturnsResult()
	{
		var body = JsonSerializer.Serialize(new { command = "GetStatus", args = Array.Empty<object>() });
		var response = await _client.PostAsync("/api",
			new StringContent(body, Encoding.UTF8, "application/json"));

		response.EnsureSuccessStatusCode();
		var json = await response.Content.ReadAsStringAsync();
		Assert.Contains("result", json);
	}
}
```

---

## Test Coverage Goals

| Area | Target coverage |
|---|---|
| ClassInfo / UI model building | ≥ 90 % |
| Attribute validation logic | 100 % |
| ApiMiddleware request dispatch | ≥ 80 % |
| Blazor components (smoke render) | All components render without exception |

---

## Running Tests

```powershell
dotnet test
```

---

## Tools

| Tool | Purpose |
|---|---|
| **xUnit** | Unit test framework |
| **bUnit** | Blazor component testing |
| **Moq** | Mocking dependencies |
| **WebApplicationFactory** | ASP.NET Core integration tests |
