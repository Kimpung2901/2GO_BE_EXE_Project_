using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

[Table("ChatbotLogs")]
public partial class ChatbotLog
{
    [Key]
    public long ChatbotLogId { get; set; }

    public long? UserId { get; set; }

    public string? Question { get; set; }

    public string? Answer { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? MatchedIntent { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? Confidence { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("ChatbotLogs")]
    public virtual User? User { get; set; }
}