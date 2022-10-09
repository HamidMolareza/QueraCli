namespace QueraCli.Models;

public class ProblemPageData {
    public SetCookieHeader HeaderCsrfToken { get; set; }
    public List<KeyValuePair<string, string>> FormDefaultValues { get; set; }
}