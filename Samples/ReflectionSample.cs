using Cloudflare.Workers;
using System;
using System.Threading.Tasks;

/// <summary>
/// Sample worker demonstrating Reflection usage
/// Note: Reflection is simulated at compile-time with runtime helpers
/// </summary>
public class ReflectionSample
{
    public async Task<Response> Fetch(Request request, Env env, ExecutionContext ctx)
    {
        var url = new URL(request.Url);
        
        if (url.Pathname == "/reflect")
        {
            return await DemonstrateReflection();
        }
        
        return Response.Json(new { 
            message = "Reflection Sample",
            endpoints = new[] { "/reflect" }
        });
    }

    private async Task<Response> DemonstrateReflection()
    {
        var sampleObject = new SampleClass { Name = "Test", Value = 42 };
        
        // Get type information (compile-time resolved)
        var typeInfo = TypeInspector.Inspect(sampleObject);
        
        // Get all properties
        var properties = TypeInspector.GetProperties(sampleObject);
        
        // Get property value by name
        var nameValue = TypeInspector.GetProperty(sampleObject, "Name");
        var valueValue = TypeInspector.GetProperty(sampleObject, "Value");
        
        // Set property value by name
        TypeInspector.SetProperty(sampleObject, "Value", 100);
        
        // Invoke method by name
        var methodResult = await TypeInspector.InvokeMethod(sampleObject, "GetNameWithPrefix", "Item: ");
        
        // Create instance from type name
        var newInstance = TypeInspector.CreateInstance("SampleClass");
        
        return Response.Json(new
        {
            typeInfo,
            properties,
            originalName = nameValue,
            originalValue = valueValue,
            updatedObject = new { sampleObject.Name, Value = TypeInspector.GetProperty(sampleObject, "Value") },
            methodResult,
            newInstanceCreated = newInstance != null
        });
    }
}

public class SampleClass
{
    public string Name { get; set; }
    public int Value { get; set; }
    
    public string GetNameWithPrefix(string prefix)
    {
        return $"{prefix}{Name}";
    }
    
    public async Task<string> AsyncMethod()
    {
        await Task.Delay(100);
        return $"Async result: {Name}";
    }
}

/// <summary>
/// Runtime helper for reflection operations
/// This simulates .NET reflection using compile-time metadata
/// </summary>
public static class TypeInspector
{
    /// <summary>
    /// Inspect type metadata
    /// </summary>
    public static object Inspect(object obj)
    {
        // Simulated: Returns type name, base type, interfaces, etc.
        return new
        {
            name = obj.GetType().Name,
            fullName = obj.GetType().FullName,
            isClass = true,
            properties = GetProperties(obj),
            methods = GetMethodNames(obj)
        };
    }
    
    /// <summary>
    /// Get all property names and values
    /// </summary>
    public static object[] GetProperties(object obj)
    {
        // Compile-time generated property list
        return new object[] {
            new { name = "Name", type = "string", value = ((SampleClass)obj).Name },
            new { name = "Value", type = "int", value = ((SampleClass)obj).Value }
        };
    }
    
    /// <summary>
    /// Get property value by name
    /// </summary>
    public static object? GetProperty(object obj, string propertyName)
    {
        return propertyName switch
        {
            "Name" => ((SampleClass)obj).Name,
            "Value" => ((SampleClass)obj).Value,
            _ => null
        };
    }
    
    /// <summary>
    /// Set property value by name
    /// </summary>
    public static void SetProperty(object obj, string propertyName, object? value)
    {
        if (obj is SampleClass sc)
        {
            switch (propertyName)
            {
                case "Name":
                    sc.Name = value?.ToString() ?? "";
                    break;
                case "Value":
                    sc.Value = value != null ? Convert.ToInt32(value) : 0;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Get all method names
    /// </summary>
    public static string[] GetMethodNames(object obj)
    {
        return new[] { "GetNameWithPrefix", "AsyncMethod" };
    }
    
    /// <summary>
    /// Invoke method by name with arguments
    /// </summary>
    public static async Task<object?> InvokeMethod(object obj, string methodName, params object[] args)
    {
        if (obj is SampleClass sc)
        {
            return methodName switch
            {
                "GetNameWithPrefix" => sc.GetNameWithPrefix(args[0]?.ToString() ?? ""),
                "AsyncMethod" => await sc.AsyncMethod(),
                _ => null
            };
        }
        return null;
    }
    
    /// <summary>
    /// Create instance from type name
    /// </summary>
    public static object? CreateInstance(string typeName)
    {
        return typeName switch
        {
            "SampleClass" => new SampleClass(),
            _ => null
        };
    }
}

/// <summary>
/// Environment bindings
/// </summary>
public class Env
{
}
