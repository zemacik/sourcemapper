using System.Net;
using System.Net.Http.Headers;

using McMaster.Extensions.CommandLineUtils;

namespace SourceMapper.App.Cli.ConsoleCommands;

public abstract class MyCommandBase
{
    private readonly IConsole _console;

    [Option("-o|--output", Description = "The output directory.")]
    protected string Output { get; } = null!;

    [Option("-i|--ignore-certificate-errors", Description = "Ignore invalid TLS certificates.")]
    protected bool Insecure { get; }

    [Option("-p|--proxy", Description = "Proxy URL to use.")]
    protected string? Proxy { get; }

    [Option("-h|--header",
        Description =
            "A header to send with the request, similar to curl's -H. Can be set multiple times, EG: \"sourcemapper extract --header \"Cookie: session=bar\" --header \"Authorization: blerp\"")]
    protected List<string> Headers { get; } = new();

    [Option("-t|--create-top-directory",
        Description =
            "Create a top level directory named by the sourceMap file. This is useful when extracting multiple sourceMaps into the same directory.")]
    protected bool CreateTopDirectory { get; }

    protected MyCommandBase(IConsole console)
    {
        _console = console;
    }

    protected virtual System.ComponentModel.DataAnnotations.ValidationResult OnValidate()
    {
        if (string.IsNullOrEmpty(Output))
        {
            return new System.ComponentModel.DataAnnotations.ValidationResult("The output directory is required.");
        }

        return System.ComponentModel.DataAnnotations.ValidationResult.Success!;
    }

    protected void ShowHeader()
    {
        _console.WriteLine("SourceMapper.App.Cli");
        _console.WriteLine("Start with --help to see all available configuration options.");
        _console.WriteLine();
    }

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
