using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class EscrowContract
{
    [Key]
    public long EscrowId { get; set; }

    public long? BuyerId { get; set; }

    public long? SellerId { get; set; }

    public long? ListingId { get; set; }

    public long? OrderId { get; set; }

    public long? PaymentId { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal? DepositAmount { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal? TotalAmount { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("BuyerId")]
    [InverseProperty("EscrowContractBuyers")]
    public virtual User? Buyer { get; set; }

    [InverseProperty("Escrow")]
    public virtual ICollection<EscrowTransaction> EscrowTransactions { get; set; } = new List<EscrowTransaction>();

    [ForeignKey("ListingId")]
    [InverseProperty("EscrowContracts")]
    public virtual Listing? Listing { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("EscrowContracts")]
    public virtual Order? Order { get; set; }

    [ForeignKey("PaymentId")]
    [InverseProperty("EscrowContracts")]
    public virtual Payment? Payment { get; set; }
    [ForeignKey("SellerId")]
    [InverseProperty("EscrowContractSellers")]
    public virtual User? Seller { get; set; }

    [InverseProperty("Escrow")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
