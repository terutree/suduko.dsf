namespace TransactionCompliance.Core.Models;

public enum ScreeningStatus
{
    Approved,
    Flagged,
    PendingReview,
    Rejected
}

public enum RuleStatus
{
    Passed,
    Triggered
}

public enum RuleSeverity
{
    Low,
    Medium,
    High
}
