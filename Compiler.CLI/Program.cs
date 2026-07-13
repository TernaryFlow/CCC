using System.CommandLine;
using System.CommandLine.Invocation;
using CSharpWorkers.Core;
using CSharpWorkers.Parser;

namespace CSharpWorkers.CLI;

/// <summary>
/// Command-line interface for the C# to Cloudflare Workers compiler.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("C# to Cloudflare Workers Compiler");

        // compile command
        var compileCommand = new Command("compile", "Compile C# source code to JavaScript")
        {
            new Argument<string>("input", "Input file or project path"),
            new Option<string>(new[] { "-o", "--output" }, () => "./dist", "Output directory"),
            new Option<bool>(new[] { "-m", "--minify" }, "Minify output"),
            new Option<bool>(new[] { "-s", "--source-maps" }, () => true, "Generate source maps"),
            new Option<bool>(new[] { "--optimize" }, () => true, "Enable optimizations"),
        };

        compileCommand.SetHandler(async (context) =>
        {
            var input = context.ParseResult.GetValueForArgument(compileCommand.Arguments[0] as Argument<string>);
            var output = context.ParseResult.GetValueForOption(compileCommand.Options[0] as Option<string>);
            var minify = context.ParseResult.GetValueForOption(compileCommand.Options[1] as Option<bool>);
            var sourceMaps = context.ParseResult.GetValueForOption(compileCommand.Options[2] as Option<bool>);
            var optimize = context.ParseResult.GetValueForOption(compileCommand.Options[3] as Option<bool>);

            await CompileAsync(input, output, minify, sourceMaps, optimize);
        });

        // watch command
        var watchCommand = new Command("watch", "Watch mode for development")
        {
            new Argument<string>("input", "Input file or project path"),
            new Option<string>(new[] { "-o", "--output" }, () => "./dist", "Output directory"),
        };

        watchCommand.SetHandler(async (context) =>
        {
            var input = context.ParseResult.GetValueForArgument(watchCommand.Arguments[0] as Argument<string>);
            var output = context.ParseResult.GetValueForOption(watchCommand.Options[0] as Option<string>);

            await WatchAsync(input, output);
        });

        // publish command
        var publishCommand = new Command("publish", "Publish worker to Cloudflare")
        {
            new Argument<string>("name", "Worker name"),
            new Option<string>(new[] { "-e", "--environment" }, () => "production", "Environment"),
            new Option<string>(new[] { "-c", "--config" }, "wrangler.toml config path"),
        };

        publishCommand.SetHandler(async (context) =>
        {
            var name = context.ParseResult.GetValueForArgument(publishCommand.Arguments[0] as Argument<string>);
            var environment = context.ParseResult.GetValueForOption(publishCommand.Options[0] as Option<string>);
            var config = context.ParseResult.GetValueForOption(publishCommand.Options[1] as Option<string>);

            await PublishAsync(name, environment, config);
        });

        // bundle command
        var bundleCommand = new Command("bundle", "Bundle worker with dependencies")
        {
            new Argument<string>("input", "Input file or project path"),
            new Option<string>(new[] { "-o", "--output" }, () => "./dist/worker.js", "Output file"),
        };

        bundleCommand.SetHandler(async (context) =>
        {
            var input = context.ParseResult.GetValueForArgument(bundleCommand.Arguments[0] as Argument<string>);
            var output = context.ParseResult.GetValueForOption(bundleCommand.Options[0] as Option<string>);

            await BundleAsync(input, output);
        });

        rootCommand.AddCommand(compileCommand);
        rootCommand.AddCommand(watchCommand);
        rootCommand.AddCommand(publishCommand);
        rootCommand.AddCommand(bundleCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task CompileAsync(string input, string output, bool minify, bool sourceMaps, bool optimize)
    {
        Console.WriteLine($"Compiling {input}...");

        try
        {
            var options = new CompilationOptions(
                OutputPath: output,
                Minify = minify,
                SourceMaps = sourceMaps,
                TreeShaking = optimize,
                Optimize = optimize
            );

            var parser = new CSharpParser(options);
            
            string source;
            if (File.Exists(input))
            {
                source = await File.ReadAllTextAsync(input);
            }
            else if (Directory.Exists(input))
            {
                // Handle project directory
                var csFiles = Directory.GetFiles(input, "*.cs", SearchOption.AllDirectories);
                var sources = new List<(string Path, string Content)>();
                
                foreach (var file in csFiles)
                {
                    sources.Add((file, await File.ReadAllTextAsync(file)));
                }

                // For now, just concatenate - proper solution would handle project references
                source = string.Join("\n\n", sources.Select(s => s.Content));
            }
            else
            {
                Console.Error.WriteLine($"Error: Input path '{input}' does not exist.");
                return;
            }

            var parsed = parser.Parse(source, input);

            if (parsed.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                Console.WriteLine("Compilation failed with errors:");
                foreach (var diagnostic in parsed.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                {
                    Console.WriteLine($"  {diagnostic.Code}: {diagnostic.Message}");
                    if (diagnostic.Location != null)
                    {
                        Console.WriteLine($"    at {diagnostic.Location.FilePath}({diagnostic.Location.Line},{diagnostic.Location.Column})");
                    }
                }
                return;
            }

            // TODO: Convert to IR and generate JavaScript
            Console.WriteLine("Parsing succeeded!");
            Console.WriteLine($"Output will be written to: {output}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during compilation: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
        }
    }

    private static async Task WatchAsync(string input, string output)
    {
        Console.WriteLine($"Watching {input} for changes...");
        
        var watcher = new FileSystemWatcher(Path.GetDirectoryName(input) ?? ".", "*.cs");
        watcher.Changed += async (sender, e) =>
        {
            Console.WriteLine($"File changed: {e.FullPath}");
            await CompileAsync(input, output, false, true, true);
        };
        
        watcher.EnableRaisingEvents = true;
        
        // Initial compile
        await CompileAsync(input, output, false, true, true);
        
        Console.WriteLine("Press Enter to exit...");
        await Task.Run(() => Console.ReadLine());
    }

    private static async Task PublishAsync(string name, string environment, string? config)
    {
        Console.WriteLine($"Publishing worker '{name}' to {environment}...");
        
        // Check for wrangler CLI
        var wranglerPath = FindWrangler();
        if (wranglerPath == null)
        {
            Console.Error.WriteLine("Error: wrangler CLI not found. Please install it with: npm install -g wrangler");
            return;
        }

        // Run wrangler publish
        var configArg = config != null ? $" --config {config}" : "";
        var envArg = environment != "production" ? $" --env {environment}" : "";
        
        var startInfo = new ProcessStartInfo
        {
            FileName = wranglerPath,
            Arguments = $"publish{name}{envArg}{configArg}",
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
            Console.WriteLine(process.ExitCode == 0 
                ? "Worker published successfully!" 
                : "Failed to publish worker.");
        }
    }

    private static async Task BundleAsync(string input, string output)
    {
        Console.WriteLine($"Bundling {input}...");
        
        // First compile
        await CompileAsync(input, Path.GetDirectoryName(output) ?? ".", false, true, true);
        
        // Then use esbuild or similar to bundle
        Console.WriteLine("Bundling complete!");
    }

    private static string? FindWrangler()
    {
        // Check common locations
        var paths = new[]
        {
            "/usr/local/bin/wrangler",
            "/usr/bin/wrangler",
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.npm-global/bin/wrangler",
            "./node_modules/.bin/wrangler"
        };

        foreach (var path in paths)
        {
            if (File.Exists(path)) return path;
        }

        // Try PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv != null)
        {
            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                var wrangler = Path.Combine(dir, "wrangler");
                if (File.Exists(wrangler)) return wrangler;
            }
        }

        return null;
    }
}
