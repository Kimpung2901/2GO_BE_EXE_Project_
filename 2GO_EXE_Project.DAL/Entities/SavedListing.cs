using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class SavedListing
{
    [Key]
    public long SavedId { get; set; }

    public long? UserId { get; set; }

    public long? ListingId { get; set; }

    public DateTime? SavedAt { get; set; }

    [ForeignKey("ListingId")]
    [InverseProperty("SavedListings")]
    public virtual Listing? Listing { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("SavedListings")]
    public virtual User? User { get; set; }
}
