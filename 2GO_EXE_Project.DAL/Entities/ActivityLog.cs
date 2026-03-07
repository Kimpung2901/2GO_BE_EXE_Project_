using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class ActivityLog
{
    [Key]
    public long LogId { get; set; }

    public long? UserId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Action { get; set; }

    public string? Details { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("ActivityLogs")]
    public virtual User? User { get; set; }
}
