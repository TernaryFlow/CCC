/**
 * Cloudflare Workers SDK for C# Workers
 * Provides C# wrappers for Cloudflare Worker APIs
 */

// ============================================
// Request/Response Types
// ============================================

class Request {
    constructor(url, init) {
        this._request = new globalThis.Request(url, init);
    }

    get url() {
        return this._request.url;
    }

    get method() {
        return this._request.method;
    }

    get headers() {
        return this._request.headers;
    }

    get cf() {
        return this._request.cf;
    }

    async text() {
        return await this._request.text();
    }

    async json() {
        return await this._request.json();
    }

    async formData() {
        return await this._request.formData();
    }

    async blob() {
        return await this._request.blob();
    }

    async arrayBuffer() {
        return await this._request.arrayBuffer();
    }

    clone() {
        return new Request(this._request.clone());
    }
}

class Response {
    constructor(body, init) {
        this._response = new globalThis.Response(body, init);
    }

    static json(data, init) {
        return new Response(JSON.stringify(data), {
            ...init,
            headers: {
                ...(init?.headers || {}),
                'Content-Type': 'application/json'
            }
        });
    }

    static html(html, init) {
        return new Response(html, {
            ...init,
            headers: {
                ...(init?.headers || {}),
                'Content-Type': 'text/html'
            }
        });
    }

    static redirect(url, status = 302) {
        return new Response(null, { status, headers: { Location: url } });
    }

    get status() {
        return this._response.status;
    }

    get statusText() {
        return this._response.statusText;
    }

    get headers() {
        return this._response.headers;
    }

    get ok() {
        return this._response.ok;
    }

    get redirected() {
        return this._response.redirected;
    }

    get url() {
        return this._response.url;
    }

    async text() {
        return await this._response.text();
    }

    async json() {
        return await this._response.json();
    }

    async formData() {
        return await this._response.formData();
    }

    async blob() {
        return await this._response.blob();
    }

    async arrayBuffer() {
        return await this._response.arrayBuffer();
    }

    clone() {
        return new Response(this._response.clone());
    }
}

// ============================================
// Environment Bindings
// ============================================

class Env {
    constructor(bindings) {
        this._bindings = bindings;
    }

    // KV Namespace binding
    getKV(namespace) {
        return new KVNamespace(this._bindings[namespace]);
    }

    // R2 Bucket binding
    getR2(bucket) {
        return new R2Bucket(this._bindings[bucket]);
    }

    // D1 Database binding
    getD1(database) {
        return new D1Database(this._bindings[database]);
    }

    // Durable Object namespace
    getDurableObject(namespace) {
        return new DurableObjectNamespace(this._bindings[namespace]);
    }

    // Queue binding
    getQueue(queue) {
        return new WorkerQueue(this._bindings[queue]);
    }

    // AI Binding
    getAI(ai) {
        return new AIBinding(this._bindings[ai]);
    }
}

// ============================================
// KV Namespace
// ============================================

class KVNamespace {
    constructor(binding) {
        this._binding = binding;
    }

    async get(key, options) {
        return await this._binding.get(key, options);
    }

    async put(key, value, options) {
        return await this._binding.put(key, value, options);
    }

    async delete(key) {
        return await this._binding.delete(key);
    }

    async list(options) {
        return await this._binding.list(options);
    }
}

// ============================================
// R2 Bucket
// ============================================

class R2Bucket {
    constructor(binding) {
        this._binding = binding;
    }

    async head(key) {
        return await this._binding.head(key);
    }

    async get(key, options) {
        return await this._binding.get(key, options);
    }

    async put(key, value, options) {
        return await this._binding.put(key, value, options);
    }

    async delete(key) {
        return await this._binding.delete(key);
    }

    async list(options) {
        return await this._binding.list(options);
    }

    async createMultipartUpload(key, options) {
        return await this._binding.createMultipartUpload(key, options);
    }

    async resumeMultipartUpload(key, uploadId) {
        return await this._binding.resumeMultipartUpload(key, uploadId);
    }
}

// ============================================
// D1 Database
// ============================================

class D1Database {
    constructor(binding) {
        this._binding = binding;
    }

    async prepare(query) {
        return new D1PreparedStatement(this._binding.prepare(query));
    }

    async exec(query) {
        return await this._binding.exec(query);
    }

    async batch(statements) {
        return await this._binding.batch(statements.map(s => s._statement));
    }

    async dump() {
        return await this._binding.dump();
    }

    async batchSync(statements) {
        return await this._binding.batchSync(statements);
    }

