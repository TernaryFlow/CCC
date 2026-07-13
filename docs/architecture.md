# Architecture Guide

## Overview

The C# to Cloudflare Workers Compiler is designed with a clean, modular architecture that separates concerns across multiple independent libraries. This document describes the overall system architecture, component responsibilities, and design decisions.

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        C# Source Code                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Compiler.Parser                             │
│                   (Roslyn-based parsing)                        │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────┐    │
│  │ SyntaxTree  │  │SemanticModel │  │ TypeResolution      │    │
│  └─────────────┘  └──────────────┘  └─────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Compiler.Semantics                           │
│              (Symbol analysis & validation)                     │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────┐    │
│  │TypeAnalyzer │  │MethodResolver│  │ FeatureValidation   │    │
│  └─────────────┘  └──────────────┘  └─────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Compiler.IR                                │
│            (Language-agnostic intermediate rep)                 │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────┐    │
│  │ClassDecl    │  │MethodDecl    │  │ Expressions/Stmts   │    │
│  └─────────────┘  └──────────────┘  └─────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Compiler.CodeGen                             │
│               (JavaScript ES2025+ output)                       │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────┐    │
│  │ClassGen     │  │MethodGen     │  │ ExpressionGen       │    │
│  └─────────────┘  └──────────────┘  └─────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Compiler.Runtime                             │
│              (Lightweight JS runtime <100KB)                    │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────┐    │
│  │Collections  │  │ Task/Promise │  │ DateTime/Guid       │    │
│  └─────────────┘  └──────────────┘  └─────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Cloudflare Workers Runtime                     │
│                    (ES2025+ Modules)                            │
└─────────────────────────────────────────────────────────────────┘
```

## Component Responsibilities

### Compiler.Core
**Purpose:** Shared types, utilities, and abstractions used across all compiler stages.

**Key Types:**
- `IrNode` - Base class for all IR nodes
- `TypeSymbol` - Type representation
- `MethodSymbol` - Method signature representation
- `CompilationOptions` - Compiler configuration
- `Diagnostic` - Error/warning reporting
- `CompilationResult` - Compilation output

**Dependencies:** None (foundational library)

### Compiler.Parser
**Purpose:** Roslyn-based C# parsing and initial semantic analysis.

**Responsibilities:**
- Parse C# source files using Roslyn
- Build syntax trees
- Create semantic models
- Resolve type references
- Report parse-time diagnostics

**Key Types:**
- `CSharpParser` - Main parser entry point
- `ParsedCompilation` - Parser output container

**Dependencies:** 
- Compiler.Core
- Microsoft.CodeAnalysis.CSharp (NuGet)

### Compiler.Semantics
**Purpose:** Deep semantic analysis and feature validation.

**Responsibilities:**
- Analyze type declarations
- Resolve method signatures
- Validate supported features
- Detect unsupported constructs
- Build symbol tables

**Key Types:**
- `SemanticAnalyzer` - Main analyzer
- `SymbolExtensions` - Extension methods for symbols

**Dependencies:**
- Compiler.Core
- Compiler.Parser

### Compiler.IR
**Purpose:** Language-agnostic intermediate representation.

**Responsibilities:**
- Define IR node types
- Represent C# constructs in language-neutral form
- Support optimization passes
- Enable multiple code generation targets

**IR Node Categories:**
- **Declarations:** ClassDeclaration, MethodDeclaration, FieldDeclaration, PropertyDeclaration
- **Statements:** IfStatement, WhileStatement, ForStatement, TryStatement, ReturnStatement
- **Expressions:** BinaryExpression, UnaryExpression, InvocationExpression, LambdaExpression
- **Patterns:** ConstantPattern, TypePattern, DiscardPattern (for pattern matching)

**Dependencies:** Compiler.Core

### Compiler.CodeGen
**Purpose:** JavaScript code generation from IR.

**Responsibilities:**
- Transform IR nodes to ES2025+ JavaScript
- Generate ES modules
- Apply optimizations (inlining, dead code elimination)
- Produce source maps
- Support minification hooks

**Key Types:**
- `JavaScriptCodeGenerator` - Main code generator

**Output Features:**
- Native ES classes with private fields
- Async/await patterns
- Optional chaining (?.)
- Nullish coalescing (??)
- Tree-shakeable exports

**Dependencies:**
- Compiler.Core
- Compiler.IR
- Compiler.Semantics

### Compiler.Runtime
**Purpose:** Lightweight JavaScript runtime library.

**Responsibilities:**
- Provide .NET-like base classes
- Implement collection types
- Support async/await via Promises
- Handle DateTime, Guid, etc.
- LINQ extension methods
- Cloudflare Workers bindings

**Runtime Modules:**
- `runtime.js` - Core types (List, Dictionary, Task, DateTime, Guid, etc.)
- `linq.js` - LINQ extension methods
- `cloudflare.js` - Cloudflare Workers API wrappers

**Size Target:** <100KB minified

**Dependencies:** None (pure JavaScript)

### Compiler.CLI
**Purpose:** Command-line interface for the compiler.

**Commands:**
- `compile` - Compile C# to JavaScript
- `watch` - Watch mode for development
- `publish` - Deploy to Cloudflare
- `bundle` - Bundle with dependencies

**Dependencies:**
- All compiler libraries
- System.CommandLine

### Compiler.SDK
**Purpose:** Developer SDK for writing Workers in C#.

**Provides:**
- `Worker` base class
- `Request`/`Response` types
- Environment binding wrappers
- Cloudflare API type definitions

**Dependencies:** Compiler.Runtime type definitions

## Design Principles

### 1. Separation of Concerns
Each library has a single, well-defined responsibility. Changes to one stage do not affect others as long as interfaces remain stable.

### 2. Immutable IR
IR nodes are immutable once created. This enables:
- Safe sharing between optimization passes
- Easy debugging and inspection
- Predictable behavior

### 3. Extensibility Points
The architecture supports future extensions:
- New target runtimes (Node.js, Browser)
- Additional optimization passes
- Custom code generators
- Plugin system (planned)

### 4. Progressive Enhancement
The compiler works with minimal features and adds capabilities incrementally:
- v1: Basic classes, methods, LINQ
- v2: Advanced pattern matching, records
- v3: Reflection support (via transformation)

### 5. Debuggability
Generated JavaScript is:
- Human-readable (before minification)
- Properly indented
- Includes source maps
- Uses meaningful variable names

## Compilation Pipeline

### Phase 1: Parsing
```
Source Files → Roslyn → SyntaxTree + SemanticModel
```

### Phase 2: Semantic Analysis
```
SyntaxTree + SemanticModel → Symbol Tables + Validated Types
```

### Phase 3: IR Generation
```
Validated Types → IR Nodes (ClassDeclaration, MethodDeclaration, etc.)
```

### Phase 4: Optimization (Optional)
```
IR Nodes → Optimized IR Nodes
- Constant folding
- Dead code elimination
- Method inlining
- Generic specialization
```

### Phase 5: Code Generation
```
Optimized IR → ES2025 JavaScript Modules
```

### Phase 6: Bundling (Optional)
```
JavaScript Modules + Runtime → Single Bundle
```

## Error Handling

Diagnostics flow through the pipeline:

1. **Parse Errors:** Reported by Roslyn during parsing
2. **Semantic Errors:** Type mismatches, undefined symbols
3. **IR Validation:** Unsupported features detected
4. **Code Gen Warnings:** Non-optimal patterns

All errors include source location information for accurate reporting.

## Performance Considerations

### Compile-Time Performance
- Parallel parsing of multiple files
- Incremental compilation (watch mode)
- Cached semantic models
- Streaming IR generation

### Runtime Performance
- Zero virtual dispatch where possible
- Direct property access
- Native JavaScript collections
- Promise-based async (no overhead)

## Security Considerations

- No eval() or dynamic code generation
- Strict mode enabled in all generated code
- Input validation in runtime helpers
- No prototype pollution vulnerabilities

## Future Extensions

### Planned Features
1. **Reflection Support:** Transform reflection to compile-time resolution
2. **Expression Trees:** Convert to JavaScript AST manipulation
3. **Threading:** Map to Web Workers / Worklets
4. **P/Invoke:** Replace with JavaScript interop

### Alternative Targets
1. **Node.js:** Adapt runtime for Node APIs
2. **Browser:** DOM integration layer
3. **Bun:** Native Bun runtime support
4. **Deno:** Deno-specific bindings

## Testing Strategy

### Unit Tests
- Individual IR node generation
- Expression translation
- Statement translation

### Integration Tests
- Full compilation pipeline
- End-to-end worker functionality

### Snapshot Tests
- Generated JavaScript output
- Runtime behavior verification

### Performance Benchmarks
- Compile time measurements
- Runtime performance vs native JavaScript
- Bundle size tracking
