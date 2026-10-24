using LiteDB;
using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;

namespace BlazorApp1.Services;

public sealed class TerminalCommand
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Category { get; set; } = "";

    public string Name { get; set; } = "";

    public string Syntax { get; set; } = "";

    public string Description { get; set; } = "";
}

public sealed class CommandHistoryEntry
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Command { get; set; } = "";

    public DateTimeOffset ExecutedAt { get; set; }

    public int? ExitCode { get; set; }

    public bool TimedOut { get; set; }

    public string? Error { get; set; }

    public string? UndoCommand { get; set; }

    public bool Undone { get; set; }

    public string? UndoOfId { get; set; }
}

public sealed class TerminalCommandStore : IDisposable
{
    public static IReadOnlyList<string> Categories { get; } =
    [
        "Navigation",
        "Files",
        "Search",
        "Git",
        "Processes",
        "Networking",
        "System",
        "Development",
        "Ubuntu",
        "Windows PowerShell"
    ];

    private static readonly Regex DotnetCommandPattern = new(@"^(?:\S*/)?dotnet\s+", RegexOptions.Compiled);
    private readonly LiteDatabase database;
    private readonly ILiteCollection<TerminalCommand> commands;
    private readonly ILiteCollection<CommandHistoryEntry> history;

    public TerminalCommandStore(IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BlazorApp1");
            Directory.CreateDirectory(dataDirectory);
            database = new LiteDatabase(Path.Combine(dataDirectory, "terminal-commands.db"));
        }
        else
        {
            database = new LiteDatabase(":memory:");
        }

        commands = database.GetCollection<TerminalCommand>("commands");
        history = database.GetCollection<CommandHistoryEntry>("commandHistory");
        commands.EnsureIndex(command => command.Category);
        history.EnsureIndex(entry => entry.ExecutedAt);
        history.EnsureIndex(entry => entry.UndoOfId);

        if (commands.Count() == 0)
        {
            commands.InsertBulk(GetStarterCommands());
        }

