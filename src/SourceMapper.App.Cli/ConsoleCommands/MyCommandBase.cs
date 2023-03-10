using McMaster.Extensions.CommandLineUtils;

namespace SourceMapper.App.Cli.ConsoleCommands;

public abstract class MyCommandBase
{
    private readonly IConsole _console;

    public MyCommandBase(IConsole console)
    {
        _console = console;
    }

    protected void ShowHeader()
    {
        _console.WriteLine("SourceMapper.App.Cli");
        _console.WriteLine("Start with --help to see all available configuration options.");
        _console.WriteLine();
    }
}
