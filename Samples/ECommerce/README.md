# E-Commerce Store Sample

این نمونه یک فروشگاه اینترنتی کامل را نشان می‌دهد که با C# نوشته شده و به Cloudflare Workers کامپایل می‌شود.

## ویژگی‌ها

### API Endpoints

| متد | مسیر | توضیحات |
|-----|------|---------|
| GET | `/products` | لیست محصولات با فیلتر و جستجو |
| GET | `/cart/{cartId}` | دریافت سبد خرید |
| POST | `/cart` | افزودن محصول به سبد |
| POST | `/checkout` | ثبت سفارش و تسویه حساب |
| GET | `/orders/{orderId}` | دریافت جزئیات سفارش |

### مدل‌های داده

- **Product**: محصول با نام، قیمت، موجودی و دسته‌بندی
- **CartItem**: آیتم سبد خرید با کمیت و مجموع قیمت
- **Order**: سفارش با وضعیت، اقلام و اطلاعات مشتری

### سرویس‌ها

- **CartService**: مدیریت سبد خرید با KV Storage
- **OrderService**: مدیریت سفارشات با D1 Database

## پیش‌نیازها

```bash
# نصب .NET SDK
dotnet --version

# نصب Wrangler CLI
npm install -g wrangler

# لاگین به Cloudflare
wrangler login
```

## راه‌اندازی دیتابیس D1

```bash
# ایجاد دیتابیس
wrangler d1 create ecommerce-db

# اجرای migration
wrangler d1 execute ecommerce-db --file=./schema.sql

# یا ایجاد جداول به صورت دستی:
wrangler d1 execute ecommerce-db --command "CREATE TABLE IF NOT EXISTS orders (id TEXT PRIMARY KEY, customer_email TEXT, total_amount REAL, status TEXT, created_at TEXT);"
```

## ساختار جداول D1

```sql
-- جدول سفارشات
CREATE TABLE orders (
    id TEXT PRIMARY KEY,
    customer_email TEXT NOT NULL,
    total_amount REAL NOT NULL,
    status TEXT DEFAULT 'Pending',
    payment_intent_id TEXT,
    created_at TEXT NOT NULL
);

CREATE INDEX idx_orders_customer ON orders(customer_email);
CREATE INDEX idx_orders_status ON orders(status);

-- جدول اقلام سفارش
CREATE TABLE order_items (
    id TEXT PRIMARY KEY,
    order_id TEXT NOT NULL,
    product_id TEXT NOT NULL,
    product_name TEXT NOT NULL,
    price REAL NOT NULL,
    quantity INTEGER NOT NULL,
    FOREIGN KEY (order_id) REFERENCES orders(id)
);

-- جدول محصولات (اختیاری - می‌تواند در کد باشد)
CREATE TABLE products (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    price REAL NOT NULL,
    stock INTEGER NOT NULL,
    category TEXT NOT NULL
);
```

## کامپایل و اجرا

### حالت توسعه

```bash
# کامپایل پروژه
csw compile Samples/ECommerce/Program.cs --output bin/worker.js

# یا با استفاده از project file
csw compile Samples/ECommerce/ECommerce.csproj

# اجرای watch mode
csw watch Samples/ECommerce

# اجرای لوکال با Wrangler
wrangler dev
```

### دیپلوی

```bash
# کامپایل و دیپلوی
csw publish Samples/ECommerce

# یا دستی
csw compile Samples/ECommerce/Program.cs --output bin/worker.js
wrangler deploy
```

## مثال‌های درخواست API

### 1. دریافت لیست محصولات

```bash
# همه محصولات
curl https://ecommerce-worker.your-subdomain.workers.dev/products

# فیلتر بر اساس دسته‌بندی
curl https://ecommerce-worker.your-subdomain.workers.dev/products?category=Electronics

# جستجو
curl https://ecommerce-worker.your-subdomain.workers.dev/products?search=laptop
```

