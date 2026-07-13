# C# Workers Samples

This directory contains example Cloudflare Workers written in C#.

## ExampleWorker.cs

A comprehensive example demonstrating:

- Basic worker structure with routing
- KV storage for user data
- D1 database for counters
- JSON serialization
- Async/await patterns
- Records and init-only properties
- Switch expressions
- LINQ operations

### Features Used

```csharp
// Worker base class
public class MyWorker : Worker { }

// Routing with switch expressions
return url.Path switch { ... };

// KV Storage
var kv = env.GetKV("USERS");
await kv.Get<string>("key");
await kv.Put("key", value);

// D1 Database
var d1 = env.GetD1("DATABASE");
await d1.Prepare("SELECT ...").Bind(...).Run();

// Records
public record User { public string Id { get; init; } }

// LINQ
users.Where(u => u.Name.StartsWith("A")).ToList();
```

## Running the Sample

1. Install wrangler: `npm install -g wrangler`
2. Create a new worker project: `wrangler init my-worker`
3. Compile the C# code: `csw compile Samples/ExampleWorker.cs -o my-worker/src`
4. Configure wrangler.toml with your bindings
5. Deploy: `wrangler publish`

## More Samples Coming Soon

- REST API with authentication
- WebSocket handler
- Scheduled worker (cron)
- Queue processor
- Image transformation
- AI integration
