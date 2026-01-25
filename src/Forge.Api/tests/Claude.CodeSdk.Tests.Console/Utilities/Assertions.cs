namespace Claude.CodeSdk.Tests.Console.Utilities;

/// <summary>
/// Assertion helpers for tests.
/// </summary>
public static class Assertions
{
    public static void Assert(bool condition, string message = "Assertion failed")
    {
        if (!condition)
        {
            throw new AssertionException(message);
        }
    }

    public static void AssertNotNull<T>(T? value, string? name = null) where T : class
    {
        if (value is null)
        {
            var message = name is not null
                ? $"Expected {name} to be non-null"
                : "Expected value to be non-null";
            throw new AssertionException(message);
        }
    }

    public static void AssertNotNullOrEmpty(string? value, string? name = null)
    {
        if (string.IsNullOrEmpty(value))
        {
            var message = name is not null
                ? $"Expected {name} to be non-null and non-empty"
                : "Expected value to be non-null and non-empty";
            throw new AssertionException(message);
        }
    }

    public static void AssertContains(string haystack, string needle, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (!haystack.Contains(needle, comparison))
        {
            throw new AssertionException($"Expected string to contain '{needle}' but was: '{Truncate(haystack, 200)}'");
        }
    }

    public static void AssertEqual<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            var msg = message ?? $"Expected '{expected}' but was '{actual}'";
            throw new AssertionException(msg);
        }
    }

    public static void AssertGreaterThan(int value, int threshold, string? name = null)
    {
        if (value <= threshold)
        {
            var msg = name is not null
                ? $"Expected {name} ({value}) to be greater than {threshold}"
                : $"Expected {value} to be greater than {threshold}";
            throw new AssertionException(msg);
        }
    }

    public static void AssertOfType<T>(object? value, string? name = null)
    {
        if (value is not T)
        {
            var actualType = value?.GetType().Name ?? "null";
            var msg = name is not null
                ? $"Expected {name} to be of type {typeof(T).Name} but was {actualType}"
                : $"Expected value to be of type {typeof(T).Name} but was {actualType}";
            throw new AssertionException(msg);
        }
    }

    public static void AssertThrows<TException>(Action action, string? message = null) where TException : Exception
    {
        try
        {
            action();
            throw new AssertionException(message ?? $"Expected {typeof(TException).Name} to be thrown but no exception was thrown");
        }
        catch (TException)
        {
            // Expected
        }
        catch (AssertionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                message ?? $"Expected {typeof(TException).Name} to be thrown but got {ex.GetType().Name}: {ex.Message}",
                ex);
        }
    }

    public static async Task AssertThrowsAsync<TException>(Func<Task> action, string? message = null) where TException : Exception
    {
        try
        {
            await action();
            throw new AssertionException(message ?? $"Expected {typeof(TException).Name} to be thrown but no exception was thrown");
        }
        catch (TException)
        {
            // Expected
        }
        catch (AssertionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                message ?? $"Expected {typeof(TException).Name} to be thrown but got {ex.GetType().Name}: {ex.Message}",
                ex);
        }
    }

    public static void AssertAny<T>(IEnumerable<T> collection, Func<T, bool> predicate, string? message = null)
    {
        if (!collection.Any(predicate))
        {
            throw new AssertionException(message ?? "Expected at least one element to match the predicate");
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;
        return value[..(maxLength - 3)] + "...";
    }
}
