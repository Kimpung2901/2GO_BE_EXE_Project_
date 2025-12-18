using _2GO_EXE_Project.DAL.Entities;

namespace _2GO_EXE_Project.DAL.Repositories.Interfaces;

public interface IActivityLogRepository : IGenericRepository<ActivityLog> { }
public interface IAiModerationLogRepository : IGenericRepository<AiModerationLog> { }
public interface IAiScanResultRepository : IGenericRepository<AiScanResult> { }
public interface IApiLogRepository : IGenericRepository<ApiLog> { }
public interface ICategoryRepository : IGenericRepository<Category> { }
public interface IChatRepository : IGenericRepository<Chat> { }
public interface ICityRepository : IGenericRepository<City> { }
public interface IDeviceLogRepository : IGenericRepository<DeviceLog> { }
public interface IDistrictRepository : IGenericRepository<District> { }
public interface IEscrowContractRepository : IGenericRepository<EscrowContract> { }
public interface IEscrowTransactionRepository : IGenericRepository<EscrowTransaction> { }
public interface IFixerAssignmentRepository : IGenericRepository<FixerAssignment> { }
public interface IFixerRequestRepository : IGenericRepository<FixerRequest> { }
public interface IFixerServiceRepository : IGenericRepository<FixerService> { }
public interface IListingRepository : IGenericRepository<Listing> { }
public interface IListingAttributeRepository : IGenericRepository<ListingAttribute> { }
public interface IListingImageRepository : IGenericRepository<ListingImage> { }
public interface IListingViewRepository : IGenericRepository<ListingView> { }
public interface IMessageRepository : IGenericRepository<Message> { }
public interface IOrderRepository : IGenericRepository<Order> { }
public interface IOrderItemRepository : IGenericRepository<OrderItem> { }
public interface IPaymentRepository : IGenericRepository<Payment> { }
public interface IPaymentLogRepository : IGenericRepository<PaymentLog> { }
public interface IPointTransactionRepository : IGenericRepository<PointTransaction> { }
public interface IReportRepository : IGenericRepository<Report> { }
public interface ISavedListingRepository : IGenericRepository<SavedListing> { }
public interface ISearchHistoryRepository : IGenericRepository<SearchHistory> { }
public interface IShippingRequestRepository : IGenericRepository<ShippingRequest> { }
public interface ISubCategoryRepository : IGenericRepository<SubCategory> { }
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
