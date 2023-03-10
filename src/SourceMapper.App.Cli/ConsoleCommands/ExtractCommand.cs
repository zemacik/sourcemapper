using System.Net;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

using McMaster.Extensions.CommandLineUtils;

using Newtonsoft.Json;

namespace SourceMapper.App.Cli.ConsoleCommands;

[Command(Name = COMMAND_NAME)]
public partial class ExtractCommand : MyCommandBase
{
    public const string COMMAND_NAME = "extract";

    private readonly IConsole _console;

    public ExtractCommand(IConsole console)
        : base(console)
    {
        _console = console;
    }

    [Option("-o|--output", Description = "The output directory.")]
    private string Output { get; } = null!;

    [Option("-u|--url", Description = "The URL to the source map file.")]
    private string Url { get; } = null!;

    [Option("-i|--ignore-certificate-errors", Description = "Ignore invalid TLS certificates.")]
    private bool Insecure { get; }

    [Option("-p|--proxy", Description = "Proxy URL to use.")]
    private string? Proxy { get; }

    [Option("-h|--header", Description = "A header to send with the request, similar to curl's -H. Can be set multiple times, EG: \"sourcemapper extract --header \"Cookie: session=bar\" --header \"Authorization: blerp\"")]
    private List<string> Headers { get; } = new();

    public System.ComponentModel.DataAnnotations.ValidationResult OnValidate()
    {
        if (string.IsNullOrEmpty(Url))
        {
            return new System.ComponentModel.DataAnnotations.ValidationResult("The URL is required.");
        }

        if (string.IsNullOrEmpty(Output))
        {
            return new System.ComponentModel.DataAnnotations.ValidationResult("The output directory is required.");
        }

        return System.ComponentModel.DataAnnotations.ValidationResult.Success!;
    }

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

        //{"header=", "", o => headers.Add(o)}

        Uri? proxyUrl = null;
        if (Proxy != null && !Uri.TryCreate(Proxy, UriKind.Absolute, out proxyUrl))
        {
            Console.WriteLine("Failed to parse proxy URL.");
            return;
        }

        var sm = await GetSourceMap(Url, Headers.ToArray(), Insecure, proxyUrl);

        if (sm == null)
        {
            Console.WriteLine("Failed to retrieve Sourcemap");
            return;
        }

        Console.WriteLine($"Retrieved Sourcemap with version {sm.Version}, containing {sm.Sources.Count} entries.");

        if (!sm.Sources.Any())
        {
            Console.WriteLine("No sources found.");
            return;
        }

        if (!sm.SourcesContent.Any())
        {
            Console.WriteLine("No source content found.");
            return;
        }

        if (sm.Version != 3)
        {
            Console.WriteLine("[!] Sourcemap is not version 3. This is untested!");
        }

        if (!Directory.Exists(Output))
        {
            try
            {
                Directory.CreateDirectory(Output);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to create directory: {e.Message}");
                return;
            }
        }

        for (var i = 0; i < sm.Sources.Count; i++)
        {
            if (cts.IsCancellationRequested)
                break;

            var sourcePath = "/" + sm.Sources[i];

            // path.Clean will ignore a leading '..', must be a '/..'
            sourcePath = sourcePath.Replace("..", "/..");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                sourcePath = CleanWindows(sourcePath);

            var scriptPath = Path.Join(Output, sourcePath);
            var scriptData = sm.SourcesContent[i];

            try
            {
                await WriteFileAsync(scriptPath, scriptData);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error writing {scriptPath} file: {e.Message}");
            }
        }

        if (cts.IsCancellationRequested)
            return;

        Console.WriteLine("Done");
    }

    private static async Task<SourceMap?> GetSourceMap(string source, string[] headers, bool insecureTls, Uri? proxyUrl)
    {
        Console.WriteLine($"Retrieving Sourcemap from {source}.");

        var handler = new HttpClientHandler();
        if (insecureTls)
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        if (proxyUrl != null)
        {
            handler.Proxy = new WebProxy(proxyUrl);
            handler.UseProxy = true;
        }

        using var client = new HttpClient(handler);

        var req = new HttpRequestMessage(HttpMethod.Get, source);

        if (headers is { Length: > 0 })
        {
            var headerString = string.Join("\r\n", headers) + "\r\n\r\n";
            Console.WriteLine($"Setting the following headers: \n{headerString}");

            var content = new StringContent(headerString);

            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            req.Content = content;
        }

        var res = await client.SendAsync(req);

        if (!res.IsSuccessStatusCode)
            throw new Exception($"sourceMap URL request return {res.StatusCode}");

        var body = await res.Content.ReadAsStringAsync();

        if (body == null)
            throw new Exception("The response body was null.");

        return JsonConvert.DeserializeObject<SourceMap>(body);
    }

    private static async Task WriteFileAsync(string filepath, string content)
    {
        var directory = Path.GetDirectoryName(filepath);

        if (directory != null && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        Console.WriteLine("Creating {0}.", filepath);

        await File.WriteAllTextAsync(filepath, content);
    }

    // cleanWindows replaces the illegal characters from a path with "-".
    private static string CleanWindows(string p)
    {
        var illegalChars = MyRegex();
        return illegalChars.Replace(p, "-");
    }

    [GeneratedRegex("[?%*|:\"<>]")]
    private static partial Regex MyRegex();
}

public class SourceMap
{
    public int Version { get; set; }
    public List<string> Sources { get; set; } = new();
    public List<string> SourcesContent { get; set; } = new();
}
