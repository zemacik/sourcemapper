using McMaster.Extensions.CommandLineUtils;

namespace SourceMapper.App.Cli.ConsoleCommands;

[Command(Name = COMMAND_NAME)]
public class ExtractCommand : MyCommandBase
{
    public const string COMMAND_NAME = "extract";

    private readonly IConsole _console;

    public ExtractCommand(IConsole console)
        : base(console)
    {
        _console = console;
    }

    [Option("-u|--url", Description = "The URL to the source map file.")]
    private string Url { get; } = "en";

    public System.ComponentModel.DataAnnotations.ValidationResult OnValidate()
    {
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



    }
}
