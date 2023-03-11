namespace SourceMapper.App.Cli.Helpers;

public class DirectoryHelpers
{
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
