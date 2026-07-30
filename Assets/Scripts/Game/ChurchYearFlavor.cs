using System.Text;
using UnityEngine;

/// <summary>Player-facing church-year lines for dashboard and event cards (LSB calendar).</summary>
public static class ChurchYearFlavor
{
    public static int CurrentTurn =>
        TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1;

    public static void GetToday(out int month, out int day, out LiturgicalSeason season)
    {
        ChurchYearCalendar.GetCivilDateForTurn(CurrentTurn, out month, out day);
        season = ChurchYearCalendar.SeasonFor(month, day);
    }

    public static bool TryGetCurrentPrincipalWatch(out ChurchYearEntry feast) =>
        ChurchYearCalendar.TryGetPrincipalWatchForTurn(CurrentTurn, out feast);

    public static string FormatDashboardLine()
    {
        GetToday(out int month, out int day, out var season);
        string seasonName = ChurchYearCalendar.SeasonDisplayName(season);

        if (ChurchYearCalendar.TryGetPrincipalWatchesForTurn(CurrentTurn, out var watch, out var also))
        {
            string line = $"<color=#C9B896>Church Year:</color> {seasonName}  -  " +
                          $"<color=#E8D5A3><b>WATCH</b></color> {watch.Name} " +
                          $"({MonthName(watch.Month)} {watch.Day})";
            if (also.HasValue)
                line += $"  ·  also {also.Value.Name} ({MonthName(also.Value.Month)} {also.Value.Day})";
            return line;
        }

        var entry = ChurchYearCalendar.PrimaryEntryForTurn(CurrentTurn);
        if (entry.HasValue)
        {
            return $"<color=#C9B896>Church Year:</color> {seasonName}  -  " +
                   $"{MonthName(month)} {day}: {entry.Value.Name}";
        }

        return $"<color=#C9B896>Church Year:</color> {seasonName}  -  {MonthName(month)} {day}";
    }

    /// <summary>Short banner line when this turn's month holds a principal feast of Christ.</summary>
    public static string FormatWatchBannerLine()
    {
        if (!ChurchYearCalendar.TryGetPrincipalWatchesForTurn(CurrentTurn, out var watch, out var also))
            return null;

        string line = $"<color=#E8D5A3><b>WATCH</b></color>  {watch.Name}  " +
                      $"({MonthName(watch.Month)} {watch.Day})";
        if (also.HasValue)
            line += $"  ·  also {also.Value.Name}";
        return line;
    }

    public static string FormatCompactObservance()
    {
        if (ChurchYearCalendar.TryGetPrincipalWatchesForTurn(CurrentTurn, out var watch, out var also))
        {
            string line = $"WATCH: {watch.Name} ({MonthName(watch.Month)} {watch.Day})";
            if (also.HasValue)
                line += $" · {also.Value.Name}";
            return line;
        }

        GetToday(out int month, out int day, out var season);
        var entry = ChurchYearCalendar.PrimaryEntryForTurn(CurrentTurn);
        if (entry.HasValue)
            return $"{entry.Value.KindLabel}: {entry.Value.Name} ({MonthName(month)} {day})";
        return $"{ChurchYearCalendar.SeasonDisplayName(season)} ({MonthName(month)} {day})";
    }

    /// <summary>Append liturgical context to a crisis/event body.</summary>
    public static string EnrichEventBody(string body, bool saturatedEmphasis = false)
    {
        if (string.IsNullOrEmpty(body))
            body = "";

        var sb = new StringBuilder(body.TrimEnd());
        sb.Append("\n\n");
        sb.Append(FormatCardCalendarBlock(saturatedEmphasis));
        return sb.ToString();
    }

