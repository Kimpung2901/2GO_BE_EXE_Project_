using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class Ward
{
    [Key]
    public int WardId { get; set; }

    public int? DistrictId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Name { get; set; }

    [ForeignKey("DistrictId")]
    [InverseProperty("Wards")]
    public virtual District? District { get; set; }

    [InverseProperty("Ward")]
    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();
}
