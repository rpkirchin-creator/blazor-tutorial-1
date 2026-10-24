using System.Diagnostics;
using System.Text;

namespace BlazorApp1.Services;

public sealed record CommandExecutionResult(
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut);

public sealed class CommandRunner(IHostEnvironment environment, IConfiguration configuration)
{
    private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(30);
    private const int MaxOutputCharacters = 32_768;

    public bool CanExecute =>
        environment.IsDevelopment() &&
        configuration.GetValue<bool>("TerminalCommands:EnableExecution");

    public async Task<CommandExecutionResult> RunAsync(string command)
    {
        if (!CanExecute)
        {
            throw new InvalidOperationException("Command execution is not enabled for this app instance.");
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Enter a command before running it.", nameof(command));
        }

        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("The embedded command runner currently requires Linux.");
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                WorkingDirectory = environment.ContentRootPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("--noprofile");
        process.StartInfo.ArgumentList.Add("--norc");
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add(command);

        if (!process.Start())
        {
            throw new InvalidOperationException("The command process could not be started.");
        }

        var standardOutputTask = ReadBoundedOutputAsync(process.StandardOutput);
        var standardErrorTask = ReadBoundedOutputAsync(process.StandardError);
        var exitTask = process.WaitForExitAsync();
        var timeoutTask = Task.Delay(ExecutionTimeout);

        if (await Task.WhenAny(exitTask, timeoutTask) != exitTask)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await exitTask;
            return new CommandExecutionResult(
                null,
                await standardOutputTask,
                await standardErrorTask,
                TimedOut: true);
        }

        await exitTask;
        return new CommandExecutionResult(
            process.ExitCode,
            await standardOutputTask,
            await standardErrorTask,
            TimedOut: false);
    }

    private static async Task<string> ReadBoundedOutputAsync(StreamReader reader)
    {
        var output = new StringBuilder();
        var buffer = new char[4096];
        var truncated = false;
        int count;

        while ((count = await reader.ReadAsync(buffer.AsMemory())) > 0)
        {
            var remaining = MaxOutputCharacters - output.Length;
            if (remaining > 0)
            {
                output.Append(buffer, 0, Math.Min(count, remaining));
            }

            if (count > remaining)
            {
                truncated = true;
            }
        }

        if (truncated)
        {
            output.AppendLine();
            output.Append("[Output truncated at 32,768 characters.]");
        }

        return output.ToString();
    }
}
