using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class ListingView
{
    [Key]
    public long ViewId { get; set; }

    public long? ListingId { get; set; }

    public long? UserId { get; set; }

    public DateTime? ViewedAt { get; set; }

    [ForeignKey("ListingId")]
    [InverseProperty("ListingViews")]
    public virtual Listing? Listing { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("ListingViews")]
    public virtual User? User { get; set; }
}
