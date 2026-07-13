# راهنمای مهاجرت از EF Core + SQL Server به Cloudflare Workers

این سند نحوه مهاجرت پروژه‌های دات نت با EF Core و SQL Server به کامپایلر C# به Cloudflare Workers را توضیح می‌دهد.

## بررسی امکان مهاجرت

### ❌ مواردی که **نمی‌توانند** مستقیماً مهاجرت کنند:

1. **EF Core کامل** - به دلیل وابستگی به CLR و Reflection سنگین
2. **SQL Server** - Cloudflare فقط از D1 (SQLite) پشتیبانی می‌کند
3. **Connection Pooling** - محیط Workers تک‌نخی است
4. **Change Tracking پیشرفته** - نیاز به Runtime دارد
5. **Lazy Loading** - نیاز به Proxy generation در زمان اجرا
6. **Migrations خودکار** - باید دستی مدیریت شوند

### ✅ مواردی که **قابل مهاجرت** هستند:

1. **منطق کسب‌وکار** (Business Logic)
2. **کوئری‌های LINQ** (با محدودیت‌هایی)
3. **Entities و DTOs**
4. **Validation Logic**
5. **Service Layer**

## راه حل: لایه سازگاری EF Core Shim

ما یک لایه سازگاری ایجاد کرده‌ایم که سینتکس EF Core را شبیه‌سازی می‌کند:

```csharp
// کد اصلی شما با EF Core
var products = await context.Products
    .Where(p => p.Price > 100)
    .OrderBy(p => p.Name)
    .ToListAsync();

// همان کد با EF Core Shim - بدون تغییر!
var products = await _db.Products
    .Where(p => p.Price > 100)
    .OrderBy(p => p.Name)
    .ToListAsync();
```

## مراحل مهاجرت

### مرحله ۱: نصب بسته‌ها

```bash
dotnet add reference Compiler.EFCore.Shim
dotnet add reference Compiler.SDK
```

### مرحله ۲: تغییر DbContext

```csharp
// قبل - EF Core کامل
public class StoreContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer(connectionString);
    
    public DbSet<Product> Products { get; set; }
}

// بعد - EF Core Shim برای Cloudflare
public class StoreDbContext : DbContext
{
    public StoreDbContext(D1Database db) : base(db) { }
    
    public DbSet<Product> Products => Set<Product>();
}
```

### مرحله ۳: تغییر Entities

```csharp
// افزودن Attributeهای لازم
[Table("Products")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; } = "";
    
    // سایر فیلدها...
}
```

### مرحله ۴: مهاجرت کوئری‌ها

#### کوئری‌های پشتیبانی شده:

```csharp
// ✅ Where با عملگرهای مقایسه‌ای
.Where(p => p.Price > 100)
.Where(p => p.Id == id)
.Where(p => p.Name != null)

// ✅ AndAlso و OrElse
.Where(p => p.Price > 100 && p.StockQuantity > 0)
.Where(p => p.CategoryId == 1 || p.CategoryId == 2)

// ✅ OrderBy
.OrderBy(p => p.Name)
.OrderByDescending(p => p.CreatedAt)

// ✅ Take و Skip
.Take(10)
.Skip(20)

// ✅ Select
.Select(p => new { p.Id, p.Name })

// ✅ Count, Any, FirstOrDefault
await CountAsync()
await AnyAsync()
await FirstOrDefaultAsync()

// ✅ متدهای رشته‌ای
.Where(p => p.Name.Contains("laptop"))
.Where(p => p.Name.StartsWith("Dell"))
.Where(p => p.Name.EndsWith("Pro"))
```

#### کوئری‌های پشتیبانی نشده:

```csharp
// ❌ Join پیچیده
// راه حل: استفاده از چند کوئری جداگانه

// ❌ GroupBy با aggregation
// راه حل: انجام aggregation در کد پس از دریافت داده

// ❌ Subqueryهای تو در تو
// راه حل: بازنویسی به صورت کوئری‌های مسطح

// ❌ Include برای navigation properties
// راه حل: Load کردن جداگانه و joining در کد
```

### مرحله ۵: مهاجرت Database Schema

#### تبدیل SQL Server به SQLite (D1):

```sql
-- SQL Server
CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- SQLite (D1)
CREATE TABLE Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Price REAL NOT NULL,
    CreatedAt TEXT DEFAULT (datetime('now'))
);
```

#### تفاوت‌های کلیدی:

| SQL Server | SQLite (D1) | نکته |
|------------|-------------|------|
| `IDENTITY` | `AUTOINCREMENT` | Auto-increment |
| `NVARCHAR` | `TEXT` | Unicode همیشه در SQLite |
| `DATETIME2` | `TEXT` | تاریخ به صورت ISO string |
| `DECIMAL` | `REAL` | اعشار شناور |
| `GETUTCDATE()` | `datetime('now')` | تابع تاریخ فعلی |
| `TOP 10` | `LIMIT 10` | محدود کردن نتایج |
| `OFFSET 20 FETCH NEXT 10` | `LIMIT 10 OFFSET 20` | Pagination |

### مرحله ۶: مدیریت Migrations

به جای EF Core Migrations، از اسکریپت‌های SQL مستقیم استفاده کنید:

```sql
-- migrations/001_initial.sql
CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Price REAL NOT NULL,
    StockQuantity INTEGER DEFAULT 0,
    CategoryId INTEGER,
    CreatedAt TEXT DEFAULT (datetime('now')),
    UpdatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS IX_Products_CategoryId ON Products(CategoryId);
CREATE INDEX IF NOT EXISTS IX_Products_Name ON Products(Name);
```

