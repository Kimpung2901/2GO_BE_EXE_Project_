using System.Text.Json;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Chatbot;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace _2GO_EXE_Project.BAL.Services;

public class ChatbotService : IChatbotService
{
    private readonly IUnitOfWork _uow;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<ChatbotService> _logger;
    private readonly string _faqPath;
    private static readonly object LockObj = new();
    private static List<FaqEntry>? Cache;

    public ChatbotService(IUnitOfWork uow, IGeminiService geminiService, ILogger<ChatbotService> logger)
    {
        _uow = uow;
        _geminiService = geminiService;
        _logger = logger;
        _faqPath = ResolveFaqPath();
    }

    public async Task<ChatbotAskResponse> AskAsync(long? userId, ChatbotAskRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return new ChatbotAskResponse("Vui lòng nhập câu hỏi.", "LOW", "FAQ_JSON", null);
        }

        var faq = LoadFaq();
        var normalized = Normalize(request.Question);

        var productQuery = ExtractProductQuery(normalized);
        if (!string.IsNullOrWhiteSpace(productQuery))
        {
            var suggestions = await FindListingsAsync(productQuery, cancellationToken);
            if (suggestions.Count > 0)
            {
                var productSearchAnswer = $"Mình tìm thấy {suggestions.Count} sản phẩm liên quan đến \"{productQuery}\".";
                await LogAsync(userId, request.Question, productSearchAnswer, "product_search", "MEDIUM", cancellationToken);
                return new ChatbotAskResponse(productSearchAnswer, "MEDIUM", "LISTINGS", "product_search", suggestions);
            }

            var noResultAnswer = $"Hiện chưa thấy sản phẩm phù hợp với \"{productQuery}\". Bạn có thể cho mình thêm thông tin (hãng, model, giá, tình trạng) không?";
            await LogAsync(userId, request.Question, noResultAnswer, "product_search", "LOW", cancellationToken);
            return new ChatbotAskResponse(noResultAnswer, "LOW", "LISTINGS", "product_search");
        }

        FaqEntry? best = null;
        var bestScore = 0.0;
        var bestPriority = int.MaxValue;
        foreach (var item in faq)
        {
            var score = Score(item, normalized);
            if (score > bestScore || (Math.Abs(score - bestScore) < 0.0001 && item.Priority < bestPriority))
            {
                bestScore = score;
                best = item;
                bestPriority = item.Priority;
            }
        }

        string answer;
        string confidence;
        string? intent = null;

        if (best != null && bestScore > 0)
        {
            answer = best.Answer;
            intent = string.IsNullOrWhiteSpace(best.Intent) ? best.Id : best.Intent;
            confidence = bestScore >= 0.7 ? "HIGH" : bestScore >= 0.45 ? "MEDIUM" : "LOW";
        }
        else
        {
            answer = await TryGeminiFallbackAsync(request.Question, request.Context, userId, cancellationToken)
                ?? "Mình chưa có thông tin này. Bạn vui lòng liên hệ CSKH để được hỗ trợ thêm.";
            confidence = answer.StartsWith("Mình chưa có", StringComparison.Ordinal) ? "LOW" : "MEDIUM";
        }

