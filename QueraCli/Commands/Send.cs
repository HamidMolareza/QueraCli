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
            name: "--id",
            description: "Quera id") {IsRequired = true};
        var solutionFileOption = new Option<string>(
            name: "--file",
            description: "Your solution file",
            parseArgument: argumentResult => {
                var filePath = argumentResult.Tokens.Single().Value;
                if (File.Exists(filePath))
                    return filePath;
                argumentResult.ErrorMessage = $"File path is not valid. Can not find any file in {filePath}";
                return null!;
            }) {IsRequired = true};
        var fileTypeOption = new Option<string>(
            name: "--type",
            description: "Your solution file type");

        var command = new Command("send", "Submit file to quera") {
            queraIdOption,
            solutionFileOption,
            fileTypeOption
        };

        command.SetHandler(SendHandlerAsync, queraIdOption, solutionFileOption, fileTypeOption);
        rootCommand.AddCommand(command);

        return rootCommand;
    }

    public static async Task SendHandlerAsync(string queraId, string solutionFile, string? fileType) {
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

        Console.WriteLine("Get the necessary information to send the file...");
        var problemPageData = await GetProblemPageDataAsync(client, problemUrl);

        var fileTypeCode = GetFileTypeCode(fileType, problemPageData.ValidFileTypes);
        if (string.IsNullOrEmpty(fileTypeCode)) {
            ConsoleHelper.WriteLine("Error! Your file type is not valid.", ConsoleColor.Red);
            Console.WriteLine();

            Console.WriteLine("Valid file types:");
            foreach (var validFileType in problemPageData.ValidFileTypes)
                Console.WriteLine(validFileType.DisplayName);

            ConsoleHelper.WriteLine("Use --help for more info.", ConsoleColor.Green);
            return;
        }

        Console.WriteLine("Submitting...");
        await SubmitFileAsync(problemPageData, solutionFile, client, problemUrl, fileTypeCode!);

        Console.WriteLine("The operation was successful.");
    }

    private static string? GetFileTypeCode(string? fileType, List<ValidFileType> validFileTypes) {
        if (string.IsNullOrEmpty(fileType))
            return null;
        fileType = fileType.ToLower();
        return validFileTypes.Find(validType => validType.Key == fileType)?.Value;
    }

    private static async Task SubmitFileAsync(ProblemPageData problemPageData, string solutionFile,
        HttpClient client, string url, string fileTypeCode) {
        client.AddCookie(problemPageData.HeaderCsrfToken.Key, problemPageData.HeaderCsrfToken.Value);

        var form = new MultipartFormDataContent();
        foreach (var (key, value) in problemPageData.FormDefaultValues)
            form.Add(new StringContent(value), key);
        form.Add(new StreamContent(new FileStream(solutionFile, FileMode.Open)), "file",
            new FileInfo(solutionFile).Name);
        form.Add(new StringContent(fileTypeCode), "file_type");

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

        var form = HtmlHelper.GetFormElement(doc.DocumentNode, "submit-form");

        var validFileTypes = form.Single().Descendants("select")
            .Single(node => node.Attributes["id"] is not null && node.Attributes["id"].Value == "id_file_type")
            .Descendants("option")
            .Select(option => {
                var key = option.InnerText.Trim();
                return new ValidFileType {
                    Key = key,
                    DisplayName = key,
                    Value = option.Attributes["value"].Value
                };
            }).ToList();

        return new ProblemPageData {
            HeaderCsrfToken = headerCsrfToken,
            FormDefaultValues = HtmlHelper.GetFormDefaultValues(form),
            ValidFileTypes = validFileTypes
        };
    }
}