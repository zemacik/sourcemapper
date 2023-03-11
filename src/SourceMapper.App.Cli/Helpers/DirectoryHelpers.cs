namespace SourceMapper.App.Cli.Helpers;

/// <summary>
/// DirectoryHelpers contains helper methods for working with directories.
/// </summary>
public class DirectoryHelpers
{
    /// <summary>
    /// Creates the output path if it does not exist.
    /// </summary>
    /// <param name="path">The path to create.</param>
    /// <returns>True if the path was created, false otherwise.</returns>
    public static bool CreateOutputPathIfNotExists(string path)
    {
        if (Directory.Exists(path))
            return true;

        try
        {
            Directory.CreateDirectory(path);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to create directory: {e.Message}");
            return false;
        }

        return true;
    }
}