        await LogAsync(userId, request.Question, answer, intent, confidence, cancellationToken);
        return new ChatbotAskResponse(answer, confidence, "FAQ_JSON", intent);
    }

    private List<FaqEntry> LoadFaq()
    {
        if (Cache != null) return Cache;
        lock (LockObj)
        {
            if (Cache != null) return Cache;
            if (string.IsNullOrWhiteSpace(_faqPath) || !File.Exists(_faqPath))
            {
                Cache = new List<FaqEntry>();
                return Cache;
            }

            var json = File.ReadAllText(_faqPath);
            Cache = JsonSerializer.Deserialize<List<FaqEntry>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<FaqEntry>();
            return Cache;
        }
    }

    private static double Score(FaqEntry item, string question)
    {
        if (item.Patterns == null || item.Patterns.Count == 0) return 0;
        if (item.NegativePatterns != null && item.NegativePatterns.Any(np =>
                question.Contains(Normalize(np), StringComparison.OrdinalIgnoreCase)))
        {
            return 0;
        }

        var hits = item.Patterns.Count(p => question.Contains(Normalize(p), StringComparison.OrdinalIgnoreCase));
        if (hits == 0) return 0;
        var ratio = (double)hits / item.Patterns.Count;
        return ratio;
    }

    private static string Normalize(string input)
    {
        return string.Join(" ", input.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? ExtractProductQuery(string normalized)
    {
        var triggers = new[]
        {
            "tôi cần",
            "toi can",
            "cần mua",
            "can mua",
            "muốn mua",
            "muon mua",
            "tìm",
            "tim",
            "mua",
            "cần"
        };

        foreach (var trigger in triggers)
        {
            var idx = normalized.IndexOf(trigger, StringComparison.Ordinal);
            if (idx < 0) continue;

            var after = normalized[(idx + trigger.Length)..].Trim();
            if (after.StartsWith("sản phẩm "))
            {
                after = after["sản phẩm ".Length..].Trim();
            }
            else if (after.StartsWith("san pham "))
            {
                after = after["san pham ".Length..].Trim();
            }

            if (after.Length >= 2)
            {
                return after;
            }
        }

        return null;
    }

    private async Task<List<ChatbotListingSuggestion>> FindListingsAsync(string term, CancellationToken cancellationToken)
    {
        var lowerTerm = term.ToLowerInvariant();
        var query = _uow.Listings.Query().AsNoTracking()
            .Where(l => l.Status == ListingStatuses.Active);

        query = query.Where(l =>
            (l.Title != null && l.Title.ToLower().Contains(lowerTerm)) ||
            (l.Description != null && l.Description.ToLower().Contains(lowerTerm)) ||
            (l.Brand != null && l.Brand.ToLower().Contains(lowerTerm)));

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new ChatbotListingSuggestion(
                l.ListingId,
                l.Title ?? "Sản phẩm",
                l.Price,
                l.Condition,
                l.Brand))
            .Take(5)
            .ToListAsync(cancellationToken);
    }

    private static string ResolveFaqPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(baseDir);
        for (var i = 0; i < 5 && current != null; i++)
        {
            var candidate = Path.Combine(current.FullName, "Data", "chatbot_faq.json");
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }
        return string.Empty;
    }

    private async Task<string?> TryGeminiFallbackAsync(string question, string? context, long? userId, CancellationToken cancellationToken)
    {
        try
        {
            var contextBlock = string.IsNullOrWhiteSpace(context)
                ? string.Empty
                : $"Ngữ cảnh trước đó: {context}\n";
            var prompt =
                "Bạn là trợ lý CSKH cho sàn mua bán đồ cũ. " +
                "Trả lời ngắn gọn bằng tiếng Việt. " +
                "Nếu không chắc chắn, hãy nói rõ là chưa có thông tin.\n" +
                $"{contextBlock}Câu hỏi: {question}";
            var userKey = userId?.ToString();
            var text = await _geminiService.GenerateAsync(prompt, userKey, cancellationToken);
            _logger.LogInformation("Gemini fallback used. userId={UserId}", userId);
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini fallback failed. userId={UserId}", userId);
            return null;
        }
    }

    private async Task LogAsync(long? userId, string question, string answer, string? intent, string confidence, CancellationToken cancellationToken)
    {
        try
        {
            var log = new ChatbotLog
            {
                UserId = userId,
                Question = question,
                Answer = answer,
                MatchedIntent = intent,
                Confidence = confidence,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.ChatbotLogs.AddAsync(log, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // ignore logging failures
        }
    }

    private sealed class FaqEntry
    {
        public string Id { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Intent { get; set; } = "";
        public int Priority { get; set; } = 2;
        public List<string> Patterns { get; set; } = new();
        public List<string> NegativePatterns { get; set; } = new();
        public string Answer { get; set; } = "";
    }
}









