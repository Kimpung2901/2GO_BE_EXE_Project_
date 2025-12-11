using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class FixerRequest
{
    [Key]
    public long RequestId { get; set; }

    public long? ListingId { get; set; }

    public long? UserId { get; set; }

    public long? ServiceId { get; set; }

    public string? Notes { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    [InverseProperty("Request")]
    public virtual ICollection<FixerAssignment> FixerAssignments { get; set; } = new List<FixerAssignment>();

    [ForeignKey("ListingId")]
    [InverseProperty("FixerRequests")]
    public virtual Listing? Listing { get; set; }

    [ForeignKey("ServiceId")]
    [InverseProperty("FixerRequests")]
    public virtual FixerService? Service { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("FixerRequests")]
    public virtual User? User { get; set; }
}