    public static string FormatCardCalendarBlock(bool saturatedEmphasis)
    {
        GetToday(out int month, out int day, out var season);
        var entries = ChurchYearCalendar.EntriesFor(month, day);
        bool hasWatch = ChurchYearCalendar.TryGetPrincipalWatchesForTurn(CurrentTurn, out var watch, out var also);
        var sb = new StringBuilder();

        sb.Append("<size=12><color=#C9B896>");
        sb.Append($"<b>Church Year</b>  -  {ChurchYearCalendar.SeasonDisplayName(season)}, {MonthName(month)} {day}");
        sb.Append("</color></size>");

        if (hasWatch)
        {
            sb.Append('\n');
            sb.Append("<size=12><color=#E8D5A3>");
            sb.Append($"<b>WATCH — Principal Feast:</b> {watch.Name} ({MonthName(watch.Month)} {watch.Day})");
            if (also.HasValue)
                sb.Append($"  ·  also {also.Value.Name} ({MonthName(also.Value.Month)} {also.Value.Day})");
            sb.Append("</color></size>");
            sb.Append('\n');
            sb.Append("<size=12><i>");
            sb.Append(FlavorForPrincipalWatch(watch, saturatedEmphasis));
            sb.Append("</i></size>");
        }
        else if (entries.Count > 0)
        {
            sb.Append('\n');
            sb.Append("<size=12><color=#AABBCC>");
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append($"• {entries[i].KindLabel}: <b>{entries[i].Name}</b>");
            }

            sb.Append("</color></size>");
            sb.Append('\n');
            sb.Append("<size=12><i>");
            sb.Append(FlavorForEntry(entries[0], saturatedEmphasis));
            sb.Append("</i></size>");
        }
        else
        {
            sb.Append('\n');
            sb.Append("<size=12><i>");
            sb.Append(FlavorForSeason(season, saturatedEmphasis));
            sb.Append("</i></size>");
        }

        if (saturatedEmphasis)
        {
            sb.Append('\n');
            sb.Append("<size=12><color=#EEAA66>");
            sb.Append(SaturationWitnessLine());
            sb.Append("</color></size>");
        }

