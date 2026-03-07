using _2GO_EXE_Project.BAL.DTOs.Chatbot;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IChatbotService
{
    Task<ChatbotAskResponse> AskAsync(long? userId, ChatbotAskRequest request, CancellationToken cancellationToken = default);
}