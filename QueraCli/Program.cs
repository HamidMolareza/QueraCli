using System.CommandLine;
using QueraCli.Commands;

namespace QueraCli;

public static class Program {
    public static async Task<int> Main(string[] args) {
        var rootCommand = new RootCommand("Quera Command Line");
        rootCommand.AddLoginCommand();

        return await rootCommand.InvokeAsync(args);
    }
}