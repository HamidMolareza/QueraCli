using HtmlAgilityPack;

namespace QueraCli.Helpers;

public static class HtmlHelper {
    public static IQueryable<HtmlNode> GetFormElement(HtmlNode htmlNode, string? formId = null) {
        var forms = htmlNode.Descendants("form").AsQueryable();
        if (formId is not null)
            forms = forms.Where(form => form.Attributes["id"] != null && form.Attributes["id"].Value == formId);
        return forms;
    }

    public static List<KeyValuePair<string, string>> GetFormDefaultValues(HtmlNode htmlNode, string? formId = null) {
        var forms = GetFormElement(htmlNode, formId);

        return GetFormDefaultValues(forms);
    }

    public static List<KeyValuePair<string, string>> GetFormDefaultValues(IQueryable<HtmlNode> forms) =>
        forms.Single()
            .Descendants("input")
            .Where(input => input.Attributes["name"] is not null && input.Attributes["value"] is not null)
            .Select(input =>
                new KeyValuePair<string, string>(input.Attributes["name"].Value, input.Attributes["value"].Value))
            .ToList();
}