/// <summary>
/// Maps confession documents to tier-2 emphasis lines and scales document bonuses
/// when the matching emphasis has not been adopted.
/// </summary>
public static class EmphasisDocumentRules
{
    public const float UnmatchedDocumentPotency = 0.5f;
    public const int WildernessManuscriptCap = 3;

    public static bool HasMatchingEmphasis(ConfessionTechId techId)
    {
        var tier2 = Tier2EmphasisManager.Instance;
        if (tier2 == null)
            return false;

        if (TryGetConfessionalEmphasis(techId, out var confessional))
            return tier2.OwnsConfessionalEmphasis(confessional);

        if (TryGetCultureEmphasis(techId, out var culture))
            return tier2.OwnsCultureEmphasis(culture);

        if (TryGetSynodicalEmphasis(techId, out var synodical))
        {
            var syn = SynodicalEmphasisManager.Instance;
            return syn != null && syn.OwnsSynodicalEmphasis(synodical);
        }

        return true;
    }

    public static bool IsEmphasisLinkedDocument(ConfessionTechId techId) =>
        TryGetConfessionalEmphasis(techId, out _) ||
        TryGetCultureEmphasis(techId, out _) ||
        TryGetSynodicalEmphasis(techId, out _);

    public static float DocumentPotencyFor(ConfessionTechId techId)
    {
        if (!IsEmphasisLinkedDocument(techId))
            return 1f;

        return HasMatchingEmphasis(techId) ? 1f : UnmatchedDocumentPotency;
    }

    public static float CombinedDocumentPotency(ConfessionTechId techId)
    {
        float potency = DocumentPotencyFor(techId);
        if (ConfessionResearchManager.Instance != null)
            potency *= ConfessionResearchManager.Instance.ForkPotencyFor(techId);
        return potency;
    }

    public static ConfessionModifiers ApplyDocumentPotency(ConfessionModifiers raw, float documentPotency)
    {
        if (documentPotency >= 1f)
            return raw;

        var scaled = ConfessionModifiers.Scaled(raw, documentPotency);
        scaled.AntinomianGuard = raw.AntinomianGuard;
        scaled.LegalismGuard = raw.LegalismGuard;
        return scaled;
    }

    public static void CapWildernessManuscriptBonus(ConfessionModifiers mods)
    {
        if (mods.WildernessManuscriptBonus > WildernessManuscriptCap)
            mods.WildernessManuscriptBonus = WildernessManuscriptCap;
    }

    public static string DocumentEmphasisHint(ConfessionTechId techId)
    {
        if (TryGetConfessionalEmphasis(techId, out var confessional))
        {
            string label = confessional switch
            {
                ConfessionalEmphasisChoice.InternalFormula => "Formula (internal)",
                ConfessionalEmphasisChoice.AugsburgPublic => "Augsburg (public)",
                ConfessionalEmphasisChoice.SmalcaldPolemic => "Smalcald (polemic)",
                _ => "matching confessional emphasis"
            };
            return ConfessionalUiVocabulary.FormatDocumentWithoutEmphasis(label);
        }

        if (TryGetCultureEmphasis(techId, out var culture))
        {
            string label = culture switch
            {
                ConfessionsCultureEmphasisChoice.ChoraleLiturgy => "Chorale liturgy",
                ConfessionsCultureEmphasisChoice.GerhardtCross => "Gerhardt cross",
                _ => "matching culture emphasis"
            };
            return ConfessionalUiVocabulary.FormatDocumentWithoutEmphasis(label);
        }

        if (TryGetSynodicalEmphasis(techId, out var synodical))
        {
            string label = synodical switch
            {
                SynodicalEmphasisId.WaltherPastoral => "Walther pastoral",
                SynodicalEmphasisId.PieperDogmatic => "Pieper dogmatic",
                _ => "matching synodical emphasis"
            };
            return ConfessionalUiVocabulary.FormatDocumentWithoutEmphasis(label);
        }

        return "";
    }

    static bool TryGetConfessionalEmphasis(ConfessionTechId techId, out ConfessionalEmphasisChoice choice)
    {
        choice = techId switch
        {
            ConfessionTechId.FormulaOfConcord => ConfessionalEmphasisChoice.InternalFormula,
            ConfessionTechId.AugsburgConfession => ConfessionalEmphasisChoice.AugsburgPublic,
            ConfessionTechId.SmalcaldArticles => ConfessionalEmphasisChoice.SmalcaldPolemic,
            _ => ConfessionalEmphasisChoice.None
        };
        return choice != ConfessionalEmphasisChoice.None;
    }

    static bool TryGetCultureEmphasis(ConfessionTechId techId, out ConfessionsCultureEmphasisChoice choice)
    {
        choice = techId switch
        {
            ConfessionTechId.ChoraleTradition => ConfessionsCultureEmphasisChoice.ChoraleLiturgy,
            ConfessionTechId.PaulGerhardt => ConfessionsCultureEmphasisChoice.GerhardtCross,
            _ => ConfessionsCultureEmphasisChoice.None
        };
        return choice != ConfessionsCultureEmphasisChoice.None;
    }

    static bool TryGetSynodicalEmphasis(ConfessionTechId techId, out SynodicalEmphasisId emphasis)
    {
        emphasis = techId switch
        {
            ConfessionTechId.WaltherPastoralTheology => SynodicalEmphasisId.WaltherPastoral,
            ConfessionTechId.FrancisPieper => SynodicalEmphasisId.PieperDogmatic,
            _ => SynodicalEmphasisId.None
        };
        return emphasis != SynodicalEmphasisId.None;
    }
}