**پاسخ نمونه:**
```json
[
  {
    "id": "uuid-1",
    "name": "Laptop Pro X1",
    "description": "High performance laptop",
    "price": 1299.99,
    "stock": 10,
    "category": "Electronics",
    "available": true
  },
  {
    "id": "uuid-2",
    "name": "Wireless Mouse",
    "description": "Ergonomic wireless mouse",
    "price": 49.99,
    "stock": 50,
    "category": "Electronics",
    "available": true
  }
]
```

### 2. افزودن به سبد خرید

```bash
curl -X POST https://ecommerce-worker.your-subdomain.workers.dev/cart \
  -H "Content-Type: application/json" \
  -d '{
    "productId": "uuid-1",
    "quantity": 2,
    "cartId": "user-123-cart"
  }'
```

**پاسخ نمونه:**
```json
{
  "success": true,
  "item": {
    "productId": "uuid-1",
    "productName": "Laptop Pro X1",
    "price": 1299.99,
    "quantity": 2,
    "total": 2599.98
  }
}
```

### 3. دریافت سبد خرید

```bash
curl https://ecommerce-worker.your-subdomain.workers.dev/cart/user-123-cart
```

**پاسخ نمونه:**
```json
{
  "cartId": "user-123-cart",
  "items": [
    {
      "productId": "uuid-1",
      "productName": "Laptop Pro X1",
      "price": 1299.99,
      "quantity": 2,
      "total": 2599.98
    }
  ],
  "total": 2599.98
}
```

### 4. تسویه حساب (Checkout)

```bash
curl -X POST https://ecommerce-worker.your-subdomain.workers.dev/checkout \
  -H "Content-Type: application/json" \
  -d '{
    "email": "customer@example.com",
    "cartId": "user-123-cart"
  }'
```

**پاسخ نمونه:**
```json
{
  "success": true,
  "order": {
    "id": "order-uuid",
    "customerEmail": "customer@example.com",
    "items": [...],
    "totalAmount": 2599.98,
    "createdAt": "2024-01-15T10:30:00Z",
    "status": "Pending"
  }
}
```

### 5. دریافت سفارش

```bash
curl https://ecommerce-worker.your-subdomain.workers.dev/orders/order-uuid
```

## مهاجرت از EF Core

اگر پروژه فعلی شما از EF Core استفاده می‌کند، این تغییرات لازم است:

### قبل (EF Core)
```csharp
public class MyDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer(connectionString);
}

// Usage
var products = await context.Products
    .Where(p => p.Category == "Electronics")
    .ToListAsync();
```

### بعد (Cloudflare D1)
```csharp
// استفاده از LINQ که به SQL ترجمه می‌شود
var products = MockDb.Products // یا از D1 بخوانید
    .Where(p => p.Category == "Electronics")
    .ToList();

// یا مستقیم با D1
var result = await db.ExecuteAsync(
    "SELECT * FROM products WHERE category = ?", 
    "Electronics");
```

## بهینه‌سازی‌های پیشنهادی

1. **کشینگ**: استفاده از Cache API برای محصولات پربازدید
2. **Queue**: پردازش ناهمزمان سفارشات با Workers Queues
3. **Durable Objects**: مدیریت سبد خرید توزیع‌شده
4. **AI**: پیشنهادات محصول با Cloudflare AI
5. **Analytics**: ردیابی رویدادها با Analytics Engine

## امنیت

- اعتبارسنجی ورودی‌ها قبل از پردازش
- محدود کردن نرخ درخواست (Rate Limiting)
- احراز هویت با Cloudflare Access
- رمزنگاری داده‌های حساس

## مانیتورینگ

```csharp
// ارسال رویداد به Analytics Engine
env.ANALYTICS.writeDataPoint({
    blobs: ["product_view", productId],
    doubles: [1],
    indexes: [categoryId]
});
```

## هزینه‌ها

Cloudflare Workers برای این نوع برنامه مناسب است زیرا:

- پرداخت به ازای درخواست
- بدون نیاز به سرور همیشه روشن
- مقیاس خودکار
- کش جهانی رایگان

برای ۱ میلیون درخواست ماهانه: ~$۵-۱۰
