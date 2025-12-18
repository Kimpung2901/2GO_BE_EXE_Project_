using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class ListingImage
{
    [Key]
    public long ImageId { get; set; }

    public long? ListingId { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? ImageUrl { get; set; }

    public bool? IsPrimary { get; set; }

    [ForeignKey("ListingId")]
    [InverseProperty("ListingImages")]
    public virtual Listing? Listing { get; set; }
}
