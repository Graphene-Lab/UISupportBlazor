# UISupportBlazor – Architecture Decision Records

## ADR-001: Assembly-Driven Deterministic UI Generation

**Date**: 2024  
**Status**: Accepted

### Context
Traditional Blazor development requires writing both back-end logic and front-end Razor components, effectively doubling development effort. Generative AI tools (Copilot, ChatGPT) can suggest code but produce probabilistic output that varies across runs and may introduce subtle errors.

### Decision
Generate the entire Blazor front-end **deterministically at runtime** by reflecting over the compiled back-end assembly. The assembly is treated as the canonical, unambiguous specification for the UI.

### Consequences
- **Positive**: single codebase; front-end is always in sync with back-end; output is reproducible; no generative AI non-determinism.
- **Positive**: suitable for certified/regulated systems (banking, healthcare, industrial).
- **Negative**: UI customisation beyond the generated defaults requires additional layer (CSS, partial class overrides).

---

## ADR-002: ApiMiddleware as a Single POST Endpoint

**Date**: 2024  
**Status**: Accepted

### Context
The auto-generated UI needs to call back-end methods. REST APIs with separate routes per method would require either code generation or reflection at request time.

### Decision
Expose a **single POST `/api` endpoint** that accepts `{ "command": "MethodName", "args": [...] }`. The middleware uses reflection to locate and invoke the method at runtime.

### Consequences
- **Positive**: zero boilerplate — any `[IsPublicApi]`-decorated method becomes callable automatically.
- **Positive**: optional request encryption can be applied uniformly to the single endpoint.
- **Negative**: non-standard REST convention; tooling such as Swagger requires a custom description adapter.

---

## ADR-003: .NET Standard 2.1 as Target Framework

**Date**: 2024  
**Status**: Accepted

### Context
The library must be usable from both .NET 5+ server applications and Blazor WebAssembly (which has a restricted runtime).

### Decision
Target **.NET Standard 2.1** to maximise compatibility.

### Consequences
- **Positive**: compatible with .NET 5, 6, 7, 8, 9 and Blazor WASM.
- **Negative**: cannot use APIs introduced after .NET Standard 2.1 (e.g., `Span<T>` overloads, generic math).

---

## ADR-004: XML Doc-Comments as UI Labels and Tooltips

**Date**: 2024  
**Status**: Accepted

### Context
The generated UI needs human-readable labels, descriptions, and help text without requiring a separate metadata file.

### Decision
Read `<summary>` and `<param>` XML doc-comments from the assembly's embedded XML documentation file and use them as labels, descriptions, and tooltip text in the generated UI.

### Consequences
- **Positive**: documentation and UI labels are written once, in the source code.
- **Positive**: Markdown formatting in doc-comments is rendered via `MarkdownParser`.
- **Negative**: XML documentation must be enabled in the back-end project (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`).
