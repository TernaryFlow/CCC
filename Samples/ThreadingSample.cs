using Cloudflare.Workers;
using System.Threading.Tasks;

/// <summary>
/// Sample worker demonstrating Threading with Web Workers
/// Note: Uses simulated parallel execution via JavaScript workers
/// </summary>
public class ThreadingSample
{
    public async Task<Response> Fetch(Request request, Env env, ExecutionContext ctx)
    {
        var url = new URL(request.Url);
        
        if (url.Pathname == "/parallel")
        {
            return await DemonstrateParallelExecution();
        }
        
        if (url.Pathname == "/task-run")
        {
            return await DemonstrateTaskRun();
        }
        
        return Response.Json(new { 
            message = "Threading Sample",
            endpoints = new[] { "/parallel", "/task-run" }
        });
    }

    private async Task<Response> DemonstrateParallelExecution()
    {
        // Simulate parallel execution using Task.Run (maps to Web Workers)
        var tasks = new[]
        {
            Task.Run(() => HeavyComputation(1)),
            Task.Run(() => HeavyComputation(2)),
            Task.Run(() => HeavyComputation(3)),
            Task.Run(() => HeavyComputation(4))
        };
        
        var startTime = System.DateTime.Now;
        
        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);
        
        var endTime = System.DateTime.Now;
        var elapsedMs = (endTime - startTime).TotalMilliseconds;
        
        return Response.Json(new
        {
            message = "Parallel execution completed",
            results,
            totalTasks = tasks.Length,
            elapsedMilliseconds = elapsedMs,
            averageResult = results.Average()
        });
    }

    private async Task<Response> DemonstrateTaskRun()
    {
        // Single Task.Run example
        var result = await Task.Run(() =>
        {
            // Simulated background work
            var sum = 0;
            for (int i = 0; i < 1000; i++)
            {
                sum += i;
            }
            return sum;
        });
        
        return Response.Json(new
        {
            message = "Task.Run completed",
            result,
            description = "Background computation executed in separate context"
        });
    }

    private int HeavyComputation(int taskId)
    {
        // Simulated heavy computation
        var result = 0;
        for (int i = 0; i < 10000; i++)
        {
            result += i * taskId;
        }
        return result;
    }
}

/// <summary>
/// Extension methods for threading operations
/// </summary>
public static class ParallelExtensions
{
    /// <summary>
    /// Execute actions in parallel
    /// </summary>
    public static async Task ParallelForEach<T>(this IEnumerable<T> items, Func<T, Task> action)
    {
        var tasks = items.Select(item => Task.Run(() => action(item)));
        await Task.WhenAll(tasks);
    }
    
    /// <summary>
    /// Run computations in parallel and collect results
    /// </summary>
    public static async Task<TResult[]> ParallelSelect<TSource, TResult>(
        this IEnumerable<TSource> items, 
        Func<TSource, TResult> selector)
    {
        var tasks = items.Select(item => Task.Run(() => selector(item)));
        return await Task.WhenAll(tasks);
    }
}

/// <summary>
/// Environment bindings
/// </summary>
public class Env
{
}
