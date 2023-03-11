using System.Net;
using System.Net.Http.Headers;

using McMaster.Extensions.CommandLineUtils;

namespace SourceMapper.App.Cli.ConsoleCommands;

/// <summary>
/// The base class for all commands.
/// </summary>
public abstract class MyCommandBase
{
    private readonly IConsole _console;

    /// <summary>
    /// The output directory.
    /// </summary>
    [Option("-o|--output", Description = "The output directory.")]
    protected string Output { get; } = null!;

    /// <summary>
    /// If set, invalid TLS certificates will be ignored.
    /// </summary>
    [Option("-i|--ignore-certificate-errors", Description = "Ignore invalid TLS certificates.")]
    protected bool Insecure { get; }

    /// <summary>
    /// The proxy URL to use.
    /// </summary>
    [Option("-p|--proxy", Description = "Proxy URL to use.")]
    protected string? Proxy { get; }

    /// <summary>
    /// Allows setting a header to send with the request, similar to curl's -H.
    /// </summary>
    [Option("-h|--header",
        Description =
            "A header to send with the request, similar to curl's -H. Can be set multiple times, EG: \"sourcemapper extract --header \"Cookie: session=bar\" --header \"Authorization: blerp\"")]
    protected List<string> Headers { get; } = new();

    /// <summary>
    /// Create a top level directory named by the sourceMap file. This is useful when extracting multiple sourceMaps into the same directory.
    /// </summary>
    [Option("-t|--create-top-directory",
        Description =
            "Create a top level directory named by the sourceMap file. This is useful when extracting multiple sourceMaps into the same directory.")]
    protected bool CreateTopDirectory { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="MyCommandBase"/> class.
    /// </summary>
    /// <param name="console">The console.</param>
    protected MyCommandBase(IConsole console)
    {
        _console = console;
    }

    /// <summary>
    /// Validates the command.
    /// </summary>
    /// <returns>Vaidation result.</returns>
    protected virtual System.ComponentModel.DataAnnotations.ValidationResult OnValidate()
    {
        if (string.IsNullOrEmpty(Output))
        {
            return new System.ComponentModel.DataAnnotations.ValidationResult("The output directory is required.");
        }

        return System.ComponentModel.DataAnnotations.ValidationResult.Success!;
    }

    /// <summary>
    /// Writes header text to the console.
    /// </summary>
    protected void ShowHeader()
    {
        _console.WriteLine("SourceMapper.App.Cli");
        _console.WriteLine("Start with --help to see all available configuration options.");
        _console.WriteLine();
    }


    /// <summary>
    /// Method tries to get remote content and returns the content or error message.
    /// </summary>
    /// <param name="source">The source URL.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="insecureTls">If set to <c>true</c> [insecure TLS].</param>
    /// <param name="proxyUri">The proxy URI.</param>
    /// <param name="mediaType">The media type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The content or error message.</returns>
    protected async Task<(string? content, string? errorMessage)> TryGetRemoteContent(
        string source,
        string[] headers,
        bool insecureTls,
        Uri? proxyUri,
        string mediaType,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await GetRemoteContent(source, headers, insecureTls, proxyUri, mediaType, cancellationToken);
            return (content, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Gets the remote content from the specified url.
    /// </summary>
    /// <param name="source">The source URL.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="insecureTls">If set to <c>true</c> [insecure TLS].</param>
    /// <param name="proxyUri">The proxy URI.</param>
    /// <param name="mediaType">The media type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The content or error message.</returns>
    /// <returns>The content.</returns>
    protected async Task<string> GetRemoteContent(string source,
        string[] headers,
        bool insecureTls,
        Uri? proxyUri,
        string mediaType = "application/json",
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Retrieving contnet from {source}.");

        var handler = new HttpClientHandler();
        if (insecureTls)
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        if (proxyUri != null)
        {
            handler.Proxy = new WebProxy(proxyUri);
            handler.UseProxy = true;
        }

        using var client = new HttpClient(handler);

        var req = new HttpRequestMessage(HttpMethod.Get, source);

        if (headers is { Length: > 0 })
        {
            var headerString = string.Join("\r\n", headers) + "\r\n\r\n";
            Console.WriteLine($"Setting the following headers: \n{headerString}");

            var content = new StringContent(headerString);

            content.Headers.ContentType = new MediaTypeHeaderValue(mediaType ?? "text/plain");
            req.Content = content;
        }

        var res = await client.SendAsync(req, cancellationToken);

        if (!res.IsSuccessStatusCode)
            throw new Exception($"sourceMap URL request return {res.StatusCode}");

        var body = await res.Content.ReadAsStringAsync(cancellationToken);

        if (body == null)
            throw new Exception("The response body was null.");

        return body;
    }
}
