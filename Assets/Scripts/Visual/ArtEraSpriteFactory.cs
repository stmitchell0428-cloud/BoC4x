using System.Collections.Generic;
using UnityEngine;

/// <summary>Applies woodcut / stained-glass / modern styling to procedural sprite masks.</summary>
public static class ArtEraSpriteFactory
{
    static readonly Dictionary<string, Sprite> Cache = new();

    public static void ClearCache() => Cache.Clear();

    public static Sprite StyleSprite(Sprite mask, Color fill, VisualArtEra era, string cacheKey)
    {
        if (mask == null)
            return null;

        int fillHash = fill.r.GetHashCode() ^ fill.g.GetHashCode() ^ fill.b.GetHashCode();
        string key = $"{cacheKey}_{era}_{fillHash}";
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        var src = mask.texture;
        int width = (int)mask.rect.width;
        int height = (int)mask.rect.height;
        int x0 = (int)mask.rect.x;
        int y0 = (int)mask.rect.y;

        var styled = new Texture2D(width, height, TextureFormat.RGBA32, false);
        styled.filterMode = era == VisualArtEra.Modern ? FilterMode.Bilinear : FilterMode.Point;

        var maskPixels = src.GetPixels(x0, y0, width, height);
        var outPixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                float alpha = maskPixels[i].a;
                if (alpha < 0.05f)
                {
                    outPixels[i] = Color.clear;
                    continue;
                }

                bool edge = IsEdge(maskPixels, width, height, x, y);
                outPixels[i] = era switch
                {
                    VisualArtEra.WoodcutPaper => StyleWoodcut(fill, x, y, edge),
                    VisualArtEra.StainedGlass => StyleStainedGlass(fill, x, y, edge),
                    _ => StyleModern(fill, x, y, height, edge)
                };
            }
        }

        styled.SetPixels(outPixels);
        styled.Apply();

        var sprite = Sprite.Create(styled, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), mask.pixelsPerUnit);
        Cache[key] = sprite;
        return sprite;
    }

    static bool IsEdge(Color[] mask, int width, int height, int x, int y)
    {
        int i = y * width + x;
        if (mask[i].a < 0.5f)
            return false;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                    return true;
                if (mask[ny * width + nx].a < 0.5f)
                    return true;
            }
        }

        return false;
    }

    static Color StyleWoodcut(Color fill, int x, int y, bool edge)
    {
        float grain = 0.82f + Hash(x, y) * 0.18f;
        var tinted = ArtEraPalette.TintFactionColor(fill, VisualArtEra.WoodcutPaper);
        var color = tinted * grain;
        if (edge)
            color *= 0.55f;
        color.a = 1f;
        return color;
    }

    static Color StyleStainedGlass(Color fill, int x, int y, bool edge)
    {
        if (edge)
            return new Color(0.04f, 0.04f, 0.06f, 1f);

        int segment = (x / 4 + y / 4) % 3;
        var tinted = ArtEraPalette.TintFactionColor(fill, VisualArtEra.StainedGlass);
        Color pane = segment switch
        {
            0 => tinted,
            1 => new Color(tinted.r * 0.82f, tinted.g * 1.08f, tinted.b * 1.18f, 1f),
            _ => new Color(tinted.r * 1.12f, tinted.g * 0.88f, tinted.b * 0.92f, 1f)
        };
        pane.a = 1f;
        return pane;
    }

    static Color StyleModern(Color fill, int x, int y, int height, bool edge)
    {
        float gradient = 0.92f + (y / (float)Mathf.Max(1, height - 1)) * 0.12f;
        var tinted = ArtEraPalette.TintFactionColor(fill, VisualArtEra.Modern);
        var color = tinted * gradient;
        if (edge)
            color = Color.Lerp(color, Color.white, 0.18f);
        color.a = 1f;
        return color;
    }

    static float Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 73856093 ^ y * 19349663;
            return (h & 0xFFFF) / 65535f;
        }
    }
}
