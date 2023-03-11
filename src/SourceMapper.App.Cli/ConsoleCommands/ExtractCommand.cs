using McMaster.Extensions.CommandLineUtils;

namespace SourceMapper.App.Cli.ConsoleCommands;

/// <summary>
/// Command to extract maps from a given URLs.
/// </summary>
[Command(Name = COMMAND_NAME)]
public class ExtractCommand : MyCommandBase
{
    public const string COMMAND_NAME = "extract";

    private readonly IConsole _console;

    /// <summary>
    /// Creates a new instance of the <see cref="ExtractCommand"/> class.
    /// </summary>
    /// <param name="console">The console.</param>
    public ExtractCommand(IConsole console)
        : base(console)
    {
        _console = console;
    }

    /// <summary>
    /// The URLs to the source map files.
    /// </summary>
    [Option("-mu|--map-url",
        Description =
            "The URL to the source map file. Can be set multiple times, EG: \"sourcemapper extract --map-url https://example.com/sourcemap1.js.map --map-url https://example.com/sourcemap2.css.map")]
    private List<string> Urls { get; } = new();

    /// <summary>
    /// Validates the command.
    /// </summary>
    /// <returns>The validation result.</returns>
    protected override System.ComponentModel.DataAnnotations.ValidationResult OnValidate()
    {
        var baseValidate = base.OnValidate();

        if (baseValidate != System.ComponentModel.DataAnnotations.ValidationResult.Success)
            return baseValidate;

        if (Urls.Count == 0)
            return new System.ComponentModel.DataAnnotations.ValidationResult("The URL is required.");

        return System.ComponentModel.DataAnnotations.ValidationResult.Success;
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    protected async Task OnExecute()
    {
        ShowHeader();

        using var cts = new CancellationTokenSource();

        _console.CancelKeyPress += (_, e) =>
        {
            _console.WriteLine("Canceling...");
            e.Cancel = true;
            cts.Cancel();
        };

        Uri? proxyUrl = null;
        if (Proxy != null && !Uri.TryCreate(Proxy, UriKind.Absolute, out proxyUrl))
        {
            Console.WriteLine("Failed to parse proxy URL.");
            return;
        }

        foreach (var url in Urls)
        {
            Console.WriteLine("Processing url: " + url);
            Console.WriteLine("--------------------");

            if (cts.IsCancellationRequested)
                return;

            var doc = await GetRemoteContent(url, Headers.ToArray(), Insecure, proxyUrl, "application/json", cts.Token);
            await SourceMapExtractor.Extract(doc, url, Output, CreateTopDirectory, cts.Token);
        }

        if (cts.IsCancellationRequested)
            return;

        Console.WriteLine("Done");
    }
}
