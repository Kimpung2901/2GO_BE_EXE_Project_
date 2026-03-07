using _2GO_EXE_Project.DAL.Entities;

namespace _2GO_EXE_Project.DAL.Repositories.Interfaces;

public interface IActivityLogRepository : IGenericRepository<ActivityLog> { }
public interface IAiModerationLogRepository : IGenericRepository<AiModerationLog> { }
public interface IAiScanResultRepository : IGenericRepository<AiScanResult> { }
public interface IAiAnalysisLogRepository : IGenericRepository<AiAnalysisLog> { }
public interface IAiImageVisionCacheRepository : IGenericRepository<AiImageVisionCache> { }
public interface IApiLogRepository : IGenericRepository<ApiLog> { }
public interface ICategoryRepository : IGenericRepository<Category> { }
public interface ICartRepository : IGenericRepository<Cart> { }
public interface ICartItemRepository : IGenericRepository<CartItem> { }
public interface IChatRepository : IGenericRepository<Chat> { }
public interface IChatbotLogRepository : IGenericRepository<ChatbotLog> { }
public interface ICityRepository : IGenericRepository<City> { }
public interface IDeviceLogRepository : IGenericRepository<DeviceLog> { }
public interface IDistrictRepository : IGenericRepository<District> { }
public interface IEscrowContractRepository : IGenericRepository<EscrowContract> { }
public interface IEscrowTransactionRepository : IGenericRepository<EscrowTransaction> { }
public interface IBankRepository : IGenericRepository<Bank> { }
public interface IFixerAssignmentRepository : IGenericRepository<FixerAssignment> { }
public interface IFixerRequestRepository : IGenericRepository<FixerRequest> { }
public interface IFixerServiceRepository : IGenericRepository<FixerService> { }
public interface IListingRepository : IGenericRepository<Listing> { }
public interface IListingAttributeRepository : IGenericRepository<ListingAttribute> { }
public interface IListingMediaRepository : IGenericRepository<ListingMedia> { }
public interface IListingViewRepository : IGenericRepository<ListingView> { }
public interface IListingCommentRepository : IGenericRepository<ListingComment>  
{
    Task<ListingComment?> GetByIdWithDetailsAsync(long commentId, CancellationToken cancellationToken = default);
    Task<(int Total, IReadOnlyList<ListingComment> Items)> GetByListingIdAsync(long listingId, int skip, int take, CancellationToken cancellationToken = default);
    Task<(int Total, IReadOnlyList<ListingComment> Items)> GetRepliesByParentIdAsync(long listingId, long parentId, int skip, int take, CancellationToken cancellationToken = default);
}
public interface IManualReviewQueueRepository : IGenericRepository<ManualReviewQueue> { }
public interface IMarketPriceRepository : IGenericRepository<MarketPrice> { }
public interface IMessageRepository : IGenericRepository<Message> { }
public interface INotificationRepository : IGenericRepository<Notification> { }
public interface IOrderRepository : IGenericRepository<Order> { }
public interface IOrderItemRepository : IGenericRepository<OrderItem> { }
public interface IOrderTransactionRepository : IGenericRepository<OrderTransaction> { }
public interface IOrderInvoiceRepository : IGenericRepository<OrderInvoice> { }
public interface IPaymentRepository : IGenericRepository<Payment> { }
public interface IPaymentLogRepository : IGenericRepository<PaymentLog> { }
public interface ITransferRepository : IGenericRepository<Transfer> { }
public interface ITransferTransactionRepository : IGenericRepository<TransferTransaction> { }
public interface IPointTransactionRepository : IGenericRepository<PointTransaction> { }
public interface IReportRepository : IGenericRepository<Report> { }
public interface ISavedListingRepository : IGenericRepository<SavedListing> { }
public interface ISearchHistoryRepository : IGenericRepository<SearchHistory> { }
public interface IShippingRequestRepository : IGenericRepository<ShippingRequest> { }
public interface ISubCategoryRepository : IGenericRepository<SubCategory> { }
public interface ISubscriptionPlanRepository : IGenericRepository<SubscriptionPlan> { }
public interface ISubscriptionPlanAuditRepository : IGenericRepository<SubscriptionPlanAudit> { }
public interface ISupportTicketRepository : IGenericRepository<SupportTicket> { }
public interface IUserRepository : IGenericRepository<User> { }
public interface IUserDeviceRepository : IGenericRepository<UserDevice> { }
public interface IUserPointRepository : IGenericRepository<UserPoint> { }
public interface IUserProfileRepository : IGenericRepository<UserProfile> { }
public interface IUserRatingRepository : IGenericRepository<UserRating> { }
public interface IUserVerificationRepository : IGenericRepository<UserVerification> { }
public interface IWardRepository : IGenericRepository<Ward> { }
public interface IRefreshTokenRepository : IGenericRepository<RefreshToken> { }
public interface IVerificationCodeRepository : IGenericRepository<VerificationCode> { }
