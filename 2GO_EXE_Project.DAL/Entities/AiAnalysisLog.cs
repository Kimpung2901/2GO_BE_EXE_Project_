using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

[Table("AIAnalysisLog")]
public partial class AiAnalysisLog
{
    [Key]
    public long LogId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Type { get; set; }

    public string? RequestJson { get; set; }

    public string? ResponseJson { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; }
}
