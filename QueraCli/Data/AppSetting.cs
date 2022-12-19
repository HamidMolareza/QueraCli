namespace QueraCli.Data;

public static class AppSetting {
    public static readonly string ProgramDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".{nameof(QueraCli)}");

    public static readonly string DbPath = Path.Join(ProgramDirectory, $"{nameof(QueraCli)}.db");

    public static readonly List<KeyValuePair<string, string>> DefaultHttpHeaders = new() {
        new KeyValuePair<string, string>("Host", "quera.org"),
        new KeyValuePair<string, string>("User-Agent",
            "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:105.0) Gecko/20100101 Firefox/105.0"),
        new KeyValuePair<string, string>("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8"),
        new KeyValuePair<string, string>("Accept-Language", "en-US,en;q=0.5"),
        // new KeyValuePair<string, string>("Accept-Encoding", "gzip, deflate"),
        new KeyValuePair<string, string>("Dnt", "1"),
        new KeyValuePair<string, string>("Upgrade-Insecure-Requests", "1"),
        new KeyValuePair<string, string>("Sec-Fetch-Dest", "document"),
        new KeyValuePair<string, string>("Sec-Fetch-Mode", "navigate"),
        new KeyValuePair<string, string>("Sec-Fetch-Site", "none"),
        new KeyValuePair<string, string>("Sec-Fetch-User", "?1"),
        new KeyValuePair<string, string>("Te", "trailers")
    };

    public const string QueraDomain = "https://quera.org";
    public const string ProfileUrl = "profile/";
    public const string QueraSettings = "/accounts/settings/personal/";
    public const string LoginUrl = "/accounts/login";
    public const string LogoutUrl = "/accounts/logout";
    public const string ProblemsUrl = "problemset/{0}/";
    public const string SubmissionsPageUrl = "problemset/{0}/submissions/";
    public const string GetSubmissionInfoUrl = "assignment/submission_action";
}