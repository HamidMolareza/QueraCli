using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using QueraCli.Commands;
using QueraCli.Data;

namespace QueraCli;

public static class Program {
    public static async Task<int> Main(string[] args) {
        await using (var db = new QueraContext()) {
            await db.Database.EnsureCreatedAsync();
            await db.Database.MigrateAsync();
        }

        var rootCommand = new RootCommand("Quera Command Line");
        rootCommand.AddLoginCommand()
            .AddSendCommand()
            .AddResultCommand();

        return await rootCommand.InvokeAsync(args);
    }
}