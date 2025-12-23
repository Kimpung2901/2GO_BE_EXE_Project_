using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class SubCategory
{
    [Key]
    public int SubCategoryId { get; set; }

    public int? CategoryId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Name { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey("CategoryId")]
    [InverseProperty("SubCategories")]
    public virtual Category? Category { get; set; }

    [InverseProperty("SubCategory")]
    public virtual ICollection<Listing> Listings { get; set; } = new List<Listing>();
}
