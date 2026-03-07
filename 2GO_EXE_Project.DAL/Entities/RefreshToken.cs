using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

[Table("RefreshTokens")]
public class RefreshToken
{
    [Key]
    public long RefreshTokenId { get; set; }

    public long UserId { get; set; }

    [Required]
    [StringLength(500)]
    [Unicode(false)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? ReplacedByToken { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.RefreshTokens))]
    public virtual User User { get; set; } = null!;
}
