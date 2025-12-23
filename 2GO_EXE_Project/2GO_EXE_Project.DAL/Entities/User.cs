using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class User
{
    [Key]
    public long UserId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? Phone { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Email { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? PasswordHash { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Salt { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Role { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    public DateTime? BanUntil { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    [InverseProperty("User1")]
    public virtual ICollection<Chat> ChatUser1s { get; set; } = new List<Chat>();

    [InverseProperty("User2")]
    public virtual ICollection<Chat> ChatUser2s { get; set; } = new List<Chat>();

    [InverseProperty("User")]
    public virtual ICollection<DeviceLog> DeviceLogs { get; set; } = new List<DeviceLog>();

    [InverseProperty("Buyer")]
    public virtual ICollection<EscrowContract> EscrowContractBuyers { get; set; } = new List<EscrowContract>();

    [InverseProperty("Seller")]
    public virtual ICollection<EscrowContract> EscrowContractSellers { get; set; } = new List<EscrowContract>();

    [InverseProperty("FixerUser")]
    public virtual ICollection<FixerAssignment> FixerAssignments { get; set; } = new List<FixerAssignment>();

    [InverseProperty("User")]
    public virtual ICollection<FixerRequest> FixerRequests { get; set; } = new List<FixerRequest>();

    [InverseProperty("User")]
    public virtual ICollection<ListingView> ListingViews { get; set; } = new List<ListingView>();

    [InverseProperty("Seller")]
    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();

    [InverseProperty("Sender")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    [InverseProperty("Buyer")]
    public virtual ICollection<Order> OrderBuyers { get; set; } = new List<Order>();

    [InverseProperty("Seller")]
    public virtual ICollection<Order> OrderSellers { get; set; } = new List<Order>();

    [InverseProperty("User")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [InverseProperty("User")]
    public virtual ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();

    [InverseProperty("Reporter")]
    public virtual ICollection<Report> ReportReporters { get; set; } = new List<Report>();

    [InverseProperty("TargetUser")]
    public virtual ICollection<Report> ReportTargetUsers { get; set; } = new List<Report>();

    [InverseProperty("User")]
    public virtual ICollection<SavedListing> SavedListings { get; set; } = new List<SavedListing>();

    [InverseProperty("User")]
    public virtual ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();

    [InverseProperty("User")]
    public virtual ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();

    [InverseProperty("User")]
    public virtual ICollection<UserDevice> UserDevices { get; set; } = new List<UserDevice>();

    [InverseProperty("User")]
    public virtual ICollection<UserPoint> UserPoints { get; set; } = new List<UserPoint>();

    [InverseProperty("User")]
    public virtual ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();

    [InverseProperty("RatedUser")]
    public virtual ICollection<UserRating> UserRatingRatedUsers { get; set; } = new List<UserRating>();

    [InverseProperty("Rater")]
    public virtual ICollection<UserRating> UserRatingRaters { get; set; } = new List<UserRating>();

    [InverseProperty("User")]
    public virtual ICollection<UserVerification> UserVerifications { get; set; } = new List<UserVerification>();

    [InverseProperty("User")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [InverseProperty("User")]
    public virtual ICollection<VerificationCode> VerificationCodes { get; set; } = new List<VerificationCode>();
}
