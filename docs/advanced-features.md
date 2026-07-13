# Advanced Features Support

This document describes the implementation of advanced C# features that were initially marked as unsupported.

## Overview

The compiler now provides **limited** or **simulated** support for the following advanced features:

1. **Reflection** - Type names and basic member access
2. **Dynamic** - Runtime binding simulation using JavaScript Proxies
3. **Expression Trees** - Compilation to JavaScript functions
4. **Unsafe Code** - Conversion to safe JavaScript operations
5. **P/Invoke** - JavaScript FFI interop (limited)
6. **Threading** - Async-based concurrency primitives
7. **AppDomain** - Module isolation mapping
8. **Marshal** - ArrayBuffer/DataView operations
9. **COM** - Not supported (Windows-specific)

---

## 1. Reflection

### Supported Operations

| Operation | Support Level | Notes |
|-----------|--------------|-------|
| `typeof(T)` | ✅ Full | Returns type name at compile time |
| `Type.GetType()` | ⚠️ Limited | Only for known types in compilation |
| `Type.GetProperties()` | ⚠️ Limited | Returns property names only |
| `Type.GetMethods()` | ⚠️ Limited | Returns method signatures |
| `Type.GetFields()` | ⚠️ Limited | Returns field names |
| `PropertyInfo.GetValue()` | ✅ Full | Via JavaScript property access |
| `MethodInfo.Invoke()` | ✅ Full | Via JavaScript function call |
| `Activator.CreateInstance()` | ✅ Full | Via JavaScript constructor |

### Limitations

- No runtime type discovery
- No custom attribute inspection at runtime
- No dynamic assembly loading
- Type information is embedded at compile time

### Example

```csharp
// C# Source
var type = typeof(MyClass);
Console.WriteLine(type.Name); // Compiled to JS string

var prop = type.GetProperty("Value");
var value = prop.GetValue(obj); // Direct property access
```

```javascript
// Generated JavaScript
const type = new Type('MyClass', 'MyNamespace.MyClass', 'MyNamespace');
console.log(type.getName());

const prop = type.getProperty('Value');
const value = prop.getValue(obj);
```

### Configuration

Enable via compiler options:
```json
{
  "advancedFeatures": {
    "enableLimitedReflection": true
  }
}
```

---

## 2. Dynamic

### Implementation

Dynamic objects are wrapped using JavaScript `Proxy` for natural syntax:

```javascript
class DynamicObject {
    constructor(target) {
        this.$target = target;
        this.$cache = new Map();
    }
    
    static createProxy(target) {
        return new Proxy(target, {
            get: (t, prop) => t[prop],
            set: (t, prop, value) => { t[prop] = value; return true; }
        });
    }
}
```

### Supported Operations

| Operation | Support |
|-----------|---------|
| Dynamic variable declaration | ✅ |
| Property access | ✅ |
| Method invocation | ✅ |
| Indexer access | ✅ |
| Conversion operators | ⚠️ Limited |

### Performance Warning

Dynamic operations bypass compile-time optimization and use runtime lookup. Expect 5-10x slower performance compared to statically typed code.

### Example

```csharp
// C# Source
dynamic obj = GetDynamicObject();
obj.Name = "Test";
obj.SetValue(42);
Console.WriteLine(obj.Name);
```

```javascript
// Generated JavaScript
let obj = DynamicObject.createProxy(getDynamicObject());
obj.Name = "Test";
obj.setValue(42);
console.log(obj.Name);
```

---

## 3. Expression Trees

### Implementation

Expression trees are compiled to JavaScript functions at compile-time:

```javascript
// Expression: x => x * 2 + 1
const expr = Expression.lambda(
    Expression.add(
        Expression.multiply(param, Expression.constant(2)),
        Expression.constant(1)
    ),
    param
);

const fn = expr.compile(); // Returns JavaScript function
```

### Supported Expression Types

- ✅ Binary operations (+, -, *, /, %, ==, !=, <, >, <=, >=, &&, ||)
- ✅ Unary operations (!, -)
- ✅ Conditional expressions (?:)
- ✅ Member access
- ✅ Method calls
- ✅ Lambda expressions
- ✅ Block expressions
- ✅ Constant and parameter expressions

### Not Supported

- ❌ Expression tree modification at runtime
- ❌ Dynamic expression compilation
- ❌ Complex query providers (like LINQ to SQL)

### Example

```csharp
// C# Source
Expression<Func<int, int>> expr = x => x * 2 + 1;
var fn = expr.Compile();
var result = fn(5); // Returns 11
```

```javascript
// Generated JavaScript
const expr = Expression.lambda(
    Expression.add(
        Expression.multiply(x, Expression.constant(2)),
        Expression.constant(1)
    ),
    x
);
const fn = expr.compile();
const result = fn(5);
```

---

## 4. Unsafe Code

### Implementation

Unsafe code is converted to safe JavaScript equivalents:

| C# Unsafe | JavaScript Equivalent |
|-----------|----------------------|
| `byte* ptr` | `Uint8Array` |
| `fixed` statement | Typed array view |
| `stackalloc` | Pre-allocated ArrayBuffer |
| Pointer arithmetic | Array index manipulation |

### Example

```csharp
// C# Source (unsafe)
unsafe {
    int[] arr = { 1, 2, 3, 4, 5 };
    fixed (int* ptr = arr) {
        int* p = ptr;
        Console.WriteLine(*p);
        p++;
        Console.WriteLine(*p);
    }
}
```

```javascript
// Generated JavaScript
const arr = [1, 2, 3, 4, 5];
const buffer = new ArrayBuffer(arr.length * 4);
const view = new Int32Array(buffer);
view.set(arr);

let index = 0;
console.log(view[index]);
index++;
console.log(view[index]);
```

