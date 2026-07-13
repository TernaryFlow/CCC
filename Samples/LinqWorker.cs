using Cloudflare.Workers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Sample worker demonstrating LINQ operations
/// </summary>
public class LinqWorker
{
    public async Task<Response> Fetch(Request request, Env env, ExecutionContext ctx)
    {
        var url = new URL(request.Url);
        
        if (url.Pathname == "/linq-demo")
        {
            return await DemonstrateLinq();
        }
        
        return Response.Json(new { 
            message = "LINQ Worker",
            endpoints = new[] { "/linq-demo" }
        });
    }

    private async Task<Response> DemonstrateLinq()
    {
        // Sample data
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Laptop", Price = 999.99m, Category = "Electronics", InStock = true },
            new Product { Id = 2, Name = "Mouse", Price = 29.99m, Category = "Electronics", InStock = true },
            new Product { Id = 3, Name = "Desk Chair", Price = 199.99m, Category = "Furniture", InStock = false },
            new Product { Id = 4, Name = "Monitor", Price = 349.99m, Category = "Electronics", InStock = true },
            new Product { Id = 5, Name = "Keyboard", Price = 79.99m, Category = "Electronics", InStock = true },
            new Product { Id = 6, Name = "Bookshelf", Price = 149.99m, Category = "Furniture", InStock = true },
        };

        // Where - Filter products
        var electronics = products.Where(p => p.Category == "Electronics").ToList();

        // Select - Project to anonymous type
        var productNames = products.Select(p => new { p.Name, p.Price }).ToList();

        // OrderBy - Sort by price descending
        var expensiveFirst = products.OrderByDescending(p => p.Price).ToList();

        // Take/Skip - Pagination
        var page1 = products.OrderBy(p => p.Id).Skip(0).Take(3).ToList();

        // Aggregate - Calculate total value
        var totalValue = products.Where(p => p.InStock).Sum(p => p.Price);

        // GroupBy - Group by category
        var byCategory = products.GroupBy(p => p.Category)
            .Select(g => new { Category = g.Key, Count = g.Count(), AveragePrice = g.Average(p => p.Price) })
            .ToList();

        // Any/All - Predicates
        var hasExpensiveItems = products.Any(p => p.Price > 500);
        var allInStock = products.All(p => p.InStock);

        // FirstOrDefault/LastOrDefault
        var firstElectronic = products.FirstOrDefault(p => p.Category == "Electronics");
        var lastProduct = products.LastOrDefault();

        // Distinct
        var categories = products.Select(p => p.Category).Distinct().ToList();

        // Concat, Union, Intersect, Except
        var list1 = new List<int> { 1, 2, 3, 4, 5 };
        var list2 = new List<int> { 4, 5, 6, 7, 8 };
        var union = list1.Union(list2).ToList();
        var intersect = list1.Intersect(list2).ToList();
        var except = list1.Except(list2).ToList();

        return Response.Json(new
        {
            electronics = electronics.Select(p => p.Name).ToList(),
            productNames,
            expensiveFirst = expensiveFirst.Select(p => p.Name).ToList(),
            page1 = page1.Select(p => p.Name).ToList(),
            totalValue,
            byCategory,
            hasExpensiveItems,
            allInStock,
            firstElectronic = firstElectronic?.Name,
            lastProduct = lastProduct?.Name,
            categories,
            union,
            intersect,
            except
        });
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
    public bool InStock { get; set; }
}
