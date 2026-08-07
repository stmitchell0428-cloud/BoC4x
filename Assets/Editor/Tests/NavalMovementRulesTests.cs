using NUnit.Framework;

public class NavalMovementRulesTests
{
    static HexTile MakeTile(TerrainType terrain, bool navigable = false, bool navalCoast = false)
    {
        var go = new UnityEngine.GameObject("tile");
        var tile = go.AddComponent<HexTile>();
        tile.Initialize(new HexCoordinates(0, 0), terrain);
        tile.SetNavigableWater(navigable);
        tile.SetNavalCoast(navalCoast);
        return tile;
    }

    [Test]
    public void Explorer_CanEnterCoastalSeaWhenNavigable()
    {
        var tile = MakeTile(TerrainType.Ocean, navigable: true);
        Assert.IsTrue(NavalMovementRules.CanEnterTile(UnitType.CoastalExplorer, tile));
        UnityEngine.Object.DestroyImmediate(tile.gameObject);
    }

    [Test]
    public void Explorer_CannotEnterDeepOcean()
    {
        var tile = MakeTile(TerrainType.Ocean, navigable: false);
        Assert.IsFalse(NavalMovementRules.CanEnterTile(UnitType.CoastalExplorer, tile));
        UnityEngine.Object.DestroyImmediate(tile.gameObject);
    }

    [Test]
    public void Galley_CannotEnterLand()
    {
        var tile = MakeTile(TerrainType.Pasture, navalCoast: true);
        Assert.IsFalse(NavalMovementRules.CanEnterTile(UnitType.CoastalGalley, tile));
        UnityEngine.Object.DestroyImmediate(tile.gameObject);
    }

    [Test]
    public void DeepSea_CannotEnterLand()
    {
        var tile = MakeTile(TerrainType.Shore);
        Assert.IsFalse(NavalMovementRules.CanEnterTile(UnitType.DeepSeaShip, tile));
        UnityEngine.Object.DestroyImmediate(tile.gameObject);
    }

    [Test]
    public void DeepSea_CanEnterDeepOcean()
    {
        var tile = MakeTile(TerrainType.Ocean, navigable: false);
        Assert.IsTrue(NavalMovementRules.CanEnterTile(UnitType.DeepSeaShip, tile));
        UnityEngine.Object.DestroyImmediate(tile.gameObject);
    }

    [Test]
    public void Galley_CanEnterCoastalSea()
    {
        var tile = MakeTile(TerrainType.Ocean, navigable: true);
        Assert.IsTrue(NavalMovementRules.CanEnterTile(UnitType.CoastalGalley, tile));
        UnityEngine.Object.DestroyImmediate(tile.gameObject);
    }
}
