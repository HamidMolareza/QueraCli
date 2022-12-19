namespace QueraCli.Helpers;

public static class Utility {
    public static int CountNullOrEmpty(params string?[] strings) =>
        strings.Count(string.IsNullOrEmpty);
}