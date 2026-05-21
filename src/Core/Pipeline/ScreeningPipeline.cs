using Microsoft.Extensions.Logging;
using TransactionCompliance.Core.Models;
using TransactionCompliance.Core.Rules;
using TransactionCompliance.Core.Stores;

namespace TransactionCompliance.Core.Pipeline;

public sealed class ScreeningPipeline : IScreeningPipeline
{
    private readonly IEnumerable<IScreeningRule> _rules;
    private readonly IDailyAggregateStore _dailyStore;
    private readonly IScreeningResultStore _resultStore;
    private readonly ILogger<ScreeningPipeline> _logger;

    public ScreeningPipeline(
        IEnumerable<IScreeningRule> rules,
        IDailyAggregateStore dailyStore,
        IScreeningResultStore resultStore,
        ILogger<ScreeningPipeline> logger)
    {
        _rules = rules;
        _dailyStore = dailyStore;
        _resultStore = resultStore;
        _logger = logger;
    }

    public async Task<ScreeningResponse> ScreenAsync(
        string requestId,
        ScreeningRequest request,
        CancellationToken ct = default)
    {
        var ruleResults = new List<RuleResult>();

        foreach (var rule in _rules)
        {
            var result = await rule.EvaluateAsync(request, ct);
            ruleResults.Add(result);
        }

        var finalStatus = DetermineStatus(ruleResults);

        if (finalStatus != ScreeningStatus.Rejected)
        {
            await _dailyStore.AddAsync(
                request.Sender.AccountId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                request.Amount,
                ct);
        }

        var response = new ScreeningResponse(
            requestId,
            request.TransactionId,
            finalStatus,
            DateTimeOffset.UtcNow,
            ruleResults.AsReadOnly());

        await _resultStore.SaveAsync(response, ct);

        _logger.LogInformation(
            "Screening decision {RequestId} {TransactionId} {Status}",
            requestId,
            request.TransactionId,
            finalStatus);

        return response;
    }

    private static ScreeningStatus DetermineStatus(IEnumerable<RuleResult> results)
    {
        var hasRejected = false;
        var hasPendingReview = false;
        var hasFlagged = false;

        foreach (var r in results)
        {
            if (r.Status != RuleStatus.Triggered)
                continue;

            switch (r.Rule)
            {
                case "duplicate_transaction":
                case "sanctioned_country":
                case "currency_restriction":
                    hasRejected = true;
                    break;
                case "pep_check":
                    hasPendingReview = true;
                    break;
                case "amount_threshold":
                case "cumulative_daily_limit":
                    hasFlagged = true;
                    break;
            }
        }

        if (hasRejected) return ScreeningStatus.Rejected;
        if (hasPendingReview) return ScreeningStatus.PendingReview;
        if (hasFlagged) return ScreeningStatus.Flagged;
        return ScreeningStatus.Approved;
    }
}
