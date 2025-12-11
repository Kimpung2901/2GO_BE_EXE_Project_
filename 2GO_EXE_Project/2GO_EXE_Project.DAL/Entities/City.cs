using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class City
{
    [Key]
    public int CityId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Name { get; set; }

    [InverseProperty("City")]
    public virtual ICollection<District> Districts { get; set; } = new List<District>();
}
