namespace Claude.CodeSdk.Tests.Console.Utilities;

/// <summary>
/// Console output formatting helpers.
/// </summary>
public static class ConsoleHelper
{
    private static readonly object Lock = new();

    public static void WriteHeader(string text)
    {
        lock (Lock)
        {
            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine(new string('=', 60));
            System.Console.WriteLine($"  {text}");
            System.Console.WriteLine(new string('=', 60));
            System.Console.ResetColor();
            System.Console.WriteLine();
        }
    }

    public static void WriteTestStart(string testName)
    {
        lock (Lock)
        {
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.Write($"  Running: ");
            System.Console.ResetColor();
            System.Console.WriteLine(testName);
        }
    }

    public static void WritePass(string testName, TimeSpan duration)
    {
        lock (Lock)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.Write("  [PASS] ");
            System.Console.ResetColor();
            System.Console.Write(testName);
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine($" ({duration.TotalMilliseconds:F0}ms)");
            System.Console.ResetColor();
        }
    }

    public static void WriteFail(string testName, TimeSpan duration, string message)
    {
        lock (Lock)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.Write("  [FAIL] ");
            System.Console.ResetColor();
            System.Console.Write(testName);
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine($" ({duration.TotalMilliseconds:F0}ms)");
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"         {message}");
            System.Console.ResetColor();
        }
    }

    public static void WriteSkip(string testName, string reason)
    {
        lock (Lock)
        {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.Write("  [SKIP] ");
            System.Console.ResetColor();
            System.Console.Write(testName);
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine($" - {reason}");
            System.Console.ResetColor();
        }
    }

    public static void WriteError(string testName, TimeSpan duration, Exception ex)
    {
        lock (Lock)
        {
            System.Console.ForegroundColor = ConsoleColor.Magenta;
            System.Console.Write("  [ERROR] ");
            System.Console.ResetColor();
            System.Console.Write(testName);
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine($" ({duration.TotalMilliseconds:F0}ms)");
            System.Console.ForegroundColor = ConsoleColor.Magenta;
            System.Console.WriteLine($"          {ex.GetType().Name}: {ex.Message}");
            System.Console.ResetColor();
        }
    }

    public static void WriteSummary(int passed, int failed, int skipped, int errors, TimeSpan totalDuration)
    {
        lock (Lock)
        {
            System.Console.WriteLine();
            System.Console.WriteLine(new string('-', 60));
            System.Console.WriteLine();

            System.Console.Write("  Results: ");

            if (passed > 0)
            {
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.Write($"{passed} passed");
                System.Console.ResetColor();
            }

            if (failed > 0)
            {
                if (passed > 0) System.Console.Write(", ");
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.Write($"{failed} failed");
                System.Console.ResetColor();
            }

            if (errors > 0)
            {
                if (passed > 0 || failed > 0) System.Console.Write(", ");
                System.Console.ForegroundColor = ConsoleColor.Magenta;
                System.Console.Write($"{errors} errors");
                System.Console.ResetColor();
            }

            if (skipped > 0)
            {
                if (passed > 0 || failed > 0 || errors > 0) System.Console.Write(", ");
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.Write($"{skipped} skipped");
                System.Console.ResetColor();
            }

            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine($" ({totalDuration.TotalSeconds:F2}s)");
            System.Console.ResetColor();

            System.Console.WriteLine();

            var allPassed = failed == 0 && errors == 0;
            System.Console.ForegroundColor = allPassed ? ConsoleColor.Green : ConsoleColor.Red;
            System.Console.WriteLine(allPassed ? "  All tests passed!" : "  Some tests failed.");
            System.Console.ResetColor();
            System.Console.WriteLine();
        }
    }

    public static void WriteVerbose(string message)
    {
        lock (Lock)
        {
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine($"    {message}");
            System.Console.ResetColor();
        }
    }

    public static void WriteInfo(string message)
    {
        lock (Lock)
        {
            System.Console.ForegroundColor = ConsoleColor.Blue;
            System.Console.WriteLine($"  {message}");
            System.Console.ResetColor();
        }
    }

    public static void WriteWarning(string message)
    {
        lock (Lock)
        {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($"  Warning: {message}");
            System.Console.ResetColor();
        }
    }
}
