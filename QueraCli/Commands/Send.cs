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
            }) {IsRequired = false};

        var solutionUrlOption = new Option<string>(
            name: "--uri",
            description: "Your solution uri");

        var fileTypeOption = new Option<string>(
            name: "--type",
            description: "Your solution file type");

        var command = new Command("send", "Submit file to quera") {
            queraIdOption,
            solutionFileOption,
            solutionUrlOption,
            fileTypeOption
        };

        command.SetHandler(SendHandlerAsync, queraIdOption, solutionFileOption, solutionUrlOption, fileTypeOption);
        rootCommand.AddCommand(command);

        return rootCommand;
    }

    public static async Task SendHandlerAsync(string queraId, string? solutionFile, string? solutionUri,
        string? fileType) {
        var solutionParameters = Utility.CountNullOrEmpty(solutionFile, solutionUri);
        switch (solutionParameters) {
            case 0:
                ConsoleHelper.WriteLine("Enter only one of the parameters file or uri.", Colors.Error);
                return;
            case 2:
                ConsoleHelper.WriteLine("Enter at least one of the parameters file or uri.", Colors.Error);
                return;
        }

        await using var db = new QueraContext();
        var configs = await db.Configs.FirstOrDefaultAsync();
        if (!await Login.IsLoginAsync(configs?.SessionId)) {
            ConsoleHelper.WriteLine("You need login first.", Colors.Error);
            return;
        }

        HttpContent content;
        string solutionFileName;
        if (!string.IsNullOrEmpty(solutionFile)) {
            content = new StreamContent(new FileStream(solutionFile, FileMode.Open));
            solutionFileName = new FileInfo(solutionFile).Name;
        }
        else {
            content = new StringContent(await UriHelper.DownloadTextAsync(solutionUri!));
            solutionFileName = UriHelper.GetUriFileName(solutionUri!);
        }

        if (string.IsNullOrEmpty(solutionFileName)) {
            solutionFileName = "file";
            ConsoleHelper.WriteLine($"Can not detect file name. using default file name ({solutionFileName})",
                Colors.Warning);
        }

        var problemUrl = string.Format(AppSetting.ProblemsUrl, queraId);

        var client = HttpHelper.CreateHttpClient();
        client.AddSessionIdToHeader(configs!.SessionId);
        client.DefaultRequestHeaders.Add("Referer", HttpHelper.CombineUrls(AppSetting.QueraDomain, problemUrl));
        client.DefaultRequestHeaders.Add("Origin", "https://quera.org");

        ConsoleHelper.WriteLine("Get the necessary information to send the file...", Colors.Info);
        var problemPageData = await GetProblemPageDataAsync(client, problemUrl);

        if (string.IsNullOrEmpty(fileType)) {
            fileType = TryDetectFileType(solutionFileName);
        }

        var fileTypeCode = GetFileTypeCode(fileType, problemPageData.ValidFileTypes);
        if (string.IsNullOrEmpty(fileTypeCode)) {
            ConsoleHelper.WriteLine("Error! Your file type is not valid.", Colors.Error);
            Console.WriteLine();

            Console.WriteLine("Valid file types:");
            foreach (var validFileType in problemPageData.ValidFileTypes)
                Console.WriteLine(validFileType.DisplayName);

            ConsoleHelper.WriteLine("Use --help for more info.", Colors.Info);
            return;
        }

        Console.WriteLine("Submitting...");
        await SubmitFileAsync(problemPageData, content, solutionFileName, client, problemUrl, fileTypeCode);

        Console.WriteLine("The operation was successful.");
    }

    private static string? TryDetectFileType(string solutionFileName) {
        var fileExtension = new FileInfo(solutionFileName.ToLower()).Extension;
        if (fileExtension.StartsWith("."))
            fileExtension = fileExtension.Remove(0, 1);

        return fileExtension switch {
            "c" => "c",
            "cpp" => "c++",
            "py" => "python 3.8",
            "cs" => "mono c#",
            "php" => "php8",
            "pl" => "perl",
            "go" => "go",
            "rb" => "ruby",
            "rs" => "rust",
            "m" => "obj-c",
            "swift" => "swift",
            "hs" => "haskell",
            _ => null
        };
    }

    private static string? GetFileTypeCode(string? fileType, List<ValidFileType> validFileTypes) {
        if (string.IsNullOrEmpty(fileType))
            return null;
        fileType = fileType.ToLower();
        return validFileTypes.Find(validType => validType.Key == fileType)?.Value;
    }

    private static async Task SubmitFileAsync(ProblemPageData problemPageData, HttpContent solutionContent,
        string solutionName, HttpClient client, string url, string fileTypeCode) {
        client.AddCookie(problemPageData.HeaderCsrfToken.Key, problemPageData.HeaderCsrfToken.Value);

        var form = new MultipartFormDataContent();
        foreach (var (key, value) in problemPageData.FormDefaultValues)
            form.Add(new StringContent(value), key);
        form.Add(solutionContent, "file", solutionName);
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