using System.CommandLine;

namespace QueraCli.Commands;

public static class Login {
    public static RootCommand AddLoginCommand(this RootCommand rootCommand) {
        var usernameOption = new Option<string>(
            name: "--username",
            description: "Your Quera username");
        var passwordOption = new Option<string>(
            name: "--password",
            description: "Your Quera password");

        var command = new Command("login", "Login to Quera") {
            usernameOption,
            passwordOption
        };

        command.SetHandler(LoginHandler, usernameOption, passwordOption);
        rootCommand.Add(command);

        return rootCommand;
    }

    public static void LoginHandler(string username, string password) {
        var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Console.WriteLine(userDir);
    }
}