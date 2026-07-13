using Cloudflare.Workers;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Sample worker demonstrating D1 Database operations
/// </summary>
public class D1DatabaseWorker
{
    public async Task<Response> Fetch(Request request, Env env, ExecutionContext ctx)
    {
        var url = new URL(request.Url);
        var method = request.Method.ToUpper();

        // Route handling
        if (url.Pathname.StartsWith("/users"))
        {
            return await HandleUsersRequest(request, env, url, method);
        }

        return Response.Json(new
        {
            message = "D1 Database Worker",
            endpoints = new[]
            {
                "GET /users - List all users",
                "GET /users/{id} - Get user by ID",
                "POST /users - Create new user",
                "PUT /users/{id} - Update user",
                "DELETE /users/{id} - Delete user"
            }
        });
    }

    private async Task<Response> HandleUsersRequest(Request request, Env env, URL url, string method)
    {
        var db = env.DATABASE; // D1 binding
        
        // Extract ID from path if present: /users/{id}
        var pathSegments = url.Pathname.Trim('/').Split('/');
        string? id = pathSegments.Length > 1 ? pathSegments[1] : null;

        switch (method)
        {
            case "GET":
                return id != null 
                    ? await GetUserById(db, id) 
                    : await GetAllUsers(db);
            case "POST":
                return await CreateUser(db, request);
            case "PUT":
                return id != null 
                    ? await UpdateUser(db, id, request) 
                    : Response.Json(new { error = "ID required for update" }, status: 400);
            case "DELETE":
                return id != null 
                    ? await DeleteUser(db, id) 
                    : Response.Json(new { error = "ID required for delete" }, status: 400);
            default:
                return Response.Json(new { error = "Method not allowed" }, status: 405);
        }
    }

    private async Task<Response> GetAllUsers(dynamic db)
    {
        // Parameterized query to prevent SQL injection
        var result = await db.prepare("SELECT * FROM users ORDER BY created_at DESC").all();
        
        var users = new List<User>();
        if (result.results != null)
        {
            foreach (var row in result.results)
            {
                users.Add(new User
                {
                    Id = row.id?.ToString() ?? "",
                    Name = row.name?.ToString() ?? "",
                    Email = row.email?.ToString() ?? "",
                    CreatedAt = row.created_at?.ToString() ?? ""
                });
            }
        }

        return Response.Json(new { count = users.Count, users });
    }

    private async Task<Response> GetUserById(dynamic db, string id)
    {
        // Parameterized query with binding
        var stmt = db.prepare("SELECT * FROM users WHERE id = ?").bind(id);
        var result = await stmt.first();
        
        if (result == null)
        {
            return Response.Json(new { error = "User not found" }, status: 404);
        }

        var user = new User
        {
            Id = result.id?.ToString() ?? "",
            Name = result.name?.ToString() ?? "",
            Email = result.email?.ToString() ?? "",
            CreatedAt = result.created_at?.ToString() ?? ""
        };

        return Response.Json(user);
    }

    private async Task<Response> CreateUser(dynamic db, Request request)
    {
        try
        {
            var body = await request.Json();
            var name = body?.name?.ToString();
            var email = body?.email?.ToString();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
            {
                return Response.Json(new { error = "Name and email are required" }, status: 400);
            }

            // Insert with parameterized query
            var stmt = db.prepare("INSERT INTO users (name, email, created_at) VALUES (?, ?, datetime('now'))")
                .bind(name, email);
            await stmt.run();

            return Response.Json(new 
            { 
                success = true, 
                message = "User created successfully",
                user = new { name, email }
            }, status: 201);
        }
        catch (System.Exception ex)
        {
            return Response.Json(new { error = $"Failed to create user: {ex.Message}" }, status: 400);
        }
    }

    private async Task<Response> UpdateUser(dynamic db, string id, Request request)
    {
        try
        {
            var body = await request.Json();
            var name = body?.name?.ToString();
            var email = body?.email?.ToString();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
            {
                return Response.Json(new { error = "Name and email are required" }, status: 400);
            }

            // Update with parameterized query
            var stmt = db.prepare("UPDATE users SET name = ?, email = ? WHERE id = ?")
                .bind(name, email, id);
            var result = await stmt.run();

            if (result.meta?.changes == 0)
            {
                return Response.Json(new { error = "User not found or no changes made" }, status: 404);
            }

            return Response.Json(new 
            { 
                success = true, 
                message = "User updated successfully",
                user = new { id, name, email }
            });
        }
        catch (System.Exception ex)
        {
            return Response.Json(new { error = $"Failed to update user: {ex.Message}" }, status: 400);
        }
    }

    private async Task<Response> DeleteUser(dynamic db, string id)
    {
        // Delete with parameterized query
        var stmt = db.prepare("DELETE FROM users WHERE id = ?").bind(id);
        var result = await stmt.run();

        if (result.meta?.changes == 0)
        {
            return Response.Json(new { error = "User not found" }, status: 404);
        }

        return Response.Json(new 
        { 
            success = true, 
            message = "User deleted successfully",
            deletedId = id
        });
    }
}

public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string CreatedAt { get; set; }
}

/// <summary>
/// Environment bindings for D1
/// </summary>
public class Env
{
    public dynamic DATABASE { get; set; }
}

/*
-- D1 Schema (run this in your D1 database)
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    email TEXT NOT NULL UNIQUE,
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_created_at ON users(created_at);
*/
