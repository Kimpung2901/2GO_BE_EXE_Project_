using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class Report
{
    [Key]
    public long ReportId { get; set; }

    public long? OrderId { get; set; }

    public long? ReporterId { get; set; }

    public long? TargetUserId { get; set; }

    public long? ListingId { get; set; }

    public string? Reason { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    public long? WaitingForUserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    [ForeignKey("ListingId")]
    [InverseProperty("Reports")]
    public virtual Listing? Listing { get; set; }

    [ForeignKey("ReporterId")]
    [InverseProperty("ReportReporters")]
    public virtual User? Reporter { get; set; }

    [ForeignKey("TargetUserId")]
    [InverseProperty("ReportTargetUsers")]
    public virtual User? TargetUser { get; set; }
}
