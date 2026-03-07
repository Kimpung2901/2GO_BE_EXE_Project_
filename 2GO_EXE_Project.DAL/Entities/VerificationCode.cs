using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

[Table("VerificationCodes")]
public class VerificationCode
{
    [Key]
    public long VerificationCodeId { get; set; }

    public long UserId { get; set; }

    [Required]
    [StringLength(100)]
    [Unicode(false)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Unicode(false)]
    public string Purpose { get; set; } = string.Empty; // EmailVerify, ForgotPassword

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ConsumedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.VerificationCodes))]
    public virtual User User { get; set; } = null!;
}
