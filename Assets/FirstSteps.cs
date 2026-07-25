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

    public const float MaxCrisisAdherenceFloor = 32f;

    public float EffectiveMinAdherenceFloor =>
        Mathf.Min(Modifiers.MinAdherenceFloor, MaxCrisisAdherenceFloor);

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

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        RefreshDashboard();
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (!TurnManager.Instance || !TurnManager.Instance.IsPlayerTurn) return;

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
        if (galley == null || galley.Type != UnitType.CoastalGalley)
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

        if (!AmphibiousTransport.TryDisembark(galley, best))
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

        // District founding is organic-only (growth offers)  -  colonist F disabled.
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

    public void RefreshDashboard()
    {
        ResolveDashboardRefs();

        int turn = TurnManager.Instance ? TurnManager.Instance.TurnNumber : 1;
        string synodStatus = FormatSynodStatusLine();
        string factionLine = TurnManager.Instance
            ? $"Lutheran Synod  |  Turn {turn}"
            : $"Turn {turn}";

        string researchLine = ConfessionResearchManager.Instance != null
            ? ConfessionResearchManager.Instance.ActiveResearchLabel()
            : "idle";

        string cityQueueLine = CityManager.Instance != null
            ? CityManager.Instance.FormatPlayerProductionQueueLine()
            : "idle";

        if (populationUIText != null)
        {
            string cityYield = CityManager.Instance != null
                ? CityManager.Instance.FormatPlayerCityYieldLine()
                : "";
            populationUIText.text = TmpTextSanitizer.Sanitize(
                $"{factionLine}\n" +
                $"{synodStatus}  |  Population {population}\n" +
                $"<b>Production</b>  {cityQueueLine}\n" +
                cityYield);
        }
        if (adherenceUIText != null)
        {
            string victoryHint = MatchController.Instance != null
                ? MatchController.Instance.AdherenceVictoryProgress()
                : "";
            adherenceUIText.text = TmpTextSanitizer.Sanitize(string.IsNullOrEmpty(victoryHint)
                ? $"Confessional Adherence  {confessionalAdherence:F1}%"
                : $"Confessional Adherence  {confessionalAdherence:F1}%  |  {victoryHint}");
        }
        if (manuscriptUIText != null)
        {
            string potency = ConfessionResearchManager.Instance != null
                ? ConfessionResearchManager.Instance.AdherencePotencyLabel()
                : "";
            manuscriptUIText.text = TmpTextSanitizer.Sanitize(
                $"Scripture Manuscripts  {scriptureManuscripts}  |  Catechisms  {boundCatechisms}\n" +
                $"Confessional Fame  {confessionalFame}" +
                (confessionalIdentity != ConfessionalIdentityId.None
                    ? $"  |  {ConfessionalIdentityDatabase.DisplayName(confessionalIdentity)}"
                    : "") +
                $"\n<b>Research</b>  {researchLine}\n" +
                ArtEraVisualController.FormatEraLabel() + "\n" +
                potency);
            string legacy = SynodLegacyManager.Instance?.FormatLegacyLine() ?? "";
            if (!string.IsNullOrEmpty(legacy))
                manuscriptUIText.text += TmpTextSanitizer.Sanitize($"\n{legacy}");
        }
        if (waltherDashboardUIText != null)
        {
            string crisis = CrisisManager.Instance?.FormatCrisisLine();
            if (string.IsNullOrEmpty(crisis))
                crisis = FormatWaltherCrisisWarning();
            waltherDashboardUIText.text = TmpTextSanitizer.Sanitize(
                "Walther Dialectic  (T tech  |  C city  |  F found capital)\n" +
                $"  Civic Restraint (Law)  {civicRestraint:F0}%\n" +
                $"  Spiritual Comfort (Gospel)  {spiritualComfort:F0}%" +
                (string.IsNullOrEmpty(crisis) ? "" : $"\n  {crisis}"));
        }

        GameHUD.Instance?.Relayout();
    }

    void ResolveDashboardRefs()
    {
        if (populationUIText != null && adherenceUIText != null &&
            manuscriptUIText != null && waltherDashboardUIText != null)
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
            if (EndTurnPhaseController.Instance != null &&
                EndTurnPhaseController.Instance.TryBeginPhasedEndTurn())
                return;
            TurnManager.Instance.EndTurn();
        }
    }

    public void RunGrowthPhase()
    {
        CityGrowthManager.Instance?.ProcessGrowthFoodPhase(FactionId.LutheranSynod);
        CityGrowthManager.Instance?.TickCooldowns();
        RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshCityYield();
    }

    public void RunMigrationPhase()
    {
        CityGrowthManager.Instance?.ProcessMigrationPhase(FactionId.LutheranSynod, offerDistricts: true);
        RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshCityYield();
    }

    public void RunProductionPhase()
    {
        CityManager.Instance?.AdvancePlayerCities();
        CityManager.Instance?.AdvanceCityCulture();
        CityManager.Instance?.CollectWorkedTileManuscripts();
        CityManager.Instance?.CollectHamletTribute();
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
        ChaplainSpecialty.ProcessEndTurn(FactionId.LutheranSynod);
        EpiscopalOversight.ProcessEndTurn(FactionId.LutheranSynod);
        CrisisManager.Instance?.OnPlayerTurnEnded();
        MatchController.Instance?.OnPlayerTurnEnded();
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
    }

    void ApplyConfessionalTurnLogic()
    {
        int turn = TurnManager.Instance ? TurnManager.Instance.TurnNumber : 1;
        var mods = Modifiers;

        string activeTerrainType = "SETTLEMENT";
        TerrainType missionaryTerrain = TerrainType.Pasture;
        if (HexGridMap.Instance != null && trackedUnit != null &&
            HexGridMap.Instance.TryGetTile(trackedUnit.HexPosition, out var missionaryTile))
        {
            missionaryTerrain = missionaryTile.Terrain;
            activeTerrainType = HexGridMap.GameplayTerrainCategory(missionaryTerrain).ToUpperInvariant();
        }

        float drift = mods.LawGospelDriftMultiplier;
        float legalismDrift = mods.LegalismDriftMultiplier;
        civicRestraint = Mathf.Clamp(civicRestraint + Random.Range(-4f, 4f) * drift * legalismDrift + 0.35f, 0f, 100f);
        spiritualComfort = Mathf.Clamp(
            spiritualComfort + Random.Range(-3f, 5f) * drift + mods.SpiritualComfortTurnBonus + 0.25f, 0f, 100f);

        if (CityManager.Instance?.HasAnyPlayerBuilding(CityBuildId.BuildOrphanage) == true)
            spiritualComfort = Mathf.Clamp(spiritualComfort + 2f, 0f, 100f);

        float randomDecay = Random.Range(2f, 6f) * mods.AdherenceDecayMultiplier + 0.5f;
        int randomPopulationChange = Random.Range(0, 3) + mods.PopulationGrowthBonus;
        string terrainNarrativeLog;

        if (missionaryTerrain == TerrainType.Pasture || missionaryTerrain == TerrainType.Shore)
        {
            randomDecay -= 2f;
            randomDecay *= mods.SettlementAdherenceDecayMultiplier;
            if (randomDecay < 0f) randomDecay = 0f;
            scriptureManuscripts += mods.SettlementManuscriptBonus;
            if (missionaryTerrain == TerrainType.Shore)
                scriptureManuscripts += 1;
            randomPopulationChange += mods.SettlementPopulationBonus;
            terrainNarrativeLog = missionaryTerrain == TerrainType.Shore
                ? "Coastal shore: trade scrolls and steady parish life."
                : "Settlement comfort: chorales and art sustain the congregation.";
        }
        else if (!TerrainRules.IsWater(missionaryTerrain))
        {
            randomDecay += 3f * mods.AdherenceDecayMultiplier;
            scriptureManuscripts += 1 + mods.WildernessManuscriptBonus;
            randomPopulationChange -= 1;
            terrainNarrativeLog = "Wilderness hardship: manuscripts found, extra doctrinal drift.";
        }
        else
        {
            terrainNarrativeLog = "Waters unfit for encampment.";
        }

        confessionalAdherence = Mathf.Clamp(confessionalAdherence - randomDecay, EffectiveMinAdherenceFloor, 100f);
        population = Mathf.Max(0, population + randomPopulationChange);

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

            population = Mathf.Max(0, population - Random.Range(1, 3));
            CrisisManager.Instance?.HandleLegalismCrisis(hadGuard: false);
            Debug.LogWarning($"Turn {turn}: legalistic preaching shrank population to {population}");
            return;
        }

        if (spiritualComfort > 62f && confessionalAdherence < 68f)
        {
            if (mods.AntinomianGuard)
            {
                confessionalAdherence = Mathf.Clamp(confessionalAdherence + 8f, 0f, 100f);
                spiritualComfort = 55f;
                CrisisManager.Instance?.HandleAntinomianCrisis(hadGuard: true);
                Debug.LogWarning($"Turn {turn}: Formula of Concord checked antinomian drift.");
                return;
            }

            population = Mathf.Max(1, population - Mathf.Max(1, population / 4));
            confessionalAdherence = Mathf.Clamp(confessionalAdherence + 5f, EffectiveMinAdherenceFloor, 100f);
            spiritualComfort = 40f;
            CrisisManager.Instance?.HandleAntinomianCrisis(hadGuard: false);
            Debug.LogError($"Turn {turn}: antinomian fracture! Remaining pop: {population}");
            return;
        }

        Debug.Log($"Turn {turn}: {terrainNarrativeLog} Pop {population} | Adherence {confessionalAdherence:F1}%");
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
            UnitType.Chaplain => ChaplainSpecialty.GetPreachAdherenceBonus(preacher) + Modifiers.PreachAdherenceBonus * 0.5f,
            UnitType.Pastor => 4f + Modifiers.PreachAdherenceBonus * 0.5f,
            UnitType.Bishop => 5f + Modifiers.PreachAdherenceBonus * 0.55f,
            UnitType.Archbishop => 6f + Modifiers.PreachAdherenceBonus * 0.6f,
            UnitType.Deaconess => 2f + Modifiers.PreachAdherenceBonus * 0.35f,
            UnitType.Settler => Modifiers.PreachAdherenceBonus + 1f,
            _ => Modifiers.PreachAdherenceBonus
        };

        float parishBonus = ClergyRoster.GetParishPreachBonus(preacher);
        if (parishBonus > 0f)
            preachBonus += parishBonus;

        float oversightBonus = EpiscopalOversight.GetPassivePreachBonus(preacher);
        if (oversightBonus > 0f)
            preachBonus += oversightBonus;

        if (useCatechism)
        {
            boundCatechisms -= 1;
            preachBonus += 4f;
        }
        else if (!freePreach)
            scriptureManuscripts -= 1;

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

        AddFame(2);
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
        AddFame(2);
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

    string FormatSynodStatusLine()
    {
        if (SchismaticBlocRegistry.Instance != null && SchismaticBlocRegistry.Instance.HasAnySchism)
            return SchismaticBlocRegistry.Instance.FormatStatusLine();

        if (CityManager.Instance?.GetPrimaryPlayerCity() == null)
        {
            string progress = NomadicFoundingGate.FormatProgressLine();
            return progress ?? "<color=#FFDD88>Nomadic  -  preparing to found Wittenberg</color>";
        }

        return "<color=#88AAFF>Synod united</color>";
    }

    string FormatWaltherCrisisWarning()
    {
        if (CrisisManager.Instance != null && CrisisManager.Instance.IsAwaitingPlayerChoice)
            return "";

        var mods = Modifiers;

        if (civicRestraint > 62f && spiritualComfort < 52f && !mods.LegalismGuard)
            return "<color=#FFAA66><b>Warning:</b> legalism risk  -  Law high, Gospel low (schism)</color>";

        if (spiritualComfort > 58f && confessionalAdherence < 72f && !mods.AntinomianGuard)
            return "<color=#FFAA66><b>Warning:</b> antinomian drift  -  comfort without adherence (schism)</color>";

        if (confessionalAdherence <= 58f)
            return "<color=#FFAA66><b>Warning:</b> adherence falling  -  dissent may split the synod</color>";

        return "";
    }
}
