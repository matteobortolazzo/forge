using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Claude.CodeSdk.Exceptions;

namespace Claude.CodeSdk.Internal;

/// <summary>
/// Manages the CLI process lifecycle and I/O.
/// </summary>
internal sealed class CliProcess : IAsyncDisposable
{
    private readonly Process _process;
    private CancellationTokenRegistration _ctRegistration;
    private readonly StringBuilder _stderrBuilder = new();
    private readonly TaskCompletionSource<bool> _stderrCompleted = new();
    private bool _disposed;
    private bool _stdinClosed;

    private CliProcess(Process process)
    {
        _process = process;
    }

    /// <summary>
    /// Starts a new CLI process with the given arguments.
    /// </summary>
    /// <param name="cliPath">The path to the CLI executable.</param>
    /// <param name="arguments">The arguments to pass.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="environmentVariables">Additional environment variables.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="keepStdinOpen">If true, stdin is kept open for bidirectional communication.</param>
    /// <returns>The started CLI process wrapper.</returns>
    public static CliProcess Start(
        string cliPath,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        IReadOnlyDictionary<string, string>? environmentVariables,
        CancellationToken ct,
        bool keepStdinOpen = false)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = cliPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        // Add arguments
        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        // Set working directory
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        // Add environment variables
        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                startInfo.Environment[key] = value;
            }
        }

        var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                throw new CliConnectionException("Failed to start Claude Code CLI process.");
            }

            var wrapper = new CliProcess(process);

            // Close stdin immediately unless we need bidirectional communication
            if (!keepStdinOpen)
            {
                process.StandardInput.Close();
                wrapper._stdinClosed = true;
            }
            wrapper.StartStderrCapture();

            // Register cancellation to kill the process
            // Captures 'wrapper' instead of 'process' to avoid AccessToDisposedClosure
            wrapper._ctRegistration = ct.Register(() =>
            {
                try
                {
                    // Check _disposed to avoid accessing disposed process
                    if (wrapper is { _disposed: false, _process.HasExited: false })
                    {
                        wrapper._process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Process may have already exited
                }
            });

            return wrapper;
        }
        catch (Exception ex) when (ex is not ClaudeAgentException)
        {
            process.Dispose();
            throw new CliConnectionException($"Failed to start Claude Code CLI: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads output lines from stdout as an async enumerable.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of output lines.</returns>
    public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var reader = _process.StandardOutput;

        while (!ct.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(ct);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (line is null)
            {
                // End of stream
                break;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return line;
            }
        }
    }

    /// <summary>
    /// Waits for the process to exit and validates the exit code.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ProcessException">Thrown when the process exits with non-zero code.</exception>
    public async Task WaitForExitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await _process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // If cancelled, try to kill the process
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Ignore
            }
            throw;
        }

        // Wait for stderr to be fully captured
        await _stderrCompleted.Task;

        if (_process.ExitCode != 0)
        {
            var stderr = _stderrBuilder.ToString();
            throw new ProcessException(_process.ExitCode, stderr);
        }
    }

    /// <summary>
    /// Gets the exit code of the process if it has exited.
    /// </summary>
    public int? ExitCode => _process.HasExited ? _process.ExitCode : null;

    /// <summary>
    /// Gets the captured stderr output.
    /// </summary>
    public string Stderr => _stderrBuilder.ToString();

    /// <summary>
    /// Writes a line to stdin and flushes the stream.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown if stdin has been closed.</exception>
    public async Task WriteLineAsync(string content, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_stdinClosed)
        {
            throw new InvalidOperationException("Cannot write to stdin after it has been closed.");
        }

        await _process.StandardInput.WriteLineAsync(content.AsMemory(), ct);
        await _process.StandardInput.FlushAsync(ct);
    }

    /// <summary>
    /// Closes stdin to signal end of input.
    /// </summary>
    public void CloseStdin()
    {
        if (_disposed || _stdinClosed)
        {
            return;
        }

        _stdinClosed = true;
        _process.StandardInput.Close();
    }

    private void StartStderrCapture()
    {
        Task.Run(async () =>
        {
            try
            {
                var reader = _process.StandardError;
                string? line;
                while ((line = await reader.ReadLineAsync()) is not null)
                {
                    _stderrBuilder.AppendLine(line);
                }
            }
            finally
            {
                _stderrCompleted.TrySetResult(true);
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Dispose the cancellation registration first to prevent callback from firing
        await _ctRegistration.DisposeAsync();

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
        }
        catch
        {
            // Ignore errors during disposal
        }

        _process.Dispose();
    }
}
