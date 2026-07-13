using Cloudflare.Workers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyWorker;

/// <summary>
/// Example Cloudflare Worker written in C#
/// </summary>
public class MyWorker : Worker
{
    public override async Task<Response> Fetch(Request request, Env env, ExecutionContext ctx)
    {
        var url = new Url(request.Url);
        
        // Route handling
        return url.Path switch
        {
            "/" => Response.Json(new { message = "Hello from C#!" }),
            "/api/users" => await HandleUsers(request, env),
            "/api/counter" => await HandleCounter(request, env),
            _ => new Response("Not Found", new { status = 404 })
        };
    }

    private async Task<Response> HandleUsers(Request request, Env env)
    {
        var kv = env.GetKV("USERS");
        
        if (request.Method == "GET")
        {
            // Get all users from KV
            var userIds = await kv.Get<string>("user_ids");
            var users = new List<User>();
            
            if (!string.IsNullOrEmpty(userIds))
            {
                foreach (var id in userIds.Split(','))
                {
                    var userData = await kv.Get<string>($"user:{id}");
                    if (!string.IsNullOrEmpty(userData))
                    {
                        users.Add(JsonSerializer.Deserialize<User>(userData));
                    }
                }
            }
            
            return Response.Json(users);
        }
        else if (request.Method == "POST")
        {
            // Create new user
            var body = await request.Json();
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Name = body.name,
                Email = body.email,
                CreatedAt = DateTime.Now
            };
            
            // Save to KV
            await kv.Put($"user:{user.Id}", JsonSerializer.Serialize(user));
            
            // Update index
            var existingIds = await kv.Get<string>("user_ids") ?? "";
            var newIds = string.IsNullOrEmpty(existingIds) 
                ? user.Id 
                : $"{existingIds},{user.Id}";
            await kv.Put("user_ids", newIds);
            
            return Response.Json(user, new { status = 201 });
        }
        
        return new Response("Method Not Allowed", new { status = 405 });
    }

    private async Task<Response> HandleCounter(Request request, Env env)
    {
        var d1 = env.GetD1("DATABASE");
        
        if (request.Method == "POST")
        {
            // Increment counter
            var stmt = d1.Prepare("INSERT INTO counters (name, value) VALUES (?, 1) ON CONFLICT (name) DO UPDATE SET value = value + 1")
                .Bind("page_views");
            
            await stmt.Run();
            
            var result = await d1.Prepare("SELECT value FROM counters WHERE name = ?")
                .Bind("page_views")
                .First<int?>();
            
            return Response.Json(new { count = result ?? 0 });
        }
        
        return new Response("Method Not Allowed", new { status = 405 });
    }
}

/// <summary>
/// Example data model
/// </summary>
public record User
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// URL helper for parsing request URLs
/// </summary>
public class Url
{
    private readonly System.Uri _uri;
    
    public Url(string url)
    {
        _uri = new System.Uri(url);
    }
    
    public string Path => _uri.AbsolutePath;
    public string Query => _uri.Query;
    public string Host => _uri.Host;
    
    public Dictionary<string, string> ParseQuery()
    {
        var result = new Dictionary<string, string>();
        var query = _uri.Query.TrimStart('?');
        
        if (string.IsNullOrEmpty(query))
            return result;
        
        foreach (var pair in query.Split('&'))
        {
            var parts = pair.Split('=');
            if (parts.Length == 2)
            {
                result[parts[0]] = parts[1];
            }
        }
        
        return result;
    }
}
