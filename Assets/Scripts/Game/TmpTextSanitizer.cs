using TMPro;

/// <summary>Normalize strings for LiberationSans SDF and other default TMP fonts.</summary>
public static class TmpTextSanitizer
{
    public static string Sanitize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return TransliterateUmlauts(text)
            .Replace("&amp;", "&")
            .Replace('\u25C0', '<')
            .Replace('\u25B6', '>')
            .Replace('\u25B2', '^')
            .Replace('\u25BC', 'v')
            .Replace('\u2713', '*')
            .Replace('\u2714', '*')
            .Replace('\u2717', 'x')
            .Replace('\u2718', 'x')
            .Replace('\u2022', '*')
            .Replace("\u2026", "...")
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('\u2212', '-')
            .Replace('\u00B7', '|')
            .Replace('\u00D7', 'x')
            .Replace("\u2192", "->")
            .Replace("\u2190", "<-")
            .Replace('\u2018', '\'')
            .Replace('\u2019', '\'')
            .Replace('\u201C', '"')
            .Replace('\u201D', '"')
            .Replace('\u00A0', ' ');
    }

    public static void Set(TextMeshProUGUI tmp, string text)
    {
        if (tmp != null)
            tmp.text = Sanitize(text);
    }

    static string TransliterateUmlauts(string text)
    {
        return text
            .Replace("ä", "ae").Replace("Ä", "Ae")
            .Replace("ö", "oe").Replace("Ö", "Oe")
            .Replace("ü", "ue").Replace("Ü", "Ue")
            .Replace("ß", "ss");
    }
}
