using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2GO_EXE_Project.DAL.Entities;

[Table("TransferTransactions")]
public class TransferTransaction
{
    [Key]
    public long TransferTransactionId { get; set; }

    [Required]
    public long TransferId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ReferenceId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? PayoutTransactionId { get; set; }

    [Required]
    public long Amount { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ToBin { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ToAccountNumber { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ToAccountName { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    public DateTime? TransactionDatetime { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    [MaxLength(50)]
    public string? ErrorCode { get; set; }

    [Required]
    [MaxLength(50)]
    public string State { get; set; } = "Received"; // Received, Processing, Success, Failed, Cancelled

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(TransferId))]
    public virtual Transfer Transfer { get; set; } = null!;
}
