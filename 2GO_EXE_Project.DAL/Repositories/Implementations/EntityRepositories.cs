using _2GO_EXE_Project.DAL.Context;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Repositories.Implementations;

public class ActivityLogRepository : GenericRepository<ActivityLog>, IActivityLogRepository { public ActivityLogRepository(AppDbContext ctx) : base(ctx) { } }
public class AiModerationLogRepository : GenericRepository<AiModerationLog>, IAiModerationLogRepository { public AiModerationLogRepository(AppDbContext ctx) : base(ctx) { } }
public class AiScanResultRepository : GenericRepository<AiScanResult>, IAiScanResultRepository { public AiScanResultRepository(AppDbContext ctx) : base(ctx) { } }
public class AiAnalysisLogRepository : GenericRepository<AiAnalysisLog>, IAiAnalysisLogRepository { public AiAnalysisLogRepository(AppDbContext ctx) : base(ctx) { } }
public class AiImageVisionCacheRepository : GenericRepository<AiImageVisionCache>, IAiImageVisionCacheRepository { public AiImageVisionCacheRepository(AppDbContext ctx) : base(ctx) { } }
public class ApiLogRepository : GenericRepository<ApiLog>, IApiLogRepository { public ApiLogRepository(AppDbContext ctx) : base(ctx) { } }
public class CategoryRepository : GenericRepository<Category>, ICategoryRepository { public CategoryRepository(AppDbContext ctx) : base(ctx) { } }
public class CartRepository : GenericRepository<Cart>, ICartRepository { public CartRepository(AppDbContext ctx) : base(ctx) { } }
public class CartItemRepository : GenericRepository<CartItem>, ICartItemRepository { public CartItemRepository(AppDbContext ctx) : base(ctx) { } }
public class ChatRepository : GenericRepository<Chat>, IChatRepository { public ChatRepository(AppDbContext ctx) : base(ctx) { } }
public class ChatbotLogRepository : GenericRepository<ChatbotLog>, IChatbotLogRepository { public ChatbotLogRepository(AppDbContext ctx) : base(ctx) { } }
public class CityRepository : GenericRepository<City>, ICityRepository { public CityRepository(AppDbContext ctx) : base(ctx) { } }
public class DeviceLogRepository : GenericRepository<DeviceLog>, IDeviceLogRepository { public DeviceLogRepository(AppDbContext ctx) : base(ctx) { } }
public class DistrictRepository : GenericRepository<District>, IDistrictRepository { public DistrictRepository(AppDbContext ctx) : base(ctx) { } }
public class EscrowContractRepository : GenericRepository<EscrowContract>, IEscrowContractRepository { public EscrowContractRepository(AppDbContext ctx) : base(ctx) { } }
public class EscrowTransactionRepository : GenericRepository<EscrowTransaction>, IEscrowTransactionRepository { public EscrowTransactionRepository(AppDbContext ctx) : base(ctx) { } }
public class BankRepository : GenericRepository<Bank>, IBankRepository { public BankRepository(AppDbContext ctx) : base(ctx) { } }
public class FixerAssignmentRepository : GenericRepository<FixerAssignment>, IFixerAssignmentRepository { public FixerAssignmentRepository(AppDbContext ctx) : base(ctx) { } }
public class FixerRequestRepository : GenericRepository<FixerRequest>, IFixerRequestRepository { public FixerRequestRepository(AppDbContext ctx) : base(ctx) { } }
public class FixerServiceRepository : GenericRepository<FixerService>, IFixerServiceRepository { public FixerServiceRepository(AppDbContext ctx) : base(ctx) { } }
public class ListingRepository : GenericRepository<Listing>, IListingRepository { public ListingRepository(AppDbContext ctx) : base(ctx) { } }
public class ListingAttributeRepository : GenericRepository<ListingAttribute>, IListingAttributeRepository { public ListingAttributeRepository(AppDbContext ctx) : base(ctx) { } }
public class ListingMediaRepository : GenericRepository<ListingMedia>, IListingMediaRepository { public ListingMediaRepository(AppDbContext ctx) : base(ctx) { } }
public class ListingViewRepository : GenericRepository<ListingView>, IListingViewRepository { public ListingViewRepository(AppDbContext ctx) : base(ctx) { } }
public class ListingCommentRepository : GenericRepository<ListingComment>, IListingCommentRepository
{
    private readonly AppDbContext _context;

