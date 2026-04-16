using LenovoSmartFix.Core.Models;
using LenovoSmartFix.Core.Rules;

namespace LenovoSmartFix.Service.Remediation;

/// <summary>
/// Maps triggered rules to the approved V1 remediation library.
///
/// Each call to <see cref="BuildFrom"/> produces new action instances with
/// freshly generated <see cref="RemediationAction.ActionInstanceId"/> values,
/// so repeated scans never share or collide on the same instance ID.
/// </summary>
public static class RemediationActionFactory
{
    // ── Library definitions ───────────────────────────────────────────────────
    // These are templates only; Clone() stamps a new ActionInstanceId each time.

    private static readonly IReadOnlyDictionary<string, RemediationAction> Library =
        new Dictionary<string, RemediationAction>
        {
            ["REM-TEMP-CLEANUP"] = new()
            {
                ActionId       = "REM-TEMP-CLEANUP",
                ActionName     = "Clear Temporary Files",
                Description    = "Remove temporary files from %TEMP% to free disk space.",
                SafetyLevel    = RemediationSafetyLevel.Safe,
                IsRollbackable = false
            },
            ["REM-POWER-PLAN"] = new()
            {
                ActionId       = "REM-POWER-PLAN",
                ActionName     = "Switch to Balanced Power Plan",
                Description    = "Set the active power plan to Balanced.",
                SafetyLevel    = RemediationSafetyLevel.Safe,
                IsRollbackable = true
            },
            ["REM-DNS-FLUSH"] = new()
            {
                ActionId       = "REM-DNS-FLUSH",
                ActionName     = "Flush DNS Cache",
                Description    = "Clear the DNS resolver cache to resolve stale address mappings.",
                SafetyLevel    = RemediationSafetyLevel.Safe,
                IsRollbackable = false
            },
            ["REM-NET-SVC"] = new()
            {
                ActionId       = "REM-NET-SVC",
                ActionName     = "Restart Network Services",
                Description    = "Restart DNS Client, NLA, and WLAN AutoConfig services.",
                SafetyLevel    = RemediationSafetyLevel.Consent,
                IsRollbackable = false
            },
            ["REM-STARTUP-OPT"] = new()
            {
                ActionId       = "REM-STARTUP-OPT",
                ActionName     = "Review Startup Programs",
                Description    = "Open the startup manager so you can disable high-impact startup items.",
                SafetyLevel    = RemediationSafetyLevel.Consent,
                IsRollbackable = true
            },
            ["REM-UPDATE-VANTAGE"] = new()
            {
                ActionId       = "REM-UPDATE-VANTAGE",
                ActionName     = "Launch Lenovo Vantage Updates",
                Description    = "Open the Lenovo Vantage update flow to apply BIOS, driver, and utility updates.",
                SafetyLevel    = RemediationSafetyLevel.Consent,   // never auto-launch
                IsRollbackable = false
            }
        };

    // ── Build per-scan action list ────────────────────────────────────────────

    /// <summary>
    /// Returns a new list of action instances appropriate for the triggered rules.
    /// Every call produces distinct ActionInstanceId values — safe to call
    /// multiple times for the same scan without collision.
    /// </summary>
    public static List<RemediationAction> BuildFrom(
        IReadOnlyList<RuleResult> triggered, DiagnosisPath path)
    {
        var actions  = new List<RemediationAction>();
        var ruleIds  = triggered.Select(r => r.RuleId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Performance / storage
        if (ruleIds.Contains("PERF-003"))
            actions.Add(Clone("REM-TEMP-CLEANUP"));

        if (ruleIds.Contains("PERF-001") || ruleIds.Contains("PERF-002"))
            actions.Add(Clone("REM-STARTUP-OPT"));

        // Battery / power
        if (ruleIds.Contains("BATT-003") || ruleIds.Contains("BATT-002"))
            actions.Add(Clone("REM-POWER-PLAN"));

        // Network
        if (ruleIds.Contains("NET-001"))
        {
            actions.Add(Clone("REM-DNS-FLUSH"));
            actions.Add(Clone("REM-NET-SVC"));
        }

        // Updates — only when update rules actually triggered (not on Unknown)
        if (ruleIds.Contains("UPD-001") || ruleIds.Contains("UPD-002")
            || ruleIds.Contains("UPD-003"))
            actions.Add(Clone("REM-UPDATE-VANTAGE"));

        return actions;
    }

    private static RemediationAction Clone(string id)
    {
        var src = Library[id];
        return new RemediationAction
        {
            // ActionInstanceId is auto-generated (Guid.NewGuid()) in the constructor
            ActionId       = src.ActionId,
            ActionName     = src.ActionName,
            Description    = src.Description,
            SafetyLevel    = src.SafetyLevel,
            IsRollbackable = src.IsRollbackable
        };
    }
}
