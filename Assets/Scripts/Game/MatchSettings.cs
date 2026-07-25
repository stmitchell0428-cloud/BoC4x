using System;

/// <summary>Pre-game lobby choices (Decisions 21, 22, 24).</summary>
[Serializable]
public class MatchSettings
{
    public int MapSeed;
    public int MapWidth = 64;
    public int MapHeight = 42;
    public int PlayerCount = 1;
    public MapWrapStyle WrapStyle = MapWrapStyle.Toroidal;
    public HeresyPackId HeresyPack = HeresyPackId.FullCanon;
    public CoastalDensity CoastalDensity = CoastalDensity.Normal;

    public static MatchSettings CreateDefault() => new MatchSettings();

    public string SummaryLine() => FormatSummary(multiline: false);

    public string FormatSummary(bool multiline = true)
    {
        string seedLabel = MapSeedPresets.SummaryLabel(MapSeed);
        var preset = MapSizePresets.PresetFor(MapWidth, MapHeight);
        string players = PlayerCount == 1 ? "solo synod" : $"{PlayerCount} players";

        if (multiline)
        {
            return
                $"{MapSizePresets.Label(preset)}\n" +
                $"{MatchSettingsLabels.Wrap(WrapStyle)}\n" +
                $"{MatchSettingsLabels.HeresyPack(HeresyPack)}\n" +
                $"{MatchSettingsLabels.Coastal(CoastalDensity)}\n" +
                $"{players}  |  seed {seedLabel}";
        }

        return
            $"{MapSizePresets.Label(preset)}  |  {MatchSettingsLabels.Wrap(WrapStyle)}  |  " +
            $"{MatchSettingsLabels.HeresyPack(HeresyPack)}  |  {players}  |  seed {seedLabel}";
    }
}

public enum MapWrapStyle
{
    Toroidal,
    Bounded,
    Cylindrical
}

public enum HeresyPackId
{
    FullCanon,
    ReformationCore,
    RadicalFringe
}

public enum CoastalDensity
{
    Normal,
    Archipelago
}

public enum MapSizePreset
{
    Compact,
    Standard,
    Grand
}

public static class MapSizePresets
{
    public static (int width, int height) Dimensions(MapSizePreset preset) => preset switch
    {
        MapSizePreset.Compact => (40, 28),
        MapSizePreset.Standard => (64, 42),
        MapSizePreset.Grand => (80, 52),
        _ => (64, 42)
    };

    public static MapSizePreset PresetFor(int width, int height)
    {
        foreach (MapSizePreset preset in Enum.GetValues(typeof(MapSizePreset)))
        {
            var (w, h) = Dimensions(preset);
            if (w == width && h == height)
                return preset;
        }

        return MapSizePreset.Standard;
    }

    public static string Label(MapSizePreset preset) => preset switch
    {
        MapSizePreset.Compact => "Compact (40x28)",
        MapSizePreset.Standard => "Standard (64x42)",
        MapSizePreset.Grand => "Grand (80x52)",
        _ => preset.ToString()
    };
}

public static class MatchSettingsLabels
{
    public static string Wrap(MapWrapStyle style) => style switch
    {
        MapWrapStyle.Toroidal => "Toroidal (wrap N/S/E/W)",
        MapWrapStyle.Bounded => "Bounded (hard edges)",
        MapWrapStyle.Cylindrical => "Cylindrical (wrap E/W only)",
        _ => style.ToString()
    };

    public static string HeresyPack(HeresyPackId pack) => pack switch
    {
        HeresyPackId.FullCanon => "Full canon (all six heresies)",
        HeresyPackId.ReformationCore => "Reformation core (Law/Gospel/Confession)",
        HeresyPackId.RadicalFringe => "Radical fringe (Schwaermer/Zwingli/Calvin)",
        _ => pack.ToString()
    };

    public static string Coastal(CoastalDensity density) => density switch
    {
        CoastalDensity.Normal => "Normal coasts & rivers",
        CoastalDensity.Archipelago => "Archipelago (coastal seas; deep ocean blocked)",
        _ => density.ToString()
    };
}