### Warnings

- Pointer casts may throw at runtime
- Memory layout assumptions are not preserved
- Stack allocation becomes heap allocation

---

## 5. P/Invoke

### Implementation

P/Invoke declarations are mapped to JavaScript FFI or WebAssembly imports:

```csharp
// C# Source
[DllImport("libc")]
static extern int printf(string format);
```

```javascript
// Generated JavaScript (with FFI enabled)
import { printf } from 'ffi:libc';

export function printf(format) {
    return $ffi.printf(format);
}
```

### Requirements

1. Target library must be available in Workers environment
2. Function signatures must match JavaScript calling convention
3. Use `--enable-pinvoke` compiler flag

### Alternative: WebAssembly

For complex native interop, compile native code to WebAssembly:

```csharp
// Reference pre-compiled WASM module
[WasmImport("my-module", "my-function")]
static extern int MyFunction(int value);
```

---

## 6. Threading

### Implementation

True multi-threading is **not available** in Cloudflare Workers (single-threaded event loop). Threading primitives are simulated using async/await:

| .NET Type | JavaScript Simulation |
|-----------|----------------------|
| `SemaphoreSlim` | Async queue with Promise |
| `Mutex` | SemaphoreSlim(1,1) |
| `ManualResetEvent` | Promise-based signaling |
| `CancellationToken` | Callback registration |
| `Thread` | ❌ Not supported |
| `ThreadPool` | ❌ Not supported |
| `Parallel.For` | ⚠️ Sequential with async |

### Example

```csharp
// C# Source
var semaphore = new SemaphoreSlim(2);
await semaphore.WaitAsync();
try {
    await DoWork();
} finally {
    semaphore.Release();
}
```

```javascript
// Generated JavaScript
const semaphore = new SemaphoreSlim(2);
await semaphore.wait();
try {
    await doWork();
} finally {
    semaphore.release();
}
```

### Important Notes

- Operations are **concurrent**, not **parallel**
- CPU-bound work does not benefit from threading
- Use async I/O for scalability

---

## 7. AppDomain

### Implementation

AppDomain concepts are mapped to JavaScript module scope or Worker isolation:

| AppDomain Feature | Workers Equivalent |
|-------------------|-------------------|
| Domain creation | ES Module scope |
| Assembly loading | Import statement |
| Type resolution | Module exports |
| Unloading | Worker restart |
| Cross-domain calls | Worker RPC |

### Limitations

- No runtime assembly loading
- No domain unloading without Worker restart
- Isolation is at Worker level, not AppDomain level

---

## 8. Marshal

### Implementation

Marshal operations use JavaScript `ArrayBuffer` and `DataView`:

```javascript
class MarshalSimulator {
    static allocHGlobal(size) {
        return new ArrayBuffer(size);
    }
    
    static copy(source, dest, startIdx, length) {
        const src = new Uint8Array(source);
        const dst = new Uint8Array(dest);
        for (let i = 0; i < length; i++) {
            dst[startIdx + i] = src[i];
        }
    }
    
    static readInt32(buffer, offset) {
        const view = new DataView(buffer);
        return view.getInt32(offset, true);
    }
}
```

### Supported Operations

- ✅ `AllocHGlobal` → `ArrayBuffer`
- ✅ `FreeHGlobal` → GC (no-op)
- ✅ `Copy` → Typed array copy
- ✅ `Read/Write Int32/Int64` → `DataView`
- ✅ `PtrToStringAuto` → `TextDecoder`
- ✅ `StructureToPtr` → `DataView` serialization

### Not Supported

- ❌ COM marshaling
- ❌ Custom marshalers
- ❌ Reference counting

---

## 9. COM

### Status: NOT SUPPORTED

COM interop is Windows-specific and requires native code execution not available in Cloudflare Workers.

### Alternatives

1. **REST APIs**: Expose COM functionality via HTTP
2. **Worker Durable Objects**: Stateful server-side logic
3. **External Services**: Use cloud services instead

---

## Compiler Configuration

Enable advanced features in `compiler.json`:

```json
{
  "advancedFeatures": {
    "enableLimitedReflection": true,
    "enableDynamic": true,
    "enableExpressionTrees": true,
    "allowUnsafeCode": true,
    "enablePInvoke": false,
    "enableThreading": true,
    "enableAppDomain": false,
    "enableMarshal": true,
    "treatUnsupportedAsErrors": false
  }
}
```

## CLI Usage

```bash
# Compile with advanced features
csw compile app.cs --enable-reflection --enable-dynamic --enable-unsafe

# Show warnings for advanced feature usage
csw compile app.cs --warn-advanced

# Treat advanced features as errors
csw compile app.cs --strict
```

## Performance Considerations

| Feature | Performance Impact | Recommendation |
|---------|-------------------|----------------|
| Reflection | Low (compile-time) | Use sparingly |
| Dynamic | High (5-10x slower) | Avoid in hot paths |
| Expression Trees | Medium (compilation overhead) | Cache compiled delegates |
| Unsafe | Low (safe conversion) | Prefer safe code |
| Threading | N/A (async simulation) | Use async/await directly |

## Testing

Run the advanced features test suite:

```bash
dotnet test Tests/Compiler.AdvancedFeatures.Tests
```

## Future Work

- [ ] Improve reflection with compile-time metadata embedding
- [ ] Optimize dynamic dispatch with inline caches
- [ ] Add more expression tree node types
- [ ] WebAssembly module integration for P/Invoke
- [ ] Better error messages for unsupported operations
