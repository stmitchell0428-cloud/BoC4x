using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public FactionId Faction { get; private set; }
    public SynodPlayerId SynodPlayer { get; private set; } = SynodPlayerId.Player1;
    public SchismaticBlocId SchismaticBloc { get; private set; } = SchismaticBlocId.None;
    public City RosterCity { get; private set; }
    public ChaplainAssignment ChaplainRole { get; private set; } = ChaplainAssignment.Parish;
    public Unit EscortUnit { get; private set; }
    public UnitType Type { get; private set; }
    public HexCoordinates HexPosition { get; private set; }
    public bool IsNomadicFounder { get; private set; }
    public bool IsFrontierSettler { get; private set; }

    public int MaxHealth { get; private set; }
    public int Health { get; private set; }
    public int Attack { get; private set; }
    public int Defense { get; private set; }
    public int MovementRange { get; private set; }
    public int MovementRemaining { get; private set; }
    public bool HasAttacked { get; private set; }
    public bool HasPreached { get; private set; }

    Unit embarkedAboard;
    readonly List<Unit> embarkedPassengers = new();

    public bool IsEmbarked => embarkedAboard != null;
    public int EmbarkedCount => embarkedPassengers.Count;
    public int EmbarkCapacity => Type == UnitType.CoastalGalley ? AmphibiousTransport.GalleyPassengerCapacity : 0;
    public bool CanEmbarkMore => EmbarkedCount < EmbarkCapacity;
    public IReadOnlyList<Unit> EmbarkedPassengers => embarkedPassengers;

    HexCoordinates? moveOrderTarget;
    bool pendingMoveOrderAdvance;
    public bool HasMoveOrder => moveOrderTarget.HasValue;
    public HexCoordinates? MoveOrderTarget => moveOrderTarget;

    SpriteRenderer spriteRenderer;
    CircleCollider2D clickCollider;

    public bool IsAlive => Health > 0;
    public bool IsOnMap => IsAlive && !IsEmbarked;
    public bool CanPreach => IsAlive &&
        (Type == UnitType.Missionary || Type == UnitType.Chaplain ||
         Type == UnitType.Pastor || Type == UnitType.Bishop || Type == UnitType.Archbishop ||
         Type == UnitType.Deaconess);
    public bool CanLeadHymn => IsAlive && Type == UnitType.Cantor;

    public bool CanAttackWithoutMoving =>
        IsAlive && !HasAttacked && (MovementRemaining > 0 || HasEnemyInAttackRange());

    bool HasEnemyInAttackRange()
    {
        if (TurnManager.Instance == null || HexGridMap.Instance == null)
            return false;

        foreach (var enemy in TurnManager.Instance.GetUnits(FactionId.Schismatic))
        {
            if (!enemy.IsAlive || !enemy.IsOnMap)
                continue;
            if (CombatSystem.AreInAttackRange(HexPosition, enemy.HexPosition, this))
                return true;
        }

        return false;
    }

    public bool CanPreachOrHymnWithoutMoving =>
        IsAlive && !HasPreached && Type is UnitType.Chaplain or UnitType.Cantor or UnitType.Pastor
            or UnitType.Bishop or UnitType.Archbishop or UnitType.Deaconess;

    public bool CanAct =>
        IsAlive && (MovementRemaining > 0 || CanAttackWithoutMoving || CanPreachOrHymnWithoutMoving);

    public bool NeedsOrders
    {
        get
        {
            if (!IsOnMap || Faction != FactionId.LutheranSynod || SynodPlayer != SynodPlayerId.Player1)
                return false;
            if (CanFoundNomadicCapital || CanFoundFrontierCity)
                return true;
            if (CanNomadicPreach && !NomadicFoundingGate.PreachCompleted &&
                FirstSteps.Instance != null &&
                (FirstSteps.Instance.ScriptureManuscripts > 0 || FirstSteps.Instance.BoundCatechisms > 0))
                return true;
            if (CanAct)
                return true;
            if (Type == UnitType.Missionary && CanPreach && !HasPreached &&
                FirstSteps.Instance != null &&
                (FirstSteps.Instance.ScriptureManuscripts > 0 || FirstSteps.Instance.BoundCatechisms > 0))
                return true;
            if (Type is UnitType.Pastor or UnitType.Bishop or UnitType.Archbishop or UnitType.Deaconess && CanPreach && !HasPreached)
                return true;
            if (Type == UnitType.Chaplain && CanPreach && !HasPreached)
                return true;
            if (Type == UnitType.Cantor && CanLeadHymn && !HasPreached)
                return true;
            if (CanUpgradeOnCity())
                return true;
            if (Type == UnitType.CoastalGalley && EmbarkedCount > 0 && MovementRemaining > 0 &&
                AmphibiousTransport.GetDisembarkHexes(this).Count > 0)
                return true;
            if (AmphibiousTransport.IsAmphibiousCargo(this) && MovementRemaining > 0 &&
                AmphibiousTransport.FindAdjacentGalley(this) != null)
                return true;
            if (HasMoveOrder)
                return true;
            return false;
        }
    }

    public bool CanUpgradeOnCity()
    {
        if (!IsAlive || Faction != FactionId.LutheranSynod) return false;
        foreach (var def in UnitUpgradeDatabase.All)
        {
            if (UnitUpgradeService.GetStatus(this, def.Id) == UnitUpgradeStatus.Available)
                return true;
        }
        return false;
    }

    public int AttackRange => Type is UnitType.Slinger or UnitType.Archer ? 2 : 1;

    public bool CanPreachOrHymn => CanPreach || CanLeadHymn;

    public int SightRange => Type switch
    {
        UnitType.Scout => 4,
        UnitType.CoastalPatrol => 3,
        UnitType.Missionary => 3,
        UnitType.Chaplain => 2,
        UnitType.Cantor => 2,
        UnitType.Bishop => 2,
        UnitType.Archbishop => 3,
        _ => 2
    };

    public bool CanFoundNomadicCapital
    {
        get
        {
            if (!IsAlive || Type != UnitType.Settler || !IsNomadicFounder)
                return false;
            if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn)
                return false;
            if (CityManager.Instance == null || CityManager.Instance.GetPrimaryPlayerCity() != null)
                return false;
            if (HexGridMap.Instance == null || !HexGridMap.Instance.TryGetTile(HexPosition, out var tile))
                return false;
            if (!TerrainRules.IsPassable(tile.Terrain))
                return false;
            if (tile.Settlement != null)
                return false;
            if (tile.Occupant != this)
                return false;
            if (!NomadicFoundingGate.RequirementsMet)
                return false;
            return true;
        }
    }

    public bool CanNomadicPreach =>
        IsAlive && Type == UnitType.Settler && IsNomadicFounder &&
        CityManager.Instance != null && CityManager.Instance.GetPrimaryPlayerCity() == null;

    bool CountsForNomadicScoutSurvey =>
        Type == UnitType.Scout &&
        Faction == FactionId.LutheranSynod &&
        SynodPlayer == SynodPlayerId.Player1;

    public bool CanFoundFrontierCity
    {
        get
        {
            if (!IsAlive || Type != UnitType.Settler || IsNomadicFounder || !IsFrontierSettler)
                return false;
            if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn)
                return false;
            if (SynodPlayer != SynodPlayerId.Player1)
                return false;
            if (CityManager.Instance == null || CityManager.Instance.GetPrimaryPlayerCity() == null)
                return false;
            if (MissionHouseChain.CountIndependentSynodCities() != 1)
                return false;
            if (HexGridMap.Instance == null || !HexGridMap.Instance.TryGetTile(HexPosition, out var tile))
                return false;
            if (!TerrainRules.IsPassable(tile.Terrain))
                return false;
            if (tile.Settlement != null)
                return false;
            if (tile.Occupant != this)
                return false;
            return true;
        }
    }

    int baseAttack;
    int baseDefense;
    int baseMovementRange;
    int bonusAttack;
    int bonusDefense;
    int bonusMovement;

    public void Initialize(
        FactionId faction,
        UnitType type,
        HexCoordinates startHex,
        bool isNomadicFounder = false,
        SynodPlayerId synodPlayer = SynodPlayerId.Player1,
        bool isFrontierSettler = false)
    {
        Faction = faction;
        SynodPlayer = synodPlayer;
        SchismaticBloc = SchismaticBlocId.None;
        Type = type;
        IsNomadicFounder = isNomadicFounder && type == UnitType.Settler;
        IsFrontierSettler = isFrontierSettler && type == UnitType.Settler && !IsNomadicFounder;
        HexPosition = HexGridMap.Instance != null
            ? HexGridMap.Instance.Wrap(startHex)
            : startHex;

        switch (type)
        {
            case UnitType.Settler:
                MaxHealth = 20;
                baseAttack = 4;
                baseDefense = 2;
                baseMovementRange = 2;
                break;
            case UnitType.Scout:
                MaxHealth = 15;
                baseAttack = 3;
                baseDefense = 1;
                baseMovementRange = 3;
                break;
            case UnitType.CoastalPatrol:
                MaxHealth = 16;
                baseAttack = 4;
                baseDefense = 2;
                baseMovementRange = 3;
                break;
            case UnitType.CoastalGalley:
                MaxHealth = 18;
                baseAttack = 8;
                baseDefense = 3;
                baseMovementRange = 2;
                break;
            case UnitType.Soldier:
                MaxHealth = 30;
                baseAttack = 12;
                baseDefense = 4;
                baseMovementRange = 2;
                break;
            case UnitType.Slinger:
                MaxHealth = 14;
                baseAttack = 8;
                baseDefense = 2;
                baseMovementRange = 2;
                break;
            case UnitType.Chaplain:
                MaxHealth = 18;
                baseAttack = 4;
                baseDefense = 2;
                baseMovementRange = 2;
                break;
            case UnitType.Cantor:
                MaxHealth = 16;
                baseAttack = 3;
                baseDefense = 1;
                baseMovementRange = 2;
                break;
            case UnitType.Defender:
                MaxHealth = 35;
                baseAttack = 8;
                baseDefense = 7;
                baseMovementRange = 2;
                break;
            case UnitType.Archer:
                MaxHealth = 13;
                baseAttack = 9;
                baseDefense = 1;
                baseMovementRange = 2;
                break;
            case UnitType.Horseman:
                MaxHealth = 18;
                baseAttack = 11;
                baseDefense = 3;
                baseMovementRange = 3;
                break;
            case UnitType.Pastor:
                MaxHealth = 18;
                baseAttack = 4;
                baseDefense = 2;
                baseMovementRange = 2;
                break;
            case UnitType.Bishop:
                MaxHealth = 20;
                baseAttack = 5;
                baseDefense = 3;
                baseMovementRange = 2;
                break;
            case UnitType.Archbishop:
                MaxHealth = 22;
                baseAttack = 5;
                baseDefense = 4;
                baseMovementRange = 2;
                break;
            case UnitType.Deaconess:
                MaxHealth = 14;
                baseAttack = 2;
                baseDefense = 2;
                baseMovementRange = 2;
                break;
            case UnitType.SiegeEngine:
                MaxHealth = 22;
                baseAttack = 5;
                baseDefense = 2;
                baseMovementRange = 1;
                break;
            default:
                MaxHealth = 20;
                baseAttack = 6;
                baseDefense = 2;
                baseMovementRange = 3;
                break;
        }

        Attack = baseAttack;
        Defense = baseDefense;
        MovementRange = baseMovementRange;

        Health = MaxHealth;
        MovementRemaining = MovementRange;
        HasAttacked = false;
        HasPreached = false;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sortingOrder = 10;
        ApplyArtEraVisuals();
        transform.localScale = Vector3.one * Type switch
        {
            UnitType.Settler => 0.54f,
            UnitType.Scout => 0.48f,
            UnitType.Soldier => 0.62f,
            UnitType.Defender => 0.66f,
            UnitType.Slinger => 0.44f,
            UnitType.Archer => 0.46f,
            UnitType.Horseman => 0.68f,
            UnitType.Chaplain => 0.50f,
            UnitType.Cantor => 0.48f,
            UnitType.Pastor => 0.52f,
            UnitType.Bishop => 0.58f,
            UnitType.Archbishop => 0.62f,
            UnitType.Deaconess => 0.44f,
            UnitType.SiegeEngine => 0.70f,
            UnitType.CoastalPatrol => 0.50f,
            UnitType.CoastalGalley => 0.58f,
            _ => 0.52f
        };

        clickCollider = GetComponent<CircleCollider2D>();
        if (clickCollider == null)
            clickCollider = gameObject.AddComponent<CircleCollider2D>();
        clickCollider.isTrigger = false;
        clickCollider.radius = 0.45f;

        SnapToHex(startHex);
        PlaceOnTile(startHex);

        if (CountsForNomadicScoutSurvey && NomadicFoundingGate.IsNomadicPhase)
            NomadicFoundingGate.RecordScoutHex(HexPosition);

        if (ConfessionResearchManager.Instance != null && faction == FactionId.LutheranSynod)
            ApplyConfessionBonuses(ConfessionResearchManager.Instance.GetEffectiveModifiers());
    }

    public void ReconfigureAs(UnitType newType, bool consumeTurn = false)
    {
        ClearMoveOrder();
        float healthRatio = MaxHealth > 0 ? Health / (float)MaxHealth : 1f;
        Type = newType;
        ApplyBaseStatsForType(newType);
        Health = Mathf.Max(1, Mathf.RoundToInt(MaxHealth * healthRatio));
        HasPreached = false;
        RefreshTypeAppearance();

        if (ConfessionResearchManager.Instance != null && Faction == FactionId.LutheranSynod)
            ApplyConfessionBonuses(ConfessionResearchManager.Instance.GetEffectiveModifiers());

        if (consumeTurn)
        {
            MovementRemaining = 0;
            HasAttacked = true;
            HasPreached = true;
        }
        else
        {
            MovementRemaining = Mathf.Min(MovementRemaining, MovementRange);
        }

        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
    }

    void ApplyBaseStatsForType(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Settler:
                MaxHealth = 20; baseAttack = 4; baseDefense = 2; baseMovementRange = 2;
                break;
            case UnitType.Scout:
                MaxHealth = 15; baseAttack = 3; baseDefense = 1; baseMovementRange = 3;
                break;
            case UnitType.CoastalPatrol:
                MaxHealth = 16; baseAttack = 4; baseDefense = 2; baseMovementRange = 3;
                break;
            case UnitType.CoastalGalley:
                MaxHealth = 18; baseAttack = 8; baseDefense = 3; baseMovementRange = 2;
                break;
            case UnitType.Soldier:
                MaxHealth = 30; baseAttack = 12; baseDefense = 4; baseMovementRange = 2;
                break;
            case UnitType.Slinger:
                MaxHealth = 14; baseAttack = 8; baseDefense = 2; baseMovementRange = 2;
                break;
            case UnitType.Chaplain:
                MaxHealth = 18; baseAttack = 4; baseDefense = 2; baseMovementRange = 2;
                break;
            case UnitType.Cantor:
                MaxHealth = 16; baseAttack = 3; baseDefense = 1; baseMovementRange = 2;
                break;
            case UnitType.Defender:
                MaxHealth = 35; baseAttack = 8; baseDefense = 7; baseMovementRange = 2;
                break;
            case UnitType.Archer:
                MaxHealth = 13; baseAttack = 9; baseDefense = 1; baseMovementRange = 2;
                break;
            case UnitType.Horseman:
                MaxHealth = 18; baseAttack = 11; baseDefense = 3; baseMovementRange = 3;
                break;
            case UnitType.Pastor:
                MaxHealth = 18; baseAttack = 4; baseDefense = 2; baseMovementRange = 2;
                break;
            case UnitType.Bishop:
                MaxHealth = 20; baseAttack = 5; baseDefense = 3; baseMovementRange = 2;
                break;
            case UnitType.Archbishop:
                MaxHealth = 22; baseAttack = 5; baseDefense = 4; baseMovementRange = 2;
                break;
            case UnitType.Deaconess:
                MaxHealth = 14; baseAttack = 2; baseDefense = 2; baseMovementRange = 2;
                break;
            case UnitType.SiegeEngine:
                MaxHealth = 22; baseAttack = 5; baseDefense = 2; baseMovementRange = 1;
                break;
            default:
                MaxHealth = 20; baseAttack = 6; baseDefense = 2; baseMovementRange = 3;
                break;
        }
        Attack = baseAttack;
        Defense = baseDefense;
        MovementRange = baseMovementRange;
    }

    void RefreshTypeAppearance()
    {
        if (spriteRenderer == null) return;
        ApplyArtEraVisuals();
        transform.localScale = Vector3.one * Type switch
        {
            UnitType.Settler => 0.54f,
            UnitType.Scout => 0.48f,
            UnitType.Soldier => 0.62f,
            UnitType.Defender => 0.66f,
            UnitType.Slinger => 0.44f,
            UnitType.Archer => 0.46f,
            UnitType.Horseman => 0.68f,
            UnitType.Chaplain => 0.50f,
            UnitType.Cantor => 0.48f,
            UnitType.Pastor => 0.52f,
            UnitType.Bishop => 0.58f,
            UnitType.Archbishop => 0.62f,
            UnitType.Deaconess => 0.44f,
            UnitType.SiegeEngine => 0.70f,
            UnitType.CoastalPatrol => 0.50f,
            UnitType.CoastalGalley => 0.58f,
            _ => 0.52f
        };
    }

    public void ApplyArtEraVisuals()
    {
        if (spriteRenderer == null)
            return;

        var mask = GetBaseMaskSprite(Type);
        var fill = FactionColor(Faction, SynodPlayer);
        spriteRenderer.sprite = ArtEraSpriteFactory.StyleSprite(
            mask, fill, ArtEraVisualController.CurrentEra, $"unit_{Type}");
        spriteRenderer.color = Color.white;
    }

    static Sprite GetBaseMaskSprite(UnitType type) => type switch
    {
        UnitType.Settler => CreateSettlerSprite(),
        UnitType.Scout => CreateScoutSprite(),
        UnitType.CoastalPatrol => CreateScoutSprite(),
        UnitType.CoastalGalley => CreateDiamondSprite(),
        UnitType.Soldier => CreateSquareSprite(),
        UnitType.Defender => CreateSquareSprite(),
        UnitType.Slinger => CreateSlingerSprite(),
        UnitType.Chaplain => CreateTriangleSprite(),
        UnitType.Cantor => CreateCircleSprite(),
        UnitType.Archer => CreateSlingerSprite(),
        UnitType.Horseman => CreateSquareSprite(),
        UnitType.Pastor => CreateTriangleSprite(),
        UnitType.Bishop => CreateStarSprite(),
        UnitType.Archbishop => CreateStarSprite(),
        UnitType.Deaconess => CreateDiamondSprite(),
        UnitType.SiegeEngine => CreateSquareSprite(),
        _ => CreateCrossSprite()
    };

    public void ApplyConfessionBonuses(ConfessionModifiers mods)
    {
        bonusAttack = Type switch
        {
            UnitType.Soldier or UnitType.Defender or UnitType.Slinger or UnitType.Archer or UnitType.Horseman or UnitType.SiegeEngine or UnitType.CoastalGalley => mods.SoldierAttackBonus,
            UnitType.Missionary => mods.MissionaryAttackBonus,
            _ => 0
        };
        bonusDefense = Type switch
        {
            UnitType.Soldier => mods.SoldierDefenseBonus,
            UnitType.Defender => mods.SoldierDefenseBonus + 2,
            _ => 0
        };
        bonusMovement = (Type == UnitType.Missionary ? mods.MissionaryMovementBonus : 0) + mods.AllUnitsMovementBonus;

        Attack = baseAttack + bonusAttack;
        Defense = baseDefense + bonusDefense;
        MovementRange = baseMovementRange + bonusMovement;

        if (MovementRemaining > MovementRange)
            MovementRemaining = MovementRange;
    }

    public void RefreshTurn()
    {
        MovementRemaining = MovementRange;
        if (Type == UnitType.CoastalPatrol && HexGridMap.Instance != null &&
            HexGridMap.Instance.TryGetTile(HexPosition, out var tile) &&
            NavalMovementRules.GetsCoastalMoveBonus(Type, tile))
        {
            MovementRemaining = Mathf.Min(MovementRange + 1, MovementRemaining + 1);
        }

        HasAttacked = false;
        HasPreached = false;

        if (IsPlayerControlledSynod)
            pendingMoveOrderAdvance = HasMoveOrder;
        else
            AdvanceMoveOrder();
    }

    bool IsPlayerControlledSynod =>
        Faction == FactionId.LutheranSynod && SynodPlayer == SynodPlayerId.Player1;

    public void ClearMoveOrder()
    {
        moveOrderTarget = null;
        pendingMoveOrderAdvance = false;
    }

    public bool CommitPendingMoveOrder()
    {
        if (!pendingMoveOrderAdvance || !HasMoveOrder)
            return false;

        pendingMoveOrderAdvance = false;
        if (MovementRemaining <= 0)
            return false;

        return AdvanceMoveOrder();
    }

    public bool TryIssueMoveOrder(HexCoordinates target)
    {
        if (IsEmbarked || !IsAlive || HexGridMap.Instance == null)
            return false;

        target = HexGridMap.Instance.Wrap(target);
        if (target == HexPosition)
        {
            ClearMoveOrder();
            return true;
        }

        if (!IsValidMoveDestination(target))
            return false;

        if (!HexGridMap.Instance.TryFindMovementPath(HexPosition, target, Faction, Type, out var fullPath) ||
            fullPath.Count <= 1)
            return false;

        pendingMoveOrderAdvance = false;
        moveOrderTarget = target;

        if (MovementRemaining > 0)
            return AdvanceMoveOrder();

        return true;
    }

    public bool AdvanceMoveOrder()
    {
        if (!HasMoveOrder || MovementRemaining <= 0 || HexGridMap.Instance == null)
            return false;

        var target = moveOrderTarget.Value;
        if (HexPosition == target)
        {
            ClearMoveOrder();
            return false;
        }

        if (!IsValidMoveDestination(target))
        {
            ClearMoveOrder();
            return false;
        }

        if (!HexGridMap.Instance.TryFindMovementPath(HexPosition, target, Faction, Type, out var fullPath))
        {
            ClearMoveOrder();
            return false;
        }

        if (!HexGridMap.Instance.TryTruncatePathToMovementBudget(fullPath, MovementRemaining, Type, out var segment, out int cost) ||
            segment.Count <= 1)
            return false;

        var destination = segment[^1];
        if (destination == HexPosition)
            return false;

        return ExecuteMoveAlongPath(destination, segment, cost);
    }

    public void SetSchismaticBloc(SchismaticBlocId blocId) => SchismaticBloc = blocId;

    public void ConvertToSchismaticBloc(SchismaticBlocId blocId)
    {
        Faction = FactionId.Schismatic;
        SetSchismaticBloc(blocId);
        ApplyArtEraVisuals();
        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
    }

    public void SetRosterCity(City city) => RosterCity = city;

    public void SetChaplainAssignment(ChaplainAssignment role, Unit escort)
    {
        if (Type != UnitType.Chaplain)
            return;
        ChaplainRole = role;
        EscortUnit = role == ChaplainAssignment.MilitaryEscort ? escort : null;
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0)
            return;
        Health = Mathf.Min(MaxHealth, Health + amount);
    }

    public bool IsRangedUnit => Type is UnitType.Slinger or UnitType.Archer;

    public bool MatchesActiveTurn()
    {
        if (TurnManager.Instance == null)
            return true;
        if (Faction != TurnManager.Instance.ActiveFaction)
            return false;
        if (Faction == FactionId.Schismatic)
            return SchismaticBloc == TurnManager.Instance.ActiveSchismaticBloc;
        return true;
    }

    public bool TryMoveTo(HexCoordinates target)
    {
        if (IsEmbarked || MovementRemaining <= 0 || HexGridMap.Instance == null) return false;

        target = HexGridMap.Instance.Wrap(target);
        if (!IsValidMoveDestination(target)) return false;

        if (!HexGridMap.Instance.TryGetMovementCost(
                HexPosition, target, MovementRemaining, Faction, Type, out int cost))
            return false;

        if (!HexGridMap.Instance.TryFindMovementPath(
                HexPosition, target, MovementRemaining, Faction, Type, out var path))
            return false;

        ClearMoveOrder();
        return ExecuteMoveAlongPath(target, path, cost);
    }

    bool IsValidMoveDestination(HexCoordinates target)
    {
        if (!HexGridMap.Instance.TryGetTile(target, out var tile)) return false;
        if (!NavalMovementRules.CanEnterTile(Type, tile)) return false;
        if (tile.Occupant != null) return false;
        return true;
    }

    bool ExecuteMoveAlongPath(HexCoordinates destination, System.Collections.Generic.List<HexCoordinates> path, int cost)
    {
        ClearTile();
        HexPosition = destination;
        MovementRemaining -= cost;
        SnapToHex(destination);
        PlaceOnTile(destination);
        HexGridMap.Instance.InvalidateMovementCostCache();

        if (CountsForNomadicScoutSurvey && NomadicFoundingGate.IsNomadicPhase)
        {
            NomadicFoundingGate.RecordScoutPath(path);
            FirstSteps.Instance?.RefreshDashboard();
            TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        }

        if (HasMoveOrder && HexPosition == moveOrderTarget.Value)
            ClearMoveOrder();

        FogOfWarManager.Instance?.Refresh();
        return true;
    }

    public void SetFogHidden(bool hidden)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = !hidden;
    }

    public void SnapToHex(HexCoordinates hex)
    {
        var pos = HexGridMap.Instance.HexToWorld(hex);
        transform.position = new Vector3(pos.x, pos.y, -0.1f);
    }

    void PlaceOnTile(HexCoordinates hex)
    {
        if (HexGridMap.Instance.TryGetTile(hex, out var tile))
            tile.SetOccupant(this);
    }

    void ClearTile()
    {
        if (HexGridMap.Instance.TryGetTile(HexPosition, out var tile) && tile.Occupant == this)
            tile.SetOccupant(null);
    }

    public void ClearTileForFounding() => ClearTile();

    public void TakeDamage(int amount)
    {
        Health = Mathf.Max(0, Health - amount);
        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        if (!IsAlive)
            Die();
    }

    void Die()
    {
        if (Type == UnitType.CoastalGalley)
        {
            for (int i = embarkedPassengers.Count - 1; i >= 0; i--)
                embarkedPassengers[i].DieFromTransportLoss();
            embarkedPassengers.Clear();
        }

        if (embarkedAboard != null)
            embarkedAboard.RemovePassenger(this);

        ClearTile();
        TurnManager.Instance?.UnregisterUnit(this);
        Destroy(gameObject);
        MatchController.Instance?.EvaluateConditions();
    }

    void DieFromTransportLoss()
    {
        embarkedAboard = null;
        ClearTile();
        TurnManager.Instance?.UnregisterUnit(this);
        Destroy(gameObject);
        MatchController.Instance?.EvaluateConditions();
    }

    public void SetEmbarkedOn(Unit galley)
    {
        embarkedAboard = galley;
        ClearMoveOrder();
        ClearTile();
        MovementRemaining = 0;
        SetMapVisible(false);
    }

    public void ClearEmbarkedState(HexCoordinates landHex)
    {
        embarkedAboard = null;
        HexPosition = landHex;
        MovementRemaining = MovementRange;
        HasAttacked = false;
        SetMapVisible(true);
        SnapToHex(landHex);
        PlaceOnTile(landHex);
    }

    public void AddPassenger(Unit passenger)
    {
        if (passenger != null && !embarkedPassengers.Contains(passenger))
            embarkedPassengers.Add(passenger);
    }

    public void RemovePassenger(Unit passenger)
    {
        embarkedPassengers.Remove(passenger);
    }

    public Unit GetFirstPassenger() =>
        embarkedPassengers.Count > 0 ? embarkedPassengers[0] : null;

    public void SpendMovement(int amount) =>
        MovementRemaining = Mathf.Max(0, MovementRemaining - amount);

    void SetMapVisible(bool visible)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;
        if (clickCollider != null)
            clickCollider.enabled = visible;
    }

    public void ConvertToMissionaryAfterFounding()
    {
        if (Type != UnitType.Settler || !IsNomadicFounder)
            return;

        IsNomadicFounder = false;
        Type = UnitType.Missionary;

        MaxHealth = 20;
        Health = Mathf.Min(Health, MaxHealth);
        baseAttack = 6;
        baseDefense = 2;
        baseMovementRange = 3;
        Attack = baseAttack;
        Defense = baseDefense;
        MovementRange = baseMovementRange;
        MovementRemaining = 0;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = CreateCrossSprite();
            transform.localScale = Vector3.one * 0.52f;
        }

        if (ConfessionResearchManager.Instance != null && Faction == FactionId.LutheranSynod)
            ApplyConfessionBonuses(ConfessionResearchManager.Instance.GetEffectiveModifiers());

        FirstSteps.Instance?.BindPlayerUnit(this);
        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
    }

    public void MarkAttacked()
    {
        HasAttacked = true;
        ClearMoveOrder();
    }

    public void MarkPreached() => HasPreached = true;

    public static Color FactionColor(FactionId faction, SynodPlayerId synodPlayer = SynodPlayerId.Player1) =>
        faction switch
    {
        FactionId.LutheranSynod => SynodPlayerDatabase.ColorFor(synodPlayer),
        FactionId.Schismatic => new Color(0.85f, 0.28f, 0.22f),
        _ => Color.gray
    };

    public void SetSynodPlayer(SynodPlayerId playerId) => SynodPlayer = playerId;

    public string FormatOwnerLabel()
    {
        if (Faction == FactionId.Schismatic)
        {
            if (SchismaticBloc != SchismaticBlocId.None && SchismaticBlocRegistry.Instance != null)
                return SchismaticBlocRegistry.Instance.ProfileForBloc(SchismaticBloc).DisplayName;
            return "Schismatic dissent";
        }

        if (Faction == FactionId.LutheranSynod)
            return SynodPlayerDatabase.DisplayName(SynodPlayer);

        return Faction.ToString();
    }

    public static string TypeDisplayName(UnitType type) => type switch
    {
        UnitType.Settler => "Settler",
        UnitType.Scout => "Scout",
        UnitType.CoastalPatrol => "Coastal Patrol",
        UnitType.CoastalGalley => "Coastal Galley",
        UnitType.Soldier => "Soldier",
        UnitType.Slinger => "Slinger",
        UnitType.Chaplain => "Chaplain",
        UnitType.Cantor => "Cantor",
        UnitType.Defender => "Defender",
        UnitType.Archer => "Archer",
        UnitType.Horseman => "Horseman",
        UnitType.Pastor => "Pastor",
        UnitType.Bishop => "Bishop",
        UnitType.Archbishop => "Archbishop",
        UnitType.Deaconess => "Deaconess",
        UnitType.SiegeEngine => "Siege Engine",
        _ => "Missionary"
    };

    public string HealthLabel => $"HP {Health}/{MaxHealth}";

    public string MovementSummary =>
        HasMoveOrder
            ? $"{MovementRemaining}/{MovementRange} move | marching"
            : $"{MovementRemaining}/{MovementRange} move";

    public string RoleSummary => Type switch
    {
        UnitType.Settler =>
            IsNomadicFounder
                ? $"{HealthLabel} | {MovementSummary} | {NomadicFoundingGate.FormatProgressShort()}"
                : IsFrontierSettler
                    ? $"{HealthLabel} | {MovementSummary} | F = found 2nd city"
                    : $"{HealthLabel} | {MovementSummary}",
        UnitType.Scout =>
            $"{HealthLabel} | {MovementSummary} | sight {SightRange}",
        UnitType.CoastalPatrol =>
            $"{HealthLabel} | {MovementSummary} | sight {SightRange} | +1 move on shore/water",
        UnitType.CoastalGalley =>
            $"{HealthLabel} | {MovementSummary} | {Attack} atk | {Defense} def | cargo {EmbarkedCount}/{EmbarkCapacity} | shore + water{GarrisonBonus.FormatRoleSuffix(this)}",
        UnitType.Soldier =>
            $"{HealthLabel} | {MovementSummary} | {Attack} atk | {Defense} def{GarrisonBonus.FormatRoleSuffix(this)}",
        UnitType.Slinger =>
            $"{HealthLabel} | {MovementSummary} | {Attack} rng atk (2 hex){GarrisonBonus.FormatRoleSuffix(this)}",
        UnitType.Archer =>
            $"{HealthLabel} | {MovementSummary} | {Attack} bow atk (2 hex){GarrisonBonus.FormatRoleSuffix(this)}",
        UnitType.Horseman =>
            $"{HealthLabel} | {MovementSummary} | {Attack} atk | {Defense} def (mounted){GarrisonBonus.FormatRoleSuffix(this)}",
        UnitType.Defender =>
            $"{HealthLabel} | {MovementSummary} | {Attack} atk | {Defense} def (guard){GarrisonBonus.FormatRoleSuffix(this)}",
        UnitType.Chaplain =>
            $"{HealthLabel} | {MovementSummary} | {ChaplainSpecialty.FormatAssignment(this)} | preach: {(HasPreached ? "used" : "ready")}",
        UnitType.Cantor =>
            $"{HealthLabel} | {MovementSummary} | free hymn: {(HasPreached ? "used" : "ready")}",
        UnitType.Pastor =>
            $"{HealthLabel} | {MovementSummary} | free preach: {(HasPreached ? "used" : "ready")}",
        UnitType.Bishop =>
            $"{HealthLabel} | {MovementSummary} | {EpiscopalOversight.FormatPassiveSummary(this)} | preach: {(HasPreached ? "used" : "ready")}",
        UnitType.Archbishop =>
            $"{HealthLabel} | {MovementSummary} | {EpiscopalOversight.FormatPassiveSummary(this)} | preach: {(HasPreached ? "used" : "ready")}",
        UnitType.Deaconess =>
            $"{HealthLabel} | {MovementSummary} | free mercy visit: {(HasPreached ? "used" : "ready")}",
        UnitType.SiegeEngine =>
            $"{HealthLabel} | {MovementSummary} | {Attack} atk | siege {CityLoyaltySystem.GetSiegePressure(this)}/turn on cities{GarrisonBonus.FormatRoleSuffix(this)}",
        _ =>
            $"{HealthLabel} | {MovementSummary} | {Attack} atk"
    };

    static Sprite crossSprite;
    static Sprite settlerSprite;
    static Sprite scoutSprite;
    static Sprite squareSprite;
    static Sprite triangleSprite;
    static Sprite diamondSprite;
    static Sprite circleSprite;
    static Sprite slingerSprite;
    static Sprite starSprite;

    static Sprite CreateTriangleSprite()
    {
        if (triangleSprite != null) return triangleSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        var apex = new Vector2(size / 2f, size - 4f);
        var left = new Vector2(4f, 4f);
        var right = new Vector2(size - 4f, 4f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x, y);
                tex.SetPixel(x, y, PointInTriangle(p, apex, left, right) ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        triangleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return triangleSprite;
    }

    static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float Sign(Vector2 p1, Vector2 p2, Vector2 p3) =>
            (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNeg && hasPos);
    }

    static Sprite CreateSettlerSprite()
    {
        if (settlerSprite != null) return settlerSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        float centerX = size / 2f;
        float centerY = size / 2f;
        const int armThickness = 5;
        const int horizontalBarY = 20;
        const int horizontalHalfSpan = 10;
        const float ringRadius = 12f;
        const float ringThickness = 2.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool verticalStem =
                    Mathf.Abs(x - centerX) <= armThickness * 0.5f &&
                    y >= 4 &&
                    y <= size - 5;

                bool horizontalBar =
                    Mathf.Abs(y - horizontalBarY) <= armThickness * 0.5f &&
                    Mathf.Abs(x - centerX) <= horizontalHalfSpan;

                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                bool ring = Mathf.Abs(dist - ringRadius) <= ringThickness;

                tex.SetPixel(x, y, verticalStem || horizontalBar || ring ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        settlerSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return settlerSprite;
    }

    static Sprite CreateScoutSprite()
    {
        if (scoutSprite != null) return scoutSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        var apex = new Vector2(size / 2f, size - 5f);
        var left = new Vector2(8f, 6f);
        var right = new Vector2(size - 8f, 6f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x, y);
                tex.SetPixel(x, y, PointInTriangle(p, apex, left, right) ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        scoutSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return scoutSprite;
    }

    static Sprite CreateCrossSprite()
    {
        if (crossSprite != null) return crossSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        float centerX = size / 2f;
        const int armThickness = 6;
        const int horizontalBarY = 22;
        const int horizontalHalfSpan = 12;
        const int margin = 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool verticalStem =
                    Mathf.Abs(x - centerX) <= armThickness * 0.5f &&
                    y >= margin &&
                    y <= size - margin - 1;

                bool horizontalBar =
                    Mathf.Abs(y - horizontalBarY) <= armThickness * 0.5f &&
                    Mathf.Abs(x - centerX) <= horizontalHalfSpan;

                tex.SetPixel(x, y, verticalStem || horizontalBar ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        crossSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return crossSprite;
    }

    static Sprite CreateSquareSprite()
    {
        if (squareSprite != null) return squareSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            tex.SetPixel(x, y, Color.white);
        tex.Apply();
        squareSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return squareSprite;
    }

    static Sprite CreateDiamondSprite()
    {
        if (diamondSprite != null) return diamondSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        var center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center.x);
                float dy = Mathf.Abs(y - center.y);
                tex.SetPixel(x, y, dx + dy <= radius ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        diamondSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return diamondSprite;
    }

    static Sprite CreateCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        var center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }

    static Sprite CreateStarSprite()
    {
        if (starSprite != null) return starSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        var center = new Vector2(size / 2f, size / 2f);
        const int points = 5;
        const float outerRadius = size / 2f - 2f;
        const float innerRadius = outerRadius * 0.42f;

        var verts = new Vector2[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            float angle = (-Mathf.PI / 2f) + i * Mathf.PI / points;
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            verts[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, PointInPolygon(new Vector2(x, y), verts) ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        starSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return starSprite;
    }

    static bool PointInPolygon(Vector2 p, Vector2[] verts)
    {
        bool inside = false;
        for (int i = 0, j = verts.Length - 1; i < verts.Length; j = i++)
        {
            if ((verts[i].y > p.y) != (verts[j].y > p.y) &&
                p.x < (verts[j].x - verts[i].x) * (p.y - verts[i].y) / (verts[j].y - verts[i].y) + verts[i].x)
                inside = !inside;
        }
        return inside;
    }

    static Sprite CreateSlingerSprite()
    {
        if (slingerSprite != null) return slingerSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        float cx = size / 2f;
        float cy = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                bool ring = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 9f) <= 1.6f;
                bool dot = dx * dx + dy * dy <= 9f;
                tex.SetPixel(x, y, ring || dot ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        slingerSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return slingerSprite;
    }
}
