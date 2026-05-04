namespace TransactionCompliance.Core.Models;

public record ScreeningResponse(
    string RequestId,
    string TransactionId,
    ScreeningStatus Status,
    DateTimeOffset Timestamp,
    IReadOnlyList<RuleResult> Rules
);
