using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class PointTransaction
{
    [Key]
    public long TxId { get; set; }

    public long? UserId { get; set; }

    public int? ChangeAmount { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Reason { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("PointTransactions")]
    public virtual User? User { get; set; }
}
