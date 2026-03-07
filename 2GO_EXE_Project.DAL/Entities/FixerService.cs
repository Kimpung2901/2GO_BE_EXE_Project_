using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class FixerService
{
    [Key]
    public long ServiceId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal? BasePrice { get; set; }

    [InverseProperty("Service")]
    public virtual ICollection<FixerRequest> FixerRequests { get; set; } = new List<FixerRequest>();
}
