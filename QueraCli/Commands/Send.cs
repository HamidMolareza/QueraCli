using System.CommandLine;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using QueraCli.Data;
using QueraCli.Helpers;
using QueraCli.Models;

namespace QueraCli.Commands;

public static class Send {
    public static RootCommand AddSendCommand(this RootCommand rootCommand) {
        var queraIdOption = new Option<string>(
            name: "--quera-id",
            description: "Quera id");
        var solutionFileOption = new Option<string>(
            name: "--file",
            description: "Your solution file");

        var command = new Command("send", "Submit file to quera") {
            queraIdOption,
            solutionFileOption
        };

        command.SetHandler(SendHandlerAsync, queraIdOption, solutionFileOption);
        rootCommand.AddCommand(command);

        return rootCommand;
    }

    public static async Task SendHandlerAsync(string queraId, string solutionFile) {
        if (!File.Exists(solutionFile)) {
            Console.WriteLine($"File path is not valid. Can not find any file in {solutionFile}");
            return;
        }

        await using var db = new QueraContext();
        var configs = await db.Configs.FirstOrDefaultAsync();

        if (!await Login.IsLoginAsync(configs?.SessionId)) {
            Console.WriteLine("You need login first.");
            return;
        }

        var problemUrl = string.Format(AppSetting.ProblemsUrl, queraId);

        var client = HttpHelper.CreateHttpClient();
        client.AddSessionIdToHeader(configs!.SessionId!);
        client.DefaultRequestHeaders.Add("Referer", HttpHelper.CombineUrls(AppSetting.QueraDomain, problemUrl));
        client.DefaultRequestHeaders.Add("Origin", "https://quera.org");

        var problemPageData = await GetProblemPageDataAsync(client, problemUrl);
        await SubmitFileAsync(problemPageData, solutionFile, client, problemUrl);
    }

    private static async Task SubmitFileAsync(ProblemPageData problemPageData, string solutionFile,
        HttpClient client, string url) {
        var form = new MultipartFormDataContent();

        foreach (var (key, value) in problemPageData.FormDefaultValues)
            form.Add(new StringContent(value), key);
        form.Add(new StreamContent(new FileStream(solutionFile, FileMode.Open)), new FileInfo(solutionFile).Name);

        var response = await client.PostAsync(url, form);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<ProblemPageData> GetProblemPageDataAsync(HttpClient client, string url) {
        var response = await client.GetAsync(url);

        var headerCsrfToken = response.Headers.GetSetCookieFromHeader("csrf_token");
        if (headerCsrfToken is null)
            throw new Exception("Can not find csrf token in header.");

        var pageContent = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(pageContent);

        return new ProblemPageData {
            HeaderCsrfToken = headerCsrfToken,
            FormDefaultValues = HtmlHelper.GetFormDefaultValues(doc.DocumentNode, "submit-form")
        };
    }
}