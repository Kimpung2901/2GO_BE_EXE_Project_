using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class ListingAttribute
{
    [Key]
    public long AttributeId { get; set; }

    public long? ListingId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Name { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Value { get; set; }

    [ForeignKey("ListingId")]
    [InverseProperty("ListingAttributes")]
    public virtual Listing? Listing { get; set; }
}
