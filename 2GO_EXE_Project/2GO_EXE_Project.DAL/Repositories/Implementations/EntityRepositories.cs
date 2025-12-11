using _2GO_EXE_Project.DAL.Context;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.DAL.Repositories.Implementations;

public class ActivityLogRepository : GenericRepository<ActivityLog>, IActivityLogRepository { public ActivityLogRepository(AppDbContext ctx) : base(ctx) { } }
public class AiModerationLogRepository : GenericRepository<AiModerationLog>, IAiModerationLogRepository { public AiModerationLogRepository(AppDbContext ctx) : base(ctx) { } }
public class AiScanResultRepository : GenericRepository<AiScanResult>, IAiScanResultRepository { public AiScanResultRepository(AppDbContext ctx) : base(ctx) { } }
public class ApiLogRepository : GenericRepository<ApiLog>, IApiLogRepository { public ApiLogRepository(AppDbContext ctx) : base(ctx) { } }
public class CategoryRepository : GenericRepository<Category>, ICategoryRepository { public CategoryRepository(AppDbContext ctx) : base(ctx) { } }
public class ChatRepository : GenericRepository<Chat>, IChatRepository { public ChatRepository(AppDbContext ctx) : base(ctx) { } }
public class CityRepository : GenericRepository<City>, ICityRepository { public CityRepository(AppDbContext ctx) : base(ctx) { } }
public class DeviceLogRepository : GenericRepository<DeviceLog>, IDeviceLogRepository { public DeviceLogRepository(AppDbContext ctx) : base(ctx) { } }
public class DistrictRepository : GenericRepository<District>, IDistrictRepository { public DistrictRepository(AppDbContext ctx) : base(ctx) { } }
public class EscrowContractRepository : GenericRepository<EscrowContract>, IEscrowContractRepository { public EscrowContractRepository(AppDbContext ctx) : base(ctx) { } }
public class EscrowTransactionRepository : GenericRepository<EscrowTransaction>, IEscrowTransactionRepository { public EscrowTransactionRepository(AppDbContext ctx) : base(ctx) { } }
public class FixerAssignmentRepository : GenericRepository<FixerAssignment>, IFixerAssignmentRepository { public FixerAssignmentRepository(AppDbContext ctx) : base(ctx) { } }
public class FixerRequestRepository : GenericRepository<FixerRequest>, IFixerRequestRepository { public FixerRequestRepository(AppDbContext ctx) : base(ctx) { } }
public class FixerServiceRepository : GenericRepository<FixerService>, IFixerServiceRepository { public FixerServiceRepository(AppDbContext ctx) : base(ctx) { } }
public class ListingRepository : GenericRepository<Listing>, IListingRepository { public ListingRepository(AppDbContext ctx) : base(ctx) { } }
public class ListingAttributeRepository : GenericRepository<ListingAttribute>, IListingAttributeRepository { public ListingAttributeRepository(AppDbContext ctx) : base(ctx) { } }
public class ListingImageRepository : GenericRepository<ListingImage>, IListingImageRepository { public ListingImageRepository(AppDbContext ctx) : base(ctx) { } }
public class ListingViewRepository : GenericRepository<ListingView>, IListingViewRepository { public ListingViewRepository(AppDbContext ctx) : base(ctx) { } }
public class MessageRepository : GenericRepository<Message>, IMessageRepository { public MessageRepository(AppDbContext ctx) : base(ctx) { } }
public class OrderRepository : GenericRepository<Order>, IOrderRepository { public OrderRepository(AppDbContext ctx) : base(ctx) { } }
public class OrderItemRepository : GenericRepository<OrderItem>, IOrderItemRepository { public OrderItemRepository(AppDbContext ctx) : base(ctx) { } }
public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository { public PaymentRepository(AppDbContext ctx) : base(ctx) { } }
public class PaymentLogRepository : GenericRepository<PaymentLog>, IPaymentLogRepository { public PaymentLogRepository(AppDbContext ctx) : base(ctx) { } }
public class PointTransactionRepository : GenericRepository<PointTransaction>, IPointTransactionRepository { public PointTransactionRepository(AppDbContext ctx) : base(ctx) { } }
public class ReportRepository : GenericRepository<Report>, IReportRepository { public ReportRepository(AppDbContext ctx) : base(ctx) { } }
public class SavedListingRepository : GenericRepository<SavedListing>, ISavedListingRepository { public SavedListingRepository(AppDbContext ctx) : base(ctx) { } }
public class SearchHistoryRepository : GenericRepository<SearchHistory>, ISearchHistoryRepository { public SearchHistoryRepository(AppDbContext ctx) : base(ctx) { } }
public class ShippingRequestRepository : GenericRepository<ShippingRequest>, IShippingRequestRepository { public ShippingRequestRepository(AppDbContext ctx) : base(ctx) { } }
public class SubCategoryRepository : GenericRepository<SubCategory>, ISubCategoryRepository { public SubCategoryRepository(AppDbContext ctx) : base(ctx) { } }
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
        ApiLogs = new ApiLogRepository(_context);
        Categories = new CategoryRepository(_context);
        Chats = new ChatRepository(_context);
        Cities = new CityRepository(_context);
        DeviceLogs = new DeviceLogRepository(_context);
        Districts = new DistrictRepository(_context);
        EscrowContracts = new EscrowContractRepository(_context);
        EscrowTransactions = new EscrowTransactionRepository(_context);
        FixerAssignments = new FixerAssignmentRepository(_context);
        FixerRequests = new FixerRequestRepository(_context);
        FixerServices = new FixerServiceRepository(_context);
        Listings = new ListingRepository(_context);
        ListingAttributes = new ListingAttributeRepository(_context);
        ListingImages = new ListingImageRepository(_context);
        ListingViews = new ListingViewRepository(_context);
        Messages = new MessageRepository(_context);
        Orders = new OrderRepository(_context);
        OrderItems = new OrderItemRepository(_context);
        Payments = new PaymentRepository(_context);
        PaymentLogs = new PaymentLogRepository(_context);
        PointTransactions = new PointTransactionRepository(_context);
        Reports = new ReportRepository(_context);
        SavedListings = new SavedListingRepository(_context);
        SearchHistories = new SearchHistoryRepository(_context);
        ShippingRequests = new ShippingRequestRepository(_context);
        SubCategories = new SubCategoryRepository(_context);
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
    public IApiLogRepository ApiLogs { get; }
    public ICategoryRepository Categories { get; }
    public IChatRepository Chats { get; }
    public ICityRepository Cities { get; }
    public IDeviceLogRepository DeviceLogs { get; }
    public IDistrictRepository Districts { get; }
    public IEscrowContractRepository EscrowContracts { get; }
    public IEscrowTransactionRepository EscrowTransactions { get; }
    public IFixerAssignmentRepository FixerAssignments { get; }
    public IFixerRequestRepository FixerRequests { get; }
    public IFixerServiceRepository FixerServices { get; }
    public IListingRepository Listings { get; }
    public IListingAttributeRepository ListingAttributes { get; }
    public IListingImageRepository ListingImages { get; }
    public IListingViewRepository ListingViews { get; }
    public IMessageRepository Messages { get; }
    public IOrderRepository Orders { get; }
    public IOrderItemRepository OrderItems { get; }
    public IPaymentRepository Payments { get; }
    public IPaymentLogRepository PaymentLogs { get; }
    public IPointTransactionRepository PointTransactions { get; }
    public IReportRepository Reports { get; }
    public ISavedListingRepository SavedListings { get; }
    public ISearchHistoryRepository SearchHistories { get; }
    public IShippingRequestRepository ShippingRequests { get; }
    public ISubCategoryRepository SubCategories { get; }
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
