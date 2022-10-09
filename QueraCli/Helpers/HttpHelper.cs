using System.Net.Http.Headers;
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
        client.DefaultRequestHeaders.Add("Cookie", $"session_id={sessionId}");
        return client;
    }

    public static SetCookieHeader? GetSetCookieHeader(this HttpResponseHeaders headers, string key) {
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
}