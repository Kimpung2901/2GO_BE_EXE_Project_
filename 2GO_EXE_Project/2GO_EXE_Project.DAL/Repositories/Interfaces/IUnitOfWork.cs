namespace _2GO_EXE_Project.DAL.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IActivityLogRepository ActivityLogs { get; }
    IAiModerationLogRepository AiModerationLogs { get; }
    IAiScanResultRepository AiScanResults { get; }
    IApiLogRepository ApiLogs { get; }
    ICategoryRepository Categories { get; }
    IChatRepository Chats { get; }
    ICityRepository Cities { get; }
    IDeviceLogRepository DeviceLogs { get; }
    IDistrictRepository Districts { get; }
    IEscrowContractRepository EscrowContracts { get; }
    IEscrowTransactionRepository EscrowTransactions { get; }
    IFixerAssignmentRepository FixerAssignments { get; }
    IFixerRequestRepository FixerRequests { get; }
    IFixerServiceRepository FixerServices { get; }
    IListingRepository Listings { get; }
    IListingAttributeRepository ListingAttributes { get; }
    IListingImageRepository ListingImages { get; }
    IListingViewRepository ListingViews { get; }
    IMessageRepository Messages { get; }
    IOrderRepository Orders { get; }
    IOrderItemRepository OrderItems { get; }
    IPaymentRepository Payments { get; }
    IPaymentLogRepository PaymentLogs { get; }
    IPointTransactionRepository PointTransactions { get; }
    IReportRepository Reports { get; }
    ISavedListingRepository SavedListings { get; }
    ISearchHistoryRepository SearchHistories { get; }
    IShippingRequestRepository ShippingRequests { get; }
    ISubCategoryRepository SubCategories { get; }
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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}
