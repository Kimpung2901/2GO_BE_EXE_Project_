using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class OrderTransaction
{
    [Key]
    public int TransactionId { get; set; }

    // Reference fields
    public long OrderId { get; set; }

    public long OrderCode { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? PaymentLinkId { get; set; }

    // Transaction details
    [StringLength(255)]
    [Unicode(false)]
    public string Reference { get; set; } = "";

    public long Amount { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string AccountNumber { get; set; } = "";

    [StringLength(500)]
    public string Description { get; set; } = "";

    public DateTimeOffset TransactionDateTime { get; set; }

    [StringLength(255)]
    public string? VirtualAccountName { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? VirtualAccountNumber { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? CounterAccountBankId { get; set; }

    [StringLength(255)]
    public string? CounterAccountBankName { get; set; }

    [StringLength(255)]
    public string? CounterAccountName { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? CounterAccountNumber { get; set; }

    // Navigation property
    [ForeignKey("OrderId")]
    [InverseProperty("OrderTransactions")]
    public virtual Order? Order { get; set; }

    [InverseProperty("Transaction")]
    public virtual ICollection<OrderInvoice> OrderInvoices { get; set; } = new List<OrderInvoice>();
}
