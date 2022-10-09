using System.CommandLine;

namespace QueraCli.Commands;

public static class Send {
    public static RootCommand AddSendCommand(this RootCommand rootCommand) {
        var queraIdOption = new Option<string>(
            name: "--quera-id",
            description: "Quera id");
        var solutionFileOption = new Option<string>(
            name: "--file",
            description: "Your solution file");

        var readCommand = new Command("send", "Submit file to quera") {
            queraIdOption,
            solutionFileOption
        };

        rootCommand.AddCommand(readCommand);
        rootCommand.SetHandler(SendHandler, queraIdOption, solutionFileOption);

        return rootCommand;
    }

    public static Task SendHandler(string queraId, string solutionFile) {
        throw new NotImplementedException();
    }
}