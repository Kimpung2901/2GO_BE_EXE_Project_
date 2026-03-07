using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

[Table("MarketPrices")]
public partial class MarketPrice
{
    [Key]
    public int MarketPriceId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string ProductKey { get; set; } = null!;

    public int? CategoryId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string Condition { get; set; } = "GOOD";

    [Column(TypeName = "decimal(15, 2)")]
    public decimal AvgPrice { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal MinPrice { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal MaxPrice { get; set; }

    public int SampleCount { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Source { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? Confidence { get; set; }

    public DateTime? UpdatedAt { get; set; }
}