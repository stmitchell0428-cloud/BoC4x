using UnityEngine;

/// <summary>Visual art eras tied to confession tech progression (Decision 23).</summary>
public enum VisualArtEra
{
    WoodcutPaper = 0,
    StainedGlass = 1,
    Modern = 2
}

public static class VisualArtEraResolver
{
    public static VisualArtEra FromTier(int highestUnlockedTier) => highestUnlockedTier switch
    {
        <= 2 => VisualArtEra.WoodcutPaper,
        <= 4 => VisualArtEra.StainedGlass,
        _ => VisualArtEra.Modern
    };

    public static string DisplayName(VisualArtEra era) => era switch
    {
        VisualArtEra.WoodcutPaper => "Woodcut & paper",
        VisualArtEra.StainedGlass => "Stained glass",
        VisualArtEra.Modern => "Modern confession",
        _ => era.ToString()
    };

    public static string ShortLabel(VisualArtEra era) => era switch
    {
        VisualArtEra.WoodcutPaper => "woodcut",
        VisualArtEra.StainedGlass => "stained glass",
        VisualArtEra.Modern => "modern",
        _ => era.ToString()
    };
}

public static class ArtEraPalette
{
    public static Color CameraBackground(VisualArtEra era) => era switch
    {
        VisualArtEra.WoodcutPaper => new Color(0.18f, 0.15f, 0.11f),
        VisualArtEra.StainedGlass => new Color(0.07f, 0.08f, 0.14f),
        VisualArtEra.Modern => new Color(0.12f, 0.14f, 0.16f),
        _ => new Color(0.12f, 0.14f, 0.16f)
    };

    public static Color UiAccent(VisualArtEra era) => era switch
    {
        VisualArtEra.WoodcutPaper => new Color(0.72f, 0.58f, 0.38f),
        VisualArtEra.StainedGlass => new Color(0.55f, 0.72f, 0.95f),
        VisualArtEra.Modern => new Color(0.62f, 0.78f, 0.88f),
        _ => Color.white
    };

    public static Color TintFactionColor(Color baseColor, VisualArtEra era)
    {
        return era switch
        {
            VisualArtEra.WoodcutPaper => SepiaTint(baseColor, 0.88f),
            VisualArtEra.StainedGlass => Saturate(baseColor, 1.35f),
            VisualArtEra.Modern => Color.Lerp(baseColor, Color.white, 0.08f),
            _ => baseColor
        };
    }

    public static Color TerrainColor(TerrainType terrain, VisualArtEra era)
    {
        Color baseColor = terrain switch
        {
            TerrainType.Wilderness => new Color(0.42f, 0.58f, 0.32f),
            TerrainType.Pasture => new Color(0.55f, 0.72f, 0.38f),
            TerrainType.Forest => new Color(0.18f, 0.42f, 0.22f),
            TerrainType.Hill => new Color(0.52f, 0.46f, 0.34f),
            TerrainType.Ocean => new Color(0.20f, 0.48f, 0.88f),
            TerrainType.Shore => new Color(0.82f, 0.76f, 0.52f),
            TerrainType.Lake => new Color(0.26f, 0.58f, 0.92f),
            TerrainType.River => new Color(0.38f, 0.72f, 0.98f),
            _ => new Color(0.35f, 0.55f, 0.30f)
        };

        return era switch
        {
            VisualArtEra.WoodcutPaper => WarmPaperTerrainTint(baseColor, terrain),
            VisualArtEra.StainedGlass => StainedGlassTerrain(baseColor, terrain),
            VisualArtEra.Modern => Color.Lerp(baseColor, Color.white, 0.06f),
            _ => baseColor
        };
    }

    static Color StainedGlassTerrain(Color baseColor, TerrainType terrain)
    {
        if (TerrainRules.IsWater(terrain))
            return Saturate(baseColor, 1.45f);
        if (terrain == TerrainType.Shore)
            return new Color(0.92f, 0.78f, 0.42f);
        return Saturate(baseColor, 1.18f);
    }

    static Color WarmPaperTerrainTint(Color color, TerrainType terrain)
    {
        if (TerrainRules.IsWater(terrain))
        {
            // Keep water clearly blue; only a light vintage mute.
            return Color.Lerp(color, new Color(color.r * 0.88f, color.g * 0.94f, color.b), 0.18f);
        }

        if (terrain == TerrainType.Shore)
        {
            return Color.Lerp(color, new Color(0.88f, 0.78f, 0.52f), 0.25f);
        }

        // Parchment warmth without collapsing hue into brown.
        Color.RGBToHSV(color, out float h, out float s, out float v);
        s = Mathf.Clamp01(s * 0.9f);
        v = Mathf.Clamp01(v * 0.96f);
        var muted = Color.HSVToRGB(h, s, v);
        var warm = new Color(
            Mathf.Clamp01(muted.r * 1.03f + 0.02f),
            Mathf.Clamp01(muted.g * 0.97f),
            Mathf.Clamp01(muted.b * 0.82f));
        return Color.Lerp(color, warm, 0.38f);
    }

    static Color SepiaTint(Color color, float strength)
    {
        float g = color.grayscale;
        var sepia = new Color(g * 0.78f, g * 0.66f, g * 0.48f, color.a);
        return Color.Lerp(color, sepia, strength);
    }

    static Color Saturate(Color color, float amount)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        s = Mathf.Clamp01(s * amount);
        v = Mathf.Clamp01(v * (0.95f + amount * 0.05f));
        return Color.HSVToRGB(h, s, v);
    }
}