        return sb.ToString();
    }

    public static string EnrichPastoralBody(string body)
    {
        bool saturated = UnionStrifeManager.IsSaturated;
        return EnrichEventBody(body, saturatedEmphasis: saturated);
    }

    static string FlavorForPrincipalWatch(ChurchYearEntry entry, bool saturated)
    {
        string name = entry.Name;
        if (saturated)
        {
            if (name.Contains("Circumcision") || name.Contains("Name of Jesus"))
                return "The Name above every name — under three dissenting synods, confess Christ without founding a fourth capital.";
            if (name.Contains("Presentation") || name.Contains("Purification"))
                return "Presented in the temple: the Church offers her life under pressure. Hold the Gospel; do not multiply walkout capitals.";
            if (name.Contains("Annunciation"))
                return "The Word takes flesh by promise. At the schism cap, receive the Word — do not invent another synod of haste.";
            if (name.Contains("John the Baptist") && name.Contains("Nativity"))
                return "The forerunner prepares the way. Prepare repentance among three rival pulpits; overflow cannot open a fourth.";
            if (name.Contains("Visitation"))
                return "Mary visits Elizabeth: Gospel greeting between households. Visit sisters in error with truth, not a new capital.";
            if (name.Contains("Michael"))
                return "St. Michael and All Angels — the Church militant under protection. Union strife is war on earth; do not found another capital.";
            if (name.Contains("All Saints"))
                return "All Saints surrounds you with a cloud of witnesses. Three dissenting synods already stand; pray with the saints, not a fourth founding.";
            return $"Principal feast: {name}. Let Christ order courage under saturation — without a fourth capital.";
        }

        if (name.Contains("Circumcision") || name.Contains("Name of Jesus"))
            return "Principal feast: the Name of Jesus. Let every synodical act bear His name this month.";
        if (name.Contains("Presentation") || name.Contains("Purification"))
            return "Principal feast: Presentation of Our Lord. Offer doctrine, hymnody, and civic work as living sacrifice.";
        if (name.Contains("Annunciation"))
            return "Principal feast: Annunciation. The Word becomes flesh — let promise, not panic, steer the synod.";
        if (name.Contains("John the Baptist") && name.Contains("Nativity"))
            return "Principal feast: Nativity of St. John. Prepare the way with Law that serves the Gospel.";
        if (name.Contains("Visitation"))
            return "Principal feast: the Visitation (1-year). Let Gospel greeting mark diplomacy and parish life.";
        if (name.Contains("Michael"))
            return "Principal feast: St. Michael and All Angels. Stand firm; the Church fights under heavenly guard.";
        if (name.Contains("All Saints"))
            return "Principal feast: All Saints. Remember the cloud of witnesses and keep faith with the living Church.";
        return $"Principal feast of Christ: {name}. Keep this watch over the synod's confession and courage.";
    }

    static string FlavorForEntry(ChurchYearEntry entry, bool saturated)
    {
        string name = entry.Name;
        if (saturated)
        {
            if (name.Contains("Augsburg"))
                return "Public confession once stood before an emperor; now three rival pulpits already stand abroad — confess without founding a fourth capital.";
            if (name.Contains("Walther"))
                return "Walther taught Law and Gospel amid American synod strife. Distinguish cleanly while sisters in error press the land.";
            if (name.Contains("Chemnitz"))
                return "Chemnitz labored for concord after fracture. Overflow today strengthens existing dissent — it cannot open a new capital.";
            if (name.Contains("Luther") && entry.Kind == ChurchYearEntryKind.Commemoration)
                return "Luther confessed under threat of schism and sword. Hold the Gospel firmly without imagining every crisis births a new synod.";
            if (name.Contains("Bach") || name.Contains("Gerhardt") || name.Contains("Heerman") || name.Contains("Nicolai"))
                return "Song once carried the Church through war and plague. Let hymnody steady hearts while union strife rises.";
            if (name.Contains("Barnes") || name.Contains("Polycarp") || name.Contains("Ignatius") || name.Contains("Martyr"))
                return "Martyrs kept Christ when parties split around them. Costly fidelity is the saturation path — not another walkout capital.";
            if (name.Contains("Reformation"))
                return "Reformation Day recalls pure Gospel recovered. At the schism cap, reformation means reforming the household you have — not multiplying capitals.";
            if (name.Contains("All Saints"))
                return "All Saints surrounds you with a cloud of witnesses. Three dissenting synods already stand; join the saints in prayer, not a fourth founding.";
            if (name.Contains("Holy Cross"))
                return "The Cross judges every party spirit. Under three active schisms, glory only in Christ crucified.";
            if (name.Contains("Michael"))
                return "St. Michael and All Angels — the Church militant under protection. Union strife is war on earth; do not confuse it with founding yet another capital.";
        }

        if (entry.Kind == ChurchYearEntryKind.FeastOrFestival)
            return $"The Church keeps {name}. Let this feast order the synod's confession and courage today.";
        return $"The Church remembers {name}. Give thanks for their witness and imitate their faith according to your calling.";
    }

    static string FlavorForSeason(LiturgicalSeason season, bool saturated)
    {
        if (saturated)
        {
            return season switch
            {
                LiturgicalSeason.Advent =>
                    "Advent watches for Christ's coming while three dissenting synods already stand. Wait in repentance — do not found a fourth capital out of impatience.",
                LiturgicalSeason.Christmas =>
                    "Christmas proclaims the Word made flesh for a divided world. Overflow strengthens sisters in error; it cannot open another Bethlehem of dissent.",
                LiturgicalSeason.Epiphany =>
                    "Epiphany reveals Christ to the nations. At the schism cap, reveal the Gospel by steadfast parish life under pressure.",
                LiturgicalSeason.Lent =>
                    "Lent calls for repentance. Union strife is the fast's edge when three capitals of dissent already press the land.",
                LiturgicalSeason.Easter =>
                    "Easter announces victory over death. Live the risen life among three rival synods without inventing a fourth.",
                LiturgicalSeason.Pentecost =>
                    "Pentecost pours out the Spirit on one confession of Christ. Overflow joins existing dissent; it does not birth a new capital.",
                _ =>
                    "In the Time of the Church, green growth happens under pressure. Three dissenting synods stand abroad — absorb unrest without founding another."
            };
        }

        return season switch
        {
            LiturgicalSeason.Advent => "Advent: watch and pray. Prepare the way of the Lord in Law and Gospel.",
            LiturgicalSeason.Christmas => "Christmas: the Word became flesh. Let joy steady adherence and comfort alike.",
            LiturgicalSeason.Epiphany => "Epiphany: Christ made known. Let light order doctrine, hymnody, and civic life.",
            LiturgicalSeason.Lent => "Lent: return to the Lord. Discipline Law without crushing Gospel comfort.",
            LiturgicalSeason.Easter => "Easter: Christ is risen. Let resurrection courage mark preaching and defense.",
            LiturgicalSeason.Pentecost => "Pentecost: the Spirit and the Word create and sustain the Church.",
            _ => "Time of the Church: green growth under the ordinary means of grace."
        };
    }

    static string SaturationWitnessLine()
    {
        var hints = ChurchYearCalendar.SaturationWitnessHints;
        int idx = Mathf.Abs(CurrentTurn * 17 + UnionStrifeManager.Strife) % hints.Length;
        return $"Witness for this strife: {hints[idx]}.";
    }

    static string MonthName(int month) => month switch
    {
        1 => "January",
        2 => "February",
        3 => "March",
        4 => "April",
        5 => "May",
        6 => "June",
        7 => "July",
        8 => "August",
        9 => "September",
        10 => "October",
        11 => "November",
        12 => "December",
        _ => "?"
    };
}
