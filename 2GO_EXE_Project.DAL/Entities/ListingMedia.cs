using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class ListingMedia
{
    [Key]
    public long MediaId { get; set; }

    public long? ListingId { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? Url { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? MediaType { get; set; }

    public bool? IsPrimary { get; set; }

    public int? SortOrder { get; set; }

    [ForeignKey("ListingId")]
    [InverseProperty("ListingMedias")]
    public virtual Listing? Listing { get; set; }
}
