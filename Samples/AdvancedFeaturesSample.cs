using System;
using System.Threading.Tasks;

// Sample demonstrating advanced features support
namespace Samples.AdvancedFeatures
{
    // ==================== REFLECTION SAMPLE ====================
    
    public class ReflectionSample
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        
        public void DisplayInfo()
        {
            // typeof is fully supported
            var type = typeof(ReflectionSample);
            Console.WriteLine($"Type: {type.Name}");
            
            // Limited reflection - property access
            var prop = type.GetProperty("Name");
            if (prop != null)
            {
                var value = prop.GetValue(this);
                Console.WriteLine($"Name: {value}");
            }
        }
    }
    
    // ==================== DYNAMIC SAMPLE ====================
    
    public class DynamicSample
    {
        public async Task ProcessDynamic()
        {
            // Dynamic object with Proxy-based access
            dynamic obj = GetDynamicData();
            obj.Name = "Test";
            obj.Count = 42;
            obj.IsActive = true;
            
            Console.WriteLine(obj.Name);
            Console.WriteLine(obj.Count);
            
            // Method invocation on dynamic object
            obj.Print();
        }
        
        private dynamic GetDynamicData()
        {
            return new { Name = "", Count = 0, Print = (Action)(() => {}) };
        }
    }
    
    // ==================== EXPRESSION TREES SAMPLE ====================
    
    public class ExpressionTreeSample
    {
        public void DemonstrateExpressions()
        {
            // Expression compiled to JavaScript function
            // x => x * 2 + 1
            var expr = CreateExpression();
            var compiled = expr.Compile();
            
            var result = compiled(5); // Returns 11
            Console.WriteLine($"Expression result: {result}");
            
            // Complex expression with conditionals
            // x => x > 10 ? x * 2 : x + 5
            var conditionalExpr = CreateConditionalExpression();
            var conditionalFn = conditionalExpr.Compile();
            
            Console.WriteLine(conditionalFn(15)); // 30
            Console.WriteLine(conditionalFn(5));  // 10
        }
        
        private Func<int, int> CreateExpression()
        {
            // In real implementation, this would be an Expression<Func<int,int>>
            // For now, using regular lambda that gets compiled
            return x => x * 2 + 1;
        }
        
        private Func<int, int> CreateConditionalExpression()
        {
            return x => x > 10 ? x * 2 : x + 5;
        }
    }
    
    // ==================== THREADING SIMULATION SAMPLE ====================
    
    public class ThreadingSample
    {
        private SemaphoreSlim _semaphore = new SemaphoreSlim(2);
        private CancellationTokenSource _cts = new CancellationTokenSource();
        
        public async Task DemonstrateThreading()
        {
            try
            {
                // Concurrent operations with semaphore
                var tasks = new[]
                {
                    ProcessWithSemaphore(1),
                    ProcessWithSemaphore(2),
                    ProcessWithSemaphore(3),
                    ProcessWithSemaphore(4)
                };
                
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation was cancelled");
            }
        }
        
        private async Task ProcessWithSemaphore(int id)
        {
            await _semaphore.WaitAsync(_cts.Token);
            try
            {
                Console.WriteLine($"Task {id} starting");
                await Task.Delay(100); // Simulate work
                Console.WriteLine($"Task {id} completed");
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        public async Task DemonstrateMutex()
        {
            var mutex = new Mutex();
            
            await mutex.WaitOneAsync();
            try
            {
                // Critical section
                await PerformCriticalOperation();
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        
        private Task PerformCriticalOperation()
        {
            return Task.CompletedTask;
        }
        
        public async Task DemonstrateEventWaitHandle()
        {
            var mre = new ManualResetEventSlim(false);
            
            // Start a task that will signal the event
            var signaler = Task.Run(async () =>
            {
                await Task.Delay(100);
                mre.Set();
            });
            
            // Wait for the signal
            await mre.WaitAsync(_cts.Token);
            Console.WriteLine("Event signaled!");
            
            await signaler;
        }
    }
    
    // ==================== MARSHAL SAMPLE ====================
    
    public class MarshalSample
    {
        public void DemonstrateMarshal()
        {
            // Allocate memory (ArrayBuffer in JS)
            var buffer = Marshal.AllocHGlobal(1024);
            
            try
            {
                // Write data
                Marshal.WriteInt32(buffer, 0, 42);
                Marshal.WriteInt32(buffer, 4, 100);
                
                // Read data
                int value1 = Marshal.ReadInt32(buffer, 0);
                int value2 = Marshal.ReadInt32(buffer, 4);
                
                Console.WriteLine($"Values: {value1}, {value2}");
                
                // Copy memory
                var destBuffer = Marshal.AllocHGlobal(8);
                Marshal.Copy(buffer, destBuffer, 0, 8);
                
                // String marshaling
                string text = "Hello, Workers!";
                var ptr = Marshal.StringToHGlobalAuto(text);
                var roundtrip = Marshal.PtrToStringAuto(ptr);
                Console.WriteLine(roundtrip);
                
                Marshal.FreeHGlobal(ptr);
                Marshal.FreeHGlobal(destBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        
        public void DemonstrateStructureMarshaling()
        {
            var point = new Point { X = 10, Y = 20 };
            
            // Structure to pointer
            var ptr = Marshal.StructureToPtr(point, typeof(Point), false);
            
            // Pointer back to structure
            var roundtrip = (Point)Marshal.PtrToStructure(ptr, typeof(Point))!;
            
            Console.WriteLine($"Point: ({roundtrip.X}, {roundtrip.Y})");
            
            Marshal.DestroyStructure(ptr, typeof(Point));
        }
        
        [Serializable]
        public struct Point
        {
            public int X;
            public int Y;
        }
    }
    
    // ==================== UNSAFE CODE SAMPLE ====================
    
    public class UnsafeCodeSample
    {
        public unsafe void DemonstrateUnsafeCode()
        {
            int[] arr = { 1, 2, 3, 4, 5 };
            
            // Fixed statement converted to typed array view
            fixed (int* ptr = arr)
            {
                int* p = ptr;
                
                // Pointer dereference
                Console.WriteLine(*p); // 1
                
                // Pointer arithmetic
                p++;
                Console.WriteLine(*p); // 2
                
                // Array-style access through pointer
                Console.WriteLine(p[2]); // 4
            }
            
            // Stack allocation becomes heap allocation
            Span<byte> span = stackalloc byte[256];
            for (int i = 0; i < 256; i++)
            {
                span[i] = (byte)i;
            }
        }
    }
    
    // ==================== COMBINED SAMPLE ====================
    
    public class WorkerWithAdvancedFeatures
    {
        private readonly ReflectionSample _reflection = new();
        private readonly DynamicSample _dynamic = new();
        private readonly ExpressionTreeSample _expressions = new();
        private readonly ThreadingSample _threading = new();
        private readonly MarshalSample _marshal = new();
        
        public async Task<Response> Fetch(Request request)
        {
            // Use reflection to get type info
            _reflection.DisplayInfo();
            
            // Process dynamic data
            await _dynamic.ProcessDynamic();
            
            // Evaluate expressions
            _expressions.DemonstrateExpressions();
            
            // Concurrent processing
            await _threading.DemonstrateThreading();
            
            // Marshal binary data
            _marshal.DemonstrateMarshal();
            
            return new Response("Advanced features demo complete");
        }
    }
}
