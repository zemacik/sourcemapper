using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using SourceMapper.App.Cli.Helpers;
using SourceMapper.App.Cli.Models;

namespace SourceMapper.App.Cli;

/// <summary>
/// The SourceMapExtractor class is responsible for extracting the source files from a sourcemap.
/// </summary>
public class SourceMapExtractor
{
    /// <summary>
    /// Extracts the source files from a sourcemap.
    /// </summary>
    /// <param name="doc">The sourcemap document.</param>
    /// <param name="url">The URL of the sourcemap.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="createTopDirectory">Whether to create a top directory for the extracted files (like the filename of the sourcemap).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the extraction was successful, false otherwise.</returns>
    public static async Task<bool> Extract(
        string doc,
        string url,
        string outputPath,
        bool createTopDirectory,
        CancellationToken cancellationToken)
    {
        var sm = JsonConvert.DeserializeObject<SourceMap>(doc);

        if (sm == null)
        {
            Console.WriteLine("Failed to retrieve Sourcemap");
            return true;
        }

        Console.WriteLine($"Retrieved Sourcemap with version {sm.Version}, containing {sm.Sources.Count} entries.");

        if (!sm.Sources.Any())
        {
            Console.WriteLine("No sources found.");
            return true;
        }

        if (!sm.SourcesContent.Any())
        {
            Console.WriteLine("No source content found.");
            return true;
        }

        if (sm.Version != 3)
        {
            Console.WriteLine("[!] Sourcemap is not version 3. This is untested!");
        }

        if (!DirectoryHelpers.CreateOutputPathIfNotExists(outputPath))
            return true;

        var baseDir = outputPath;
        if (createTopDirectory)
        {
            var filename = Path.GetFileNameWithoutExtension(url);

            if (string.IsNullOrWhiteSpace(filename))
            {
                filename = $"sourcemap_{DateTime.Now:yyyyMMdd_HHmmss}";
                Console.WriteLine($"Failed to parse filename from URL, creating temporary directory {filename}.");
            }

            baseDir = Path.Join(baseDir, filename);
        }

        for (var i = 0; i < sm.Sources.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var sourcePath = "/" + sm.Sources[i];

            sourcePath = sourcePath.Replace("/..", "");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                sourcePath = CleanWindows(sourcePath);

            var scriptPath = Path.Join(baseDir, sourcePath);
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

        return false;
    }

    /// <summary>
    /// Writes a file asynchronously.
    /// </summary>
    /// <param name="filepath">The path to the file.</param>
    /// <param name="content">The content to write.</param>
    private static async Task WriteFileAsync(string filepath, string content)
    {
        var directory = Path.GetDirectoryName(filepath);

        if (directory != null && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        Console.WriteLine("Creating {0}.", filepath);

        await File.WriteAllTextAsync(filepath, content);
    }

    /// <summary>
    /// Sanitizes a path for Windows.
    /// </summary>
    /// <param name="path">The path to sanitize.</param>
    /// <returns>The sanitized path.</returns>
    private static string CleanWindows(string path)
    {
        var illegalChars = new Regex("[?%*|:\"<>]");
        return illegalChars.Replace(path, "-");
    }
}
