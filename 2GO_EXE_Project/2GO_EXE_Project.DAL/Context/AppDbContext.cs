using _2GO_EXE_Project.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace _2GO_EXE_Project.DAL.Context;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<AiModerationLog> AiModerationLogs { get; set; }

    public virtual DbSet<AiScanResult> AiScanResults { get; set; }

    public virtual DbSet<ApiLog> ApiLogs { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<DeviceLog> DeviceLogs { get; set; }

    public virtual DbSet<District> Districts { get; set; }

    public virtual DbSet<EscrowContract> EscrowContracts { get; set; }

    public virtual DbSet<EscrowTransaction> EscrowTransactions { get; set; }

    public virtual DbSet<FixerAssignment> FixerAssignments { get; set; }

    public virtual DbSet<FixerRequest> FixerRequests { get; set; }

    public virtual DbSet<FixerService> FixerServices { get; set; }

    public virtual DbSet<Listing> Listings { get; set; }

    public virtual DbSet<ListingAttribute> ListingAttributes { get; set; }

    public virtual DbSet<ListingImage> ListingImages { get; set; }

    public virtual DbSet<ListingView> ListingViews { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentLog> PaymentLogs { get; set; }

    public virtual DbSet<PointTransaction> PointTransactions { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<SavedListing> SavedListings { get; set; }

    public virtual DbSet<SearchHistory> SearchHistories { get; set; }

    public virtual DbSet<ShippingRequest> ShippingRequests { get; set; }


    public virtual DbSet<SubCategory> SubCategories { get; set; }

    public virtual DbSet<SupportTicket> SupportTickets { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserDevice> UserDevices { get; set; }

    public virtual DbSet<UserPoint> UserPoints { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<UserRating> UserRatings { get; set; }

    public virtual DbSet<UserVerification> UserVerifications { get; set; }

    public virtual DbSet<Ward> Wards { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<VerificationCode> VerificationCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Activity__5E5486480DB36993");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs).HasConstraintName("FK_ActLogs_Users");
        });

        modelBuilder.Entity<AiModerationLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__AiModera__5E5486480174A9B8");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Listing).WithMany(p => p.AiModerationLogs).HasConstraintName("FK_AiLogs_Listings");
        });

        modelBuilder.Entity<AiScanResult>(entity =>
        {
            entity.HasKey(e => e.ScanId).HasName("PK__AiScanRe__63B32681BFCE5E87");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Listing).WithMany(p => p.AiScanResults).HasConstraintName("FK_AiScan_Listings");
        });

        modelBuilder.Entity<ApiLog>(entity =>
        {
            entity.HasKey(e => e.ApiId).HasName("PK__ApiLogs__024B3BB3924051E4");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0B23FCD2DD");
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.ChatId).HasName("PK__Chats__A9FBE7C6633500B5");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User1).WithMany(p => p.ChatUser1s).HasConstraintName("FK_Chat_User1");

            entity.HasOne(d => d.User2).WithMany(p => p.ChatUser2s).HasConstraintName("FK_Chat_User2");
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.CityId).HasName("PK__Cities__F2D21B76C2FE3969");
        });

        modelBuilder.Entity<DeviceLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__DeviceLo__5E5486484AA0DD95");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.DeviceLogs).HasConstraintName("FK_DeviceLogs_Users");
        });

        modelBuilder.Entity<District>(entity =>
        {
            entity.HasKey(e => e.DistrictId).HasName("PK__District__85FDA4C64D9546B6");

            entity.HasOne(d => d.City).WithMany(p => p.Districts).HasConstraintName("FK_Districts_Cities");
        });

        modelBuilder.Entity<EscrowContract>(entity =>
        {
            entity.HasKey(e => e.EscrowId).HasName("PK__EscrowCo__557665D406EDB275");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Buyer).WithMany(p => p.EscrowContractBuyers).HasConstraintName("FK_Escrow_Buyer");

            entity.HasOne(d => d.Listing).WithMany(p => p.EscrowContracts).HasConstraintName("FK_Escrow_Listing");

            entity.HasOne(d => d.Seller).WithMany(p => p.EscrowContractSellers).HasConstraintName("FK_Escrow_Seller");
        });

        modelBuilder.Entity<EscrowTransaction>(entity =>
        {
            entity.HasKey(e => e.TxId).HasName("PK__EscrowTr__F9FA65FC96EF3600");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Escrow).WithMany(p => p.EscrowTransactions).HasConstraintName("FK_EscrowTx_Escrow");
        });

        modelBuilder.Entity<FixerAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId).HasName("PK__FixerAss__32499E77DDD307A5");

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.FixerUser).WithMany(p => p.FixerAssignments).HasConstraintName("FK_Assign_User");

            entity.HasOne(d => d.Request).WithMany(p => p.FixerAssignments).HasConstraintName("FK_Assign_Request");
        });

        modelBuilder.Entity<FixerRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__FixerReq__33A8517AE1259366");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Listing).WithMany(p => p.FixerRequests).HasConstraintName("FK_FixerReq_Listings");

            entity.HasOne(d => d.Service).WithMany(p => p.FixerRequests).HasConstraintName("FK_FixerReq_Service");

            entity.HasOne(d => d.User).WithMany(p => p.FixerRequests).HasConstraintName("FK_FixerReq_Users");
        });

        modelBuilder.Entity<FixerService>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__FixerSer__C51BB00AD5B142A7");
        });

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.HasKey(e => e.ListingId).HasName("PK__Listings__BF3EBED0512A61BA");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.HasNegotiation).HasDefaultValue(true);

            entity.HasOne(d => d.Seller).WithMany(p => p.Listings).HasConstraintName("FK_Listings_Users");

            entity.HasOne(d => d.SubCategory).WithMany(p => p.Listings).HasConstraintName("FK_Listings_SubCat");

            entity.HasOne(d => d.Ward).WithMany(p => p.Listings).HasConstraintName("FK_Listings_Wards");
        });

        modelBuilder.Entity<ListingAttribute>(entity =>
        {
            entity.HasKey(e => e.AttributeId).HasName("PK__ListingA__C18929EA80D4FC97");

            entity.HasOne(d => d.Listing).WithMany(p => p.ListingAttributes).HasConstraintName("FK_ListingAttributes_Listings");
        });

        modelBuilder.Entity<ListingImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ListingI__7516F70C45586241");

            entity.Property(e => e.IsPrimary).HasDefaultValue(false);

            entity.HasOne(d => d.Listing).WithMany(p => p.ListingImages).HasConstraintName("FK_ListingImages_Listings");
        });

        modelBuilder.Entity<ListingView>(entity =>
        {
            entity.HasKey(e => e.ViewId).HasName("PK__ListingV__1E371CF66220BD28");

            entity.Property(e => e.ViewedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Listing).WithMany(p => p.ListingViews).HasConstraintName("FK_ListingViews_Listings");

            entity.HasOne(d => d.User).WithMany(p => p.ListingViews).HasConstraintName("FK_ListingViews_Users");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Messages__C87C0C9C5FC829B3");

            entity.Property(e => e.SentAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Chat).WithMany(p => p.Messages).HasConstraintName("FK_Messages_Chat");

            entity.HasOne(d => d.Sender).WithMany(p => p.Messages).HasConstraintName("FK_Messages_Sender");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCF29ADB7E0");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Buyer).WithMany(p => p.OrderBuyers).HasConstraintName("FK_Orders_Buyer");

            entity.HasOne(d => d.Escrow).WithMany(p => p.Orders).HasConstraintName("FK_Orders_Escrow");

            entity.HasOne(d => d.Listing).WithMany(p => p.Orders).HasConstraintName("FK_Orders_Listing");

            entity.HasOne(d => d.Seller).WithMany(p => p.OrderSellers).HasConstraintName("FK_Orders_Seller");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED0681BACC3EC9");

            entity.HasOne(d => d.Listing).WithMany(p => p.OrderItems).HasConstraintName("FK_OrderItems_Listing");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems).HasConstraintName("FK_OrderItems_Orders");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A383AF28E0A");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.Payments).HasConstraintName("FK_Payments_Users");
        });

        modelBuilder.Entity<PaymentLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__PaymentL__5E548648A3C99DC5");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Payment).WithMany(p => p.PaymentLogs).HasConstraintName("FK_PaymentLogs_Payments");
        });

        modelBuilder.Entity<PointTransaction>(entity =>
        {
            entity.HasKey(e => e.TxId).HasName("PK__PointTra__F9FA65FCC8C19C20");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.PointTransactions).HasConstraintName("FK_PointTx_Users");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__Reports__D5BD4805096823C8");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Listing).WithMany(p => p.Reports).HasConstraintName("FK_Reports_Listing");

            entity.HasOne(d => d.Reporter).WithMany(p => p.ReportReporters).HasConstraintName("FK_Reports_Reporter");

            entity.HasOne(d => d.TargetUser).WithMany(p => p.ReportTargetUsers).HasConstraintName("FK_Reports_Target");
        });

        modelBuilder.Entity<SavedListing>(entity =>
        {
            entity.HasKey(e => e.SavedId).HasName("PK__SavedLis__0B058FDCCF45C708");

            entity.Property(e => e.SavedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Listing).WithMany(p => p.SavedListings).HasConstraintName("FK_SavedListings_Listings");

            entity.HasOne(d => d.User).WithMany(p => p.SavedListings).HasConstraintName("FK_SavedListings_Users");
        });

        modelBuilder.Entity<SearchHistory>(entity =>
        {
            entity.HasKey(e => e.SearchId).HasName("PK__SearchHi__21C535F47026F753");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.SearchHistories).HasConstraintName("FK_SearchHistory_Users");
        });

        modelBuilder.Entity<ShippingRequest>(entity =>
        {
            entity.HasKey(e => e.ShipId).HasName("PK__Shipping__2A05CAB3FA7A5C07");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Order).WithMany(p => p.ShippingRequests).HasConstraintName("FK_Shipping_Orders");
        });

        modelBuilder.Entity<SubCategory>(entity =>
        {
            entity.HasKey(e => e.SubCategoryId).HasName("PK__SubCateg__26BE5B1941E8BD81");

            entity.HasOne(d => d.Category).WithMany(p => p.SubCategories).HasConstraintName("FK_SubCategories_Categories");
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__SupportT__712CC6074908E846");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.SupportTickets).HasConstraintName("FK_Tickets_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C13494ED7");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<UserDevice>(entity =>
        {
            entity.HasKey(e => e.DeviceId).HasName("PK__UserDevi__49E123115414FA0C");

            entity.Property(e => e.LastActive).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.UserDevices).HasConstraintName("FK_UserDevices_Users");
        });

        modelBuilder.Entity<UserPoint>(entity =>
        {
            entity.HasKey(e => e.PointId).HasName("PK__UserPoin__40A977E1BE40A65C");

            entity.Property(e => e.CurrentPoints).HasDefaultValue(0);
            entity.Property(e => e.LifetimePoints).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.UserPoints).HasConstraintName("FK_UserPoints_Users");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__UserProf__290C88E40622A657");

            entity.HasOne(d => d.User).WithMany(p => p.UserProfiles).HasConstraintName("FK_UserProfiles_Users");
        });

        modelBuilder.Entity<UserRating>(entity =>
        {
            entity.HasKey(e => e.RatingId).HasName("PK__UserRati__FCCDF87C065FCA12");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.RatedUser).WithMany(p => p.UserRatingRatedUsers).HasConstraintName("FK_UserRatings_Rated");

            entity.HasOne(d => d.Rater).WithMany(p => p.UserRatingRaters).HasConstraintName("FK_UserRatings_Rater");
        });

        modelBuilder.Entity<UserVerification>(entity =>
        {
            entity.HasKey(e => e.VerificationId).HasName("PK__UserVeri__306D490791FA9ECD");

            entity.Property(e => e.EmailVerified).HasDefaultValue(false);
            entity.Property(e => e.PhoneVerified).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.UserVerifications).HasConstraintName("FK_UserVer_Users");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens).HasForeignKey(d => d.UserId).HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<VerificationCode>(entity =>
        {
            entity.HasKey(e => e.VerificationCodeId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Purpose).HasMaxLength(50).IsUnicode(false);
            entity.HasOne(d => d.User).WithMany(p => p.VerificationCodes).HasForeignKey(d => d.UserId).HasConstraintName("FK_VerificationCodes_Users");
        });

        modelBuilder.Entity<Ward>(entity =>
        {
            entity.HasKey(e => e.WardId).HasName("PK__Wards__C6BD9BCAC40737D0");

            entity.HasOne(d => d.District).WithMany(p => p.Wards)   .HasConstraintName("FK_Wards_Districts");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
