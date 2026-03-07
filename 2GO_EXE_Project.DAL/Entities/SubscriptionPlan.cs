using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class SubscriptionPlan
{
    [Key]
    public int PlanId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Code { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Precision(15, 2)]
    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public int? MonthlyListingLimit { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<SubscriptionPlanAudit> Audits { get; set; } = new List<SubscriptionPlanAudit>();
}
