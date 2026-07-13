using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloudflare.Workers;
using Cloudflare.Workers.Data;

namespace Samples.ECommerce
{
    // ==========================================
    // Domain Models
    // ==========================================

    public record Product
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public decimal Price { get; init; }
        public int Stock { get; init; }
        public string Category { get; init; } = "General";
        public bool IsAvailable => Stock > 0;
    }

    public record CartItem
    {
        public string ProductId { get; init; } = "";
        public string ProductName { get; init; } = "";
        public decimal Price { get; init; }
        public int Quantity { get; init; }
        public decimal Total => Price * Quantity;
    }

    public record Order
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string CustomerEmail { get; init; } = "";
        public List<CartItem> Items { get; init; } = new();
        public decimal TotalAmount => Items.Sum(i => i.Total);
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public string Status { get; init; } = "Pending"; // Pending, Paid, Shipped, Cancelled
        public string? PaymentIntentId { get; init; }
    }

    // ==========================================
    // Mock Database (In production, use D1 via Env)
    // ==========================================

    public static class MockDb
    {
        public static readonly List<Product> Products = new()
        {
            new Product { Name = "Laptop Pro X1", Description = "High performance laptop", Price = 1299.99m, Stock = 10, Category = "Electronics" },
            new Product { Name = "Wireless Mouse", Description = "Ergonomic wireless mouse", Price = 49.99m, Stock = 50, Category = "Electronics" },
            new Product { Name = "Mechanical Keyboard", Description = "RGB mechanical keyboard", Price = 129.50m, Stock = 25, Category = "Electronics" },
            new Product { Name = "USB-C Hub", Description = "7-in-1 USB-C Hub", Price = 39.99m, Stock = 100, Category = "Accessories" },
            new Product { Name = "Monitor 27\"", Description = "4K IPS Monitor", Price = 349.00m, Stock = 15, Category = "Electronics" },
            new Product { Name = "Desk Lamp", Description = "LED Desk Lamp", Price = 24.99m, Stock = 40, Category = "Office" },
            new Product { Name = "Office Chair", Description = "Ergonomic office chair", Price = 199.99m, Stock = 8, Category = "Office" },
            new Product { Name = "Notebook Set", Description = "Pack of 5 notebooks", Price = 12.50m, Stock = 200, Category = "Stationery" }
        };
    }

    // ==========================================
    // Services
    // ==========================================

    public class CartService
    {
        private readonly IKvNamespace _kv;

        public CartService(IKvNamespace kv)
        {
            _kv = kv;
        }

        public async Task<List<CartItem>> GetCartAsync(string cartId)
        {
            var data = await _kv.Get<string>(cartId);
            if (string.IsNullOrEmpty(data)) return new List<CartItem>();
            
            // Simple JSON parsing simulation (in real scenario use System.Text.Json)
            // For this sample, we assume the KV stores a serialized string we can parse
            // Since we can't use full JSON lib in this subset easily without runtime bloat, 
            // we return a mock structure or assume deserialization happens in runtime helper.
            // Here we just return a placeholder logic for brevity in sample.
            return new List<CartItem>(); // TODO: Implement proper deserialization
        }

        public async Task AddToCartAsync(string cartId, CartItem item)
        {
            var cart = await GetCartAsync(cartId);
            var existing = cart.FirstOrDefault(x => x.ProductId == item.ProductId);
            
            if (existing != null)
            {
                // Update quantity (Immutable update simulation)
                var updatedItem = existing with { Quantity = existing.Quantity + item.Quantity };
                cart.RemoveAll(x => x.ProductId == item.ProductId);
                cart.Add(updatedItem);
            }
            else
            {
                cart.Add(item);
            }

            await _kv.Put(cartId, SerializeCart(cart), new KvPutOptions { ExpirationTtl = 3600 });
        }

        private string SerializeCart(List<CartItem> cart)
        {
            // Simplified serialization for sample
            return System.Text.Json.JsonSerializer.Serialize(cart);
        }
    }

    public class OrderService
    {
        private readonly ID1Database _db;
        private readonly IKvNamespace _kv;

        public OrderService(ID1Database db, IKvNamespace kv)
        {
            _db = db;
            _kv = kv;
        }

        public async Task<Order> CreateOrderAsync(string customerEmail, List<CartItem> items, string cartId)
        {
            // 1. Validate Stock
            foreach (var item in items)
            {
                var product = MockDb.Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                {
                    throw new InvalidOperationException($"Product {item.ProductName} is out of stock.");
                }
            }

            // 2. Create Order Object
            var order = new Order
            {
                CustomerEmail = customerEmail,
                Items = items,
                Status = "Pending"
            };

            // 3. Save to D1 (Simulated SQL)
            // In real code: await _db.ExecuteAsync("INSERT INTO orders ...", order);
            Console.WriteLine($"[D1] Inserting order {order.Id} for {customerEmail}");

            // 4. Deduct Stock (Simulated)
            // In real code: Transactional update in D1
            foreach (var item in items)
            {
                var product = MockDb.Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    Console.WriteLine($"[D1] Updating stock for {product.Name}: {product.Stock} -> {product.Stock - item.Quantity}");
                }
            }

            // 5. Clear Cart
            await _kv.Delete(cartId);

            return order;
        }

        public async Task<Order?> GetOrderAsync(string orderId)
        {
            // Simulate fetching from D1
            // var result = await _db.ExecuteAsync("SELECT * FROM orders WHERE id = ?", orderId);
            return null; // Placeholder
        }
    }

    // ==========================================
    // Worker Entry Point
    // ==========================================

    public class ECommerceWorker : Worker
    {
        public override async Task<Response> Fetch(Request request, Env env, ExecutionContext ctx)
        {
            var url = new Uri(request.Url);
            var path = url.AbsolutePath;
            var method = request.Method;

            try
            {
                // Route Handling
                if (path == "/products" && method == "GET")
                {
                    return await HandleGetProducts(request, url);
                }
                else if (path.StartsWith("/cart") && method == "GET")
                {
                    return await HandleGetCart(request, env, url);
                }
                else if (path.StartsWith("/cart") && method == "POST")
                {
                    return await HandleAddToCart(request, env);
                }
                else if (path == "/checkout" && method == "POST")
                {
                    return await HandleCheckout(request, env);
                }
                else if (path.StartsWith("/orders/") && method == "GET")
                {
                    return await HandleGetOrder(request, env, url);
                }

                return new Response("Not Found", new ResponseInit { Status = 404 });
            }
            catch (Exception ex)
            {
                return new Response(
                    System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message }), 
                    new ResponseInit { Status = 500, Headers = { { "Content-Type", "application/json" } } }
                );
            }
        }

        private async Task<Response> HandleGetProducts(Request request, Uri url)
        {
            var query = System.Web.HttpUtility.ParseQueryString(url.Query);
            var category = query["category"];
            var search = query["search"];

            var products = MockDb.Products.AsQueryable();

            // Apply Filters using LINQ
            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category);
            }

            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => 
                    p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                    p.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            // Projection
            var result = products.Select(p => new 
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Stock,
                p.Category,
                Available = p.IsAvailable
            }).ToList();

            var json = System.Text.Json.JsonSerializer.Serialize(result);
            return new Response(json, new ResponseInit 
            { 
                Headers = { { "Content-Type", "application/json" } } 
            });
        }

        private async Task<Response> HandleGetCart(Request request, Env env, Uri url)
        {
            var cartId = url.Segments.LastOrDefault()?.Trim('/') ?? "default-cart";
            var kv = env.KV_STORE;
            var service = new CartService(kv);
            
            var items = await service.GetCartAsync(cartId);
            var json = System.Text.Json.JsonSerializer.Serialize(new { cartId, items, total = items.Sum(i => i.Total) });
            
            return new Response(json, new ResponseInit 
            { 
                Headers = { { "Content-Type", "application/json" } } 
            });
        }

        private async Task<Response> HandleAddToCart(Request request, Env env)
        {
            var body = await request.Json<Dictionary<string, object>>();
            if (body == null) return new Response("Invalid body", new ResponseInit { Status = 400 });

            var productId = body.ContainsKey("productId") ? body["productId"]!.ToString()! : "";
            var quantity = body.ContainsKey("quantity") ? Convert.ToInt32(body["quantity"]) : 1;
            var cartId = body.ContainsKey("cartId") ? body["cartId"]!.ToString()! : "default-cart";

            var product = MockDb.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
                return new Response("Product not found", new ResponseInit { Status = 404 });

            var item = new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = quantity
            };

            var kv = env.KV_STORE;
            var service = new CartService(kv);
            await service.AddToCartAsync(cartId, item);

            return new Response(
                System.Text.Json.JsonSerializer.Serialize(new { success = true, item }), 
                new ResponseInit { Headers = { { "Content-Type", "application/json" } } }
            );
        }

        private async Task<Response> HandleCheckout(Request request, Env env)
        {
            var body = await request.Json<Dictionary<string, object>>();
            if (body == null) return new Response("Invalid body", new ResponseInit { Status = 400 });

            var email = body.ContainsKey("email") ? body["email"]!.ToString()! : "";
            var cartId = body.ContainsKey("cartId") ? body["cartId"]!.ToString()! : "default-cart";

            if (string.IsNullOrEmpty(email))
                return new Response("Email required", new ResponseInit { Status = 400 });

            var kv = env.KV_STORE;
            var cartService = new CartService(kv);
            var items = await cartService.GetCartAsync(cartId);

            if (items.Count == 0)
                return new Response("Cart is empty", new ResponseInit { Status = 400 });

            var db = env.DB; // D1 Binding
            var orderService = new OrderService(db, kv);
            
            var order = await orderService.CreateOrderAsync(email, items, cartId);

            return new Response(
                System.Text.Json.JsonSerializer.Serialize(new { success = true, order }), 
                new ResponseInit { Status = 201, Headers = { { "Content-Type", "application/json" } } }
            );
        }

        private async Task<Response> HandleGetOrder(Request request, Env env, Uri url)
        {
            var orderId = url.Segments.LastOrDefault()?.Trim('/');
            if (string.IsNullOrEmpty(orderId))
                return new Response("Order ID required", new ResponseInit { Status = 400 });

            var db = env.DB;
            var service = new OrderService(db, env.KV_STORE);
            var order = await service.GetOrderAsync(orderId);

            if (order == null)
                return new Response("Order not found", new ResponseInit { Status = 404 });

            return new Response(
                System.Text.Json.JsonSerializer.Serialize(order), 
                new ResponseInit { Headers = { { "Content-Type", "application/json" } } }
            );
        }
    }
}
