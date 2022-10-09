using System.Net.Http.Headers;
using System.Text;
using QueraCli.Data;
using QueraCli.Models;

namespace QueraCli.Helpers;

public static class HttpHelper {
    public static HttpClient CreateHttpClient(HttpMessageHandler messageHandler,
        string baseUrl = AppSetting.QueraDomain) {
        var client = new HttpClient(messageHandler).AddDefaultHeaders();
        client.BaseAddress = new Uri(baseUrl);
        return client;
    }

    public static HttpClient CreateHttpClient(string baseUrl = AppSetting.QueraDomain) {
        var client = new HttpClient().AddDefaultHeaders();
        client.BaseAddress = new Uri(baseUrl);
        return client;
    }

    public static HttpClient AddDefaultHeaders(this HttpClient client) {
        foreach (var (key, value) in AppSetting.DefaultHttpHeaders)
            client.DefaultRequestHeaders.Add(key, value);

        return client;
    }

    public static HttpClient AddSessionIdToHeader(this HttpClient client, string sessionId) {
        client.AddCookie("session_id", sessionId);
        return client;
    }

    public static HttpClient AddCookie(this HttpClient client, string key, string value) {
        var lastCookies = (client.DefaultRequestHeaders
                .DefaultIfEmpty(new KeyValuePair<string, IEnumerable<string>>("Cookie", new List<string>()))
                .SingleOrDefault(header => header.Key == "Cookie")
                .Value ?? new List<string>())
            .ToList();

        var cookies = new List<string> {$"{key}={value}"};
        if (lastCookies.Any())
            cookies.AddRange(lastCookies);

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies));

        return client;
    }

    public static SetCookieHeader? GetSetCookieFromHeader(this HttpResponseHeaders headers, string key) {
        var setCookieHeader = headers
            .SingleOrDefault(header => header.Key == "Set-Cookie");
        if (string.IsNullOrEmpty(setCookieHeader.Key))
            return null;

        var header = setCookieHeader.Value
            .SingleOrDefault(str => str.StartsWith($"{key}="))
            ?.Split(";").FirstOrDefault();
        if (header is null)
            return null;

        var split = header.Split("=");
        return new SetCookieHeader {
            Header = header,
            Key = split[0],
            Value = split[1]
        };
    }

    public static string CombineUrls(string url1, string url2) {
        var result = new StringBuilder();
        result.Append(url1);
        if (result[^1] != '/' && url2[0] != '/')
            result.Append('/');
        result.Append(url2);
        return result.ToString();
    }

    public static async Task PrintRequestAsync(HttpRequestMessage? requestMessage) {
        if (requestMessage is null) return;

        Console.WriteLine($"Request Uri: {requestMessage.RequestUri}");
        Console.WriteLine($"Method: {requestMessage.Method}");
        Console.WriteLine($"Headers:\n{requestMessage.Headers}");
        var content = await requestMessage.Content?.ReadAsStringAsync();
        Console.WriteLine($"Content:\n{content}");
    }
}