namespace LeadHype.Api.Core.Models.ExternalApi;

public record ClassificationResponse(
    bool Success,
    string? ClassificationResult = null,
    string? Error = null
);

public record ClassificationRequest(
    string EmailBody
);