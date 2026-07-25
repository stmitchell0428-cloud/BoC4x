using UnityEngine;

/// <summary>Named map seeds for the match lobby.</summary>
public static class MapSeedPresets
{
    public readonly struct Entry
    {
        public readonly int Seed;
        public readonly string Name;

        public Entry(int seed, string name)
        {
            Seed = seed;
            Name = name;
        }
    }

    public static readonly Entry[] Presets =
    {
        new(0, "Random wilderness"),
        new(42, "Classic balanced"),
        new(95, "Wittenberg Theses (1517)"),
        new(325, "Nicaea"),
        new(1530, "Augsburg Confession"),
        new(1580, "Book of Concord"),
        new(1817, "Walther era"),
        new(1847, "Missouri Synod"),
        new(622, "Saxon heartland"),
        new(1337, "Elbe crossing"),
        new(1492, "New world voyage"),
        new(3003, "Triune frontier"),
    };

    public static int Count => Presets.Length;

    public static int IndexOf(int seed)
    {
        for (int i = 0; i < Presets.Length; i++)
        {
            if (Presets[i].Seed == seed)
                return i;
        }

        return -1;
    }

    public static int WrapIndex(int index) => (index % Presets.Length + Presets.Length) % Presets.Length;

    public static int SeedAt(int index) => Presets[WrapIndex(index)].Seed;

    public static string Caption(int seed)
    {
        int idx = IndexOf(seed);
        if (idx < 0)
            return seed == 0 ? "Random wilderness" : $"Custom seed ({seed})";

        var entry = Presets[idx];
        return entry.Seed == 0 ? entry.Name : $"{entry.Seed}  -  {entry.Name}";
    }

    public static string SummaryLabel(int seed)
    {
        int idx = IndexOf(seed);
        if (idx < 0)
            return seed == 0 ? "random" : seed.ToString();

        var entry = Presets[idx];
        return entry.Seed == 0 ? "random" : $"{entry.Seed} ({entry.Name})";
    }
}
