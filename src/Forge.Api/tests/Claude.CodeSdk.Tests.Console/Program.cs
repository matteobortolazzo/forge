using Claude.CodeSdk;
using Claude.CodeSdk.Exceptions;
using Claude.CodeSdk.Tests.Console;
using Claude.CodeSdk.Tests.Console.Utilities;

// Parse command line arguments
var verbose = args.Contains("--verbose") || args.Contains("-v");
var filter = GetArgValue(args, "--filter") ?? GetArgValue(args, "-f");
var help = args.Contains("--help") || args.Contains("-h");

if (help)
{
    PrintHelp();
    return 0;
}

// Check if Claude CLI is available
var cliAvailable = CheckCliAvailability();

// Run tests
var runner = new TestRunner(verbose, filter, cliAvailable);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    ConsoleHelper.WriteWarning("Cancellation requested...");
};

return await runner.RunAllAsync(cts.Token);

// Helper methods
static bool CheckCliAvailability()
{
    try
    {
        // Try to create a client - this will throw if CLI is not found
        // No resources are held until a query is made, so no dispose needed
        _ = new ClaudeAgentClient();
        return true;
    }
    catch (CliNotFoundException)
    {
        return false;
    }
}

static string? GetArgValue(string[] args, string argName)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == argName)
        {
            return args[i + 1];
        }
    }

    // Also check for --arg=value format
    foreach (var arg in args)
    {
        if (arg.StartsWith($"{argName}="))
        {
            return arg[(argName.Length + 1)..];
        }
    }

    return null;
}

static void PrintHelp()
{
    Console.WriteLine();
    Console.WriteLine("Claude.CodeSdk Test Console");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -h, --help              Show this help message");
    Console.WriteLine("  -v, --verbose           Show verbose output");
    Console.WriteLine("  -f, --filter <pattern>  Run only tests matching the pattern");
    Console.WriteLine();
    Console.WriteLine("Exit codes:");
    Console.WriteLine("  0 - All tests passed");
    Console.WriteLine("  1 - One or more tests failed");
    Console.WriteLine("  2 - Infrastructure error (e.g., CLI not found for required tests)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run                        # Run all tests");
    Console.WriteLine("  dotnet run --verbose              # Run with verbose output");
    Console.WriteLine("  dotnet run --filter QueryText     # Run tests containing 'QueryText'");
    Console.WriteLine("  dotnet run -- --filter=CliLocator # Alternative filter syntax");
    Console.WriteLine();
}
