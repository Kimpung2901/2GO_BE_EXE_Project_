using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

[Table("Banks")]
public class Bank
{
    [Key]
    public int BankId { get; set; }

    [Required]
    [MaxLength(255)]
    [Unicode(false)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Unicode(false)]
    public string Bin { get; set; } = string.Empty;

    [MaxLength(50)]
    [Unicode(false)]
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
