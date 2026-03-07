using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class EscrowTransaction
{
    [Key]
    public long TxId { get; set; }

    public long? EscrowId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Method { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal? Amount { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Type { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("EscrowId")]
    [InverseProperty("EscrowTransactions")]
    public virtual EscrowContract? Escrow { get; set; }
}
