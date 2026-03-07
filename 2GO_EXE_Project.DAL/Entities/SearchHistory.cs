using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

[Table("SearchHistory")]
public partial class SearchHistory
{
    [Key]
    public long SearchId { get; set; }

    public long? UserId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Query { get; set; }

    public string? Filters { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("SearchHistories")]
    public virtual User? User { get; set; }
}
