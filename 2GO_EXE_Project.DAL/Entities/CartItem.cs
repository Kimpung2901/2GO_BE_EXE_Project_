using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class CartItem
{
    [Key]
    public long CartItemId { get; set; }

    public long? CartId { get; set; }

    public long? ListingId { get; set; }

    public long? SellerId { get; set; }

    public long? VariantId { get; set; }

    public int? Quantity { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal? PriceSnapshot { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal? OriginalPrice { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? Currency { get; set; }

    public string? VariantSnapshot { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public bool? IsSelected { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("CartId")]
    [InverseProperty("CartItems")]
    public virtual Cart? Cart { get; set; }

    [ForeignKey("ListingId")]
    [InverseProperty("CartItems")]
    public virtual Listing? Listing { get; set; }

    [ForeignKey("SellerId")]
    [InverseProperty("CartItemsAsSeller")]
    public virtual User? Seller { get; set; }
}
