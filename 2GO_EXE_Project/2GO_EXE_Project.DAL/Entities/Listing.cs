using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class Listing
{
    [Key]
    public long ListingId { get; set; }

    public long? SellerId { get; set; }

    public int? SubCategoryId { get; set; }

    public int? WardId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Condition { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal? Price { get; set; }

    public bool? HasNegotiation { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Dimensions { get; set; }

    public double? Weight { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Brand { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Listing")]
    public virtual ICollection<AiModerationLog> AiModerationLogs { get; set; } = new List<AiModerationLog>();

    [InverseProperty("Listing")]
    public virtual ICollection<AiScanResult> AiScanResults { get; set; } = new List<AiScanResult>();

    [InverseProperty("Listing")]
    public virtual ICollection<EscrowContract> EscrowContracts { get; set; } = new List<EscrowContract>();

    [InverseProperty("Listing")]
    public virtual ICollection<FixerRequest> FixerRequests { get; set; } = new List<FixerRequest>();

    [InverseProperty("Listing")]
    public virtual ICollection<ListingAttribute> ListingAttributes { get; set; } = new List<ListingAttribute>();

    [InverseProperty("Listing")]
    public virtual ICollection<ListingImage> ListingImages { get; set; } = new List<ListingImage>();

    [InverseProperty("Listing")]
    public virtual ICollection<ListingView> ListingViews { get; set; } = new List<ListingView>();

    [InverseProperty("Listing")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [InverseProperty("Listing")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [InverseProperty("Listing")]
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    [InverseProperty("Listing")]
    public virtual ICollection<SavedListing> SavedListings { get; set; } = new List<SavedListing>();

    [ForeignKey("SellerId")]
    [InverseProperty("Listings")]
    public virtual User? Seller { get; set; }

    [ForeignKey("SubCategoryId")]
    [InverseProperty("Listings")]
    public virtual SubCategory? SubCategory { get; set; }

    [ForeignKey("WardId")]
    [InverseProperty("Listings")]
    public virtual Ward? Ward { get; set; }
}
