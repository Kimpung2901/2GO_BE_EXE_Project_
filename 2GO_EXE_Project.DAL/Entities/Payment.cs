using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class Payment
{
    [Key]
    public long PaymentId { get; set; }

    public long? UserId { get; set; }

    public long? OrderId { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal? Amount { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Method { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? PaymentStage { get; set; }

    [StringLength(50)]
    public string? PaymentType { get; set; }  // "DEPOSIT", "FULL", "REMAINING"

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? CommissionRate { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal? CommissionBaseAmount { get; set; }

    public int? SubscriptionDays { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? SubscriptionPlanCode { get; set; }

    public DateTime? SubscriptionValidFrom { get; set; }

    public DateTime? SubscriptionValidUntil { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? ReferenceCode { get; set; }

    public long? PayosOrderCode { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? PayosPaymentLinkId { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? PayosCheckoutUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    [InverseProperty("Payment")]
    public virtual ICollection<PaymentLog> PaymentLogs { get; set; } = new List<PaymentLog>();

    [ForeignKey("UserId")]
    [InverseProperty("Payments")]
    public virtual User? User { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("Payments")]
    public virtual Order? Order { get; set; }

    [InverseProperty("Payment")]
    public virtual ICollection<EscrowContract> EscrowContracts { get; set; } = new List<EscrowContract>();

    [InverseProperty("Payment")]
    public virtual ICollection<OrderInvoice> OrderInvoices { get; set; } = new List<OrderInvoice>();
}
