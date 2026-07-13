# Samples - C# to Cloudflare Workers Compiler

این پوشه شامل نمونه‌های کامل برای کامپایلر C# به Cloudflare Workers است.

## دسته‌بندی نمونه‌ها

### 🟢 نمونه‌های پایه (Basic)

| فایل | توضیحات | ویژگی‌ها |
|------|---------|----------|
| `ExampleWorker.cs` | ساده‌ترین Worker | HTTP Handler، Response |
| `LinqWorker.cs` | عملیات LINQ کامل | Where, Select, OrderBy, GroupBy |
| `KvStorageWorker.cs` | مدیریت KV Storage | GET, PUT, DELETE, List |
| `D1DatabaseWorker.cs` | CRUD با D1 Database | Query, Insert, Update, Delete |

### 🟡 نمونه‌های پیشرفته (Advanced)

| فایل | توضیحات | ویژگی‌ها |
|------|---------|----------|
| `ReflectionSample.cs` | شبیه‌سازی Reflection | Type inspection, dynamic invocation |
| `ThreadingSample.cs` | اجرای موازی | Task.Run, ParallelForEach |
| `UnsafeSample.cs` | کد ناامن شبیه‌سازی شده | Memory allocation, pointer ops |
| `AdvancedFeaturesSample.cs` | ترکیب ویژگی‌ها | Async, LINQ, Exception handling |

### 🔵 پروژه‌های کامل (Full Projects)

| پوشه | توضیحات | ویژگی‌ها |
|------|---------|----------|
| `ECommerce/` | فروشگاه اینترنتی کامل | Products, Cart, Orders, D1, KV |

---

## راه‌اندازی سریع

### 1. کامپایل یک نمونه ساده

```bash
csw compile Samples/ExampleWorker.cs --output bin/worker.js
```

### 2. اجرای لوکال

```bash
wrangler dev bin/worker.js
```

### 3. دیپلوی

```bash
csw publish Samples/ExampleWorker.cs
```

---

## نمونه‌های پایه

### ExampleWorker.cs

ساده‌ترین Worker ممکن:

```csharp
public class ExampleWorker : Worker
{
    public override Task<Response> Fetch(Request request, Env env, ExecutionContext ctx)
    {
        return Task.FromResult(new Response("Hello from C#!"));
    }
}
```

**خروجی:**
```javascript
export default {
    async fetch(request, env, ctx) {
        return new Response("Hello from C#!");
    }
}
```

### LinqWorker.cs

نمایش قدرت LINQ که به متدهای آرایه JavaScript ترجمه می‌شود:

```csharp
var numbers = new[] { 1, 2, 3, 4, 5 };
var result = numbers
    .Where(x => x > 2)
    .Select(x => x * 2)
    .OrderBy(x => x)
    .ToList();
```

**ترجمه به JavaScript:**
```javascript
const numbers = [1, 2, 3, 4, 5];
const result = numbers
    .filter(x => x > 2)
    .map(x => x * 2)
    .sort((a, b) => a - b);
```

### KvStorageWorker.cs

کار با Cloudflare KV:

```csharp
public class KvStorageWorker : Worker
{
    public override async Task<Response> Fetch(Request request, Env env, ExecutionContext ctx)
    {
        var kv = env.KV_STORE;
        
        // ذخیره
        await kv.Put("key", "value", new KvPutOptions { ExpirationTtl = 3600 });
        
        // خواندن
        var value = await kv.Get<string>("key");
        
        // حذف
        await kv.Delete("key");
        
        return new Response($"Value: {value}");
    }
}
```

### D1DatabaseWorker.cs

کار با Cloudflare D1 (SQLite):

```csharp
public class D1DatabaseWorker : Worker
{
    public override async Task<Response> Fetch(Request request, Env env, ExecutionContext ctx)
    {
        var db = env.DB;
        
        // Insert
        await db.ExecuteAsync(
            "INSERT INTO users (name, email) VALUES (?, ?)",
            "John", "john@example.com");
        
        // Select
        var result = await db.ExecuteAsync(
            "SELECT * FROM users WHERE id = ?",
            1);
        
        return new Response(JsonSerializer.Serialize(result));
    }
}
```

---

## پروژه کامل: فروشگاه اینترنتی

پوشه `ECommerce/` یک فروشگاه کامل را نشان می‌دهد.

### ساختار

```
ECommerce/
├── Program.cs          # کد اصلی Worker
├── ECommerce.csproj    # فایل پروژه
├── wrangler.toml       # تنظیمات Cloudflare
├── schema.sql          # اسکیمای دیتابیس D1
└── README.md           # مستندات کامل
```

### API Endpoints

| متد | مسیر | توضیحات |
|-----|------|---------|
| GET | `/products` | لیست محصولات |
| POST | `/cart` | افزودن به سبد |
| GET | `/cart/{id}` | دریافت سبد |
| POST | `/checkout` | ثبت سفارش |
| GET | `/orders/{id}` | دریافت سفارش |

### راه‌اندازی

```bash
# ایجاد دیتابیس
wrangler d1 create ecommerce-db
wrangler d1 execute ecommerce-db --file=Samples/ECommerce/schema.sql

# کامپایل
csw compile Samples/ECommerce --output bin/worker.js

# اجرای لوکال
wrangler dev

# دیپلوی
wrangler deploy
```

---

## دستورات CLI

```bash
# کامپایل فایل تکی
csw compile Samples/ExampleWorker.cs

# کامپایل پروژه
csw compile Samples/ECommerce/ECommerce.csproj

# Watch mode
csw watch Samples/ECommerce

# دیپلوی مستقیم
csw publish Samples/ECommerce

# ایجاد پروژه جدید
csw new my-worker --template basic
csw new my-store --template ecommerce
```

---

## نکات مهم

### ✅ بهترین روش‌ها

1. **استفاده از Record**: برای مدل‌های داده از `record` استفاده کنید
2. **LINQ بهینه**: LINQ به متدهای native آرایه ترجمه می‌شود
3. **Async/Await**: تمام عملیات I/O باید async باشد
4. **Dependency Injection**: سرویس‌ها را در constructor تزریق کنید

### ⚠️ محدودیت‌ها

- Reflection کامل پشتیبانی نمی‌شود
- Threading واقعی وجود ندارد (فقط async/await)
- P/Invoke و Unsafe code شبیه‌سازی می‌شوند
- حداکثر اندازه Bundle: 1MB (فشرده)

### 🚀 بهینه‌سازی

```csharp
// ❌ بد - ایجاد اشیاء زیاد
for (int i = 0; i < 1000; i++)
{
    var obj = new MyObject();
}

// ✅ خوب - استفاده از Pooling
var pool = ObjectPool<MyObject>.Create();
for (int i = 0; i < 1000; i++)
{
    var obj = pool.Get();
    // use obj
    pool.Return(obj);
}
```

---

## تست نمونه‌ها

```bash
# تست محصولات
curl https://your-worker.workers.dev/products

# تست سبد خرید
curl -X POST https://your-worker.workers.dev/cart \
  -H "Content-Type: application/json" \
  -d '{"productId": "prod-1", "quantity": 2}'

# تست checkout
curl -X POST https://your-worker.workers.dev/checkout \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "cartId": "default-cart"}'
```

---

## منابع بیشتر

- [مستندات اصلی](../README.md)
- [معماری کامپایلر](../docs/architecture.md)
- [نقشه راه](../docs/roadmap.md)
- [مستندات Cloudflare Workers](https://developers.cloudflare.com/workers/)
