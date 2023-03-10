using McMaster.Extensions.CommandLineUtils;

using SourceMapper.App.Cli.ConsoleCommands;

[HelpOption("-?| -h | --help")]
[Command(Name = "sourcemapper"),
 Subcommand(typeof(ExtractCommand))]
internal class Program
{
    private const string DEFAULT_COMMAND = ExtractCommand.COMMAND_NAME;

    public static async Task Main(string[] args)
        => await CommandLineApplication.ExecuteAsync<Program>(args);

    private Task<int> OnExecute(CommandLineApplication app)
    {
        var defaultCommand = app.Commands.First(x => x.Name == DEFAULT_COMMAND);

        return defaultCommand.ExecuteAsync(app.RemainingArguments.ToArray());
    }
}