```csharp
// اجرای migration در Startup
public class Worker
{
    public async Task<Response> Fetch(Request request, Env env)
    {
        // اجرای migrations در اولین راه‌اندازی
        await env.DB.Prepare(migrationSql).RunAsync();
        
        // ادامه منطق...
    }
}
```

### مرحله ۷: نمونه کامل مهاجرت فروشگاه

#### کد اصلی با EF Core + SQL Server:

```csharp
public class ProductsController : ControllerBase
{
    private readonly StoreContext _context;
    
    public ProductsController(StoreContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        int? categoryId, 
        decimal? minPrice, 
        decimal? maxPrice,
        string? search)
    {
        IQueryable<Product> query = _context.Products;
        
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);
            
        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
            
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);
            
        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.Contains(search) || 
                                    p.Description.Contains(search));
        
        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
            
        return Ok(products);
    }
}
```

#### کد مهاجرت یافته به Cloudflare Workers:

```csharp
using Microsoft.EntityFrameworkCore;
using Cloudflare.Workers;

public class ProductsWorker
{
    private readonly StoreDbContext _db;
    
    public ProductsWorker(D1Database db)
    {
        _db = new StoreDbContext(db);
    }
    
    public async Task<Response> GetProducts(Request request)
    {
        var url = new URL(request.Url);
        var categoryParam = url.SearchParams.Get("category");
        var minPriceParam = url.SearchParams.Get("minPrice");
        var maxPriceParam = url.SearchParams.Get("maxPrice");
        var searchParam = url.SearchParams.Get("search");
        
        IQueryable<Product> query = _db.Products;
        
        if (!string.IsNullOrEmpty(categoryParam) && 
            int.TryParse(categoryParam, out var categoryId))
            query = query.Where(p => p.CategoryId == categoryId);
            
        if (!string.IsNullOrEmpty(minPriceParam) && 
            decimal.TryParse(minPriceParam, out var minPrice))
            query = query.Where(p => p.Price >= minPrice);
            
        if (!string.IsNullOrEmpty(maxPriceParam) && 
            decimal.TryParse(maxPriceParam, out var maxPrice))
            query = query.Where(p => p.Price <= maxPrice);
            
        if (!string.IsNullOrEmpty(searchParam))
            query = query.Where(p => p.Name.Contains(searchParam) || 
                                    (p.Description != null && 
                                     p.Description.Contains(searchParam)));
        
        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
            
        return Response.Json(new { success = true, data = products });
    }
}
```

## نکات مهم عملکرد

### ۱. بهینه‌سازی کوئری‌ها

```csharp
// ❌ بد - Load کردن همه داده‌ها
var all = await _db.Products.ToListAsync();
var filtered = all.Where(p => p.Price > 100).ToList();

// ✅ خوب - فیلتر در دیتابیس
var filtered = await _db.Products
    .Where(p => p.Price > 100)
    .ToListAsync();
```

### ۲. Pagination ضروری است

```csharp
// همیشه از Take و Skip استفاده کنید
var products = await _db.Products
    .OrderBy(p => p.Name)
    .Skip(pageNumber * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### ۳. اجتناب از N+1 Query

```csharp
// ❌ بد - N+1 problem
var orders = await _db.Orders.ToListAsync();
foreach (var order in orders)
{
    var customer = await _db.Customers
        .FirstOrDefaultAsync(c => c.Id == order.CustomerId);
}

// ✅ خوب - Single query
var orders = await _db.Orders.ToListAsync();
var customerIds = orders.Select(o => o.CustomerId).Distinct();
var customers = await _db.Customers
    .Where(c => customerIds.Contains(c.Id))
    .ToListAsync();
    
// Join در کد
var customerLookup = customers.ToDictionary(c => c.Id);
foreach (var order in orders)
{
    order.Customer = customerLookup.GetValueOrDefault(order.CustomerId);
}
```

## محدودیت‌ها

| ویژگی | وضعیت | راه جایگزین |
|-------|-------|------------|
| LINQ to Entities | ✅ پشتیبانی | - |
| Change Tracking | ⚠️ محدود | SaveChanges دستی |
| Lazy Loading | ❌ پشتیبانی نمی‌شود | Explicit loading |
| Eager Loading (Include) | ❌ پشتیبانی نمی‌شود | Separate queries |
| Raw SQL | ✅ پشتیبانی | ExecuteCommandAsync |
| Transactions | ✅ پشتیبانی | ExecuteTransactionAsync |
| Stored Procedures | ❌ پشتیبانی نمی‌شود | - |
| Views | ✅ پشتیبانی | به عنوان جدول |
| Indexes | ✅ پشتیبانی | در migration |

## نمونه‌های عملی

- `Samples/ECommerceEFCore.cs` - فروشگاه کامل با EF Core Shim
- `Samples/LinqWorker.cs` - مثال‌های LINQ
- `Samples/D1DatabaseWorker.cs` - کار مستقیم با D1

## جمع‌بندی

| جنبه | امکان مهاجرت | تلاش مورد نیاز |
|------|--------------|----------------|
| Business Logic | ✅ کامل | کم |
| Data Access Layer | ✅ با Shim | متوسط |
| Database Schema | ✅ با تغییرات | متوسط |
| LINQ Queries | ✅ 80% | کم |
| Navigation Properties | ⚠️ محدود | زیاد |
| Advanced EF Features | ❌ | بازنویسی کامل |

**نتیجه**: پروژه‌های تجاری مانند فروشگاه، بیمه و CRM قابل مهاجرت هستند، به شرطی که:
1. از EF Core Shim استفاده شود
2. کوئری‌های پیچیده بازنویسی شوند
3. Schema به SQLite تبدیل شود
4. Navigation properties به صورت دستی load شوند
