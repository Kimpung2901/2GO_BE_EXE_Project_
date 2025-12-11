using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class AiModerationLog
{
    [Key]
    public long LogId { get; set; }

    public long? ListingId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Action { get; set; }

    public string? Reason { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("ListingId")]
    [InverseProperty("AiModerationLogs")]
    public virtual Listing? Listing { get; set; }
}
