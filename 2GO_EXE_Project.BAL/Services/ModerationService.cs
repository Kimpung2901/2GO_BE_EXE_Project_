using System.Text.RegularExpressions;
using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class ModerationService : IModerationService
{
    private static readonly Regex PhoneRegex = new(@"(?<!\d)(0?\d{9,11})(?!\d)", RegexOptions.Compiled);
    private static readonly Regex LinkRegex = new(@"(https?://|www\.|fb\.com|facebook\.com|zalo\.me|t\.me|telegram\.me|telegram\.org)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SpaceRegex = new(@"\s+", RegexOptions.Compiled);

    public AiRiskResult AnalyzeRisk(
        string title,
        string description,
        decimal? listingPrice,
        decimal? suggestedMin,
        decimal? suggestedMax,
        AiUserRiskInfo userInfo)
    {
        var flags = new List<string>();
        var score = 0;

        var normalizedTitle = NormalizeText(title);
        var normalizedDescription = NormalizeText(description);
        var content = $"{normalizedTitle} {normalizedDescription}";

        // a) Price anomalies (only when pricing has confidence)
        if (listingPrice.HasValue && suggestedMin.HasValue && suggestedMin.Value > 0 && listingPrice.Value < suggestedMin.Value * 0.4m)
        {
            flags.Add("PRICE_TOO_LOW");
            score += 40;
        }
        if (listingPrice.HasValue && suggestedMax.HasValue && suggestedMax.Value > 0 && listingPrice.Value > suggestedMax.Value * 2.0m)
        {
            flags.Add("PRICE_TOO_HIGH");
            score += 20;
        }

        // b) Spam content
        if (IsKeywordSpam(content))
        {
            flags.Add("KEYWORD_SPAM");
            score += 20;
        }
        if (IsDuplicateContent(normalizedTitle, normalizedDescription))
        {
            flags.Add("DUPLICATE_CONTENT");
            score += 20;
        }

        // c) External contact info
        if (PhoneRegex.IsMatch(content))
        {
            flags.Add("PHONE_NUMBER");
            score += 30;
        }
        if (LinkRegex.IsMatch(content))
        {
            flags.Add("EXTERNAL_LINK");
            score += 30;
        }

        // d) User behavior
        if (userInfo.AccountAgeDays < 7 && flags.Contains("PRICE_TOO_LOW"))
        {
            flags.Add("NEW_USER_RISK");
            score += 20;
        }
        if (userInfo.RecentListingsCount > 3)
        {
            flags.Add("MASS_POSTING");
            score += 20;
        }

        // e) Seller fake indicators
        if (!userInfo.PhoneVerified)
        {
            flags.Add("UNVERIFIED_PHONE");
            score += 5;
        }
        if (!userInfo.EmailVerified)
        {
            flags.Add("UNVERIFIED_EMAIL");
            score += 5;
        }
        if (userInfo.CompletedSalesCount == 0 && userInfo.TotalListingsCount >= 5)
        {
            flags.Add("NO_SALES_HISTORY");
            score += 5;
        }
        if (userInfo.ReportsCount >= 3)
        {
            flags.Add("MULTIPLE_REPORTS");
            score += 30;
        }
        if (userInfo.AccountAgeDays < 30 && userInfo.DeviceCount >= 5)
        {
            flags.Add("MANY_DEVICES_NEW_ACCOUNT");
            score += 20;
        }

        var severeFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PRICE_TOO_LOW",
            "PRICE_TOO_HIGH",
            "KEYWORD_SPAM",
            "DUPLICATE_CONTENT",
            "PHONE_NUMBER",
            "EXTERNAL_LINK",
            "MASS_POSTING",
            "MULTIPLE_REPORTS",
            "MANY_DEVICES_NEW_ACCOUNT"
        };

        var hasSevere = flags.Any(f => severeFlags.Contains(f));
        if (!hasSevere && flags.All(f => f is "UNVERIFIED_PHONE" or "UNVERIFIED_EMAIL" or "NO_SALES_HISTORY" or "NEW_USER_RISK"))
        {
            score = 0;
        }

        var action = score switch
        {
            < 30 => "PUBLISHED",
            < 70 => "PENDING_REVIEW",
            _ => "REJECTED"
        };

        return new AiRiskResult(score, flags, action);
    }

    private static string NormalizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var trimmed = SpaceRegex.Replace(input.ToLowerInvariant(), " ").Trim();
        return trimmed;
    }

    private static bool IsKeywordSpam(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 6) return false;

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            if (w.Length < 3) continue;
            freq[w] = freq.TryGetValue(w, out var c) ? c + 1 : 1;
        }

        var max = freq.Count > 0 ? freq.Values.Max() : 0;
        return max >= 5 && max >= Math.Max(2, words.Length / 4);
    }

    private static bool IsDuplicateContent(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description)) return false;
        var titleTokens = title.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
        var descTokens = description.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
        if (titleTokens.Count == 0 || descTokens.Count == 0) return false;

        var intersection = titleTokens.Intersect(descTokens).Count();
        var union = titleTokens.Union(descTokens).Count();
        var jaccard = union == 0 ? 0 : (double)intersection / union;
        return jaccard >= 0.8;
    }
}