        ImportDevelopmentCommands(Path.Combine(AppContext.BaseDirectory, "commands.txt"));
    }

    public List<TerminalCommand> GetCommands(string? category = null)
    {
        var results = string.IsNullOrWhiteSpace(category)
            ? commands.FindAll()
            : commands.Find(command => command.Category == category);

        return results
            .OrderBy(command => command.Category)
            .ThenBy(command => command.Name)
            .ToList();
    }

    public void AddCommand(string category, string name, string syntax, string description)
    {
        if (!Categories.Contains(category, StringComparer.Ordinal))
        {
            throw new ArgumentException("Select a valid command type.", nameof(category));
        }

        commands.Insert(new TerminalCommand
        {
            Category = category,
            Name = name.Trim(),
            Syntax = syntax.Trim(),
            Description = description.Trim()
        });
    }

    public (int ImportedCount, int SkippedCount) ImportCommands(Stream stream, string category)
    {
        if (!Categories.Contains(category, StringComparer.Ordinal))
        {
            throw new ArgumentException("Select a valid command type.", nameof(category));
        }

        using var parser = new TextFieldParser(stream);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = false;

        if (parser.EndOfData)
        {
            throw new InvalidDataException("The CSV file is empty.");
        }

        var headers = parser.ReadFields()
            ??             throw new InvalidDataException("The CSV header could not be read.");
        var nameIndex = Array.IndexOf(headers, "newName");
        var descriptionIndex = Array.IndexOf(headers, "newDescription");
        var commandIndex = Array.IndexOf(headers, "commandToRun");

        if (nameIndex < 0 || descriptionIndex < 0 || commandIndex < 0)
        {
            throw new InvalidDataException(
                "The CSV must have newName, newDescription, and commandToRun columns.");
        }

        var requiredFieldCount = Math.Max(nameIndex, Math.Max(descriptionIndex, commandIndex)) + 1;
        var existingCommands = commands.Find(command => command.Category == category)
            .Select(command => command.Syntax)
            .ToHashSet(StringComparer.Ordinal);
        var importedCommands = new List<TerminalCommand>();
        var skippedCount = 0;
        var rowNumber = 1;

        while (!parser.EndOfData)
        {
            rowNumber++;
            var fields = parser.ReadFields();
            if (fields is null || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (fields.Length < requiredFieldCount)
            {
                throw new InvalidDataException($"CSV row {rowNumber} is missing one or more required fields.");
            }

            var name = fields[nameIndex].Trim();
            var description = fields[descriptionIndex].Trim();
            var syntax = fields[commandIndex].Trim();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(description) ||
                string.IsNullOrWhiteSpace(syntax))
            {
                throw new InvalidDataException($"CSV row {rowNumber} must include a name, description, and command.");
            }

            if (name.Length > 80 || description.Length > 500 || syntax.Length > 2000)
            {
                throw new InvalidDataException($"CSV row {rowNumber} exceeds the supported field lengths.");
            }

            if (!existingCommands.Add(syntax))
            {
                skippedCount++;
                continue;
            }

            importedCommands.Add(new TerminalCommand
            {
                Category = category,
                Name = name,
                Syntax = syntax,
                Description = description
            });
        }

        if (importedCommands.Count > 0)
        {
            commands.InsertBulk(importedCommands);
        }

        return (importedCommands.Count, skippedCount);
    }

    private void ImportDevelopmentCommands(string path)
    {
        var lines = File.ReadAllLines(path);
        var description = "";
        var importedCount = 0;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("# ", StringComparison.Ordinal))
            {
                description = trimmedLine[2..].Trim();
                continue;
            }

            if (!DotnetCommandPattern.IsMatch(trimmedLine))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new InvalidDataException($"A description is required before command: {trimmedLine}");
            }

            var name = description
                .Split([", then open ", " to "], 2, StringSplitOptions.None)[0]
                .Trim();

            if (commands.FindOne(command =>
                    command.Category == "Development" && command.Syntax == trimmedLine) is null)
            {
                commands.Insert(new TerminalCommand
                {
                    Category = "Development",
                    Name = name,
                    Syntax = trimmedLine,
                    Description = description
                });
            }

            importedCount++;
            description = "";
        }

        if (importedCount == 0)
        {
            throw new InvalidDataException($"No .NET commands were found in {path}.");
        }
    }

    public List<CommandHistoryEntry> GetHistory()
    {
        return history.FindAll()
            .OrderByDescending(entry => entry.ExecutedAt)
            .Take(50)
            .ToList();
    }

    public void ClearHistory()
    {
        history.DeleteAll();
    }

    public string AddHistoryEntry(string command, string? undoOfId = null)
    {
        var entry = new CommandHistoryEntry
        {
            Command = command,
            ExecutedAt = DateTimeOffset.Now,
            UndoOfId = undoOfId
        };
        history.Insert(entry);
        return entry.Id;
    }

    public void CompleteHistoryEntry(string id, CommandExecutionResult result)
    {
        var entry = history.FindById(id)
            ?? throw new InvalidOperationException("The command history entry could not be found.");

        entry.ExitCode = result.ExitCode;
        entry.TimedOut = result.TimedOut;
        history.Update(entry);

        if (entry.UndoOfId is not null && !result.TimedOut && result.ExitCode == 0)
        {
            var original = history.FindById(entry.UndoOfId)
                ?? throw new InvalidOperationException("The original command history entry could not be found.");
            original.Undone = true;
            history.Update(original);
        }
    }

    public void FailHistoryEntry(string id, string error)
    {
        var entry = history.FindById(id)
            ?? throw new InvalidOperationException("The command history entry could not be found.");
        entry.Error = error;
        history.Update(entry);
    }

    public void SetUndoCommand(string id, string undoCommand)
    {
        var entry = history.FindById(id)
            ?? throw new InvalidOperationException("The command history entry could not be found.");
        entry.UndoCommand = undoCommand.Trim();
        history.Update(entry);
    }

    public void Dispose()
    {
        database.Dispose();
    }

    private static IEnumerable<TerminalCommand> GetStarterCommands()
    {
        return
        [
            new() { Category = "Navigation", Name = "Show current directory", Syntax = "pwd", Description = "Print the current working directory." },
            new() { Category = "Navigation", Name = "Change directory", Syntax = "cd <directory>", Description = "Move into another directory." },
            new() { Category = "Navigation", Name = "Go to home directory", Syntax = "cd ~", Description = "Move to your home directory." },
            new() { Category = "Files", Name = "List files", Syntax = "ls -la", Description = "List files, including hidden files, with details." },
            new() { Category = "Files", Name = "Copy a file", Syntax = "cp <source> <destination>", Description = "Copy a file to another path." },
            new() { Category = "Files", Name = "Move or rename a file", Syntax = "mv <source> <destination>", Description = "Move a file or rename it." },
            new() { Category = "Files", Name = "Create a directory", Syntax = "mkdir <directory>", Description = "Create a new directory." },
            new() { Category = "Search", Name = "Find text in files", Syntax = "grep -R \"text\" <directory>", Description = "Search recursively for text in files." },
            new() { Category = "Search", Name = "Find files by name", Syntax = "find <directory> -name \"*.txt\"", Description = "Find files matching a name pattern." },
            new() { Category = "Git", Name = "Check working tree", Syntax = "git status", Description = "Show changed, staged, and untracked files." },
            new() { Category = "Git", Name = "Review changes", Syntax = "git diff", Description = "Show unstaged changes." },
            new() { Category = "Git", Name = "View recent commits", Syntax = "git log --oneline -10", Description = "Show the ten most recent commits in compact form." },
            new() { Category = "Processes", Name = "List processes", Syntax = "ps aux", Description = "Show running processes." },
            new() { Category = "Processes", Name = "Monitor processes", Syntax = "top", Description = "Interactively view process and resource usage." },
            new() { Category = "Networking", Name = "Check connectivity", Syntax = "ping <host>", Description = "Send test packets to check host connectivity." },
            new() { Category = "Networking", Name = "Show network interfaces", Syntax = "ip addr", Description = "Display network interfaces and IP addresses." },
            new() { Category = "System", Name = "Show disk space", Syntax = "df -h", Description = "Display filesystem capacity in readable units." },
            new() { Category = "System", Name = "Show memory usage", Syntax = "free -h", Description = "Display memory and swap usage in readable units." }
        ];
    }
}
