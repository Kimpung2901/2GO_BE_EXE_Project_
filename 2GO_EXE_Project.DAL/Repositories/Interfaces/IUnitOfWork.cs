namespace _2GO_EXE_Project.DAL.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IActivityLogRepository ActivityLogs { get; }
    IAiModerationLogRepository AiModerationLogs { get; }
    IAiScanResultRepository AiScanResults { get; }
    IAiAnalysisLogRepository AiAnalysisLogs { get; }
    IAiImageVisionCacheRepository AiImageVisionCaches { get; }
    IApiLogRepository ApiLogs { get; }
    ICategoryRepository Categories { get; }
    ICartRepository Carts { get; }
    ICartItemRepository CartItems { get; }
    IChatRepository Chats { get; }
    IChatbotLogRepository ChatbotLogs { get; }
    ICityRepository Cities { get; }
    IDeviceLogRepository DeviceLogs { get; }
    IDistrictRepository Districts { get; }
    IEscrowContractRepository EscrowContracts { get; }
    IEscrowTransactionRepository EscrowTransactions { get; }
    IBankRepository Banks { get; }
    IFixerAssignmentRepository FixerAssignments { get; }
    IFixerRequestRepository FixerRequests { get; }
    IFixerServiceRepository FixerServices { get; }
    IListingRepository Listings { get; }
    IListingAttributeRepository ListingAttributes { get; }
    IListingMediaRepository ListingMedias { get; }
    IListingViewRepository ListingViews { get; }
    IListingCommentRepository ListingComments { get; }  
    IManualReviewQueueRepository ManualReviewQueues { get; }
    IMarketPriceRepository MarketPrices { get; }
    IMessageRepository Messages { get; }
    INotificationRepository Notifications { get; }
    IOrderRepository Orders { get; }
    IOrderItemRepository OrderItems { get; }
    IOrderTransactionRepository OrderTransactions { get; }
    IOrderInvoiceRepository OrderInvoices { get; }
    IPaymentRepository Payments { get; }
    IPaymentLogRepository PaymentLogs { get; }
    ITransferRepository Transfers { get; }
    ITransferTransactionRepository TransferTransactions { get; }
    IPointTransactionRepository PointTransactions { get; }
    IReportRepository Reports { get; }
    ISavedListingRepository SavedListings { get; }
    ISearchHistoryRepository SearchHistories { get; }
    IShippingRequestRepository ShippingRequests { get; }
    ISubCategoryRepository SubCategories { get; }
    ISubscriptionPlanRepository SubscriptionPlans { get; }
    ISubscriptionPlanAuditRepository SubscriptionPlanAudits { get; }
    ISupportTicketRepository SupportTickets { get; }
    IUserRepository Users { get; }
    IUserDeviceRepository UserDevices { get; }
    IUserPointRepository UserPoints { get; }
    IUserProfileRepository UserProfiles { get; }
    IUserRatingRepository UserRatings { get; }
    IUserVerificationRepository UserVerifications { get; }
    IWardRepository Wards { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IVerificationCodeRepository VerificationCodes { get; }

    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}
