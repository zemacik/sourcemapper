// Copyright (c) Michal Krchnavy. All rights reserved.
// Licensed under the MIT license.See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using SourceMapper.App.Cli.Helpers;
using SourceMapper.App.Cli.Models;

namespace SourceMapper.App.Cli;

public class SourceMapExtractor
{
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
        var illegalChars = new Regex("[?%*|:\"<>]");
        return illegalChars.Replace(p, "-");
    }
}
