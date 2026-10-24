using BlazorApp1.Components;
using BlazorApp1.Services;
using System.IO.Compression;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<TerminalCommandStore>();
builder.Services.AddSingleton<CommandRunner>();

var app = builder.Build();

if (app.Services.GetRequiredService<CommandRunner>().CanExecute)
{
    app.Use(async (context, next) =>
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        var isLoopback = remoteIp is not null &&
            IPAddress.IsLoopback(remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4() : remoteIp);

        if (!isLoopback)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Command execution is only enabled for local connections.");
            return;
        }

        await next();
    });
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/download/source", (IWebHostEnvironment environment) =>
{
    var projectRoot = environment.ContentRootPath;
    var sourceDirectories = new[]
    {
        "Components",
        "Services",
        "wwwroot"
    };
    var rootSourceFiles = new[]
    {
        "BlazorApp1.csproj",
        "Program.cs",
        "README.md",
        "commands.txt",
        "windows_powershell_commands.csv",
        ".gitignore"
    };
    using var archiveStream = new MemoryStream();
    using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
    {
        var filePaths = sourceDirectories
            .SelectMany(directory =>
            {
                var directoryPath = Path.Combine(projectRoot, directory);
                return Directory.Exists(directoryPath)
                    ? Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                    : [];
            })
            .Concat(rootSourceFiles.Select(fileName => Path.Combine(projectRoot, fileName))
                .Where(File.Exists))
            .Append(Path.Combine(projectRoot, "Properties", "launchSettings.json"))
            .Append(Path.Combine(projectRoot, "wwwroot", "ubuntu_commands.csv"))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in filePaths)
        {
            var relativePath = Path.GetRelativePath(projectRoot, filePath);
            var entry = archive.CreateEntry(
                relativePath.Replace(Path.DirectorySeparatorChar, '/'),
                CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var sourceStream = File.OpenRead(filePath);
            sourceStream.CopyTo(entryStream);
        }
    }

    return Results.File(archiveStream.ToArray(), "application/zip", "BlazorApp1-source.zip");
});
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