    public ListingCommentRepository(AppDbContext ctx) : base(ctx)
    {
        _context = ctx;
    }

    public async Task<ListingComment?> GetByIdWithDetailsAsync(long commentId, CancellationToken cancellationToken = default)
    {
        return await _context.ListingComments
            .Include(c => c.User)
                .ThenInclude(u => u!.UserProfiles)
            .Include(c => c.Replies)
            .FirstOrDefaultAsync(c => c.CommentId == commentId, cancellationToken);
    }

    public async Task<(int Total, IReadOnlyList<ListingComment> Items)> GetByListingIdAsync(
        long listingId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ListingComments
            .Include(c => c.User)
                .ThenInclude(u => u!.UserProfiles)
            .Include(c => c.Replies)
            .Where(c => c.ListingId == listingId && c.ParentId == null)
            .OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (total, items);
    }

    public async Task<(int Total, IReadOnlyList<ListingComment> Items)> GetRepliesByParentIdAsync(
        long listingId,
        long parentId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ListingComments
            .Include(c => c.User)
                .ThenInclude(u => u!.UserProfiles)
            .Include(c => c.Replies)
            .Where(c => c.ListingId == listingId && c.ParentId == parentId)
            .OrderBy(c => c.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (total, items);
    }
}

public class ManualReviewQueueRepository : GenericRepository<ManualReviewQueue>, IManualReviewQueueRepository { public ManualReviewQueueRepository(AppDbContext ctx) : base(ctx) { } }
public class MarketPriceRepository : GenericRepository<MarketPrice>, IMarketPriceRepository { public MarketPriceRepository(AppDbContext ctx) : base(ctx) { } }
public class MessageRepository : GenericRepository<Message>, IMessageRepository { public MessageRepository(AppDbContext ctx) : base(ctx) { } }
public class NotificationRepository : GenericRepository<Notification>, INotificationRepository { public NotificationRepository(AppDbContext ctx) : base(ctx) { } }
public class OrderRepository : GenericRepository<Order>, IOrderRepository { public OrderRepository(AppDbContext ctx) : base(ctx) { } }
public class OrderItemRepository : GenericRepository<OrderItem>, IOrderItemRepository { public OrderItemRepository(AppDbContext ctx) : base(ctx) { } }
public class OrderTransactionRepository : GenericRepository<OrderTransaction>, IOrderTransactionRepository { public OrderTransactionRepository(AppDbContext ctx) : base(ctx) { } }
public class OrderInvoiceRepository : GenericRepository<OrderInvoice>, IOrderInvoiceRepository { public OrderInvoiceRepository(AppDbContext ctx) : base(ctx) { } }
public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository { public PaymentRepository(AppDbContext ctx) : base(ctx) { } }
public class PaymentLogRepository : GenericRepository<PaymentLog>, IPaymentLogRepository { public PaymentLogRepository(AppDbContext ctx) : base(ctx) { } }
public class TransferRepository : GenericRepository<Transfer>, ITransferRepository { public TransferRepository(AppDbContext ctx) : base(ctx) { } }
public class TransferTransactionRepository : GenericRepository<TransferTransaction>, ITransferTransactionRepository { public TransferTransactionRepository(AppDbContext ctx) : base(ctx) { } }
public class PointTransactionRepository : GenericRepository<PointTransaction>, IPointTransactionRepository { public PointTransactionRepository(AppDbContext ctx) : base(ctx) { } }
public class ReportRepository : GenericRepository<Report>, IReportRepository { public ReportRepository(AppDbContext ctx) : base(ctx) { } }
public class SavedListingRepository : GenericRepository<SavedListing>, ISavedListingRepository { public SavedListingRepository(AppDbContext ctx) : base(ctx) { } }
public class SearchHistoryRepository : GenericRepository<SearchHistory>, ISearchHistoryRepository { public SearchHistoryRepository(AppDbContext ctx) : base(ctx) { } }
public class ShippingRequestRepository : GenericRepository<ShippingRequest>, IShippingRequestRepository { public ShippingRequestRepository(AppDbContext ctx) : base(ctx) { } }
public class SubCategoryRepository : GenericRepository<SubCategory>, ISubCategoryRepository { public SubCategoryRepository(AppDbContext ctx) : base(ctx) { } }
public class SubscriptionPlanRepository : GenericRepository<SubscriptionPlan>, ISubscriptionPlanRepository { public SubscriptionPlanRepository(AppDbContext ctx) : base(ctx) { } }
public class SubscriptionPlanAuditRepository : GenericRepository<SubscriptionPlanAudit>, ISubscriptionPlanAuditRepository { public SubscriptionPlanAuditRepository(AppDbContext ctx) : base(ctx) { } }
public class SupportTicketRepository : GenericRepository<SupportTicket>, ISupportTicketRepository { public SupportTicketRepository(AppDbContext ctx) : base(ctx) { } }
public class UserRepository : GenericRepository<User>, IUserRepository { public UserRepository(AppDbContext ctx) : base(ctx) { } }
public class UserDeviceRepository : GenericRepository<UserDevice>, IUserDeviceRepository { public UserDeviceRepository(AppDbContext ctx) : base(ctx) { } }
public class UserPointRepository : GenericRepository<UserPoint>, IUserPointRepository { public UserPointRepository(AppDbContext ctx) : base(ctx) { } }
public class UserProfileRepository : GenericRepository<UserProfile>, IUserProfileRepository { public UserProfileRepository(AppDbContext ctx) : base(ctx) { } }
public class UserRatingRepository : GenericRepository<UserRating>, IUserRatingRepository { public UserRatingRepository(AppDbContext ctx) : base(ctx) { } }
public class UserVerificationRepository : GenericRepository<UserVerification>, IUserVerificationRepository { public UserVerificationRepository(AppDbContext ctx) : base(ctx) { } }
public class WardRepository : GenericRepository<Ward>, IWardRepository { public WardRepository(AppDbContext ctx) : base(ctx) { } }
public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository { public RefreshTokenRepository(AppDbContext ctx) : base(ctx) { } }
public class VerificationCodeRepository : GenericRepository<VerificationCode>, IVerificationCodeRepository { public VerificationCodeRepository(AppDbContext ctx) : base(ctx) { } }

public class UnitOfWork : IUnitOfWork
{
    private bool _disposed;
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        ActivityLogs = new ActivityLogRepository(_context);
        AiModerationLogs = new AiModerationLogRepository(_context);
        AiScanResults = new AiScanResultRepository(_context);
        AiAnalysisLogs = new AiAnalysisLogRepository(_context);
        AiImageVisionCaches = new AiImageVisionCacheRepository(_context);
        ApiLogs = new ApiLogRepository(_context);
        Categories = new CategoryRepository(_context);
        Carts = new CartRepository(_context);
        CartItems = new CartItemRepository(_context);
        Chats = new ChatRepository(_context);
        ChatbotLogs = new ChatbotLogRepository(_context);
        Cities = new CityRepository(_context);
        DeviceLogs = new DeviceLogRepository(_context);
        Districts = new DistrictRepository(_context);
        EscrowContracts = new EscrowContractRepository(_context);
        EscrowTransactions = new EscrowTransactionRepository(_context);
        Banks = new BankRepository(_context);
        FixerAssignments = new FixerAssignmentRepository(_context);
        FixerRequests = new FixerRequestRepository(_context);
        FixerServices = new FixerServiceRepository(_context);
        Listings = new ListingRepository(_context);
        ListingAttributes = new ListingAttributeRepository(_context);
        ListingMedias = new ListingMediaRepository(_context);
        ListingViews = new ListingViewRepository(_context);
        ListingComments = new ListingCommentRepository(_context);
        ManualReviewQueues = new ManualReviewQueueRepository(_context);
        MarketPrices = new MarketPriceRepository(_context);
        Messages = new MessageRepository(_context);
        Notifications = new NotificationRepository(_context);
        Orders = new OrderRepository(_context);
        OrderItems = new OrderItemRepository(_context);
        OrderTransactions = new OrderTransactionRepository(_context);
        OrderInvoices = new OrderInvoiceRepository(_context);
        Payments = new PaymentRepository(_context);
        PaymentLogs = new PaymentLogRepository(_context);
        Transfers = new TransferRepository(_context);
        TransferTransactions = new TransferTransactionRepository(_context);
        PointTransactions = new PointTransactionRepository(_context);
        Reports = new ReportRepository(_context);
        SavedListings = new SavedListingRepository(_context);
        SearchHistories = new SearchHistoryRepository(_context);
        ShippingRequests = new ShippingRequestRepository(_context);
        SubCategories = new SubCategoryRepository(_context);
        SubscriptionPlans = new SubscriptionPlanRepository(_context);
        SubscriptionPlanAudits = new SubscriptionPlanAuditRepository(_context);
        SupportTickets = new SupportTicketRepository(_context);
        Users = new UserRepository(_context);
        UserDevices = new UserDeviceRepository(_context);
        UserPoints = new UserPointRepository(_context);
        UserProfiles = new UserProfileRepository(_context);
        UserRatings = new UserRatingRepository(_context);
        UserVerifications = new UserVerificationRepository(_context);
        Wards = new WardRepository(_context);
        RefreshTokens = new RefreshTokenRepository(_context);
        VerificationCodes = new VerificationCodeRepository(_context);
    }

