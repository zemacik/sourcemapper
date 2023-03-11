using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using McMaster.Extensions.CommandLineUtils;

using HtmlAgilityPack;

using SourceMapper.App.Cli.Helpers;

namespace SourceMapper.App.Cli.ConsoleCommands;

/// <summary>
/// Command to extract all source files and maps from a web page.
/// </summary>
[Command(Name = COMMAND_NAME)]
public class AllWebCommand : MyCommandBase
{
    public const string COMMAND_NAME = "all";

    private readonly IConsole _console;

    /// <summary>
    /// Creates a new instance of the <see cref="AllWebCommand"/> class.
    /// </summary>
    /// <param name="console">The console.</param>
    public AllWebCommand(IConsole console)
        : base(console)
    {
        _console = console;
    }

    /// <summary>
    /// The URL to the web page.
    /// </summary>
    [Option("-u|--url", Description = "The URL to the web page.")]
    private string Url { get; } = null!;

    /// <summary>
    /// If set, the source files will be extracted.
    /// </summary>
    [Option("-e|--extract", Description = "Extract the source maps.")]
    private bool Extract { get; }

    /// <summary>
    /// Validates the command.
    /// </summary>
    /// <returns>The validation result.</returns>
    protected override System.ComponentModel.DataAnnotations.ValidationResult OnValidate()
    {
        var baseValidate = base.OnValidate();

        if (baseValidate != System.ComponentModel.DataAnnotations.ValidationResult.Success)
            return baseValidate;

        if (string.IsNullOrWhiteSpace(Url))
        {
            return new System.ComponentModel.DataAnnotations.ValidationResult("The URL is required.");
        }

        return System.ComponentModel.DataAnnotations.ValidationResult.Success!;
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

        if (!DirectoryHelpers.CreateOutputPathIfNotExists(Output))
            return;

        var sourceMapUrls = GetSourceMaps(proxyUrl, cts.Token);

        await foreach (var (sourceMapUrl, content) in sourceMapUrls.WithCancellation(cts.Token))
        {
            if (cts.IsCancellationRequested)
                return;

            if (Extract)
            {
                Console.WriteLine("Extracting source map: " + sourceMapUrl);
                var output = Path.Combine(Output, "extract");
                await SourceMapExtractor.Extract(content, sourceMapUrl, output, CreateTopDirectory, cts.Token);
            }
        }

        if (cts.IsCancellationRequested)
            return;

        Console.WriteLine("Done");
    }

    /// <summary>
    /// Gets all source map urls from the web page from linked css and js files.
    /// </summary>
    /// <param name="proxyUrl">The proxy url.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of source map urls.</returns>
    private async IAsyncEnumerable<(string url, string content)> GetSourceMaps(Uri? proxyUrl,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var doc = await GetRemoteContent(Url, Headers.ToArray(), Insecure, proxyUrl, "text/html", cancellationToken);
        await WriteUrlContentToFile(Url, doc, cancellationToken);

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(doc);

        // gets all script tags with src attribute
        var scriptNodes = htmlDoc.DocumentNode.Descendants("script")
            .Select(n => new { link = n.Attributes["src"]?.Value, type = SourceMapType.JavaScript })
            .Where(n => n.link != null);

        // gets all link tags with rel="stylesheet" attribute
        var styleNodes = htmlDoc.DocumentNode.Descendants("link")
            .Where(n => n.Attributes["rel"]?.Value == "stylesheet")
            .Select(n => new { link = n.Attributes["href"]?.Value, type = SourceMapType.Css })
            .Where(n => n.link != null);

        var stylesAndScriptFiles = scriptNodes.Concat(styleNodes).ToList();

        foreach (var fileLinkInfo in stylesAndScriptFiles)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            // check if it is absolute or relative url and create full url from relative url if needed
            var fullUrl = fileLinkInfo.link.StartsWith("http")
                ? fileLinkInfo.link
                : new Uri(new Uri(Url), fileLinkInfo.link).ToString();

            var mimeType = fileLinkInfo.type == SourceMapType.JavaScript ? "application/javascript" : "text/css";

            var cResult =
                await TryGetRemoteContent(fullUrl, Headers.ToArray(), Insecure, proxyUrl, mimeType, cancellationToken);

            if (cResult.errorMessage != null)
            {
                Console.WriteLine($"Failed to get content from {fullUrl}: {cResult.errorMessage}");
                continue;
            }

            await WriteUrlContentToFile(fullUrl, cResult.content, cancellationToken);

            // get sourcemap url from file like sourceMappingURL=//example.com/sourcemap.js.map
            var sourceMapUrl = GetSourceMapUrl(cResult.content, fileLinkInfo.type);

            if (sourceMapUrl == null)
            {
                Console.WriteLine($"Failed to find sourcemap url in {fullUrl}");
                continue;
            }

            // check if it is absolute or relative url and create full url from relative url if needed
            var fullSourceMapUrl = sourceMapUrl.StartsWith("http")
                ? sourceMapUrl
                : new Uri(new Uri(fullUrl), sourceMapUrl).ToString();

            var sourceMapResult =
                await TryGetRemoteContent(fullSourceMapUrl, Headers.ToArray(), Insecure, proxyUrl, mimeType, cancellationToken);

            if (sourceMapResult.errorMessage != null)
            {
                Console.WriteLine($"Failed to get content from {fullSourceMapUrl}: {sourceMapResult.errorMessage}");
                continue;
            }

            await WriteUrlContentToFile(fullSourceMapUrl, sourceMapResult.content, cancellationToken);

            yield return (fullSourceMapUrl, sourceMapResult.content);
        }
    }

    /// <summary>
    /// Writes the content to a file. The file name is extracted from the url.
    /// </summary>
    /// <param name="url">The url of the content.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task WriteUrlContentToFile(string url, string content, CancellationToken cancellationToken = default)
    {
        // TODO: this is a very simple implementation and will not work for all cases
        var fileName = Path.GetFileName(url);

        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "index.html";

        fileName = SanitizeFileName(fileName);

        var ext = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(ext))
            fileName += ".html";

        var filePath = Path.Combine(Output, fileName);

        try
        {
            Console.WriteLine("Creating {0}.", filePath);
            await File.WriteAllTextAsync(filePath, content, cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to create {0}: {1}", filePath, e.Message);
            throw;
        }
    }

    /// <summary>
    /// Sanitizes the file name by replacing invalid characters with an underscore.
    /// </summary>
    /// <param name="fileName">The file name to sanitize.</param>
    /// <returns>The sanitized file name.</returns>
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", string.Join("", invalidChars));
        return Regex.Replace(fileName, invalidRegStr, "_");
    }

    /// <summary>
    /// Extracts the source map url from the content, if it exists.
    /// </summary>
    /// <param name="content">The content to search in.</param>
    /// <param name="type">The type of the content. (JavaScript or CSS)</param>
    /// <returns>The source map url or null if it does not exist.</returns>
    private string? GetSourceMapUrl(string content, SourceMapType type)
    {
        var searchString = type == SourceMapType.JavaScript
            ? "//# sourceMappingURL="
            : "/*# sourceMappingURL=";

        if (!content.Contains(searchString))
            return null;

        var index = content.IndexOf(searchString, StringComparison.Ordinal);
        var url = content.Substring(index + searchString.Length);

        if (type == SourceMapType.Css)
            url = url.Replace("*/", "");

        return url.Trim();
    }

    /// <summary>
    /// The type of the source map.
    /// </summary>
    private enum SourceMapType
    {
        JavaScript,
        Css
    }
}
