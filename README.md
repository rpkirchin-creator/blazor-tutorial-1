# BlazorApp1 learning guide

This guide describes how the app is put together and what happens when you use
each page. The project is a .NET 10 Blazor Server app: its UI is written in
Razor components, and interactive page events are handled by the ASP.NET Core
server.

## Prerequisites

- Install the .NET 10 SDK to build and run the app.
- Windows users can run the web interface and follow the tutorial in a browser.
  The embedded terminal command runner is Linux-only, so running commands from
  the app is unavailable on Windows.

## Run and build the app

The ready-to-use commands are in [commands.txt](wwwroot/commands.txt). Run them
from the project directory containing `BlazorApp1.csproj` (the repository root
after cloning or extracting the source). The HTTP profile serves the app at
`http://127.0.0.1:5236`.

## Publish for hosting

> The source repository can be hosted on GitHub; the interactive Blazor Server
> app itself must be deployed to a separate .NET-capable host.

Build a Release deployment with:

```sh
dotnet publish BlazorApp1.csproj --configuration Release
```

Deploy the published output to a host with the .NET 10 runtime, set
`ASPNETCORE_ENVIRONMENT=Production`, and configure HTTPS at the host or its
trusted reverse proxy. Do not run a public instance in Development. Public
mode is a read-only tutorial: the command runner and command-management
controls are disabled. Hosting-provider setup, domain names, TLS certificates,
and proxy configuration depend on the provider you choose.

## Hands-on learning path

Use this tutorial with the app open in a browser and this README open in the
VS Code editor. If you want to inspect source code beside the guide, use
**Split Editor Right** to open the matching files in another editor group.
The app runs locally, so you can use this guide offline as long as the
project files are available.

Try each step before reading the explanation below it. You can make the
small practice edits locally, then undo those edits yourself or restore the
original text after the experiment.

### 1. Explore the Home page and its background

1. Open the Home page in the running app.
2. Find the welcome heading in
   [Home.razor](Components/Pages/Home.razor).
3. Find `.home-hero` in
   [Home.razor.css](Components/Pages/Home.razor.css), then follow the shared
   background rules in
   [MainLayout.razor.css](Components/Layout/MainLayout.razor.css).
4. Practice: change the heading text in `Home.razor` and save. With the
   `dotnet watch` command running, observe the browser update. Then find which
   CSS property controls how the background image fills its area.

