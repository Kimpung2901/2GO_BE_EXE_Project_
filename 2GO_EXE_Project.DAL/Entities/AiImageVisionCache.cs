using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public class AiImageVisionCache
{
    [Key]
    public long CacheId { get; set; }

    [StringLength(1024)]
    public string ImageUrl { get; set; } = string.Empty;

    public int? QualityScore { get; set; }

    [StringLength(1000)]
    public string? DamageLabels { get; set; }

    [StringLength(10)]
    public string? ConditionLabel { get; set; }

    public string? RawResponse { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
