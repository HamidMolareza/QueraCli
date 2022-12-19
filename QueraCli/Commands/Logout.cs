using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using QueraCli.Data;
using QueraCli.Helpers;
using QueraCli.Models;

namespace QueraCli.Commands;

public static class Logout {
    public static RootCommand AddLogoutCommand(this RootCommand rootCommand) {
        var command = new Command("logout", "Logout from account") { };

        command.SetHandler(SendHandlerAsync);
        rootCommand.AddCommand(command);

        return rootCommand;
    }

    public static async Task SendHandlerAsync() {
        await using var db = new QueraContext();
        var configs = await db.Configs.FirstOrDefaultAsync();
        if (!await Login.IsLoginAsync(configs?.SessionId)) {
            ConsoleHelper.WriteLine("You are logout.", Colors.Success);
            return;
        }

        var client = HttpHelper.CreateHttpClient(new HttpClientHandler {
            AllowAutoRedirect = false
        });
        client.AddSessionIdToHeader(configs!.SessionId);
        client.DefaultRequestHeaders.Add("Referer", AppSetting.QueraDomain);
        client.DefaultRequestHeaders.Add("Origin", "https://quera.org");

        ConsoleHelper.WriteLine("Get the necessary information...", Colors.Info);
        var csrfTokenHeader = await GetCsrfTokenAsync(client);
        if (csrfTokenHeader is null) {
            ConsoleHelper.WriteLine($"Unexpected Error! Can not find csrf token from {AppSetting.ProfileUrl}",
                Colors.Error);
            return;
        }

        client.AddCookie(csrfTokenHeader.Key, csrfTokenHeader.Value);

        var formData = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("csrfmiddlewaretoken", csrfTokenHeader.Value),
        });

        var response = await client.PostAsync(AppSetting.LogoutUrl, formData);
        response.EnsureSuccessStatusCode();

        db.Configs.Remove(configs);
        await db.SaveChangesAsync();

        ConsoleHelper.WriteLine("The operation was successful.", Colors.Success);
    }

    private static async Task<SetCookieHeader?> GetCsrfTokenAsync(HttpClient client) {
        var response = await client.GetAsync(AppSetting.QueraSettings);
        return response.Headers.GetSetCookieFromHeader("csrf_token");
    }
}