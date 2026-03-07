using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class OrderInvoice
{
    [Key]
    public int InvoiceId { get; set; }

    // Reference fields
    public long OrderId { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? InvoiceNumber { get; set; }

    public long? IssuedTimestamp { get; set; }

    public DateTime? IssuedDatetime { get; set; }

    public int? TransactionId { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? ReservationCode { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? CodeOfTax { get; set; }

    public long? PaymentId { get; set; }

    // Navigation properties
    [ForeignKey("OrderId")]
    [InverseProperty("OrderInvoices")]
    public virtual Order? Order { get; set; }

    [ForeignKey("PaymentId")]
    [InverseProperty("OrderInvoices")]
    public virtual Payment? Payment { get; set; }

    [ForeignKey("TransactionId")]  
    [InverseProperty("OrderInvoices")]
    public virtual OrderTransaction? Transaction { get; set; }
}
