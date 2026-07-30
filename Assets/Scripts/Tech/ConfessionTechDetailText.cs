using System.Text;

/// <summary>
/// Shared tech detail copy for the confession tech panel (and offline length checks).
/// </summary>
public static class ConfessionTechDetailText
{
    /// <summary>
    /// Worst-case static detail for a tech (all optional flavor blocks that can appear),
    /// used to verify the sidebar must scroll for long entries.
    /// </summary>
    public static string BuildOfflinePreview(ConfessionTechId id)
    {
        var node = ConfessionTechDatabase.Get(id);
        var sb = new StringBuilder();
        sb.AppendLine($"<size=22><b>{node.Name}</b></size>");

        if (node.HasFigure)
            sb.AppendLine($"<color=#C9B896>{node.FigureName} ({node.Lifespan})</color>");

        sb.AppendLine();
        sb.AppendLine(node.Description);
        sb.AppendLine();
        sb.AppendLine($"<b>Effect</b>\n{node.EffectSummary}");

        string documentHint = EmphasisDocumentRules.DocumentEmphasisHint(id);
        if (!string.IsNullOrEmpty(documentHint))
        {
            sb.AppendLine();
            sb.AppendLine(
                $"<size=12><color=#AABBCC><i>Emphasis is how we live; confessions are what we bind.</i> " +
                $"{documentHint} Guards on documents stay full.</color></size>");
        }

        // Include branch / reception / study lines at full length so scroll stress-tests cover them.
        string advance = EraBranchRules.FormatAdvanceForkHint(id);
        if (!string.IsNullOrEmpty(advance))
        {
            sb.AppendLine();
            sb.AppendLine(advance);
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine(
                "<size=12><color=#AABBCC><i>Era path closed after another choice — " +
                "integration can reopen sibling study.</i></color></size>");
        }        sb.AppendLine();
        sb.AppendLine(
            $"<b>Cost</b>  {node.ManuscriptCost} manuscripts + 4 study colloquy, {node.TurnsToComplete} turns");
        sb.AppendLine(
            $"<size=12><color=#AABBCC><i>{ConfessionalUiVocabulary.FormatStudyColloquyCost(4)}. " +
            "Completing both era paths in this branch grants full reception.</i></color></size>");
        sb.AppendLine();
        sb.AppendLine(
            $"<size=12><color=#AABBCC><i>Current reception: {ConfessionalUiVocabulary.FormatEraForkPotencyLabel(0.5f)}.</i></color></size>");

        if (node.MinAdherence > 0f &&
            TechTreeRules.RequiresAdherence(TechTreeRules.CategoryFor(node.Id)))
        {
            sb.AppendLine($"<b>Adherence</b>  {node.MinAdherence:F0}%+ required (doctrine/culture track)");
        }

        if (TechTreeRules.CategoryFor(node.Id) == TechTreeCategory.Secular)
        {
            sb.AppendLine(
                $"<b>{TechTreeRules.DisplayName(TechTreeCategory.Secular)} track</b>  " +
                $"({TechTreeRules.FlavorSubtitle(TechTreeCategory.Secular)})  " +
                $"research allowed at any adherence; bonuses dormant ≤{ConfessionResearchManager.BonusPotencyThreshold:F0}%");
        }

        if (node.Prerequisites.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Requires</b>");
            foreach (var prereq in node.Prerequisites)
                sb.AppendLine($"* {ConfessionTechDatabase.Get(prereq).Name}");
        }

        AppendSpecialCaseHints(sb, id);

        sb.AppendLine();
        sb.AppendLine("Potency now: 100%");
        return sb.ToString();
    }

    public static void AppendSpecialCaseHints(StringBuilder sb, ConfessionTechId id)
    {
        if (id == ConfessionTechId.SynodicalEmphasis)
        {
            sb.AppendLine();
            sb.AppendLine(
                "<size=12><color=#AABBCC>Completing this tech opens a choice card: Walther (pastoral) or Pieper (dogmatic) " +
                "emphasis at full bonus. After Johann Gerhard, the other path can be taken for 4 mss as " +
                $"{ConfessionalUiVocabulary.SecondaryReception}.</color></size>");
        }

        if (id == ConfessionTechId.ConfessionalEmphasis)
        {
            sb.AppendLine();
            sb.AppendLine(
                "<size=12><color=#AABBCC>Opens a confessional emphasis card. Internal (Formula) is always available. " +
                "<b>Augsburg</b> appears after scout contact with a schismatic bloc; <b>Smalcald</b> after battle with one. " +
                "Large Catechism unlocks secondary paths; Mutual Conference unlocks integration (deepens secondary reception).</color></size>");
        }

        if (id == ConfessionTechId.ConfessionsCultureEmphasis)
        {
            sb.AppendLine();
            sb.AppendLine(
                "<size=12><color=#AABBCC>Opens a culture emphasis card. Chorale liturgy is always available. " +
                "Gerhardt cross-comfort appears only after your units have fought (any battle). " +
                "Chorale Tradition or Sacred Hymnody unlock secondary paths; CTCR Reports unlock integration.</color></size>");
        }

        if (id == ConfessionTechId.SynodicalGovernance)
        {
            sb.AppendLine();
            sb.AppendLine(
                "<size=12><color=#AABBCC>With primary + secondary confessional emphasis chosen, opens an integration colloquy " +
                $"(deepens secondary reception, tertiary emphasis, {ConfessionalUiVocabulary.FormatReopenEraForkSiblings()}; " +
                $"{EraBranchRules.ColloquyCostForTier(ConfessionTechDatabase.Get(Tier2EmphasisManager.ConfessionalIntegrationUnlockTech).Tier)} mss).</color></size>");
        }

        if (id == ConfessionTechId.CTCRReports)
        {
            sb.AppendLine();
            sb.AppendLine(
                "<size=12><color=#AABBCC>With primary + secondary culture emphasis chosen, opens an integration colloquy " +
                $"(deepens secondary reception, {ConfessionalUiVocabulary.FormatReopenEraForkSiblings()}; " +
                $"{EraBranchRules.ColloquyCostForTier(ConfessionTechDatabase.Get(Tier2EmphasisManager.CultureIntegrationUnlockTech).Tier)} mss).</color></size>");
        }
    }
}
