---
description: C# code style rules for this project
---

# Code Style

This project follows [Microsoft's C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

An `.editorconfig` will be added to the repository root in the future to enforce these rules automatically. Until then, follow the conventions manually.

Key rules to apply:

- PascalCase for types, methods, properties, and public fields
- camelCase with `_` prefix for private fields (`_myField`)
- `var` when the type is apparent from the right-hand side; explicit type otherwise
- Allman brace style (opening brace on its own line)
- One class per file; file name matches the type name
- Prefer expression-bodied members for single-line getters and trivial methods
- Prefer `is null` / `is not null` over `== null` / `!= null`