namespace QueraCli.Models;

public class ValidFileType {
    private string _key;

    public string Key {
        get => _key;
        set => _key = value.ToLower();
    }

    public string DisplayName { get; set; }
    public string Value { get; set; }
}