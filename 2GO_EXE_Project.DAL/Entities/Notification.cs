using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

[Table("Notifications")]
public partial class Notification
{
    [Key]
    public long NotificationId { get; set; }

    public long? UserId { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string? Title { get; set; }

    public string? Message { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Type { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? Link { get; set; }

    public bool IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Notifications")]
    public virtual User? User { get; set; }
}