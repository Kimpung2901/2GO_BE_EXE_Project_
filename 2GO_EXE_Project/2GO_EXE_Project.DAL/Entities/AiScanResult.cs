using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class AiScanResult
{
    [Key]
    public long ScanId { get; set; }

    public long? ListingId { get; set; }

    public double? SpamScore { get; set; }

    public double? NudityScore { get; set; }

    public double? ScamScore { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal? PriceEstimation { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? CategoryPrediction { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("ListingId")]
    [InverseProperty("AiScanResults")]
    public virtual Listing? Listing { get; set; }
}
