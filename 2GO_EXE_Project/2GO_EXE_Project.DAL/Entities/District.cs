using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class District
{
    [Key]
    public int DistrictId { get; set; }

    public int? CityId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Name { get; set; }

    [ForeignKey("CityId")]
    [InverseProperty("Districts")]
    public virtual City? City { get; set; }

    [InverseProperty("District")]
    public virtual ICollection<Ward> Wards { get; set; } = new List<Ward>();
}
