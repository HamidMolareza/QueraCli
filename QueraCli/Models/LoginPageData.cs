namespace QueraCli.Models;

public class LoginPageData {
    public SetCookieHeader HeaderCsrfToken { get; set; }
    public List<KeyValuePair<string, string>> FormDefaultValues { get; set; }
}