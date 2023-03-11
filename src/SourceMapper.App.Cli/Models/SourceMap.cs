namespace SourceMapper.App.Cli.Models;

public class SourceMap
{
    public int Version { get; set; }
    public List<string> Sources { get; set; } = new();
    public List<string> SourcesContent { get; set; } = new();
}
