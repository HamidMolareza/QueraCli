namespace QueraCli.Helpers;

public static class UriHelper {
    public static async Task<string> DownloadTextAsync(string uri) {
        using var client = new HttpClient();
        return await client.GetStringAsync(uri);
    }

    public static string GetUriFileName(string url) {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            uri = new Uri(url);

        return Path.GetFileName(uri.LocalPath);
    }
}