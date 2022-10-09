using System.CommandLine;
using System.Net;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using QueraCli.Data;
using QueraCli.Helpers;
using QueraCli.Models;

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

        command.SetHandler(LoginHandlerAsync, usernameOption, passwordOption);
        rootCommand.Add(command);

        return rootCommand;
    }

    public static async Task LoginHandlerAsync(string username, string password) {
        await using var db = new QueraContext();
        var config = await db.Configs.FirstOrDefaultAsync();

        if (await IsLoginAsync(config?.SessionId)) {
            Console.WriteLine("You are login. :)");
            return;
        }

        if (config is not null) {
            //Last session id has expired.
            config.SessionId = null;
            await db.SaveChangesAsync();
        }

        var sessionId = await TryLoginAsync(username, password);
        if (sessionId is null) {
            Console.WriteLine("Username or password is not valid.");
            return;
        }

        Console.WriteLine("Login successful. :)");
        if (config is null) {
            config = new ConfigDb {SessionId = sessionId};
            db.Configs.Add(config);
        }
        else {
            config.SessionId = sessionId;
        }

        await db.SaveChangesAsync();
    }

    private static async Task<string?> TryLoginAsync(string username, string password) {
        //Get data from login page

        var client = HttpHelper.CreateHttpClient(new HttpClientHandler {AllowAutoRedirect = false});
        var getResponse = await client.GetAsync(AppSetting.QueraLogin);

        var headerCsrfToken = getResponse.Headers.GetSetCookieHeader("csrf_token");
        if (headerCsrfToken is null)
            throw new Exception("Can not find csrf token in header.");

        var loginPage = await getResponse.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(loginPage);

        //Prepare data for post request:

        var formValues = HtmlHelper.GetFormDefaultValues(doc.DocumentNode);
        formValues.Add(new KeyValuePair<string, string>("login", username));
        formValues.Add(new KeyValuePair<string, string>("password", password));

        var form = new FormUrlEncodedContent(formValues);

        client.DefaultRequestHeaders.Add("Cookie", headerCsrfToken.Header);
        client.DefaultRequestHeaders.Add("Referer", "https://quera.org");

        var postResponse = await client.PostAsync(AppSetting.QueraLogin, form);
        if (!postResponse.IsSuccessStatusCode && postResponse.StatusCode != HttpStatusCode.Found)
            throw new Exception($"Response status code does not indicate success: {postResponse.StatusCode}");

        var sessionId = postResponse.Headers.GetSetCookieHeader("session_id")?.Value;

        return sessionId;
    }

    private static async Task<bool> IsLoginAsync(string? sessionId) {
        if (sessionId is null)
            return false;

        var client = HttpHelper.CreateHttpClient(new HttpClientHandler {
            AllowAutoRedirect = false
        });
        client.AddSessionIdToHeader(sessionId);

        var response = await client.GetAsync(AppSetting.QueraProfile);
        switch (response.StatusCode) {
            case HttpStatusCode.OK:
                return true;
            case HttpStatusCode.Found:
                var redirectToLoginPage = response.Headers
                    .SingleOrDefault(header => header.Key.ToLower() == "location")
                    .Value.FirstOrDefault()?
                    .Contains("login");
                return redirectToLoginPage is null or false;
            default:
                return true;
        }
    }
}