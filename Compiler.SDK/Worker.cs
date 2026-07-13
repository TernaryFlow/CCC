namespace Cloudflare.Workers;

/// <summary>
/// Base class for Cloudflare Workers.
/// Override the Fetch method to handle incoming requests.
/// </summary>
public abstract class Worker
{
    /// <summary>
    /// Handles an incoming HTTP request.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="env">Environment bindings (KV, R2, D1, etc.).</param>
    /// <param name="ctx">Execution context for waitUntil and passThroughOnException.</param>
    /// <returns>A response to send back to the client.</returns>
    public abstract Task<Response> Fetch(Request request, Env env, ExecutionContext ctx);
}

/// <summary>
/// Represents an HTTP request.
/// </summary>
public class Request
{
    private readonly global::Request _inner;

    public Request(string url, RequestInit? init = null)
    {
        _inner = new global::Request(url, init);
    }

    public string Url => _inner.url;
    public string Method => _inner.method;
    public Headers Headers => new Headers(_inner.headers);
    public CfProperties? Cf => _inner.cf;

    public Task<string> Text() => _inner.text();
    public Task<dynamic> Json() => _inner.json();
    public Task<FormData> FormData() => _inner.formData();
    public Task<Blob> Blob() => _inner.blob();
    public Task<ArrayBuffer> ArrayBuffer() => _inner.arrayBuffer();

    public Request Clone() => new Request(_inner.clone());
}

/// <summary>
/// Represents an HTTP response.
/// </summary>
public class Response
{
    private readonly global::Response _inner;

    public Response(object? body = null, ResponseInit? init = null)
    {
        _inner = new global::Response(body, init);
    }

    public static Response Json(object data, ResponseInit? init = null)
    {
        return new Response(global::Response.json(data, init));
    }

    public static Response Html(string html, ResponseInit? init = null)
    {
        return new Response(global::Response.html(html, init));
    }

    public static Response Redirect(string url, int status = 302)
    {
        return new Response(global::Response.redirect(url, status));
    }

    public int Status => _inner.status;
    public string StatusText => _inner.statusText;
    public Headers Headers => new Headers(_inner.headers);
    public bool Ok => _inner.ok;
    public bool Redirected => _inner.redirected;
    public string Url => _inner.url;

    public Task<string> Text() => _inner.text();
    public Task<dynamic> Json() => _inner.json();
    public Task<FormData> FormData() => _inner.formData();
    public Task<Blob> Blob() => _inner.blob();
    public Task<ArrayBuffer> ArrayBuffer() => _inner.arrayBuffer();

    public Response Clone() => new Response(_inner.clone());
}

/// <summary>
/// Environment bindings for Cloudflare Workers.
/// </summary>
public class Env
{
    private readonly global::Env _inner;

    public Env(dynamic bindings)
    {
        _inner = new global::Env(bindings);
    }

    public KVNamespace GetKV(string namespaceName) => _inner.getKV(namespaceName);
    public R2Bucket GetR2(string bucketName) => _inner.getR2(bucketName);
    public D1Database GetD1(string databaseName) => _inner.getD1(databaseName);
    public DurableObjectNamespace GetDurableObject(string namespaceName) => _inner.getDurableObject(namespaceName);
    public WorkerQueue GetQueue(string queueName) => _inner.getQueue(queueName);
    public AIBinding GetAI(string aiName) => _inner.getAI(aiName);
}

/// <summary>
/// Execution context for managing background tasks.
/// </summary>
public class ExecutionContext
{
    private readonly global::ExecutionContext _inner;

    public ExecutionContext(global::ExecutionContext inner)
    {
        _inner = inner;
    }

    public void WaitUntil(Task task) => _inner.waitUntil(task);
    public void PassThroughOnException() => _inner.passThroughOnException();
}

/// <summary>
/// KV Namespace for key-value storage.
/// </summary>
public class KVNamespace
{
    private readonly global::KVNamespace _inner;

    public KVNamespace(global::KVNamespace inner)
    {
        _inner = inner;
    }

    public Task<T?> Get<T>(string key, KVGetOptions? options = null) 
        => _inner.get(key, options);

    public Task Put(string key, object value, KVPutOptions? options = null) 
        => _inner.put(key, value, options);

    public Task Delete(string key) 
        => _inner.delete(key);

    public Task<KVListResult> List(KVListOptions? options = null) 
        => _inner.list(options);
}

/// <summary>
/// R2 Bucket for object storage.
/// </summary>
public class R2Bucket
{
    private readonly global::R2Bucket _inner;

    public R2Bucket(global::R2Bucket inner)
    {
        _inner = inner;
    }

    public Task<R2Object?> Head(string key) => _inner.head(key);
    public Task<R2ObjectBody?> Get(string key, R2GetOptions? options = null) => _inner.get(key, options);
    public Task<R2Object> Put(string key, object value, R2PutOptions? options = null) => _inner.put(key, value, options);
    public Task Delete(string key) => _inner.delete(key);
    public Task<R2Objects> List(R2ListOptions? options = null) => _inner.list(options);
}

/// <summary>
/// D1 Database for SQL storage.
/// </summary>
public class D1Database
{
    private readonly global::D1Database _inner;

    public D1Database(global::D1Database inner)
    {
        _inner = inner;
    }

    public D1PreparedStatement Prepare(string query) => new D1PreparedStatement(_inner.prepare(query));
    public Task<D1Result> Exec(string query) => _inner.exec(query);
}

/// <summary>
/// Prepared SQL statement for D1.
/// </summary>
public class D1PreparedStatement
{
    private readonly global::D1PreparedStatement _inner;

    public D1PreparedStatement(global::D1PreparedStatement inner)
    {
        _inner = inner;
    }

    public D1PreparedStatement Bind(params object[] values) 
        => new D1PreparedStatement(_inner.bind(values));

    public Task<T?> First<T>() => _inner.first<T>();
    public Task<D1Result> Run() => _inner.run();
    public Task<D1ResultAll> All() => _inner.all();
}

/// <summary>
/// Durable Object namespace.
/// </summary>
public class DurableObjectNamespace
{
    private readonly global::DurableObjectNamespace _inner;

    public DurableObjectNamespace(global::DurableObjectNamespace inner)
    {
        _inner = inner;
    }

    public DurableObjectId NewUniqueId(DurableObjectUniqueidOptions? options = null) 
        => _inner.newUniqueId(options);

    public DurableObjectId IdFromName(string name) => _inner.idFromName(name);
    public DurableObjectId IdFromString(string id) => _inner.idFromString(id);
    public DurableObjectStub Get(DurableObjectId id, DurableObjectGetOptions? options = null) 
        => _inner.get(id, options);
}

/// <summary>
/// Stub for communicating with a Durable Object.
/// </summary>
public class DurableObjectStub
{
    private readonly global::DurableObjectStub _inner;

    public DurableObjectStub(global::DurableObjectStub inner)
    {
        _inner = inner;
    }

    public Task<Response> Fetch(RequestInfo init) => _inner.fetch(init);
}

// Additional types would be defined here...

/// <summary>
/// Request initialization options.
/// </summary>
public record RequestInit(
    string? Method = null,
    HeadersInit? Headers = null,
    object? Body = null,
    string? Redirect = null,
    CfProperties? Cf = null
);

/// <summary>
/// Response initialization options.
/// </response clipped><NOTE>Due to the max output limit, only part of this file has been saved to your workspace. It may be too long and needs to be shortened before being stored completely.</NOTE>
