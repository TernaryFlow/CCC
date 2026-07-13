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
| `Compiler.EFCore.Shim` | EF Core compatibility layer for D1 (NEW) |
| `Compiler.CLI` | Command-line interface |
| `Compiler.SDK` | Developer SDK for Workers |
| `Samples` | Example projects including full E-Commerce store |
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

### Advanced Features (v2+)
- 🔄 Reflection (limited, compile-time only)
- 🔄 Dynamic (via dynamic dispatch helpers)
- 🔄 Expression Trees (partial, lambda analysis)
- 🔄 Unsafe code (simulated with ArrayBuffer)
- 🔄 P/Invoke (JavaScript interop bridge)
- 🔄 Threading (Web Workers integration)
- 🔄 AppDomain (simulated isolation)
- 🔄 Marshal (memory simulation)
- 🔄 COM (JavaScript object interop)

> Note: Advanced features have runtime overhead and should be used judiciously. See [Advanced Features Guide](docs/advanced-features.md) for details.

## CLI Commands

```bash
csw compile <file|project>    # Compile C# to JavaScript
csw watch                      # Watch mode for development
csw publish <worker-name>      # Deploy to Cloudflare
csw bundle                     # Generate bundled output
csw new <template>             # Create new worker from template
```

## Cloudflare Bindings
- ✅ KV Storage
- ✅ R2 Buckets
- ✅ D1 Database
- ✅ Durable Objects
- ✅ Queues
- ✅ Cache API
- ✅ AI Binding
- ✅ Workers Analytics

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

## Samples

Explore the `Samples/` directory for complete examples:

### Basic Samples
| Sample | Description | Features Used |
|--------|-------------|---------------|
| `ExampleWorker.cs` | Basic HTTP worker | Request/Response, async/await |
| `LinqWorker.cs` | LINQ operations | LINQ, collections, lambdas |
| `KvStorageWorker.cs` | KV storage operations | KV binding, async patterns |
| `D1DatabaseWorker.cs` | Database queries | D1 binding, parameterized queries |

### Advanced Samples
| Sample | Description | Features Used |
|--------|-------------|---------------|
| `ReflectionSample.cs` | Reflection usage | Type inspection, dynamic dispatch |
| `ThreadingSample.cs` | Web Workers threading | Task.Run, parallel operations |
| `UnsafeSample.cs` | Unsafe code simulation | ArrayBuffer, pointer-like ops |
| `AdvancedFeaturesSample.cs` | All advanced features | Reflection, dynamic, expressions |

### Enterprise Samples (EF Core Compatible)
| Sample | Description | Features Used |
|--------|-------------|---------------|
| `ECommerceEFCore.cs` | Full e-commerce store | EF Core Shim, LINQ, D1, Transactions |
| `ECommerceStore.cs` | Alternative store impl | Direct D1 API, custom DAL |

See [Samples README](Samples/README.md) and [Migration Guide](docs/migration-guide.md) for detailed instructions.

## Documentation

- [Architecture Guide](docs/architecture.md)
- [Compilation Pipeline](docs/pipeline.md)
- [Extensibility Model](docs/extensibility.md)
- [API Reference](docs/api.md)
- [Advanced Features Guide](docs/advanced-features.md)
- **[Migration Guide: EF Core to Cloudflare](docs/migration-guide.md)** - How to migrate .NET + EF Core + SQL Server projects
- [Implementation Roadmap](docs/roadmap.md)

## License

MIT License
