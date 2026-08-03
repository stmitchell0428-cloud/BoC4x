using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Lutheran Synod faction state  -  confessional metrics and turn-end processing.
/// </summary>
public class FirstSteps : MonoBehaviour
{
    public static FirstSteps Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI queueReviewUIText;
    public TextMeshProUGUI populationUIText;
    public TextMeshProUGUI adherenceUIText;
    public TextMeshProUGUI manuscriptUIText;
    public TextMeshProUGUI waltherDashboardUIText;

    [Header("Tribe Metrics")]
    public int population = 30;
    public int scriptureManuscripts = 6;
    public int boundCatechisms = 0;
    public int confessionalFame = 0;
    public float confessionalAdherence = 85f;

    [Header("Synod Identity")]
    public ConfessionalIdentityId confessionalIdentity = ConfessionalIdentityId.None;
    public bool identityRespecUsed;

    [Header("Walther Trackers")]
    public float civicRestraint = 50f;
    public float spiritualComfort = 50f;

    Unit trackedUnit;

    public int ScriptureManuscripts
    {
        get => scriptureManuscripts;
        set => scriptureManuscripts = Mathf.Max(0, value);
    }

    public float ConfessionalAdherence => confessionalAdherence;
    public float CivicRestraint => civicRestraint;
    public float SpiritualComfort => spiritualComfort;
    public int ConfessionalFame => confessionalFame;
    public int BoundCatechisms => boundCatechisms;

    public void SetConfessionalIdentity(ConfessionalIdentityId id) => confessionalIdentity = id;

    public bool CanRespecIdentity =>
        !identityRespecUsed &&
        confessionalIdentity != ConfessionalIdentityId.None &&
        (confessionalFame >= 35 ||
         SynodLegacyManager.Instance?.HasTrait(SynodLegacyTraitId.CrisisSurvivor) == true);

    public void MarkIdentityRespecUsed() => identityRespecUsed = true;

    public void TryOpenIdentityRespec()
    {
        if (!CanRespecIdentity) return;
        IdentityPickerPanel.Instance?.ShowRespec();
    }

    public void AddFame(int amount)
    {
        if (amount <= 0) return;
        confessionalFame += amount;
        SynodLegacyManager.Instance?.CheckFameMilestones();
    }

    public void AdjustSpiritualComfort(float delta)
    {
        spiritualComfort = Mathf.Clamp(spiritualComfort + delta, 0f, 100f);
    }

    public void AdjustCivicRestraint(float delta)
    {
        civicRestraint = Mathf.Clamp(civicRestraint + delta, 0f, 100f);
    }

    public const float MaxCrisisAdherenceFloor = 0f;

    public float EffectiveMinAdherenceFloor
    {
        get
        {
            if (ConfessionResearchManager.Instance == null)
                return 0f;

            var mods = ConfessionResearchManager.Instance.GetEffectiveModifiers();
            return mods.MinAdherenceFloor + mods.MinAdherenceFloorBonus;
        }
    }

    static bool ConfessionalPopulationGrowthAllowed()
    {
        var city = CityManager.Instance?.GetPrimaryPlayerCity();
        if (city == null)
            return true;

        return CityGrowthSystem.Evaluate(city).FoodSurplus > 0;
    }

    public HexCoordinates? SynodAnchorHex
    {
        get
        {
            var city = CityManager.Instance?.GetPrimaryPlayerCity();
            if (city != null)
                return city.HexPosition;
            if (trackedUnit != null)
                return trackedUnit.HexPosition;
            return null;
        }
    }

    public void AdjustConfessionalAdherence(float delta)
    {
        confessionalAdherence = Mathf.Clamp(
            confessionalAdherence + delta,
            EffectiveMinAdherenceFloor,
            100f);
    }

    public void AddBoundCatechism(int amount = 1)
    {
        if (amount <= 0) return;
        boundCatechisms += amount;
    }

    ConfessionModifiers Modifiers =>
        ConfessionResearchManager.Instance != null
            ? ConfessionResearchManager.Instance.GetEffectiveModifiers()
            : new ConfessionModifiers();

    void Awake() => Instance = this;

    void OnEnable() => TrySubscribeTurnStarted();

    void Start()
    {
        TrySubscribeTurnStarted();
        RefreshDashboard();
    }

