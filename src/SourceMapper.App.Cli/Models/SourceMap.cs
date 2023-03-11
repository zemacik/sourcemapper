namespace SourceMapper.App.Cli.Models;

/// <summary>
/// The SourceMap class represents a sourcemap structure.
/// This class is used for deserializing the sourcemap from JSON.
/// </summary>
public class SourceMap
{
    public int Version { get; set; }
    public List<string> Sources { get; set; } = new();
    public List<string> SourcesContent { get; set; } = new();
}