**What to notice:** the image is on the layout's `<main>`, not the hero
section. The shared parent paints behind both the About header and the page
content. The layout adds special classes only on the Home route, so other
pages keep their ordinary background. See
[How the Home background covers the header](#how-the-home-background-covers-the-header)
for the detailed walkthrough.

### 2. Follow navigation and routing

1. Click Home, Learn Blazor, Code, and Terminal Commands in the left menu.
2. Compare the links in
   [NavMenu.razor](Components/Layout/NavMenu.razor) with the `@page`
   directives at the top of each page component. The Learn item opens
   `Learn.razor`, which displays this guide. The Code item opens the source
   viewer described below.
3. Follow the route from [Routes.razor](Components/Routes.razor) into
   [MainLayout.razor](Components/Layout/MainLayout.razor).
4. Practice: compare the menu destinations with the `@page` directives on
   those pages.

**What to notice:** the URL route selects a page; the layout wraps that page.
The layout's `@Body` is the slot where the selected page appears.

### 3. Explore saved commands without running them

1. Open Terminal Commands, choose the **Development** category, and select a
   command. Observe how its text and description fill the form.
2. Click **Preview** to inspect a command without executing it. Click
   **Cancel** to dismiss the preview.
3. Follow the `commands` list and `SelectCommand` method in
   [TerminalCommands.razor](Components/Pages/TerminalCommands.razor), then
   find `GetCommands` in
   [TerminalCommandStore.cs](Services/TerminalCommandStore.cs).

**What to notice:** the page component handles the user interaction; the
store handles persistence. **Preview** is not execution. Only **Confirm and
run** starts a process.

### 4. Understand history and clearing it

1. To create a history entry, use only a harmless command you understand,
   such as `pwd` (show the current directory), then preview it and choose
   **Confirm and run**.
2. Find the new entry under Command history. If you want to explore the clear
   action, click **Clear history**, read the warning, and either cancel or
   confirm it.
3. Trace `RequestHistoryClear`, `CancelHistoryClear`, and `ClearHistory` in
   `TerminalCommands.razor` into `ClearHistory` in
   `TerminalCommandStore.cs`.

**What to notice:** the page asks for confirmation, then the store deletes
records from the history collection. Saved commands live in a different
collection and are unaffected. Clearing history also clears the displayed
undo drafts; future runs create fresh history entries.

### 5. Trace a command from the button to the operating system

1. In `TerminalCommands.razor`, follow `RunCommand`: it creates a history
   entry, calls the runner, saves the result, and reloads the history.
2. In [CommandRunner.cs](Services/CommandRunner.cs), find where it sets
   `/bin/bash`, the working directory, and the timeout.
3. If you choose to run a command, read the complete command first and only
   confirm something safe and familiar.

**Safety:** commands run on the computer hosting the app and use the app's
permissions. The preview/confirmation flow is a learning aid, not a sandbox.
Avoid commands that delete, overwrite, install, or change system settings.

### 6. Find where the app starts

1. Open [Program.cs](Program.cs) and follow it from `WebApplication.CreateBuilder`
   down to `app.Run()`.
2. Find where Razor Components and the command services are registered.
3. Compare the HTTP URL and environment settings in
   [launchSettings.json](Properties/launchSettings.json).

**What to notice:** `Program.cs` wires the application together. Dependency
injection registrations let Razor components request services with `@inject`
instead of constructing those services themselves.

### 7. Make a small change and verify it

1. Change a heading or button label in a `.razor` file.
2. Save and check that the running app reflects the change.
3. Run the build command from `wwwroot/commands.txt` to check that the project still
   compiles.
4. If the page does not update, read the terminal output, make sure the app
   is still running, and refresh the browser.

**Review questions:** Which file defines a page's route? Where does a click
handler live? Which service stores data? Which service runs shell commands?
What does `@Body` do? Try answering those from the files before checking
the explanations earlier in this guide.

## Pages

The `@page` directive at the top of a Razor file gives it a URL. For example,
`@page "/learn"` makes `Learn.razor` available at `/learn`.

### Learn (`/learn`)

[Learn.razor](Components/Pages/Learn.razor) reads this README file and renders
its Markdown inside a reading panel. On desktop, the right-hand main area is
the scroll container while the navigation stays in place; on smaller screens
the page uses normal document scrolling. The guide is copied beside
the application during build and publish, so the Learn page can load it
without an internet connection. The **Print / Save as PDF** button opens the
browser's print dialog; choose **Save as PDF** there to make a PDF copy.

The **Learning Resources** section on the same page has a **View PDF** button
that opens `wwwroot/Learning Resources.pdf` in an embedded viewer. The button
can hide the viewer again, and a link below it opens the PDF in a new tab if
the browser cannot display it inline. Static files in `wwwroot` are served
directly by the app; the URL encodes the space in the PDF filename as `%20`.

The component uses Markdig to convert Markdown text into HTML and disables
embedded raw HTML in that conversion. Its scoped CSS controls the reading
panel and removes its height limit when printing, so the complete guide—not
just the visible scroll area—is included in the printout. The layout also
hides the sidebar and header for printing.

### Code (`/code`)

[Code.razor](Components/Pages/Code.razor) is a source-code reader for the
app. Its dropdown includes every Razor component under `Components`—pages,
layouts, routing, and shared UI—plus the services, `Program.cs`, the project
file, and the global `wwwroot/app.css` stylesheet. Choose a file to display
its source and, for Razor components, its matching `.razor.css` stylesheet.
The source is rendered as text inside `<pre><code>` elements, so markup is
shown for study instead of being interpreted as part of the page.

The component finds source files under the app's output directory. The
project file copies the selected source files after both build and publish
because compiled app output does not otherwise guarantee that they are
available. This lets the viewer work in local and published output. Source
paths in the dropdown include their folders, which distinguishes files such
as `Components/Layout/MainLayout.razor` from page components with similar
names. A Razor component without a matching `.razor.css` still displays its
markup and explains that no paired stylesheet exists.

**Print / Save as PDF** calls the browser's print dialog. The Code page's
print styles hide its toolbar, allow long code blocks to flow across pages,
and start the stylesheet on a new printed page. The shared layout hides the
navigation and site header so the printout contains the selected source.

**Download Source** downloads a ZIP of the project files, including the
project file, source, documentation, launch profile, and `wwwroot` assets.
Only the project source and required tutorial files are included; local
databases, build output, and appsettings files are excluded. Extract the ZIP,
open the extracted project folder in an editor, and run `dotnet restore`
followed by `dotnet run --project BlazorApp1.csproj` with the .NET 10 SDK
installed.

### Home (`/`)

[Home.razor](Components/Pages/Home.razor) is the landing page. It currently
contains a welcome heading and starter text. It has no C# state or event
handlers, so it is a useful place to start when learning the simplest Razor
page.

### Terminal Commands (`/terminal-commands`)

[TerminalCommands.razor](Components/Pages/TerminalCommands.razor) is the main
feature page.

In Development, the component injects two services with `@inject`:

- `TerminalCommandStore` reads and writes saved commands and command history.
- `CommandRunner` starts a command and returns its output.

Outside Development, the page only shows command examples; command execution,
imports, edits, and history are disabled. In Development,
`OnInitialized` loads the selected category's commands and recent history.
The page then uses Razor controls and Blazor event handlers to update its
component state:

1. Selecting a category calls `ChangeCategory`, which loads that category and
   clears the previous selection.
2. Selecting a saved command fills in the command text.
3. The add-command form validates its fields, saves the command, then refreshes
   the displayed list.
4. **Preview** copies the text into `pendingCommand`. This displays the exact
   command and enables the separate **Confirm and run** button; previewing
   alone does not execute it.
5. Confirming creates a history record, runs the command asynchronously, then
   saves its result and refreshes history. The `finally` block clears the
   running indicator even if an error occurs.
6. Each history entry can store a user-provided undo command. **Preview undo**
   sends that command through the same preview-and-confirm flow. The app does
   not guess or automatically run an inverse command.
7. **Clear history** removes saved history entries after a confirmation step.
   It does not remove saved commands. Future runs create new history entries.

#### How Clear history works

The clear-history feature spans the Razor page and the database service:

- In `TerminalCommands.razor`, the Clear history button is shown only when
  `history` contains entries. It is disabled while a command is running, so
  the history for an active run cannot be cleared midway through execution.
- Clicking it calls `RequestHistoryClear`, which sets the
  `confirmingHistoryClear` component-state field to `true`. Razor re-renders
  and shows a confirmation message with **Yes, clear history** and **Cancel**.
  Cancel calls `CancelHistoryClear` and hides that message without changing
  stored data.
- Confirming calls the page's `ClearHistory` method. That delegates to
  `TerminalCommandStore.ClearHistory`, where LiteDB's `DeleteAll` removes
  records from the history collection. The saved-command collection is
  separate and is not touched.
- After the database operation succeeds, the page empties its in-memory
  `history` list and `undoDrafts` dictionary so the displayed cards and undo
  inputs disappear immediately. If an undo was waiting for confirmation,
  its pending command is also discarded because the history entry it refers
  to no longer exists. Other saved commands and ordinary command previews are
  left alone.
- The page hides the confirmation prompt and displays a status message.
  Because the database history is empty, the normal “Commands you run will
  appear here” message is rendered. The next confirmed run is inserted into
  the history collection as a new record.

The store method intentionally has a single responsibility: deleting all
history records. The component handles the user interaction and updates its
own UI state after the store call.

Razor's `@bind` keeps input values connected to C# fields. `@if` and
`@foreach` conditionally render the right status, errors, output, and history
cards.

### Error (`/Error`)

[Error.razor](Components/Pages/Error.razor) is the production exception page
configured by ASP.NET Core in [Program.cs](Program.cs). It displays a request
ID that can help correlate a report with server logs. The Development
environment shows more diagnostic detail, so it should not be enabled for a
public deployment.

### Not Found (`/not-found`)

[NotFound.razor](Components/Pages/NotFound.razor) is the friendly page for
unmatched URLs. [Program.cs](Program.cs) uses `UseStatusCodePagesWithReExecute`
to send status-code responses through this route while preserving the
appropriate HTTP status.

## Shared UI and routing

- [App.razor](Components/App.razor) is the HTML document shell. It loads CSS
  and the Blazor browser script, renders the router, and includes the reconnect
  dialog.
- [Routes.razor](Components/Routes.razor) finds a component that matches the
  current URL, places it in the default layout, and moves keyboard focus to
  the page heading after navigation.
- [MainLayout.razor](Components/Layout/MainLayout.razor) is the shared page
  frame. `@Body` is where the currently selected page is inserted.
- [MainLayout.razor.css](Components/Layout/MainLayout.razor.css) styles that
  shared frame, including the Home-only background and header, plus the
  navigation sidebar background.
- [NavMenu.razor](Components/Layout/NavMenu.razor) contains the navigation
  links for Home, Learn Blazor, Code, and Terminal Commands. The link
  destinations match the routes on those pages. Its logo uses
  `wwwroot/pinkface.jpg`.
- [NavMenu.razor.css](Components/Layout/NavMenu.razor.css) sizes the logo to
  the full 250-pixel desktop sidebar width while keeping it smaller on mobile.
- [ReconnectModal.razor](Components/Layout/ReconnectModal.razor) provides
  messages and buttons for the built-in Blazor Server connection recovery
  script.
- [\_Imports.razor](Components/_Imports.razor) contains namespaces imported
  into Razor components, so each page does not have to repeat common `@using`
  directives.

The `.razor.css` files alongside layout components are CSS isolation files:
their styles are scoped to the corresponding component.

### How the Home background covers the header

The background is applied to the layout's `<main>`, rather than just the
Home page's content section. That is what lets it show behind both the header
and the page body:

1. `MainLayout.razor` injects Blazor's `NavigationManager`, which knows the
   current URL and the app's base URL.
2. The `IsHomePage` property compares their paths. When they match, the
   current route is the app root (`/`).
3. Razor uses that result to conditionally add `home-main` to `<main>` and
   `home-top-row` to the header. Other routes get neither class, so their
   original appearance is unchanged.
4. In `MainLayout.razor.css`, `main.home-main` sets `wwwroot/wall1.jpeg` as a
   centered, non-repeating background with `background-size: cover`. The
   semi-transparent black gradient is layered over the image to make white
   text easier to read.
5. `home-top-row` makes the Home header transparent so the `<main>` background
   can show through it. Its link rule makes **About** white and bold. The
   `::deep` selector reaches the link inside the header while respecting
   Blazor's CSS isolation.
6. `Home.razor.css` styles `.home-hero` as a flex container to center its
   content. Its negative margins let the section extend across the layout's
   content padding. The hero itself has no background image: the parent
   `<main>` supplies the single continuous background behind both sections.

This is a useful pattern when a page needs a visual treatment that includes
shared layout elements: put the background on a common parent, then use
route-specific classes to avoid changing other pages.

### How the navigation images are layered

The navigation has two separate images with different jobs. The sidebar
background in `MainLayout.razor.css` uses `b4vichpuj2c81.jpg`, centered and
scaled with `background-size: cover`. A semi-transparent black gradient is
listed before the image in `background-image`, so it is painted above the
photo and helps the menu text remain readable. The pink face is a regular
`<img>` in `NavMenu.razor`, so it appears in front of the CSS background.
The header removes Bootstrap's `ps-3` class, which adds left padding, and
`NavMenu.razor.css` removes the container's left padding at every screen size.
At desktop widths it also removes the right padding and sets the logo width to
100%, matching the sidebar. The smaller default logo width is retained on
mobile.

This distinction is useful when deciding whether an image is content
(an `<img>` that sits in the page layout) or decoration (a CSS background
painted behind content).

## Services and data

### `TerminalCommandStore`

[TerminalCommandStore.cs](Services/TerminalCommandStore.cs) defines the
database models and the service that persists them using LiteDB, an embedded
document database.

- `TerminalCommand` describes a saved command with an ID, category, display
  name, shell syntax, and explanation.
- `CommandHistoryEntry` stores a command attempt, time, result/error, and
  optional undo information.
- In Development, the database file is stored under the current user's local
  application data directory in a `BlazorApp1/terminal-commands.db` folder
  and file. Other environments use an in-memory database and do not retain
  command history or edits.
- When the command collection is empty, the store inserts its starter
  commands. It then imports the .NET commands from `wwwroot/commands.txt` into the
  **Development** category.
- The import runs when the store is created. It copies only lines beginning
  with a .NET CLI command, associates each with the preceding `#` description,
  and skips commands already present with the same category and syntax. The
  project file includes `wwwroot/commands.txt` as a static web asset so the
  importer can read it from the published app directory.
- History reads are sorted newest-first and limited to 50 entries.

### `CommandRunner`

[CommandRunner.cs](Services/CommandRunner.cs) starts a separate `/bin/bash`
process for each command. It sets the working directory to the app's content
root, captures standard output and standard error separately, and returns an
exit code. A command is stopped after 30 seconds, and each output stream is
limited to 32,768 characters.

**Safety note:** shell commands execute with the app process's permissions.
Command execution, command imports, editing, and history require both the
Development environment and the explicit `TerminalCommands:EnableExecution`
setting. Only local launch profiles enable that setting, and the app rejects
non-local connections whenever command execution is enabled. Other instances
show commands as read-only examples and use an in-memory store, so command
history is not shared or persisted. The Development-only runner requires
Linux. Keep public deployments in Production (the default when no environment
is specified), use HTTPS, and keep the .NET runtime and hosting system patched.

## Startup and configuration

- [Program.cs](Program.cs) creates the ASP.NET Core app, registers Razor
  Components and the two services, configures exception/status handling and
  antiforgery, maps the Blazor app and source download, and starts the server.
- [BlazorApp1.csproj](BlazorApp1.csproj) says this is a web project targeting
  .NET 10, enables nullable reference checking and implicit usings, and
  references LiteDB. It also copies Razor page source and page-specific CSS so
  the Code viewer can read them at runtime. `wwwroot/commands.txt` is included
  with the static web assets.
- [launchSettings.json](Properties/launchSettings.json) defines the local
  HTTP and HTTPS launch profiles and sets the Development environment for
  local runs. These are development launch settings, not production server
  configuration.
- [appsettings.json](appsettings.json) sets the default logging levels and
  allowed hosts. The Development settings are in
  [appsettings.Development.json](appsettings.Development.json).

## Follow a request through the app

For example, when you open the Learn page:

1. The browser sends a request to the ASP.NET Core app started in
   `Program.cs`.
2. `Routes.razor` matches `/learn` to `Learn.razor`.
3. `MainLayout.razor` provides the common frame around the page.
4. The Learn component reads the Markdown guide and renders it in the page.

For a terminal command, the same routing and layout apply, but the page also
calls `TerminalCommandStore` for persistence and `CommandRunner` for process
execution.
