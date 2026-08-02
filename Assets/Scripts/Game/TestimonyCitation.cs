/// <summary>Patristic / confessional source lines for testimony colloquies and briefings.</summary>
public static class TestimonyCitation
{
    public static string Append(string body, string citation)
    {
        if (string.IsNullOrEmpty(citation))
            return body;
        return body + $"\n\n<size=12><color=#8899AA><b>Source:</b> <i>{citation}</i></color></size>";
    }

    public const string SmalcaldCatalog = "Smalcald Articles, Part II — Catalog of Testimonies (1537)";
    public const string AugustineSpiritLetter = "Augustine, On the Spirit and the Letter";
    public const string ChrysostomJohn = "Chrysostom, Homilies on the Gospel of John";
    public const string ChemnitzTrent = "Chemnitz, Examination of the Council of Trent";
    public const string GerhardLoci = "Gerhard, Loci Theologici";
    public const string JeromeScripture = "Jerome, Preface to the Book of Isaiah";
    public const string AthanasiusIncarnation = "Athanasius, On the Incarnation of the Word";
    public const string AmbroseOffices = "Ambrose, On the Offices of the Clergy";
}
