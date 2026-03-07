using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2GO_EXE_Project.DAL.Entities;

[Table("Transfers")]
public class Transfer
{
    [Key]
    public long TransferId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ReferenceId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? PayoutId { get; set; }

    public long? UserId { get; set; }

    public int? TotalCredit { get; set; }

    [MaxLength(500)]
    public string? Category { get; set; } // Stored as JSON array string

    [Required]
    [MaxLength(50)]
    public string ApprovalState { get; set; } = "Drafting"; // Drafting, Approved, Rejected, Processing, Completed

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    public virtual ICollection<TransferTransaction> Transactions { get; set; } = new List<TransferTransaction>();
}
