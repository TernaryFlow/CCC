# CSharp to Cloudflare Workers Compiler

A modern open-source compiler that allows developers to execute a subset of C# on Cloudflare Workers without requiring the .NET CLR.

## Overview

This compiler transforms C# source code into clean, readable, tree-shakeable ES2025+ JavaScript that runs natively on Cloudflare Workers.

**Key Features:**
- ✅ No WebAssembly
- ✅ No .NET runtime dependency
- ✅ No Mono or CoreCLR embedding
- ✅ Clean, optimized JavaScript output
- ✅ Full support for modern C# features
- ✅ Cloudflare Workers native integration

## Architecture

```
C# Source
    ↓
Roslyn Parser (Compiler.Parser)
    ↓
Syntax Tree + Semantic Model (Compiler.Semantics)
    ↓
Intermediate Representation (Compiler.IR)
    ↓
JavaScript Code Generator (Compiler.CodeGen)
    ↓
ES2025 Modules + Runtime (Compiler.Runtime)
```

## Project Structure

| Project | Description |
|---------|-------------|
| `Compiler.Core` | Shared types, utilities, and abstractions |
| `Compiler.Parser` | Roslyn-based C# parsing |
| `Compiler.Semantics` | Semantic analysis, type resolution |
| `Compiler.IR` | Intermediate representation definitions |
| `Compiler.CodeGen` | JavaScript code generation |
| `Compiler.Runtime` | Lightweight JavaScript runtime (<100KB) |
| `Compiler.CLI` | Command-line interface |
| `Compiler.SDK` | Developer SDK for Workers |
| `Samples` | Example projects |
| `Tests` | Unit, integration, and snapshot tests |

## Installation

```bash
dotnet tool install -g csharp-workers
```

## Quick Start

### Create a Worker

```csharp
using Cloudflare.Workers;
using System.Threading.Tasks;

public class MyWorker
{
    public async Task<Response> Fetch(Request request, Env env, ExecutionContext ctx)
    {
        return Response.Json(new { message = "Hello from C#!" });
    }
}
```

### Compile

```bash
csw compile Worker.cs
csw publish worker
```

## Supported Features

### Language Features
- ✅ Namespaces, classes, records, structs
- ✅ Interfaces, inheritance, virtual/abstract methods
- ✅ Properties, fields, events, delegates
- ✅ Lambdas, local functions
- ✅ Pattern matching, switch expressions
- ✅ Nullable reference types
- ✅ LINQ (compiled to native array methods)
- ✅ async/await
- ✅ Exceptions
- ✅ Generics
- ✅ Object/collection initializers

### Cloudflare Bindings
- ✅ KV Storage
- ✅ R2 Buckets
- ✅ D1 Database
- ✅ Durable Objects
- ✅ Queues
- ✅ Cache API
- ✅ AI Binding
- ✅ Workers Analytics

## Unsupported Features (v1)

These features are intentionally not supported in v1:
- ❌ Reflection
- ❌ Dynamic
- ❌ Expression Trees
- ❌ Unsafe code
- ❌ P/Invoke
- ❌ Threading (beyond async/await)
- ❌ AppDomain
- ❌ Marshal
- ❌ COM

Extension points are designed for future implementation.

## CLI Commands

```bash
csw compile <file|project>    # Compile C# to JavaScript
csw watch                      # Watch mode for development
csw publish <worker-name>      # Deploy to Cloudflare
csw bundle                     # Generate bundled output
```

## Runtime

The runtime provides essential .NET-like APIs:
- `System.Object`, `String` helpers
- `List<T>`, `Dictionary<TKey,TValue>`, `Queue`, `Stack`
- `IEnumerable<T>`, `IEnumerator<T>`
- LINQ extension methods
- `Task`, `CancellationToken`
- `DateTime`, `Guid`
- JSON serialization

Total runtime size: <100KB minified.

## Documentation

- [Architecture Guide](docs/architecture.md)
- [Compilation Pipeline](docs/pipeline.md)
- [Extensibility Model](docs/extensibility.md)
- [API Reference](docs/api.md)

## License

MIT License
