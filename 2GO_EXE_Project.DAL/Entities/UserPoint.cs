using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class UserPoint
{
    [Key]
    public long PointId { get; set; }

    public long? UserId { get; set; }

    public int? CurrentPoints { get; set; }

    public int? LifetimePoints { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Tier { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserPoints")]
    public virtual User? User { get; set; }
}
