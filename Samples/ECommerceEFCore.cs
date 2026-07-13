using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Cloudflare.Workers;

namespace Samples.EFCoreStore
{
    // ==================== ENTITIES ====================

    [Table("Products")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = "";

        [Column("description")]
        public string? Description { get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        [Column("stock_quantity")]
        public int StockQuantity { get; set; }

        [Column("category_id")]
        public int CategoryId { get; set; }

        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property (will be loaded separately)
        public Category? Category { get; set; }
    }

    [Table("Categories")]
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = "";

        [Column("parent_id")]
        public int? ParentId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("Customers")]
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("email")]
        public string Email { get; set; } = "";

        [Column("first_name")]
        public string FirstName { get; set; } = "";

        [Column("last_name")]
        public string LastName { get; set; } = "";

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("Orders")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("order_date")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Column("status")]
        public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("shipping_address")]
        public string? ShippingAddress { get; set; }

        [Column("tracking_number")]
        public string? TrackingNumber { get; set; }

        // Navigation
        public Customer? Customer { get; set; }
        public List<OrderItem>? Items { get; set; }
    }

    [Table("OrderItems")]
    public class OrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("unit_price")]
        public decimal UnitPrice { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        // Navigation
        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }

    [Table("ShoppingCart")]
    public class ShoppingCartItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("added_at")]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Product? Product { get; set; }
    }

    // ==================== DB CONTEXT ====================

    public class StoreDbContext : DbContext
    {
        public StoreDbContext(D1Database db) : base(db) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<ShoppingCartItem> ShoppingCart => Set<ShoppingCartItem>();

        public override async Task<int> SaveChangesAsync()
        {
            // In a real implementation, this would track changes
            // For now, changes are saved immediately via AddAsync/Update/Remove
            return await Task.FromResult(0);
        }
    }

    // ==================== WORKER ====================

    public class ECommerceWorker
    {
        private readonly StoreDbContext _db;

        public ECommerceWorker(D1Database db)
        {
            _db = new StoreDbContext(db);
        }

        // ==================== PRODUCT APIs ====================

        /// <summary>
        /// Get all products with optional filtering
        /// GET /api/products?category=5&minPrice=10&maxPrice=100&search=laptop
        /// </summary>
        public async Task<Response> GetProducts(Request request)
        {
            var url = new URL(request.Url);
            var categoryParam = url.SearchParams.Get("category");
            var minPriceParam = url.SearchParams.Get("minPrice");
            var maxPriceParam = url.SearchParams.Get("maxPrice");
            var searchParam = url.SearchParams.Get("search");

            IQueryable<Product> query = _db.Products;

            // Apply filters using EF Core LINQ syntax
            if (!string.IsNullOrEmpty(categoryParam) && int.TryParse(categoryParam, out var categoryId))
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            if (!string.IsNullOrEmpty(minPriceParam) && decimal.TryParse(minPriceParam, out var minPrice))
            {
                query = query.Where(p => p.Price >= minPrice);
            }

            if (!string.IsNullOrEmpty(maxPriceParam) && decimal.TryParse(maxPriceParam, out var maxPrice))
            {
                query = query.Where(p => p.Price <= maxPrice);
            }

            if (!string.IsNullOrEmpty(searchParam))
            {
                query = query.Where(p => p.Name.Contains(searchParam) || 
                                        (p.Description != null && p.Description.Contains(searchParam)));
            }

            // Order by newest first
            query = query.OrderByDescending(p => p.CreatedAt);

            var products = await query.ToListAsync();

            return Response.Json(new { success = true, data = products, count = products.Count });
        }

        /// <summary>
        /// Get single product by ID
        /// GET /api/products/{id}
        /// </summary>
        public async Task<Response> GetProductById(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return Response.Json(new { success = false, error = "Product not found" }, 404);
            }

            return Response.Json(new { success = true, data = product });
        }

        /// <summary>
        /// Create new product
        /// POST /api/products
        /// </summary>
        public async Task<Response> CreateProduct(Request request)
        {
            var product = await request.JsonAsync<Product>();

            if (product == null)
            {
                return Response.Json(new { success = false, error = "Invalid product data" }, 400);
            }

            // Validate
            if (string.IsNullOrEmpty(product.Name))
            {
                return Response.Json(new { success = false, error = "Product name is required" }, 400);
            }

            if (product.Price <= 0)
            {
                return Response.Json(new { success = false, error = "Price must be greater than 0" }, 400);
            }

            await _db.Products.AddAsync(product);

            return Response.Json(new { success = true, data = product }, 201);
        }

        /// <summary>
        /// Update product
        /// PUT /api/products/{id}
        /// </summary>
        public async Task<Response> UpdateProduct(int id, Request request)
        {
            var existing = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null)
            {
                return Response.Json(new { success = false, error = "Product not found" }, 404);
            }

            var updates = await request.JsonAsync<Product>();
            if (updates == null)
            {
                return Response.Json(new { success = false, error = "Invalid data" }, 400);
            }

            // Update fields
            existing.Name = updates.Name ?? existing.Name;
            existing.Description = updates.Description ?? existing.Description;
            existing.Price = updates.Price > 0 ? updates.Price : existing.Price;
            existing.StockQuantity = updates.StockQuantity >= 0 ? updates.StockQuantity : existing.StockQuantity;
            existing.CategoryId = updates.CategoryId > 0 ? updates.CategoryId : existing.CategoryId;
            existing.UpdatedAt = DateTime.UtcNow;

            // In full implementation, _db.SaveChangesAsync() would persist changes
            await _db.SaveChangesAsync();

            return Response.Json(new { success = true, data = existing });
        }

        /// <summary>
        /// Delete product
        /// DELETE /api/products/{id}
        /// </summary>
        public async Task<Response> DeleteProduct(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return Response.Json(new { success = false, error = "Product not found" }, 404);
            }

            // Check if product has orders
            var hasOrders = await _db.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrders)
            {
                return Response.Json(new { success = false, error = "Cannot delete product with existing orders" }, 400);
            }

            // Delete logic here
            return Response.Json(new { success = true, message = "Product deleted" });
        }

        // ==================== CATEGORY APIs ====================

        /// <summary>
        /// Get all categories with product counts
        /// GET /api/categories
        /// </summary>
        public async Task<Response> GetCategories()
        {
            var categories = await _db.Categories.ToListAsync();

            // Get product count for each category
            var result = new List<object>();
            foreach (var cat in categories)
            {
                var productCount = await _db.Products.CountAsync(p => p.CategoryId == cat.Id);
                result.Add(new
                {
                    category = cat,
                    productCount = productCount
                });
            }

            return Response.Json(new { success = true, data = result });
        }

        // ==================== SHOPPING CART APIs ====================

        /// <summary>
        /// Get shopping cart for customer
        /// GET /api/cart/{customerId}
        /// </summary>
        public async Task<Response> GetCart(int customerId)
        {
            var cartItems = await _db.ShoppingCart
                .Where(i => i.CustomerId == customerId)
                .ToListAsync();

            var total = cartItems.Sum(i => i.Quantity * 10); // Would need product price in real impl

            return Response.Json(new
            {
                success = true,
                data = new
                {
                    items = cartItems,
                    totalItems = cartItems.Sum(i => i.Quantity),
                    totalPrice = total
                }
            });
        }

        /// <summary>
        /// Add item to cart
        /// POST /api/cart/add
        /// </summary>
        public async Task<Response> AddToCart(Request request)
        {
            var cartRequest = await request.JsonAsync<AddToCartRequest>();
            if (cartRequest == null)
            {
                return Response.Json(new { success = false, error = "Invalid request" }, 400);
            }

            // Check product exists and has stock
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == cartRequest.ProductId);
            if (product == null)
            {
                return Response.Json(new { success = false, error = "Product not found" }, 404);
            }

            if (product.StockQuantity < cartRequest.Quantity)
            {
                return Response.Json(new { success = false, error = "Insufficient stock" }, 400);
            }

            // Check if already in cart
            var existing = await _db.ShoppingCart
                .FirstOrDefaultAsync(i => i.CustomerId == cartRequest.CustomerId && i.ProductId == cartRequest.ProductId);

            if (existing != null)
            {
                existing.Quantity += cartRequest.Quantity;
            }
            else
            {
                var newItem = new ShoppingCartItem
                {
                    CustomerId = cartRequest.CustomerId,
                    ProductId = cartRequest.ProductId,
                    Quantity = cartRequest.Quantity
                };
                await _db.ShoppingCart.AddAsync(newItem);
            }

            return Response.Json(new { success = true, message = "Added to cart" });
        }

        // ==================== ORDER APIs ====================

        /// <summary>
        /// Create order from cart
        /// POST /api/orders
        /// </summary>
        public async Task<Response> CreateOrder(Request request)
        {
            var orderRequest = await request.JsonAsync<CreateOrderRequest>();
            if (orderRequest == null)
            {
                return Response.Json(new { success = false, error = "Invalid request" }, 400);
            }

            // Get cart items
            var cartItems = await _db.ShoppingCart
                .Where(i => i.CustomerId == orderRequest.CustomerId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return Response.Json(new { success = false, error = "Cart is empty" }, 400);
            }

            // Calculate total and create order items
            var orderItems = new List<OrderItem>();
            decimal total = 0;

            foreach (var cartItem in cartItems)
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == cartItem.ProductId);
                if (product == null)
                {
                    return Response.Json(new { success = false, error = $"Product {cartItem.ProductId} not found" }, 404);
                }

                if (product.StockQuantity < cartItem.Quantity)
                {
                    return Response.Json(new { success = false, error = $"Insufficient stock for {product.Name}" }, 400);
                }

                var subtotal = product.Price * cartItem.Quantity;
                total += subtotal;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = cartItem.Quantity,
                    UnitPrice = product.Price,
                    Subtotal = subtotal
                });

                // Update stock
                product.StockQuantity -= cartItem.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
            }

            // Create order
            var order = new Order
            {
                CustomerId = orderRequest.CustomerId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                TotalAmount = total,
                ShippingAddress = orderRequest.ShippingAddress
            };

            // In real implementation, use transaction
            // await _db.Orders.AddAsync(order);
            // Add order items
            // Clear cart

            return Response.Json(new
            {
                success = true,
                data = new
                {
                    orderId = 1, // order.Id,
                    total = total,
                    items = orderItems.Count
                }
            }, 201);
        }

        /// <summary>
        /// Get customer orders
        /// GET /api/orders?customerId=1&status=Pending
        /// </summary>
        public async Task<Response> GetOrders(Request request)
        {
            var url = new URL(request.Url);
            var customerParam = url.SearchParams.Get("customerId");
            var statusParam = url.SearchParams.Get("status");

            IQueryable<Order> query = _db.Orders;

            if (!string.IsNullOrEmpty(customerParam) && int.TryParse(customerParam, out var customerId))
            {
                query = query.Where(o => o.CustomerId == customerId);
            }

            if (!string.IsNullOrEmpty(statusParam))
            {
                query = query.Where(o => o.Status == statusParam);
            }

            query = query.OrderByDescending(o => o.OrderDate);

            var orders = await query.ToListAsync();

            return Response.Json(new { success = true, data = orders });
        }

        /// <summary>
        /// Update order status
        /// PATCH /api/orders/{id}/status
        /// </summary>
        public async Task<Response> UpdateOrderStatus(int id, Request request)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return Response.Json(new { success = false, error = "Order not found" }, 404);
            }

            var statusRequest = await request.JsonAsync<StatusUpdateRequest>();
            if (statusRequest == null || string.IsNullOrEmpty(statusRequest.Status))
            {
                return Response.Json(new { success = false, error = "Invalid status" }, 400);
            }

            var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(statusRequest.Status))
            {
                return Response.Json(new { success = false, error = "Invalid status value" }, 400);
            }

            order.Status = statusRequest.Status;
            
            if (statusRequest.Status == "Shipped" && !string.IsNullOrEmpty(statusRequest.TrackingNumber))
            {
                order.TrackingNumber = statusRequest.TrackingNumber;
            }

            await _db.SaveChangesAsync();

            return Response.Json(new { success = true, data = order });
        }

        // ==================== ANALYTICS APIs ====================

        /// <summary>
        /// Get sales statistics
        /// GET /api/analytics/sales?days=30
        /// </summary>
        public async Task<Response> GetSalesStats(Request request)
        {
            var url = new URL(request.Url);
            var daysParam = url.SearchParams.Get("days") ?? "30";
            var days = int.Parse(daysParam);

            var startDate = DateTime.UtcNow.AddDays(-days);

            var orders = await _db.Orders
                .Where(o => o.OrderDate >= startDate && o.Status != "Cancelled")
                .ToListAsync();

            var totalRevenue = orders.Sum(o => o.TotalAmount);
            var totalOrders = orders.Count;
            var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            // Group by status
            var byStatus = orders.GroupBy(o => o.Status)
                .Select(g => new { status = g.Key, count = g.Count(), revenue = g.Sum(o => o.TotalAmount) })
                .ToList();

            return Response.Json(new
            {
                success = true,
                data = new
                {
                    period = $"{days} days",
                    totalRevenue = totalRevenue,
                    totalOrders = totalOrders,
                    averageOrderValue = avgOrderValue,
                    byStatus = byStatus
                }
            });
        }

        /// <summary>
        /// Get top selling products
        /// GET /api/analytics/top-products?limit=10
        /// </summary>
        public async Task<Response> GetTopProducts(Request request)
        {
            var url = new URL(request.Url);
            var limitParam = url.SearchParams.Get("limit") ?? "10";
            var limit = int.Parse(limitParam);

            // This would require a more complex query with joins
            // Simplified version:
            var products = await _db.Products
                .OrderByDescending(p => p.StockQuantity) // Placeholder - would use order items
                .Take(limit)
                .ToListAsync();

            return Response.Json(new { success = true, data = products });
        }
    }

    // ==================== DTOs ====================

    public class AddToCartRequest
    {
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateOrderRequest
    {
        public int CustomerId { get; set; }
        public string ShippingAddress { get; set; } = "";
    }

    public class StatusUpdateRequest
    {
        public string Status { get; set; } = "";
        public string? TrackingNumber { get; set; }
    }
}
