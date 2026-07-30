using System.Collections.Generic;
using System.Linq;

/// <summary>Which integration colloquy reopens locked era-fork siblings at partial reception.</summary>
public enum EraForkIntegrationTrack
{
    None = 0,
    Confessional,
    Culture,
    Synodical
}

/// <summary>
/// Era branch groups: pick one tech per group; siblings stay locked until integration
/// reopens them at <see cref="IntegratedSiblingPotency"/>.
/// Group format: "{track}:{branchId}" e.g. "confessional:Era2-Confession".
/// </summary>
public static class EraBranchRules
{
    public const float IntegratedSiblingPotency = 0.5f;
    public const float StudiedSiblingPotency = 0.75f;
    public const float FullDualPathPotency = 1f;
    public const int ColloquyDeferTurns = 5;

    public static bool TryParseBranchGroup(
        string eraBranchGroup,
        out EraForkIntegrationTrack track,
        out string branchId)
    {
        track = EraForkIntegrationTrack.None;
        branchId = null;
        if (string.IsNullOrEmpty(eraBranchGroup))
            return false;

        int sep = eraBranchGroup.IndexOf(':');
        if (sep <= 0 || sep >= eraBranchGroup.Length - 1)
            return false;

        string trackKey = eraBranchGroup.Substring(0, sep);
        branchId = eraBranchGroup.Substring(sep + 1);
        track = trackKey switch
        {
            "confessional" => EraForkIntegrationTrack.Confessional,
            "culture" => EraForkIntegrationTrack.Culture,
            "synodical" => EraForkIntegrationTrack.Synodical,
            _ => EraForkIntegrationTrack.None
        };
        return track != EraForkIntegrationTrack.None && !string.IsNullOrEmpty(branchId);
    }

    public static EraForkIntegrationTrack TrackFor(ConfessionTechNode node)
    {
        if (TryParseBranchGroup(node.EraBranchGroup, out var track, out _))
            return track;
        return EraForkIntegrationTrack.None;
    }

    public static IEnumerable<ConfessionTechId> SiblingsInBranch(ConfessionTechId id)
    {
        var node = ConfessionTechDatabase.Get(id);
        if (!TryParseBranchGroup(node.EraBranchGroup, out _, out var branchId))
            yield break;

        foreach (var other in ConfessionTechDatabase.All.Values)
        {
            if (other.Id == id)
                continue;
            if (!TryParseBranchGroup(other.EraBranchGroup, out _, out var otherBranch))
                continue;
            if (otherBranch == branchId)
                yield return other.Id;
        }
    }

    public static ConfessionTechId? ChosenSiblingInBranch(
        IReadOnlyCollection<ConfessionTechId> unlocked,
        ConfessionTechId id)
    {
        var node = ConfessionTechDatabase.Get(id);
        if (!TryParseBranchGroup(node.EraBranchGroup, out _, out var branchId))
            return null;

        foreach (var other in ConfessionTechDatabase.All.Values)
        {
            if (other.Id == id)
                continue;
            if (!TryParseBranchGroup(other.EraBranchGroup, out _, out var otherBranch))
                continue;
            if (otherBranch == branchId && unlocked.Contains(other.Id))
                return other.Id;
        }

        return null;
    }

    public static ConfessionModifiers ApplyForkPotency(ConfessionModifiers raw, float forkPotency)
    {
        if (forkPotency >= 1f)
            return raw;

        var scaled = ConfessionModifiers.Scaled(raw, forkPotency);
        scaled.AntinomianGuard = raw.AntinomianGuard;
        scaled.LegalismGuard = raw.LegalismGuard;
        return scaled;
    }

