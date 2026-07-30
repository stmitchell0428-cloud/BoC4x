using System.Collections.Generic;

/// <summary>
/// Fixed-date feasts/festivals and commemorations from the Lutheran Service Book calendar
/// (as published by LCMS Worship). Movable Sundays/seasons are approximated separately.
/// </summary>
public enum ChurchYearEntryKind
{
    FeastOrFestival,
    Commemoration,
    Occasion
}

public readonly struct ChurchYearEntry
{
    public readonly int Month;
    public readonly int Day;
    public readonly string Name;
    public readonly ChurchYearEntryKind Kind;
    /// <summary>LSB p. xi boldface: principal feast of Christ (1-year calendar).</summary>
    public readonly bool IsPrincipalFeast;

    public ChurchYearEntry(int month, int day, string name, ChurchYearEntryKind kind, bool isPrincipalFeast = false)
    {
        Month = month;
        Day = day;
        Name = name;
        Kind = kind;
        IsPrincipalFeast = isPrincipalFeast;
    }

    public string KindLabel =>
        IsPrincipalFeast
            ? "Principal Feast"
            : Kind switch
            {
                ChurchYearEntryKind.FeastOrFestival => "Feast/Festival",
                ChurchYearEntryKind.Commemoration => "Commemoration",
                ChurchYearEntryKind.Occasion => "Occasion",
                _ => "Observance"
            };
}

public enum LiturgicalSeason
{
    Advent,
    Christmas,
    Epiphany,
    Lent,
    Easter,
    Pentecost,
    TimeOfTheChurch
}

/// <summary>LSB church-year fixed calendar + turn clock helpers.</summary>
public static class ChurchYearCalendar
{
    /// <summary>
    /// Each match turn advances this many civil days on a looping year.
    /// ~one synodical month so 2–4 turn techs/builds feel like a season of work, not a fortnight.
    /// </summary>
    public const int DaysPerTurn = 28;

    /// <summary>Match turn 1 lands near St. Andrew / the turn into Advent.</summary>
    public const int StartMonth = 11;
    public const int StartDay = 30;

    static readonly Dictionary<(int month, int day), List<ChurchYearEntry>> byDate = BuildIndex();

    public static IReadOnlyDictionary<(int month, int day), List<ChurchYearEntry>> AllByDate => byDate;

    public static void GetCivilDateForTurn(int turnNumber, out int month, out int day)
    {
        turnNumber = System.Math.Max(1, turnNumber);
        int startIndex = DayOfYear(StartMonth, StartDay);
        int index = (startIndex + (turnNumber - 1) * DaysPerTurn) % 365;
        if (index < 0) index += 365;
        FromDayOfYear(index, out month, out day);
    }

    public static LiturgicalSeason SeasonFor(int month, int day)
    {
        int doy = DayOfYear(month, day);
        // Approximate Western/Lutheran bands (non-computus; match starts near St. Andrew).
        if (doy >= DayOfYear(11, 30) && doy <= DayOfYear(12, 24))
            return LiturgicalSeason.Advent;
        if (doy >= DayOfYear(12, 25) || doy <= DayOfYear(1, 5))
            return LiturgicalSeason.Christmas;
        if (doy >= DayOfYear(1, 6) && doy <= DayOfYear(2, 28))
            return LiturgicalSeason.Epiphany;
        if (doy >= DayOfYear(3, 1) && doy <= DayOfYear(4, 10))
            return LiturgicalSeason.Lent;
        if (doy >= DayOfYear(4, 11) && doy <= DayOfYear(5, 25))
            return LiturgicalSeason.Easter;
        if (doy >= DayOfYear(5, 26) && doy <= DayOfYear(6, 15))
            return LiturgicalSeason.Pentecost;
        return LiturgicalSeason.TimeOfTheChurch;
    }

    public static string SeasonDisplayName(LiturgicalSeason season) => season switch
    {
        LiturgicalSeason.Advent => "Advent",
        LiturgicalSeason.Christmas => "Christmas",
        LiturgicalSeason.Epiphany => "Epiphany",
        LiturgicalSeason.Lent => "Lent",
        LiturgicalSeason.Easter => "Easter",
        LiturgicalSeason.Pentecost => "Pentecost",
        _ => "Time of the Church"
    };

    public static List<ChurchYearEntry> EntriesFor(int month, int day)
    {
        if (byDate.TryGetValue((month, day), out var list))
            return list;
        return new List<ChurchYearEntry>();
    }

    public static List<ChurchYearEntry> EntriesForTurn(int turnNumber)
    {
        GetCivilDateForTurn(turnNumber, out int month, out int day);
        return EntriesFor(month, day);
    }

