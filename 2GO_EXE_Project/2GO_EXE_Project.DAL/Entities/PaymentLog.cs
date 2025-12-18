using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class PaymentLog
{
    [Key]
    public long LogId { get; set; }

    public long? PaymentId { get; set; }

    public string? RawResponse { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("PaymentId")]
    [InverseProperty("PaymentLogs")]
    public virtual Payment? Payment { get; set; }
}
