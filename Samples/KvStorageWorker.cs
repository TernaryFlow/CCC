using Cloudflare.Workers;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Sample worker demonstrating KV Storage operations
/// </summary>
public class KvStorageWorker
{
    public async Task<Response> Fetch(Request request, Env env, ExecutionContext ctx)
    {
        var url = new URL(request.Url);
        var method = request.Method.ToUpper();

        // Route handling
        if (url.Pathname.StartsWith("/kv/"))
        {
            return await HandleKvRequest(request, env, url.Pathname, method);
        }

        return Response.Json(new
        {
            message = "KV Storage Worker",
            endpoints = new[]
            {
                "GET /kv/{key} - Get value by key",
                "PUT /kv/{key} - Set value (body as JSON)",
                "DELETE /kv/{key} - Delete key",
                "GET /kv/list - List all keys"
            }
        });
    }

    private async Task<Response> HandleKvRequest(Request request, Env env, string pathname, string method)
    {
        var kv = env.MY_KV_NAMESPACE; // KVNamespace binding
        
        if (pathname == "/kv/list" && method == "GET")
        {
            return await ListKeys(kv);
        }

        // Extract key from path: /kv/{key}
        var key = pathname.Substring(4); // Remove "/kv/"
        
        if (string.IsNullOrEmpty(key))
        {
            return Response.Json(new { error = "Key is required" }, status: 400);
        }

        switch (method)
        {
            case "GET":
                return await GetValue(kv, key);
            case "PUT":
            case "POST":
                return await SetValue(kv, key, request);
            case "DELETE":
                return await DeleteValue(kv, key);
            default:
                return Response.Json(new { error = "Method not allowed" }, status: 405);
        }
    }

    private async Task<Response> ListKeys(dynamic kv)
    {
        // List keys with optional prefix
        var result = await kv.list(new { prefix = "", limit = 100 });
        
        var keys = new List<string>();
        if (result.keys != null)
        {
            foreach (var key in result.keys)
            {
                keys.Add(key.name?.ToString() ?? "");
            }
        }

        return Response.Json(new
        {
            count = keys.Count,
            keys
        });
    }

    private async Task<Response> GetValue(dynamic kv, string key)
    {
        var value = await kv.get(key);
        
        if (value == null)
        {
            return Response.Json(new { error = "Key not found" }, status: 404);
        }

        return Response.Json(new { key, value });
    }

    private async Task<Response> SetValue(dynamic kv, string key, Request request)
    {
        try
        {
            var body = await request.Json();
            var value = body?.value?.ToString();
            
            if (string.IsNullOrEmpty(value))
            {
                return Response.Json(new { error = "Value is required in request body" }, status: 400);
            }

            // Store with optional expiration (in seconds)
            var expiration = body?.expiration != null ? int.Parse(body.expiration.ToString()) : (int?)null;
            
            var options = expiration.HasValue ? new { expirationTtl = expiration.Value } : null;
            await kv.put(key, value, options);

            return Response.Json(new 
            { 
                success = true, 
                key, 
                message = "Value stored successfully",
                expirationSeconds = expiration
            });
        }
        catch (System.Exception ex)
        {
            return Response.Json(new { error = $"Failed to parse request body: {ex.Message}" }, status: 400);
        }
    }

    private async Task<Response> DeleteValue(dynamic kv, string key)
    {
        await kv.delete(key);
        
        return Response.Json(new 
        { 
            success = true, 
            key, 
            message = "Key deleted successfully" 
        });
    }
}

/// <summary>
/// Environment bindings interface
/// </summary>
public class Env
{
    public dynamic MY_KV_NAMESPACE { get; set; }
}
