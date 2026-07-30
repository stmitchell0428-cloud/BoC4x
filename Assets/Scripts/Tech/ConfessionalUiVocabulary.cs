/// <summary>Player-facing confessional language for emphasis, documents, and era forks.</summary>
public static class ConfessionalUiVocabulary
{
    /// <summary>Integrated era-fork sibling — study without full institutional adoption.</summary>
    public const string PartialReception = "partial reception (half institutional strength)";

    /// <summary>Secondary or tertiary emphasis from colloquy — pastoral weight without primary binding.</summary>
    public const string SecondaryReception = "secondary reception (half pastoral weight)";

    /// <summary>Integrated fork after study colloquy at research start.</summary>
    public const string DeepenedReception = "deepened reception (three-quarter institutional strength)";

    public static string FormatIntegratedSiblingReopen(string integrationName) =>
        $"<b>{integrationName}</b> integration may reopen this path for {PartialReception}. " +
        "A study colloquy when research begins deepens reception; completing both era paths grants full reception.";

    public static string FormatEraPathClosed(string chosenName, string integrationName) =>
        $"Era path closed — committed to <b>{chosenName}</b>. {FormatIntegratedSiblingReopen(integrationName)}";

    public static string FormatEraForkChoice(string siblingName) =>
        $"<b>Era fork</b> — choosing this locks <b>{siblingName}</b>. Integration can reopen the deferred path later.";

    public static string FormatEraForkBadge(string siblingShortName) =>
        $"<color=#E8C878>Fork</color> <color=#AABBCC>vs {siblingShortName}</color>";

    public static string FormatIntegratedSiblingAvailable() =>
        $"Integrated sibling — {PartialReception}. Pay study colloquy when research begins for {DeepenedReception}.";

    public static string FormatIntegratedSiblingReady() =>
        $"partial reception — ready (study colloquy at start)";

    public static string FormatStudyColloquyCost(int manuscriptCost) =>
        $"{manuscriptCost} mss study colloquy → {DeepenedReception}";

    public static string FormatDocumentWithoutEmphasis(string emphasisLabel) =>
        $"Full institutional strength with <b>{emphasisLabel}</b> emphasis; otherwise {PartialReception} until adopted.";

    public static string FormatColloquySecondaryCost(int manuscriptCost) =>
        $"{manuscriptCost} manuscripts for {SecondaryReception}.";

    public static string FormatReopenEraForkSiblings() =>
        $"reopen deferred era paths for {PartialReception} (study colloquy or both paths → full reception)";

    public static string FormatEraForkPotencyLabel(float potency)
    {
        if (potency >= EraBranchRules.FullDualPathPotency - 0.01f)
            return "full reception";
        if (potency >= EraBranchRules.StudiedSiblingPotency - 0.01f)
            return DeepenedReception;
        return PartialReception;
    }

    public static string FormatEmphasisPotencyTag(float potency, string role) =>
        potency >= 1f ? role : $"{potency * 100f:F0}%, {role}";
}
