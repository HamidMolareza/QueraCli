using System.CommandLine;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using QueraCli.Data;
using QueraCli.Helpers;
using QueraCli.Models;

namespace QueraCli.Commands;

public static class Result {
    public static RootCommand AddResultCommand(this RootCommand rootCommand) {
        var queraIdOption = new Option<string>(
            name: "--id",
            description: "Quera id") {IsRequired = true};

        var command = new Command("result", "Displays the last result of the submitted solution.") {queraIdOption};

        command.SetHandler(ResultHandlerAsync, queraIdOption);
        rootCommand.AddCommand(command);

        return rootCommand;
    }

    public static async Task ResultHandlerAsync(string queraId) {
        await using var db = new QueraContext();
        var configs = await db.Configs.FirstOrDefaultAsync();

        if (!await Login.IsLoginAsync(configs?.SessionId)) {
            Console.WriteLine("You need login first.");
            return;
        }

        var result = await GetResultAsync(queraId, configs!.SessionId!);
        Print(result);
    }

    private static void Print(SubmissionResult? result) {
        if (result is null) return;
        Console.WriteLine("Last submit file:");

        Console.Write("Score: ");
        ConsoleHelper.WriteLine(result.Score, result.Score is "100" or "۱۰۰" ? ConsoleColor.Green : ConsoleColor.Red);

        Console.WriteLine($"File Type: {result.FileType}");
        Console.WriteLine($"Date Time: {result.DateTime}");

        Console.WriteLine($"Detail:");
        var defaultColor = Console.BackgroundColor;
        foreach (var line in result.Detail.Split("\n")) {
            ConsoleColor color;
            switch (line) {
                case "ACCEPTED":
                    color = ConsoleColor.Green;
                    break;
                case "WRONG ANSWER":
                case "WRONG":
                    color = ConsoleColor.Red;
                    break;
                case "Time Limit Exceeded":
                    color = ConsoleColor.DarkYellow;
                    break;
                default:
                    color = defaultColor;
                    break;
            }

            ConsoleHelper.WriteLine(line, color);
        }
    }

    private static async Task<SubmissionResult?> GetResultAsync(string queraId, string sessionId) {
        var problemUrl = string.Format(AppSetting.SubmissionsPageUrl, queraId);

        var client = HttpHelper.CreateHttpClient(new HttpClientHandler {
            AllowAutoRedirect = false
        });
        client.AddSessionIdToHeader(sessionId);
        client.DefaultRequestHeaders.Add("Referer", HttpHelper.CombineUrls(AppSetting.QueraDomain, problemUrl));
        client.DefaultRequestHeaders.Add("Origin", "https://quera.org");

        var pageContent = await client.GetStringAsync(problemUrl);
        var doc = new HtmlDocument();
        doc.LoadHtml(pageContent);

        var tableNode = doc.DocumentNode.Descendants("table")
            .SingleOrDefault(table =>
                table.SelectSingleNode("thead/tr").ChildNodes.Count(child => child.Name == "th") == 4);
        if (tableNode is null) {
            Console.WriteLine("You have not sent a solution yet.");
            return null;
        }

        var lastSubmission = tableNode.Descendants("tbody").Single()
            .Descendants("tr").First();

        var dataSubmissionId = lastSubmission.Attributes["data-submission_id"].Value;
        var detail = await GetDetailSubmissionAsync(client, dataSubmissionId, queraId);

        var items = lastSubmission.Descendants("td").ToList();
        var score = items[2].InnerText.Trim();
        return new SubmissionResult {
            DateTime = items[0].InnerText.Trim(),
            FileType = items[1].InnerText.Trim(),
            Score = score == "خطای کامپایل" ? "Compile Error" : score,
            Detail = detail.Trim()
        };
    }

    private static async Task<string> GetDetailSubmissionAsync(HttpClient client, string submissionId, string queraId) {
        var problemUrl = string.Format(AppSetting.ProblemsUrl, queraId);
        var problemPageResponse = await client.GetAsync(problemUrl);
        problemPageResponse.EnsureSuccessStatusCode();
        var headerCsrfToken = problemPageResponse.Headers.GetSetCookieFromHeader("csrf_token");
        if (headerCsrfToken is null)
            throw new Exception($"Can not find csrf token in {problemUrl}");

        client.AddCookie(headerCsrfToken.Key, headerCsrfToken.Value);
        client.DefaultRequestHeaders.Add("X-Csrftoken", headerCsrfToken.Value);
        var formData = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("action", "get_result"),
            new KeyValuePair<string, string>("submission_id", submissionId)
        });
        var response = await client.PostAsync(AppSetting.GetSubmissionInfoUrl, formData);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadAsStringAsync();
        var jObject = JObject.Parse(data);

        var htmlResult = jObject["result"]?.Value<string>();
        if (string.IsNullOrEmpty(htmlResult))
            throw new Exception("Can not get submission detail. result property not found in json.");

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlResult);
        return doc.DocumentNode.InnerText;
    }
}