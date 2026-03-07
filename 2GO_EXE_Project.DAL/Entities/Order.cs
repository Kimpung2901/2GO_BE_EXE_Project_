using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class Order
{
    [Key]
    public long OrderId { get; set; }

    public long? BuyerId { get; set; }

    public long? SellerId { get; set; }

    public long? ListingId { get; set; }

    public long? EscrowId { get; set; }

    public long OrderCode { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string PaymentLinkId { get; set; } = "";

    [Column(TypeName = "decimal(15, 2)")]
    public decimal? TotalAmount { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? PaymentMethod { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? DeliveryPhone { get; set; }

    public string? CheckoutUrl { get; set; }

    public string? QrCodeUrl { get; set; }

    public DateTime? PaymentExpiredAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("BuyerId")]
    [InverseProperty("OrderBuyers")]
    public virtual User? Buyer { get; set; }

    [ForeignKey("EscrowId")]
    [InverseProperty("Order")]
    public virtual EscrowContract? Escrow { get; set; }

    [ForeignKey("ListingId")]
    [InverseProperty("Orders")]
    public virtual Listing? Listing { get; set; }

    [InverseProperty("Order")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [ForeignKey("SellerId")]
    [InverseProperty("OrderSellers")]
    public virtual User? Seller { get; set; }

    [InverseProperty("Order")]
    public virtual ICollection<ShippingRequest> ShippingRequests { get; set; } = new List<ShippingRequest>();

    [InverseProperty("Order")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [InverseProperty("Order")]
    public virtual ICollection<OrderTransaction> OrderTransactions { get; set; } = new List<OrderTransaction>();

    [InverseProperty("Order")]
    public virtual ICollection<OrderInvoice> OrderInvoices { get; set; } = new List<OrderInvoice>();
}