    public static ChurchYearEntry? PrimaryEntryForTurn(int turnNumber)
    {
        var list = EntriesForTurn(turnNumber);
        if (list.Count == 0)
            return null;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].IsPrincipalFeast)
                return list[i];
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Kind == ChurchYearEntryKind.FeastOrFestival)
                return list[i];
        }

        return list[0];
    }

    /// <summary>
    /// Principal feast(s) falling in this turn's synodical-month window (LSB boldface / 1-year dates).
    /// Used for named "watch" turns — monthly clock rarely lands on the exact civil day.
    /// Prefers non-Eve feasts when both Eve and the feast itself fall in the window.
    /// </summary>
    public static bool TryGetPrincipalWatchForTurn(int turnNumber, out ChurchYearEntry feast) =>
        TryGetPrincipalWatchesForTurn(turnNumber, out feast, out _);

    public static bool TryGetPrincipalWatchesForTurn(
        int turnNumber,
        out ChurchYearEntry primary,
        out ChurchYearEntry? secondary)
    {
        primary = default;
        secondary = null;

        GetCivilDateForTurn(turnNumber, out int month, out int day);
        int start = DayOfYear(month, day);
        var found = new List<ChurchYearEntry>(2);

        for (int offset = 0; offset < DaysPerTurn; offset++)
        {
            int index = (start + offset) % 365;
            if (index < 0) index += 365;
            FromDayOfYear(index, out int m, out int d);
            var list = EntriesFor(m, d);
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].IsPrincipalFeast)
                    continue;
                // Skip duplicate names (should not happen across distinct dates).
                bool dup = false;
                for (int j = 0; j < found.Count; j++)
                {
                    if (found[j].Month == list[i].Month && found[j].Day == list[i].Day)
                    {
                        dup = true;
                        break;
                    }
                }

                if (!dup)
                    found.Add(list[i]);
            }
        }

        if (found.Count == 0)
            return false;

        // Prefer Circumcision/Name over its Eve when both appear.
        int primaryIndex = 0;
        for (int i = 0; i < found.Count; i++)
        {
            if (!found[i].Name.StartsWith("Eve of", System.StringComparison.Ordinal))
            {
                primaryIndex = i;
                break;
            }
        }

        primary = found[primaryIndex];
        for (int i = 0; i < found.Count; i++)
        {
            if (i == primaryIndex)
                continue;
            if (found[i].Name.StartsWith("Eve of", System.StringComparison.Ordinal))
                continue;
            secondary = found[i];
            break;
        }

        return true;
    }

    /// <summary>Witnesses especially apt when living under three active schisms.</summary>
    public static readonly string[] SaturationWitnessHints =
    {
        "Presentation of the Augsburg Confession (June 25) — confessing before emperors and neighbors",
        "Martin Chemnitz — the second Martin, who labored for concord after fracture",
        "C. F. W. Walther — Law and Gospel for a church surrounded by rival pulpits",
        "Martin Luther, Doctor and Confessor — truth at all costs, peace if possible",
        "Philip Melanchthon — the Augsburg draft still teaching public confession",
        "Johann Gerhard — consolation when the household of faith is divided",
        "Paul Gerhardt — hymns of the cross when comfort is thin",
        "J. S. Bach, Kantor — Soli Deo Gloria amid earthly strife",
        "Robert Barnes, Confessor and Martyr — costly fidelity under pressure",
        "Katharina von Bora Luther — steadfast household confession",
        "Johannes von Staupitz — a father confessor who pointed to Christ",
        "Irenaeus of Lyons — truth handed down against rival gospels"
    };

    static Dictionary<(int, int), List<ChurchYearEntry>> BuildIndex()
    {
        var dict = new Dictionary<(int, int), List<ChurchYearEntry>>();
        void Add(int month, int day, string name, ChurchYearEntryKind kind, bool principalFeast = false)
        {
            var key = (month, day);
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<ChurchYearEntry>(2);
                dict[key] = list;
            }

            list.Add(new ChurchYearEntry(month, day, name, kind, principalFeast));
        }

        // Feasts and festivals (LCMS / LSB calendar).
        // Boldface on LSB p. xi = principal feasts of Christ (historic 1-year dates).
        Add(11, 30, "St. Andrew, Apostle", ChurchYearEntryKind.FeastOrFestival);
        Add(12, 21, "St. Thomas, Apostle", ChurchYearEntryKind.FeastOrFestival);
        Add(12, 26, "St. Stephen, Martyr", ChurchYearEntryKind.FeastOrFestival);
        Add(12, 27, "St. John, Apostle and Evangelist", ChurchYearEntryKind.FeastOrFestival);
        Add(12, 28, "The Holy Innocents, Martyrs", ChurchYearEntryKind.FeastOrFestival);
        Add(12, 31, "Eve of the Circumcision and Name of Jesus", ChurchYearEntryKind.FeastOrFestival, principalFeast: true);
        Add(1, 1, "Circumcision and Name of Jesus", ChurchYearEntryKind.FeastOrFestival, principalFeast: true);
        Add(1, 18, "The Confession of St. Peter", ChurchYearEntryKind.FeastOrFestival);
        Add(1, 24, "St. Timothy, Pastor and Confessor", ChurchYearEntryKind.FeastOrFestival);
        Add(1, 25, "The Conversion of St. Paul", ChurchYearEntryKind.FeastOrFestival);
        Add(1, 26, "St. Titus, Pastor and Confessor", ChurchYearEntryKind.FeastOrFestival);
        Add(2, 2, "The Purification of Mary and the Presentation of Our Lord", ChurchYearEntryKind.FeastOrFestival, principalFeast: true);
        Add(2, 24, "St. Matthias, Apostle", ChurchYearEntryKind.FeastOrFestival);
        Add(3, 19, "St. Joseph, Guardian of Jesus", ChurchYearEntryKind.FeastOrFestival);
        Add(3, 25, "The Annunciation of Our Lord", ChurchYearEntryKind.FeastOrFestival, principalFeast: true);
        Add(4, 25, "St. Mark, Evangelist", ChurchYearEntryKind.FeastOrFestival);
        Add(5, 1, "St. Philip and St. James, Apostles", ChurchYearEntryKind.FeastOrFestival);
        // Visitation May 31 = 3-year date (kept as ordinary festival); 1-year principal is July 2.
        Add(5, 31, "The Visitation", ChurchYearEntryKind.FeastOrFestival);
        Add(6, 11, "St. Barnabas, Apostle", ChurchYearEntryKind.FeastOrFestival);
        Add(6, 24, "The Nativity of St. John the Baptist", ChurchYearEntryKind.FeastOrFestival, principalFeast: true);
        Add(6, 29, "St. Peter and St. Paul, Apostles", ChurchYearEntryKind.FeastOrFestival);
        Add(7, 2, "The Visitation", ChurchYearEntryKind.FeastOrFestival, principalFeast: true);
        Add(7, 22, "St. Mary Magdalene", ChurchYearEntryKind.FeastOrFestival);
        Add(7, 25, "St. James the Elder, Apostle", ChurchYearEntryKind.FeastOrFestival);
        Add(8, 15, "St. Mary, Mother of Our Lord", ChurchYearEntryKind.FeastOrFestival);
        Add(8, 24, "St. Bartholomew, Apostle", ChurchYearEntryKind.FeastOrFestival);
        Add(8, 29, "The Martyrdom of St. John the Baptist", ChurchYearEntryKind.FeastOrFestival);
        Add(9, 14, "Holy Cross Day", ChurchYearEntryKind.FeastOrFestival);
        Add(9, 21, "St. Matthew, Apostle and Evangelist", ChurchYearEntryKind.FeastOrFestival);
        Add(9, 29, "St. Michael and All Angels", ChurchYearEntryKind.FeastOrFestival, principalFeast: true);
        Add(10, 18, "St. Luke, Evangelist", ChurchYearEntryKind.FeastOrFestival);
        Add(10, 23, "St. James of Jerusalem, Brother of Jesus and Martyr", ChurchYearEntryKind.FeastOrFestival);
        Add(10, 28, "St. Simon and St. Jude, Apostles", ChurchYearEntryKind.FeastOrFestival);
        Add(10, 31, "Reformation Day", ChurchYearEntryKind.FeastOrFestival);
        Add(11, 1, "All Saints' Day", ChurchYearEntryKind.FeastOrFestival, principalFeast: true);

        // Commemorations (LSB list via LCMS Worship)
        Add(1, 2, "J. K. Wilhelm Loehe, Pastor", ChurchYearEntryKind.Commemoration);
        Add(1, 10, "Basil the Great of Caesarea, Gregory of Nazianzus, and Gregory of Nyssa, Pastors and Confessors", ChurchYearEntryKind.Commemoration);
        Add(1, 20, "Sarah", ChurchYearEntryKind.Commemoration);
        Add(1, 27, "John Chrysostom, Preacher", ChurchYearEntryKind.Commemoration);
        Add(2, 5, "Jacob (Israel), Patriarch", ChurchYearEntryKind.Commemoration);
        Add(2, 10, "Silas, Fellow worker of St. Peter and St. Paul", ChurchYearEntryKind.Commemoration);
        Add(2, 13, "Aquila, Priscilla, Apollos", ChurchYearEntryKind.Commemoration);
        Add(2, 14, "Valentine, Martyr", ChurchYearEntryKind.Commemoration);
        Add(2, 15, "Philemon and Onesimus", ChurchYearEntryKind.Commemoration);
        Add(2, 16, "Philip Melanchthon (birth), Confessor", ChurchYearEntryKind.Commemoration);
        Add(2, 18, "Martin Luther, Doctor and Confessor", ChurchYearEntryKind.Commemoration);
        Add(2, 23, "Polycarp of Smyrna, Pastor and Martyr", ChurchYearEntryKind.Commemoration);
        Add(3, 7, "Perpetua and Felicitas, Martyrs", ChurchYearEntryKind.Commemoration);
        Add(3, 17, "Patrick, Missionary to Ireland", ChurchYearEntryKind.Commemoration);
        Add(3, 31, "Joseph, Patriarch", ChurchYearEntryKind.Commemoration);
        Add(4, 6, "Lucas Cranach and Albrecht Durer, Artists", ChurchYearEntryKind.Commemoration);
        Add(4, 20, "Johannes Bugenhagen, Pastor", ChurchYearEntryKind.Commemoration);
        Add(4, 21, "Anselm of Canterbury, Theologian", ChurchYearEntryKind.Commemoration);
        Add(4, 24, "Johann Walter, Kantor", ChurchYearEntryKind.Commemoration);
        Add(5, 2, "Athanasius of Alexandria, Pastor and Confessor", ChurchYearEntryKind.Commemoration);
        Add(5, 4, "Friedrich Wyneken, Pastor and Missionary", ChurchYearEntryKind.Commemoration);
        Add(5, 5, "Frederick the Wise, Christian Ruler", ChurchYearEntryKind.Commemoration);
        Add(5, 7, "C. F. W. Walther, Theologian", ChurchYearEntryKind.Commemoration);
        Add(5, 9, "Job", ChurchYearEntryKind.Commemoration);
        Add(5, 11, "Cyril and Methodius, Missionaries to the Slavs", ChurchYearEntryKind.Commemoration);
        Add(5, 21, "Emperor Constantine, Christian Ruler, and Helen, Mother of Constantine", ChurchYearEntryKind.Commemoration);
        Add(5, 24, "Esther", ChurchYearEntryKind.Commemoration);
        Add(5, 25, "Bede the Venerable, Theologian", ChurchYearEntryKind.Commemoration);
        Add(6, 1, "Justin, Martyr", ChurchYearEntryKind.Commemoration);
        Add(6, 5, "Boniface of Mainz, Missionary to the Germans", ChurchYearEntryKind.Commemoration);
        Add(6, 12, "The Ecumenical Council of Nicaea, A.D. 325", ChurchYearEntryKind.Commemoration);
        Add(6, 14, "Elisha", ChurchYearEntryKind.Commemoration);
        Add(6, 25, "Presentation of the Augsburg Confession", ChurchYearEntryKind.Commemoration);
        Add(6, 26, "Jeremiah", ChurchYearEntryKind.Commemoration);
        Add(6, 27, "Cyril of Alexandria, Pastor and Confessor", ChurchYearEntryKind.Commemoration);
        Add(6, 28, "Irenaeus of Lyons, Pastor", ChurchYearEntryKind.Commemoration);
        Add(7, 6, "Isaiah", ChurchYearEntryKind.Commemoration);
        Add(7, 16, "Ruth", ChurchYearEntryKind.Commemoration);
        Add(7, 20, "Elijah", ChurchYearEntryKind.Commemoration);
        Add(7, 21, "Ezekiel", ChurchYearEntryKind.Commemoration);
        Add(7, 28, "Johann Sebastian Bach, Kantor", ChurchYearEntryKind.Commemoration);
        Add(7, 29, "Mary, Martha, and Lazarus of Bethany", ChurchYearEntryKind.Commemoration);
        Add(7, 30, "Robert Barnes, Confessor and Martyr", ChurchYearEntryKind.Commemoration);
        Add(7, 31, "Joseph of Arimathea", ChurchYearEntryKind.Commemoration);
        Add(8, 3, "Joanna, Mary, and Salome, Myrrhbearers", ChurchYearEntryKind.Commemoration);
        Add(8, 10, "Lawrence, Deacon and Martyr", ChurchYearEntryKind.Commemoration);
        Add(8, 16, "Isaac", ChurchYearEntryKind.Commemoration);
        Add(8, 17, "Johann Gerhard, Theologian", ChurchYearEntryKind.Commemoration);
        Add(8, 19, "Bernard of Clairvaux, Hymnwriter and Theologian", ChurchYearEntryKind.Commemoration);
        Add(8, 20, "Samuel", ChurchYearEntryKind.Commemoration);
        Add(8, 27, "Monica, Mother of Augustine", ChurchYearEntryKind.Commemoration);
        Add(8, 28, "Augustine of Hippo, Pastor and Theologian", ChurchYearEntryKind.Commemoration);
        Add(9, 1, "Joshua", ChurchYearEntryKind.Commemoration);
        Add(9, 2, "Hannah", ChurchYearEntryKind.Commemoration);
        Add(9, 3, "Gregory the Great, Pastor", ChurchYearEntryKind.Commemoration);
        Add(9, 4, "Moses", ChurchYearEntryKind.Commemoration);
        Add(9, 5, "Zacharias and Elizabeth", ChurchYearEntryKind.Commemoration);
        Add(9, 16, "Cyprian of Carthage, Pastor and Martyr", ChurchYearEntryKind.Commemoration);
        Add(9, 22, "Jonah", ChurchYearEntryKind.Commemoration);
        Add(9, 30, "Jerome, Translator of Holy Scripture", ChurchYearEntryKind.Commemoration);
        Add(10, 7, "Henry Melchior Muhlenberg, Pastor", ChurchYearEntryKind.Commemoration);
        Add(10, 9, "Abraham", ChurchYearEntryKind.Commemoration);
        Add(10, 11, "Philip the Deacon", ChurchYearEntryKind.Commemoration);
        Add(10, 17, "Ignatius of Antioch, Pastor and Martyr", ChurchYearEntryKind.Commemoration);
        Add(10, 25, "Dorcas (Tabitha), Lydia, and Phoebe, Faithful Women", ChurchYearEntryKind.Commemoration);
        Add(10, 26, "Philipp Nicolai, Johann Heerman, and Paul Gerhardt, Hymnwriters", ChurchYearEntryKind.Commemoration);
        Add(11, 8, "Johannes von Staupitz, Luther's Father Confessor", ChurchYearEntryKind.Commemoration);
        Add(11, 9, "Martin Chemnitz (birth), Pastor and Confessor", ChurchYearEntryKind.Commemoration);
        Add(11, 11, "Martin of Tours, Pastor", ChurchYearEntryKind.Commemoration);
        Add(11, 14, "Emperor Justinian, Christian Ruler and Confessor of Christ", ChurchYearEntryKind.Commemoration);
        Add(11, 19, "Elizabeth of Hungary", ChurchYearEntryKind.Commemoration);
        Add(11, 23, "Clement of Rome, Pastor", ChurchYearEntryKind.Commemoration);
        Add(11, 29, "Noah", ChurchYearEntryKind.Commemoration);
        Add(12, 4, "John of Damascus, Theologian and Hymnwriter", ChurchYearEntryKind.Commemoration);
        Add(12, 6, "Nicholas of Myra, Pastor", ChurchYearEntryKind.Commemoration);
        Add(12, 7, "Ambrose of Milan, Pastor and Hymnwriter", ChurchYearEntryKind.Commemoration);
        Add(12, 13, "Lucia, Martyr", ChurchYearEntryKind.Commemoration);
        Add(12, 17, "Daniel the Prophet and the Three Young Men", ChurchYearEntryKind.Commemoration);
        Add(12, 19, "Adam and Eve", ChurchYearEntryKind.Commemoration);
        Add(12, 20, "Katharina von Bora Luther", ChurchYearEntryKind.Commemoration);
        Add(12, 29, "David", ChurchYearEntryKind.Commemoration);

        return dict;
    }

    static readonly int[] DaysBeforeMonth =
    {
        0, 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334
    };

    static int DayOfYear(int month, int day) =>
        DaysBeforeMonth[month] + day - 1;

    static void FromDayOfYear(int index, out int month, out int day)
    {
        // Non-leap 365-day cycle.
        int[] lengths = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        month = 1;
        for (int m = 1; m <= 12; m++)
        {
            if (index < lengths[m - 1])
            {
                month = m;
                day = index + 1;
                return;
            }

            index -= lengths[m - 1];
        }

        month = 12;
        day = 31;
    }
}
