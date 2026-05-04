namespace TransactionCompliance.Core.Models;

public record RuleResult(string Rule, RuleStatus Status, RuleSeverity Severity, string Message);
