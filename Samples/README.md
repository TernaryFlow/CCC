# C# Workers Samples

This directory contains example Cloudflare Workers written in C#.

## Basic Samples

### ExampleWorker.cs

A comprehensive example demonstrating:

- Basic worker structure with routing
- KV storage for user data
- D1 database for counters
- JSON serialization
- Async/await patterns
- Records and init-only properties
- Switch expressions
- LINQ operations

### LinqWorker.cs

Demonstrates LINQ operations compiled to native JavaScript array methods:

- `Where`, `Select`, `OrderBy`, `ThenBy`
- `Take`, `Skip`, pagination
- `GroupBy`, aggregations (`Sum`, `Average`, `Count`)
- `Any`, `All`, `FirstOrDefault`, `LastOrDefault`
- `Distinct`, `Union`, `Intersect`, `Except`

### KvStorageWorker.cs

Complete KV storage operations:

- GET, PUT, DELETE key-value pairs
- List keys with pagination
- Expiration (TTL) support
- Error handling

### D1DatabaseWorker.cs

Full CRUD operations with D1 database:

- Parameterized queries (SQL injection prevention)
- Create, Read, Update, Delete users
- Transaction-like operations
- Schema setup instructions

## Advanced Feature Samples

### ReflectionSample.cs

Demonstrates simulated reflection operations:

- Type inspection (name, properties, methods)
- Get/Set property values by name
- Invoke methods dynamically
- Create instances from type names

> **Note:** Reflection is simulated at compile-time with runtime helpers for performance.

### ThreadingSample.cs

Demonstrates parallel execution using Web Workers:

- `Task.Run` for background operations
- `Task.WhenAll` for parallel execution
- `ParallelForEach` extension method
- `ParallelSelect` for parallel transformations

> **Note:** Threading uses JavaScript workers under the hood, not OS threads.

### UnsafeSample.cs

Demonstrates unsafe code simulation using ArrayBuffer:

- Memory allocation and deallocation
- Pointer-like read/write operations
- Buffer slicing and concatenation
- Base64 encoding/decoding

> **Note:** Uses typed arrays to simulate unsafe memory operations safely.

### AdvancedFeaturesSample.cs

Combines multiple advanced features:

- Dynamic dispatch
- Expression tree analysis
- P/Invoke-style JavaScript interop
- AppDomain-like isolation

## Running the Samples

### Prerequisites

1. Install .NET SDK (for compilation)
2. Install wrangler: `npm install -g wrangler`
3. Configure Cloudflare account: `wrangler login`

### Compile a Sample

```bash
# Compile single file
csw compile Samples/LinqWorker.cs -o ./dist

# Compile with watch mode
csw watch Samples/KvStorageWorker.cs -o ./dist

# Bundle with runtime
csw bundle Samples/D1DatabaseWorker.cs -o ./dist --bundle
```

### Deploy to Cloudflare

1. Initialize wrangler project:
```bash
wrangler init my-worker
cd my-worker
```

2. Copy compiled output:
```bash
cp ../dist/*.js ./src/
cp ../Compiler.Runtime/Runtime/*.js ./src/
```

3. Configure `wrangler.toml`:
```toml
name = "my-csharp-worker"
main = "src/worker.js"
compatibility_date = "2024-01-01"

# Add bindings as needed
[[kv_namespaces]]
binding = "MY_KV_NAMESPACE"
id = "your-kv-namespace-id"

[[d1_databases]]
binding = "DATABASE"
database_name = "my-database"
database_id = "your-database-id"
```

4. Deploy:
```bash
wrangler deploy
```

### Local Development

```bash
# Run locally with wrangler
wrangler dev

# Watch and auto-recompile
csw watch Samples/ExampleWorker.cs -o ./src &
wrangler dev
```

## Sample Output

Each sample returns JSON responses. Example from LinqWorker:

```json
{
  "electronics": ["Laptop", "Mouse", "Monitor", "Keyboard"],
  "productNames": [
    {"name": "Laptop", "price": 999.99},
    {"name": "Mouse", "price": 29.99}
  ],
  "totalValue": 1558.96,
  "byCategory": [
    {"category": "Electronics", "count": 4, "averagePrice": 364.99},
    {"category": "Furniture", "count": 2, "averagePrice": 174.99}
  ]
}
```

## More Samples Coming Soon

- REST API with JWT authentication
- WebSocket handler for real-time communication
- Scheduled worker (cron jobs)
- Queue processor for background jobs
- Image transformation with Cloudflare Images
- AI integration with Workers AI
- R2 blob storage operations
- Durable Objects for stateful applications
- Cache API usage examples
- Analytics Engine integration