    void OnDisable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.TurnStarted -= OnTurnStarted;
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.TurnStarted -= OnTurnStarted;
        if (Instance == this)
            Instance = null;
    }

    void TrySubscribeTurnStarted()
    {
        if (TurnManager.Instance == null)
            return;

        TurnManager.Instance.TurnStarted -= OnTurnStarted;
        TurnManager.Instance.TurnStarted += OnTurnStarted;
    }

    void OnTurnStarted()
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn)
            return;

        RefreshDashboard();
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (!TurnManager.Instance || !TurnManager.Instance.IsPlayerTurn) return;
        if (CityScreenPanel.Instance != null && CityScreenPanel.Instance.IsOpen) return;
        if (ConfessionTechPanel.Instance != null && ConfessionTechPanel.Instance.IsOpen) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            PreachPureWord();

        if (Keyboard.current.fKey.wasPressedThisFrame)
            TryFoundCapital();

        if (Keyboard.current.bKey.wasPressedThisFrame)
            NomadicFoundingGate.TryBindNomadicCatechism();

        if (Keyboard.current.uKey.wasPressedThisFrame)
            TryUnitUpgrade();

        if (Keyboard.current.gKey.wasPressedThisFrame)
            AppealOverlayController.Instance?.Toggle();

        if (Keyboard.current.iKey.wasPressedThisFrame)
            TryOpenIdentityRespec();

        if (Keyboard.current.oKey.wasPressedThisFrame)
            TryEmbarkSelectedUnit();

        if (Keyboard.current.lKey.wasPressedThisFrame)
            TryDisembarkGalley();

        if (Keyboard.current.hKey.wasPressedThisFrame)
            TryToggleFortifySelected();

        if (Keyboard.current.jKey.wasPressedThisFrame)
            TrySkipTurnSelected();

        if (Keyboard.current.dKey.wasPressedThisFrame)
            DiplomacyPanel.Instance?.Toggle();

        if (Keyboard.current.yKey.wasPressedThisFrame)
            SynodBriefPanel.Instance?.Toggle();
    }

    void TrySkipTurnSelected()
    {
        var unit = TurnManager.Instance?.SelectedUnit;
        if (unit == null || !unit.IsOnMap)
            return;
        if (!unit.ToggleSkipTurn())
            return;

        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
        TurnPhaseBanner.Instance?.Refresh(
            unit.SkippedThisTurn
                ? $"{Unit.TypeDisplayName(unit.Type)} skipped this turn (J to undo)"
                : $"{Unit.TypeDisplayName(unit.Type)} back in order queue");
    }

    void TryToggleFortifySelected()
    {
        var unit = TurnManager.Instance?.SelectedUnit;
        if (unit == null || !unit.IsOnMap)
            return;
        if (!unit.ToggleFortify())
            return;

        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
        TurnPhaseBanner.Instance?.Refresh(
            unit.IsFortified
                ? $"{Unit.TypeDisplayName(unit.Type)} fortified (H to wake)"
                : $"{Unit.TypeDisplayName(unit.Type)} left fortify");
    }

    void TryEmbarkSelectedUnit()
    {
        var passenger = TurnManager.Instance?.SelectedUnit;
        if (passenger == null)
            return;

        var galley = AmphibiousTransport.FindAdjacentGalley(passenger);
        if (galley == null || !AmphibiousTransport.TryEmbark(passenger, galley))
        {
            Debug.Log("Board unavailable — select a soldier/slinger on shore with move left, adjacent to your galley on water.");
            return;
        }

        TurnManager.Instance.SelectUnit(galley);
        HexSelectionController.Instance?.FocusUnit(galley);
        PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
    }

    void TryDisembarkGalley()
    {
        var galley = TurnManager.Instance?.SelectedUnit;
        if (galley == null || galley.Type is not (UnitType.CoastalGalley or UnitType.DeepSeaShip))
            return;

        var targets = AmphibiousTransport.GetDisembarkHexes(galley);
        if (targets.Count == 0)
        {
            Debug.Log("Land unavailable — galley needs move points, cargo, and an adjacent shore hex.");
            return;
        }

        HexCoordinates best = targets[0];
        if (HexGridMap.Instance != null && CityManager.Instance != null)
        {
            int bestScore = int.MinValue;
            foreach (var city in CityManager.Instance.GetCitiesForFaction(FactionId.Schismatic))
            {
                foreach (var hex in targets)
                {
                    int score = -HexGridMap.Instance.WrappedDistance(hex, city.HexPosition);
                    if (city.IsCapital) score += 3;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = hex;
                    }
                }
            }
        }

        if (!AmphibiousTransport.TryDisembark(
                galley,
                best,
                GalleyCargoPanel.Instance?.GetSelectedPassenger(galley)))
            return;

        Unit landed = null;
        if (HexGridMap.Instance != null && HexGridMap.Instance.TryGetTile(best, out var tile))
            landed = tile.Occupant;

        if (landed != null)
        {
            TurnManager.Instance.SelectUnit(landed);
            HexSelectionController.Instance?.FocusUnit(landed);
        }
        else
        {
            HexSelectionController.Instance?.ShowReachableForUnit(galley);
        }

        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
    }

    void TryUnitUpgrade()
    {
        var unit = TurnManager.Instance?.SelectedUnit;
        if (unit == null) return;

        foreach (var def in UnitUpgradeDatabase.All)
        {
            if (UnitUpgradeService.GetStatus(unit, def.Id) == UnitUpgradeStatus.Available &&
                UnitUpgradeService.TryUpgrade(unit, def.Id))
                return;
        }

        Debug.Log("Upgrade unavailable  -  stand on a city hex, research the tech, and pay manuscript cost (C for details).");
    }

    void TryFoundCapital()
    {
        var selected = TurnManager.Instance?.SelectedUnit;
        if (selected == null)
            return;

        if (selected.Type == UnitType.Settler && selected.IsNomadicFounder &&
            CityManager.Instance?.GetPrimaryPlayerCity() == null &&
            !NomadicFoundingGate.RequirementsMet)
        {
            Debug.Log(NomadicFoundingGate.FormatBlockingReason());
            return;
        }

        if (selected.CanFoundNomadicCapital)
        {
            if (CityManager.Instance != null && CityManager.Instance.TryFoundCityFromNomadicSettler(selected))
                RefreshAfterFounding();
            return;
        }

        if (selected.CanFoundFrontierCity)
        {
            if (CityManager.Instance != null && CityManager.Instance.TryFoundCityFromFrontierSettler(selected))
                RefreshAfterFounding();
            return;
        }

        // District founding is organic-only (growth offers).
    }

    void RefreshAfterFounding()
    {
        RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshMissionaryTile();
        TerrainInfoPanel.Instance?.RefreshCityYield();
        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
    }

    public void BindPlayerUnit(Unit unit) => trackedUnit = unit;

    /// <summary>
    /// Unit whose tile drives end-turn wilderness/settlement manuscripts and the Missionary tile HUD.
    /// Prefer missionary/clergy over whichever unit was last selected.
    /// </summary>
    public Unit GetFieldSynodUnit()
    {
        if (TurnManager.Instance == null)
            return trackedUnit != null && trackedUnit.IsAlive && trackedUnit.IsOnMap ? trackedUnit : null;

        foreach (var unit in TurnManager.Instance.GetSynodUnits(SynodPlayerId.Player1))
        {
            if (!unit.IsAlive || !unit.IsOnMap)
                continue;
            if (unit.Type == UnitType.Settler && unit.IsNomadicFounder)
                return unit;
        }

        foreach (var unit in TurnManager.Instance.GetSynodUnits(SynodPlayerId.Player1))
        {
            if (!unit.IsAlive || !unit.IsOnMap)
                continue;
            if (unit.Type == UnitType.Missionary)
                return unit;
        }

        foreach (var unit in TurnManager.Instance.GetSynodUnits(SynodPlayerId.Player1))
        {
            if (!unit.IsAlive || !unit.IsOnMap)
                continue;
            if (ClergyRoster.IsClergyUnit(unit.Type))
                return unit;
        }

        if (trackedUnit != null && trackedUnit.IsAlive && trackedUnit.IsOnMap &&
            trackedUnit.Faction == FactionId.LutheranSynod &&
            trackedUnit.SynodPlayer == SynodPlayerId.Player1)
            return trackedUnit;

        return TurnManager.Instance.GetSynodUnits(SynodPlayerId.Player1)
            .FirstOrDefault(u => u.IsAlive && u.IsOnMap);
    }

    public void RefreshDashboard()
    {
        ResolveDashboardRefs();

        int turn = TurnManager.Instance ? TurnManager.Instance.TurnNumber : 1;
        string synodStatus = FormatSynodStatusLine();
        string factionLine = TurnManager.Instance
            ? $"Lutheran Synod  |  Turn {turn}"
            : $"Turn {turn}";
        string churchYearLine = NomadicFoundingGate.IsNomadicPhase
            ? SalvationHistoryFlavor.FormatDashboardLine()
            : ChurchYearFlavor.FormatDashboardLine();

        if (queueReviewUIText != null)
            queueReviewUIText.text = TmpTextSanitizer.Sanitize(ActionQueueHud.FormatDashboardBlock());

        if (populationUIText != null)
        {
            int cityPop = PopulationSync.SumSynodPopulation();
            string popWarning = MatchController.Instance?.FormatPopulationWarning() ?? "";
            string popLine = $"<b>Synod population {population}</b>" +
                             (cityPop != population ? $"  (cities total {cityPop})" : "");
            if (!string.IsNullOrEmpty(popWarning))
                popLine += $"\n{popWarning}";
            populationUIText.text = TmpTextSanitizer.Sanitize(
                $"{factionLine}\n" +
                $"{churchYearLine}\n" +
                popLine + "\n" +
                $"{synodStatus}");
        }
        if (adherenceUIText != null)
        {
            adherenceUIText.text = TmpTextSanitizer.Sanitize(
                $"Confessional Adherence  {confessionalAdherence:F1}%  |  " +
                $"<color=#99AABB>win paths in <b>Y</b> brief</color>");
        }
        if (manuscriptUIText != null)
        {
            int legacyCount = SynodLegacyManager.Instance?.ActiveSlots.Count ?? 0;
            string legacyHint = legacyCount > 0
                ? $"  |  <color=#99AABB>{legacyCount} legacy</color>"
                : "";
            string potency = ConfessionResearchManager.Instance != null
                ? ConfessionResearchManager.Instance.AdherencePotencyLabel()
                : "";
            string fameWitness = MatchNarrativeChronology.Instance != null &&
                                 MatchNarrativeChronology.Instance.IsEventResolved("formula")
                ? confessionalFame >= 100
                    ? "  |  <color=#99AABB>witness near fame win</color>"
                    : "  |  <color=#99AABB>Formula bound — fame path open</color>"
                : "";
            manuscriptUIText.text = TmpTextSanitizer.Sanitize(
                $"Scripture Manuscripts  {scriptureManuscripts}  |  Catechisms  {boundCatechisms}\n" +
                $"Confessional Fame  {confessionalFame}{legacyHint}{fameWitness}  |  " +
                $"<color=#99AABB><b>Y</b> synod brief</color>\n" +
                potency);
        }
        if (waltherDashboardUIText != null)
        {
            string crisis = CrisisManager.Instance?.FormatCrisisLine();
            if (string.IsNullOrEmpty(crisis))
                crisis = PastoralBriefingManager.Instance?.FormatStatusLine();
            if (string.IsNullOrEmpty(crisis))
                crisis = UnionStrifeManager.FormatStatusLine();
            if (string.IsNullOrEmpty(crisis))
                crisis = FormatWaltherCrisisWarning();
            waltherDashboardUIText.text = TmpTextSanitizer.Sanitize(
                "Walther Dialectic  (T tech  |  C city  |  Y brief  |  H fortify  |  J skip unit)\n" +
                $"  Civic Restraint (Law)  {civicRestraint:F0}%\n" +
                $"  Spiritual Comfort (Gospel)  {spiritualComfort:F0}%" +
                (string.IsNullOrEmpty(crisis) ? "" : $"\n  {crisis}"));

            string tier2 = Tier2EmphasisManager.Instance?.FormatStatusLine();
            if (!string.IsNullOrEmpty(tier2))
                waltherDashboardUIText.text = TmpTextSanitizer.Sanitize(waltherDashboardUIText.text + $"\n  {tier2}");

            string synodical = SynodicalEmphasisManager.Instance?.FormatStatusLine();
            if (!string.IsNullOrEmpty(synodical))
                waltherDashboardUIText.text = TmpTextSanitizer.Sanitize(waltherDashboardUIText.text + $"\n  {synodical}");
        }

        GameHUD.Instance?.Relayout();
    }

    void ResolveDashboardRefs()
    {
        if (populationUIText != null && adherenceUIText != null &&
            manuscriptUIText != null && waltherDashboardUIText != null &&
            queueReviewUIText != null)
            return;

        GameHUD.Instance?.ResolveReferences();
    }

    public void ProcessTurnTick()
    {
        if (MatchController.Instance != null && MatchController.Instance.IsMatchOver)
            return;
        if (PlayerUnitCycle.Instance != null)
            PlayerUnitCycle.Instance.TryEndTurnOrCycleNext();
        else if (TurnManager.Instance != null && TurnManager.Instance.IsPlayerTurn)
        {
            if (EndTurnPhaseController.Instance != null)
                EndTurnPhaseController.Instance.TryBeginPhasedEndTurn();
            else
                TurnManager.Instance.EndTurn();
        }
    }

    public void RunGrowthPhase()
    {
        CityGrowthManager.Instance?.ProcessGrowthFoodPhase(FactionId.LutheranSynod);
        CityGrowthManager.Instance?.TickCooldowns();
        PopulationSync.SyncPlayerFactionFromCities();
        RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshCityYield();
    }

    public void RunMigrationPhase()
    {
        CityGrowthManager.Instance?.ProcessMigrationPhase(FactionId.LutheranSynod, offerDistricts: true);
        PopulationSync.SyncPlayerFactionFromCities();
        RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshCityYield();
    }

    public void RunProductionPhase()
    {
        CityManager.Instance?.AdvancePlayerCities();
        CityManager.Instance?.AdvanceCityCulture();
        CityManager.Instance?.CollectWorkedTileManuscripts();
        CityManager.Instance?.CollectHamletTribute();
        SynodTradeSystem.ProcessEndTurn(SynodPlayerId.Player1);
        PopulationSync.SyncPlayerFactionFromCities();
        RefreshDashboard();
        ConfessionTechPanel.Instance?.Refresh();
        CityScreenPanel.Instance?.Refresh();
        TerrainInfoPanel.Instance?.RefreshCityYield();
    }

    public void RunConfessionalPhase()
    {
        ApplyConfessionalTurnLogic();
        ConfessionResearchManager.Instance?.AdvanceTurn();
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        CityLoyaltySystem.ProcessEndTurnOccupation(FactionId.LutheranSynod);
        CityLoyaltySystem.ProcessEndTurnRecovery(FactionId.LutheranSynod, SynodPlayerId.Player1);
        ChaplainSpecialty.ProcessEndTurn(FactionId.LutheranSynod);
        EpiscopalOversight.ProcessEndTurn(FactionId.LutheranSynod);
        CrisisManager.Instance?.OnPlayerTurnEnded();
        UnionStrifeManager.ProcessPlayerEndTurn();
        MatchController.Instance?.OnPlayerTurnEnded();
        SynodDiplomacyManager.Instance?.ProcessTurnEnd();
        TurnPhaseBanner.Instance?.Refresh();
        RefreshDashboard();
        ConfessionTechPanel.Instance?.Refresh();
        CityScreenPanel.Instance?.Refresh();
        TerrainInfoPanel.Instance?.RefreshMissionaryTile();
        TerrainInfoPanel.Instance?.RefreshCityYield();
    }

    public void OnPlayerTurnEnded()
    {
        RunGrowthPhase();
        RunMigrationPhase();
        RunProductionPhase();
        RunConfessionalPhase();
        CityMilitia.ProcessSynodPlayerEndTurn(SynodPlayerId.Player1);
        UnitRecovery.ProcessPlayerEndTurn();
    }

    void ApplyConfessionalTurnLogic()
    {
        int turn = TurnManager.Instance ? TurnManager.Instance.TurnNumber : 1;
        var mods = Modifiers;

        string activeTerrainType = "SETTLEMENT";
        TerrainType missionaryTerrain = TerrainType.Pasture;
        var fieldUnit = GetFieldSynodUnit();
        if (HexGridMap.Instance != null && fieldUnit != null &&
            HexGridMap.Instance.TryGetTile(fieldUnit.HexPosition, out var missionaryTile))
        {
            missionaryTerrain = missionaryTile.Terrain;
            activeTerrainType = HexGridMap.GameplayTerrainCategory(missionaryTerrain).ToUpperInvariant();
        }

        float drift = mods.LawGospelDriftMultiplier;
        float legalismDrift = mods.LegalismDriftMultiplier;
        float lawDelta = (Random.Range(-4f, 4f) * drift * legalismDrift + 0.35f) * mods.CivicRestraintGrowthMultiplier;
        civicRestraint = Mathf.Clamp(civicRestraint + lawDelta, 0f, 100f);
        spiritualComfort = Mathf.Clamp(
            spiritualComfort + Random.Range(-3f, 5f) * drift + mods.SpiritualComfortTurnBonus + 0.25f, 0f, 100f);

        if (CityManager.Instance?.HasAnyPlayerBuilding(CityBuildId.BuildOrphanage) == true)
            spiritualComfort = Mathf.Clamp(spiritualComfort + 2f, 0f, 100f);

        float randomDecay = Random.Range(3f, 7f) * mods.AdherenceDecayMultiplier + 0.75f;
        int randomPopulationChange = Random.Range(0, 3) + mods.PopulationGrowthBonus;
        int terrainManuscriptGain = 0;
        string terrainNarrativeLog;

        if (missionaryTerrain == TerrainType.Pasture || missionaryTerrain == TerrainType.Shore)
        {
            randomDecay -= 2f;
            randomDecay *= mods.SettlementAdherenceDecayMultiplier;
            if (randomDecay < 0f) randomDecay = 0f;
            terrainManuscriptGain = mods.SettlementManuscriptBonus;
            if (missionaryTerrain == TerrainType.Shore)
                terrainManuscriptGain += 1;
            scriptureManuscripts += terrainManuscriptGain;
            randomPopulationChange += mods.SettlementPopulationBonus;
            terrainNarrativeLog = missionaryTerrain == TerrainType.Shore
                ? "Coastal shore: trade scrolls and steady parish life."
                : "Settlement comfort: chorales and art sustain the congregation.";
        }
        else if (!TerrainRules.IsWater(missionaryTerrain))
        {
            randomDecay += 3f * mods.AdherenceDecayMultiplier;
            terrainManuscriptGain = 1 + mods.WildernessManuscriptBonus;
            scriptureManuscripts += terrainManuscriptGain;
            randomPopulationChange -= 1;
            terrainNarrativeLog =
                $"Wilderness hardship on {HexGridMap.TerrainDisplayName(missionaryTerrain)}: manuscripts found, extra doctrinal drift.";
        }
        else
        {
            terrainNarrativeLog = "Waters unfit for encampment.";
        }

        string manuscriptNote = terrainManuscriptGain > 0
            ? $" (+{terrainManuscriptGain} mss → {scriptureManuscripts} held)"
            : "";
        string fieldUnitNote = fieldUnit != null
            ? $" [{Unit.TypeDisplayName(fieldUnit.Type)} on {HexGridMap.TerrainDisplayName(missionaryTerrain)}]"
            : "";

        if (terrainManuscriptGain > 0)
        {
            Debug.Log(
                $"Turn {turn}: Field encampment{fieldUnitNote} yielded +{terrainManuscriptGain} manuscripts " +
                $"({scriptureManuscripts} held).");
        }

        confessionalAdherence = Mathf.Clamp(confessionalAdherence - randomDecay, EffectiveMinAdherenceFloor, 100f);
        if (randomPopulationChange != 0)
        {
            if (randomPopulationChange > 0 && !ConfessionalPopulationGrowthAllowed())
                randomPopulationChange = 0;
            if (randomPopulationChange != 0)
                PopulationSync.ApplyDeltaToPrimaryCity(randomPopulationChange);
        }

        if (civicRestraint > 68f && spiritualComfort < 45f)
        {
            if (mods.LegalismGuard)
            {
                civicRestraint = 62f;
                spiritualComfort = Mathf.Clamp(spiritualComfort + 10f, 0f, 100f);
                CrisisManager.Instance?.HandleLegalismCrisis(hadGuard: true);
                Debug.LogWarning($"Turn {turn}: Gerhard's Loci checked legalistic preaching.");
                return;
            }

            if (CrisisManager.Instance == null || CrisisManager.Instance.ActiveCrisis == CrisisType.None)
                CrisisManager.Instance?.HandleLegalismCrisis(hadGuard: false);

            return;
        }

        if (spiritualComfort > 62f && confessionalAdherence < 68f)
        {
            if (mods.AntinomianGuard)
            {
                confessionalAdherence = Mathf.Clamp(confessionalAdherence + 8f, 0f, 100f);
                spiritualComfort = 55f;
                CrisisManager.Instance?.HandleAntinomianCrisis(hadGuard: true);
                Debug.LogWarning($"Turn {turn}: Formula emphasis checked antinomian drift.");
                return;
            }

            if (CrisisManager.Instance == null || CrisisManager.Instance.ActiveCrisis == CrisisType.None)
                CrisisManager.Instance?.HandleAntinomianCrisis(hadGuard: false);

            return;
        }

        Debug.Log($"Turn {turn}: {terrainNarrativeLog}{manuscriptNote}{fieldUnitNote} Pop {population} | Adherence {confessionalAdherence:F1}%");
        PopulationSync.SyncPlayerFactionFromCities();
    }

    void PreachPureWord()
    {
        var preacher = GetPreachUnit();
        if (preacher == null) return;

        if (preacher.Type == UnitType.Cantor)
        {
            PreachCantorHymn(preacher);
            return;
        }

        bool nomadicPreach = preacher.CanNomadicPreach;
        bool freePreach = preacher.Type is UnitType.Chaplain or UnitType.Pastor or UnitType.Bishop
            or UnitType.Archbishop or UnitType.Deaconess;
        bool useCatechism = !freePreach && boundCatechisms > 0;
        if (freePreach)
        {
            if (preacher.HasPreached) return;
        }
        else if (!useCatechism && scriptureManuscripts <= 0)
        {
            return;
        }

        float preachBonus = preacher.Type switch
        {
            UnitType.Chaplain => ChaplainSpecialty.GetPreachAdherenceBonus(preacher) + Modifiers.PreachAdherenceBonus * 0.35f,
            UnitType.Pastor => 2f + Modifiers.PreachAdherenceBonus * 0.35f,
            UnitType.Bishop => 3f + Modifiers.PreachAdherenceBonus * 0.4f,
            UnitType.Archbishop => 4f + Modifiers.PreachAdherenceBonus * 0.45f,
            UnitType.Deaconess => 1f + Modifiers.PreachAdherenceBonus * 0.25f,
            UnitType.Settler => Modifiers.PreachAdherenceBonus * 0.45f + 0.5f,
            UnitType.Missionary => Modifiers.PreachAdherenceBonus * 0.55f,
            _ => Modifiers.PreachAdherenceBonus * 0.5f
        };

        float parishBonus = ClergyRoster.GetParishPreachBonus(preacher);
        if (parishBonus > 0f)
            preachBonus += parishBonus * 0.5f;

        float oversightBonus = EpiscopalOversight.GetPassivePreachBonus(preacher);
        if (oversightBonus > 0f)
            preachBonus += oversightBonus * 0.5f;

        if (useCatechism)
        {
            boundCatechisms -= 1;
            preachBonus += 2f;
        }
        else if (!freePreach)
            scriptureManuscripts -= 1;

        preachBonus = ScalePreachAdherenceGain(preachBonus, confessionalAdherence);
        confessionalAdherence = Mathf.Clamp(confessionalAdherence + preachBonus, EffectiveMinAdherenceFloor, 100f);
        float comfortBonus = Modifiers.PreachSpiritualComfortBonus + (preacher.Type == UnitType.Deaconess ? 3f : 0f);
        spiritualComfort = Mathf.Clamp(spiritualComfort + comfortBonus, 0f, 100f);
        civicRestraint = Mathf.Clamp(civicRestraint + 5f, 0f, 100f);

        if (freePreach)
        {
            preacher.MarkPreached();
            string label = preacher.Type switch
            {
                UnitType.Deaconess => "Deaconess served Word and mercy",
                UnitType.Pastor => "Pastor preached",
                UnitType.Bishop => "Bishop preached",
                UnitType.Archbishop => "Archbishop preached for the synod",
                UnitType.Chaplain => $"Chaplain preached ({ChaplainSpecialty.FormatAssignment(preacher)})",
                _ => "Chaplain preached"
            };
            if (parishBonus > 0f)
                label += $" (+{parishBonus:F0} parish)";
            if (oversightBonus > 0f)
                label += $" (+{oversightBonus:F0} oversight)";
            Debug.Log($"Spacebar: {label} (+{preachBonus:F0} adherence, no manuscript cost).");
        }
        else if (Random.value < Modifiers.PreachManuscriptRefundChance)
        {
            scriptureManuscripts += 1;
            Debug.Log("Spacebar: Thesis applied  -  Pieper's dogmatics preserved a manuscript.");
        }
        else if (useCatechism)
        {
            Debug.Log($"Spacebar: Catechism preached (+{preachBonus:F0} adherence).");
        }
        else if (nomadicPreach)
        {
            Debug.Log($"Spacebar: Settler preached the pure Word (+{preachBonus:F0} adherence).");
        }
        else
        {
            Debug.Log($"Spacebar: Thesis applied (+{preachBonus:F0} adherence).");
        }

        if (nomadicPreach)
            NomadicFoundingGate.MarkPreachCompleted();

        if (!freePreach && preacher.CanPreach)
            preacher.MarkPreached();

        AddFame(1);
        CityManager.Instance?.TryPreachCityAt(preacher, preacher.HexPosition);

        RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
        TurnPhaseBanner.Instance?.Refresh();
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
    }

    void PreachCantorHymn(Unit cantor)
    {
        if (!cantor.CanLeadHymn || cantor.HasPreached) return;

        float comfort = 10f + Modifiers.PreachSpiritualComfortBonus * 0.5f + Modifiers.CantorComfortBonus
            + EpiscopalOversight.GetPassiveHymnComfortBonus(cantor);
        spiritualComfort = Mathf.Clamp(spiritualComfort + comfort, 0f, 100f);
        confessionalAdherence = Mathf.Clamp(confessionalAdherence + 3f, EffectiveMinAdherenceFloor, 100f);
        civicRestraint = Mathf.Clamp(civicRestraint - 4f, 0f, 100f);

        cantor.MarkPreached();
        AddFame(1);
        CityManager.Instance?.TryPreachCityAt(cantor, cantor.HexPosition);
        Debug.Log($"Spacebar: Cantor led hymn (+{comfort:F0} comfort, +3 adherence).");

        RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
        TurnPhaseBanner.Instance?.Refresh();
    }

    Unit GetPreachUnit()
    {
        var selected = TurnManager.Instance?.SelectedUnit;
        if (selected != null && selected.Faction == FactionId.LutheranSynod &&
            (selected.CanPreach || selected.CanLeadHymn || selected.CanNomadicPreach))
            return selected;

        if (trackedUnit != null && trackedUnit.IsAlive &&
            (trackedUnit.CanPreach || trackedUnit.CanLeadHymn || trackedUnit.CanNomadicPreach))
            return trackedUnit;

        return null;
    }

    static float ScalePreachAdherenceGain(float bonus, float currentAdherence)
    {
        if (bonus <= 0f)
            return bonus;

        float scaled = bonus * 0.55f;

        if (currentAdherence >= 95f)
            scaled *= 0.25f;
        else if (currentAdherence >= 90f)
            scaled *= 0.4f;
        else if (currentAdherence >= 80f)
            scaled *= 0.65f;

        return Mathf.Max(0.5f, scaled);
    }

    string FormatLegacyFameHint()
    {
        if (SynodLegacyManager.Instance == null)
            return "";

        if (SynodLegacyManager.Instance.ActiveSlots.Count > 0)
            return "";

        if (confessionalFame >= 25)
            return "";

        return "25 fame: Confessional Witness  |  55 fame: Synod Repute";
    }

    public string FormatSynodBriefContent()
    {
        var sections = new System.Collections.Generic.List<string>();

        sections.Add("");
        sections.Add("<color=#DDCC88><b>VICTORY & DEFEAT</b></color>");
        if (MatchController.Instance != null)
            sections.Add(MatchController.Instance.FormatVictoryBriefSection());
        else
            sections.Add("<size=13>Victory tracking unavailable.</size>");

        if (NomadicFoundingGate.IsNomadicPhase)
        {
            sections.Add("");
            sections.Add("<color=#DDCC88><b>NOMADIC FOUNDING</b></color>");
            sections.Add(NomadicFoundingGate.FormatBriefSection());
        }

        sections.Add("");
        sections.Add("<color=#DDCC88><b>LEGACY TRAITS</b></color>");
        string legacy = SynodLegacyManager.Instance?.FormatLegacyLine();
        if (string.IsNullOrEmpty(legacy) || legacy.Contains("none yet"))
        {
            sections.Add("<size=13>No active legacy traits yet.</size>");
            sections.Add($"<size=12><color=#AABBCC>{FormatLegacyFameHint()}</color></size>");
            sections.Add("<size=12><color=#AABBCC>Crisis traits (Gerhard, Concord, Crisis Survivor) unlock by surviving Walther crises.</color></size>");
        }
        else
            sections.Add(legacy);

        sections.Add("");
        sections.Add("<color=#DDCC88><b>CONFESSIONAL IDENTITY</b></color>");
        if (confessionalIdentity != ConfessionalIdentityId.None)
        {
            sections.Add(
                $"<b>{ConfessionalIdentityDatabase.DisplayName(confessionalIdentity)}</b>\n" +
                $"<size=12><color=#AABBCC><i>{ConfessionalIdentityDatabase.Description(confessionalIdentity)}</i></color></size>\n" +
                $"<size=13><color=#DDEEAA>{ConfessionalIdentityDatabase.FormatGameplayEffects(confessionalIdentity)}</color></size>");
        }
        else
            sections.Add("<size=13>Not chosen yet  -  pick when Wittenberg is founded (I to respec later).</size>");

        sections.Add("");
        sections.Add("<color=#DDCC88><b>RESEARCH ERA</b></color>");
        sections.Add(ArtEraVisualController.FormatEraLabel());

        sections.Add("");
        sections.Add("<color=#DDCC88><b>CITY YIELDS</b></color>");
        if (CityManager.Instance != null)
        {
            sections.Add(CityManager.Instance.HasPlayerCityProduction()
                ? CityManager.Instance.FormatPlayerCityYieldLine()
                : "<size=13>No city production yet.</size>");
        }

        sections.Add("");
        sections.Add("<color=#DDCC88><b>MILITARY WITNESS</b></color>");
        sections.Add(MatchHistory.Instance?.FormatBriefMilitaryWitnessLine()
                     ?? "<size=13>No combat logged yet.</size>");

        if (MatchHistory.Instance != null)
            sections.Add(MatchHistory.Instance.FormatEmphasisGateSummary());

        sections.Add("");
        sections.Add("<color=#DDCC88><b>TRADE & DIPLOMACY</b></color>");
        string trade = SynodTradeSystem.FormatNetworkSummary(SynodPlayerId.Player1);
        if (!string.IsNullOrEmpty(trade))
            sections.Add(trade);
        string rivals = SynodDiplomacyManager.Instance?.FormatBriefRivalSection();
        if (!string.IsNullOrEmpty(rivals))
            sections.Add(rivals);
        string diplomacy = SynodDiplomacyManager.Instance?.FormatSummaryLine();
        if (!string.IsNullOrEmpty(diplomacy) && string.IsNullOrEmpty(rivals))
            sections.Add(diplomacy);
        if (string.IsNullOrEmpty(trade) && string.IsNullOrEmpty(rivals) && string.IsNullOrEmpty(diplomacy))
            sections.Add("<size=13>No trade links or rival diplomacy yet.</size>");

        sections.Add("");
        sections.Add("<color=#DDCC88><b>CHURCH YEAR</b></color>");
        sections.Add(ChurchYearFlavor.FormatDashboardLine());
        sections.Add(
            "<size=12><color=#AABBCC>Feasts, festivals, and commemorations follow the Lutheran Service Book calendar " +
            "(LCMS Worship, historic 1-year dates). Each turn is about one synodical month from St. Andrew / Advent " +
            "(~12 turns per church year). <b>WATCH</b> turns mark the eight principal feasts of Christ " +
            "(LSB p. xi boldface) when they fall in that month.</color></size>");

        sections.Add("");
        sections.Add("<color=#DDCC88><b>WALTHER DIALECTIC</b></color>");
        string crisis = CrisisManager.Instance?.FormatCrisisLine();
        if (string.IsNullOrEmpty(crisis))
            crisis = PastoralBriefingManager.Instance?.FormatStatusLine();
        if (string.IsNullOrEmpty(crisis))
            crisis = UnionStrifeManager.FormatStatusLine();
        if (string.IsNullOrEmpty(crisis))
            crisis = FormatWaltherCrisisWarning();
        sections.Add($"Civic Restraint (Law)  {civicRestraint:F0}%  |  Spiritual Comfort (Gospel)  {spiritualComfort:F0}%");
        if (!string.IsNullOrEmpty(crisis))
            sections.Add(crisis);
        sections.Add("<size=12><color=#AABBCC>High Law + low Gospel risks legalism; high Gospel + low adherence risks antinomian schism.</color></size>");
        sections.Add("<size=12><color=#AABBCC>Pastoral briefings (Luther, Walther, Gerhard, etc.) appear every few turns when Law and Gospel drift — choose a response to steer the dialectic.</color></size>");
        if (UnionStrifeManager.IsSaturated)
        {
            sections.Add(
                "<size=12><color=#EEAA66>At the schism cap, church-year witnesses deepen overflow and union-strife cards — " +
                "three sisters in error already stand; the calendar teaches fidelity without a fourth capital.</color></size>");
        }
        string emphasis = SynodicalEmphasisManager.Instance?.FormatStatusLine();
        if (!string.IsNullOrEmpty(emphasis))
            sections.Add(emphasis);
        string tier2 = Tier2EmphasisManager.Instance?.FormatStatusLine();
        if (!string.IsNullOrEmpty(tier2))
            sections.Add(tier2);

        return string.Join("\n", sections);
    }

    string FormatSynodStatusLine()
    {
        if (SchismaticBlocRegistry.Instance != null && SchismaticBlocRegistry.Instance.HasAnySchism)
            return SchismaticBlocRegistry.Instance.FormatStatusLine();

        if (CityManager.Instance?.GetPrimaryPlayerCity() == null)
            return "<color=#FFDD88>Wandering synod  -  found Wittenberg to settle</color>";

        string diplomacy = SynodDiplomacyManager.Instance?.FormatSummaryLine();
        if (!string.IsNullOrEmpty(diplomacy))
            return diplomacy;

        return "<color=#88AAFF>Synod united</color>";
    }

    string FormatWaltherCrisisWarning()
    {
        if (CrisisManager.Instance != null && CrisisManager.Instance.IsAwaitingPlayerChoice)
            return "";

        if (PastoralBriefingManager.Instance != null && PastoralBriefingManager.Instance.IsAwaitingPlayerChoice)
            return "";

        if (UnionStrifeManager.IsSaturated)
            return "";

        var mods = Modifiers;

        if (civicRestraint > 62f && spiritualComfort < 52f && !mods.LegalismGuard)
            return "<color=#FFAA66><b>Warning:</b> legalism risk  -  Law high, Gospel low (schism)</color>";

        if (spiritualComfort > 58f && confessionalAdherence < 72f && !mods.AntinomianGuard)
            return "<color=#FFAA66><b>Warning:</b> antinomian drift — take Formula emphasis or preach (schism risk)</color>";

        if (SchismaticBlocRegistry.Instance != null &&
            SchismaticBlocRegistry.Instance.HasAnySchism &&
            confessionalAdherence > 70f &&
            civicRestraint > 75f &&
            spiritualComfort > 75f)
            return "<color=#DDAA66><b>Note:</b> outward peace — dissent synods still press the land</color>";

        if (confessionalAdherence <= 58f)
            return "<color=#FFAA66><b>Warning:</b> adherence falling  -  dissent may split the synod</color>";

        return "";
    }
}
