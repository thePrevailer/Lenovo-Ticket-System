using LenovoSmartFix.Core.Models;
using LenovoSmartFix.Core.Rules;

namespace LenovoSmartFix.Core.Interfaces;

public interface IRuleEngine
{
    /// <summary>
    /// Evaluate all registered rules and return a diagnosis decision
    /// with a list of all triggered rule results.
    /// </summary>
    (DiagnosisDecision Decision, IReadOnlyList<RuleResult> TriggeredRules) Evaluate(RuleContext context);
}