    public static string FormatBranchStatusHint(ConfessionTechId id, ConfessionTechStatus status)
    {
        if (status == ConfessionTechStatus.EraForkLocked)
        {
            var chosen = ConfessionResearchManager.Instance != null
                ? ConfessionResearchManager.Instance.GetEraForkChoiceFor(id)
                : null;
            string chosenName = chosen.HasValue
                ? ConfessionTechDatabase.Get(chosen.Value).Name
                : "another path";
            var track = TrackFor(ConfessionTechDatabase.Get(id));
            string integrationName = track switch
            {
                EraForkIntegrationTrack.Confessional => ConfessionTechDatabase.Get(
                    Tier2EmphasisManager.ConfessionalIntegrationUnlockTech).Name,
                EraForkIntegrationTrack.Culture => ConfessionTechDatabase.Get(
                    Tier2EmphasisManager.CultureIntegrationUnlockTech).Name,
                EraForkIntegrationTrack.Synodical => ConfessionTechDatabase.Get(
                    SynodicalEmphasisManager.IntegrationUnlockTech).Name,
                _ => "integration"
            };
            return $"<size=12><color=#AABBCC><i>{ConfessionalUiVocabulary.FormatEraPathClosed(chosenName, integrationName)}</i></color></size>";
        }

        if (status == ConfessionTechStatus.Available &&
            ConfessionResearchManager.Instance != null &&
            ConfessionResearchManager.Instance.IsIntegratedForkSibling(id))
        {
            return $"<size=12><color=#AABBCC><i>{ConfessionalUiVocabulary.FormatIntegratedSiblingAvailable()}</i></color></size>";
        }

        // Advance warning while the fork is still open.
        if (status is ConfessionTechStatus.Available or ConfessionTechStatus.Locked or
            ConfessionTechStatus.AdherenceLocked or ConfessionTechStatus.Researching)
        {
            string advance = FormatAdvanceForkHint(id);
            if (!string.IsNullOrEmpty(advance))
                return advance;
        }

        return "";
    }

    /// <summary>Shown on both siblings before either path is taken.</summary>
    public static string FormatAdvanceForkHint(ConfessionTechId id)
    {
        var siblings = SiblingsInBranch(id).ToList();
        if (siblings.Count == 0)
            return "";

        // Only warn while no sibling in the group is unlocked yet.
        var unlocked = ConfessionResearchManager.Instance;
        if (unlocked != null)
        {
            if (unlocked.IsTechUnlocked(id))
                return "";
            foreach (var sib in siblings)
            {
                if (unlocked.IsTechUnlocked(sib))
                    return "";
            }
        }

        string siblingName = ConfessionTechDatabase.Get(siblings[0]).Name;
        return $"<size=12><color=#E8C878><i>{ConfessionalUiVocabulary.FormatEraForkChoice(siblingName)}</i></color></size>";
    }

    public static string FormatForkButtonBadge(ConfessionTechId id, ConfessionTechStatus status)
    {
        var siblings = SiblingsInBranch(id).ToList();
        if (siblings.Count == 0)
            return "";

        if (status == ConfessionTechStatus.EraForkLocked)
            return "<color=#CC8866>Fork locked</color>";

        var rm = ConfessionResearchManager.Instance;
        if (rm != null && rm.IsTechUnlocked(id))
            return "<color=#88EEAA>Fork path</color>";

        if (rm != null)
        {
            foreach (var sib in siblings)
            {
                if (rm.IsTechUnlocked(sib))
                    return "<color=#CC8866>Fork locked</color>";
            }
        }

        string shortName = ConfessionTechDatabase.Get(siblings[0]).Name;
        if (shortName.Length > 18)
            shortName = shortName.Substring(0, 16) + "…";
        return ConfessionalUiVocabulary.FormatEraForkBadge(shortName);
    }

    public static bool BothSiblingsUnlocked(IReadOnlyCollection<ConfessionTechId> unlocked, ConfessionTechId id)
    {
        var node = ConfessionTechDatabase.Get(id);
        if (!TryParseBranchGroup(node.EraBranchGroup, out _, out var branchId))
            return false;

        int count = 0;
        foreach (var other in ConfessionTechDatabase.All.Values)
        {
            if (!TryParseBranchGroup(other.EraBranchGroup, out _, out var otherBranch))
                continue;
            if (otherBranch == branchId && unlocked.Contains(other.Id))
                count++;
        }

        return count >= 2;
    }

    public static float ResolveForkPotency(
        ConfessionTechId id,
        IReadOnlyCollection<ConfessionTechId> unlocked,
        IReadOnlyCollection<ConfessionTechId> integratedForkSiblings,
        IReadOnlyCollection<ConfessionTechId> studiedForkSiblings)
    {
        if (!unlocked.Contains(id) || !integratedForkSiblings.Contains(id))
            return FullDualPathPotency;

        if (BothSiblingsUnlocked(unlocked, id))
            return FullDualPathPotency;

        if (studiedForkSiblings.Contains(id))
            return StudiedSiblingPotency;

        return IntegratedSiblingPotency;
    }

    public static int StudyColloquyCostForTier(int tier) => tier switch
    {
        >= 4 => 5,
        _ => 4
    };

    public static int ColloquyCostForTier(int tier) => tier switch
    {
        >= 5 => 5,
        4 => 4,
        _ => 3
    };
}
