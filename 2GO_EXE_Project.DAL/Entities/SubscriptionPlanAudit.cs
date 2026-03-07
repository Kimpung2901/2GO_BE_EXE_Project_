using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class SubscriptionPlanAudit
{
    [Key]
    public long AuditId { get; set; }

    public int PlanId { get; set; }

    public long? ActorUserId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Action { get; set; } = null!;

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("PlanId")]
    [InverseProperty("Audits")]
    public virtual SubscriptionPlan? Plan { get; set; }
}
