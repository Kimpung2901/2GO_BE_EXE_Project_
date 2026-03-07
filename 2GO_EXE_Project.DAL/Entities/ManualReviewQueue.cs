using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

[Table("ManualReviewQueue")]
public partial class ManualReviewQueue
{
    [Key]
    public long QueueId { get; set; }

    public long? ListingId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Reason { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
