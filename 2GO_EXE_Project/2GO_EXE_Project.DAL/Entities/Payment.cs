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

    [StringLength(255)]
    [Unicode(false)]
    public string? ReferenceCode { get; set; }

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
}
