using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class NoteGenerationService : INoteGenerationService
{
    public string BuildNote(AiPricingResult pricing, string conditionAi)
    {
        if (!pricing.MarketAvg.HasValue || pricing.MarketAvg.Value <= 0 ||
            string.Equals(pricing.Confidence, "LOW", StringComparison.OrdinalIgnoreCase))
        {
            return "Chưa đủ dữ liệu thị trường để gợi ý giá chính xác. Vui lòng cập nhật thêm thông tin sản phẩm. Lưu ý: AI rule-based.";
        }

        var note = $"Sản phẩm được nhận diện: {pricing.DetectedProduct}. " +
                   $"Tình trạng AI đánh giá: {conditionAi}. " +
                   $"Giá thị trường trung bình khoảng {pricing.MarketAvg.Value:N0} VND. " +
                   $"Gợi ý giá bán trong khoảng {pricing.SuggestedMin!.Value:N0} - {pricing.SuggestedMax!.Value:N0} VND. " +
                   "Lưu ý: AI rule-based.";

        return note;
    }
}
