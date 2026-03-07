namespace _2GO_EXE_Project.BAL.DTOs.Chatbot;

public record ChatbotAskRequest(string Question, string? Context);

public record ChatbotListingSuggestion(
    long ListingId,
    string Title,
    decimal? Price,
    string? Condition,
    string? Brand);

public record ChatbotAskResponse(
    string Answer,
    string Confidence,
    string Source,
    string? MatchedIntent,
    IReadOnlyList<ChatbotListingSuggestion>? Suggestions = null);
