namespace TransactionCompliance.Core.Models;

public record ScreeningRequest(
    string TransactionId,
    PartyInfo Sender,
    PartyInfo Receiver,
    long Amount,
    string Currency,
    string? Description = null
);