    public IActivityLogRepository ActivityLogs { get; }
    public IAiModerationLogRepository AiModerationLogs { get; }
    public IAiScanResultRepository AiScanResults { get; }
    public IAiAnalysisLogRepository AiAnalysisLogs { get; }
    public IAiImageVisionCacheRepository AiImageVisionCaches { get; }
    public IApiLogRepository ApiLogs { get; }
    public ICategoryRepository Categories { get; }
    public ICartRepository Carts { get; }
    public ICartItemRepository CartItems { get; }
    public IChatRepository Chats { get; }
    public IChatbotLogRepository ChatbotLogs { get; }
    public ICityRepository Cities { get; }
    public IDeviceLogRepository DeviceLogs { get; }
    public IDistrictRepository Districts { get; }
    public IEscrowContractRepository EscrowContracts { get; }
    public IEscrowTransactionRepository EscrowTransactions { get; }
    public IBankRepository Banks { get; }
    public IFixerAssignmentRepository FixerAssignments { get; }
    public IFixerRequestRepository FixerRequests { get; }
    public IFixerServiceRepository FixerServices { get; }
    public IListingRepository Listings { get; }
    public IListingAttributeRepository ListingAttributes { get; }
    public IListingMediaRepository ListingMedias { get; }
    public IListingViewRepository ListingViews { get; }
    public IListingCommentRepository ListingComments { get; }
    public IManualReviewQueueRepository ManualReviewQueues { get; }
    public IMarketPriceRepository MarketPrices { get; }
    public IMessageRepository Messages { get; }
    public INotificationRepository Notifications { get; }
    public IOrderRepository Orders { get; }
    public IOrderItemRepository OrderItems { get; }
    public IOrderTransactionRepository OrderTransactions { get; }
    public IOrderInvoiceRepository OrderInvoices { get; }
    public IPaymentRepository Payments { get; }
    public IPaymentLogRepository PaymentLogs { get; }
    public ITransferRepository Transfers { get; }
    public ITransferTransactionRepository TransferTransactions { get; }
    public IPointTransactionRepository PointTransactions { get; }
    public IReportRepository Reports { get; }
    public ISavedListingRepository SavedListings { get; }
    public ISearchHistoryRepository SearchHistories { get; }
    public IShippingRequestRepository ShippingRequests { get; }
    public ISubCategoryRepository SubCategories { get; }
    public ISubscriptionPlanRepository SubscriptionPlans { get; }
    public ISubscriptionPlanAuditRepository SubscriptionPlanAudits { get; }
    public ISupportTicketRepository SupportTickets { get; }
    public IUserRepository Users { get; }
    public IUserDeviceRepository UserDevices { get; }
    public IUserPointRepository UserPoints { get; }
    public IUserProfileRepository UserProfiles { get; }
    public IUserRatingRepository UserRatings { get; }
    public IUserVerificationRepository UserVerifications { get; }
    public IWardRepository Wards { get; }
    public IRefreshTokenRepository RefreshTokens { get; }
    public IVerificationCodeRepository VerificationCodes { get; }

    public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
        }

        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}