    async execSync(query) {
        return await this._binding.execSync(query);
    }
}

class D1PreparedStatement {
    constructor(statement) {
        this._statement = statement;
    }

    bind(...values) {
        return new D1PreparedStatement(this._statement.bind(...values));
    }

    first(options) {
        return this._statement.first(options);
    }

    run() {
        return this._statement.run();
    }

    all() {
        return this._statement.all();
    }

    raw(options) {
        return this._statement.raw(options);
    }
}

// ============================================
// Durable Objects
// ============================================

class DurableObjectNamespace {
    constructor(binding) {
        this._binding = binding;
    }

    newUniqueId(options) {
        return this._binding.newUniqueId(options);
    }

    idFromName(name) {
        return this._binding.idFromName(name);
    }

    idFromString(id) {
        return this._binding.idFromString(id);
    }

    get(id, options) {
        return this._binding.get(id, options);
    }
}

class DurableObjectStub {
    constructor(stub) {
        this._stub = stub;
    }

    async fetch(init) {
        return await this._stub.fetch(init);
    }

    async connect(address, options) {
        return await this._stub.connect(address, options);
    }
}

// ============================================
// Queues
// ============================================

class WorkerQueue {
    constructor(binding) {
        this._binding = binding;
    }

    async send(message, options) {
        return await this._binding.send(message, options);
    }

    async sendBatch(messages, options) {
        return await this._binding.sendBatch(messages, options);
    }
}

// ============================================
// AI Binding
// ============================================

class AIBinding {
    constructor(binding) {
        this._binding = binding;
    }

    async run(model, input) {
        return await this._binding.run(model, input);
    }

    async chat(input) {
        return await this._binding.run('@cf/meta/llama-2-7b-chat-int8', input);
    }

    async generateImage(prompt, options) {
        return await this._binding.run('@cf/stabilityai/stable-diffusion-xl-base-1.0', { prompt, ...options });
    }

    async transcribe(audio, options) {
        return await this._binding.run('@cf/openai/whisper', { audio, ...options });
    }
}

// ============================================
// Cache API
// ============================================

class CacheStorage {
    static async open(cacheName) {
        const cache = await caches.open(cacheName);
        return new Cache(cache);
    }

    static async has(cacheName) {
        return await caches.has(cacheName);
    }

    static async delete(cacheName) {
        return await caches.delete(cacheName);
    }

    static async keys() {
        return await caches.keys();
    }
}

class Cache {
    constructor(cache) {
        this._cache = cache;
    }

    async match(request, options) {
        const response = await this._cache.match(request, options);
        return response ? new Response(response.body, response) : null;
    }

    async put(request, response) {
        return await this._cache.put(request._request || request, response._response || response);
    }

    async add(request) {
        return await this._cache.add(request._request || request);
    }

    async addAll(requests) {
        return await this._cache.addAll(requests.map(r => r._request || r));
    }

    async delete(request, options) {
        return await this._cache.delete(request._request || request, options);
    }

    async keys(request, options) {
        return await this._cache.keys(request?._request || request, options);
    }
}

// ============================================
// ExecutionContext
// ============================================

class ExecutionContext {
    constructor(ctx) {
        this._ctx = ctx;
    }

    waitUntil(promise) {
        this._ctx.waitUntil(promise);
    }

    passThroughOnException() {
        this._ctx.passThroughOnException();
    }
}

// ============================================
// Utility Classes
// ============================================

class Headers {
    constructor(init) {
        this._headers = new globalThis.Headers(init);
    }

    append(name, value) {
        this._headers.append(name, value);
    }

    delete(name) {
        this._headers.delete(name);
    }

    get(name) {
        return this._headers.get(name);
    }

    has(name) {
        return this._headers.has(name);
    }

    set(name, value) {
        this._headers.set(name, value);
    }

    forEach(callback) {
        this._headers.forEach(callback);
    }

    entries() {
        return this._headers.entries();
    }

    keys() {
        return this._headers.keys();
    }

    values() {
        return this._headers.values();
    }

    [Symbol.iterator]() {
        return this._headers[Symbol.iterator]();
    }
}

// ============================================
// Exports
// ============================================

export {
    Request,
    Response,
    Env,
    KVNamespace,
    R2Bucket,
    D1Database,
    D1PreparedStatement,
    DurableObjectNamespace,
    DurableObjectStub,
    WorkerQueue,
    AIBinding,
    CacheStorage,
    Cache,
    ExecutionContext,
    Headers
};
