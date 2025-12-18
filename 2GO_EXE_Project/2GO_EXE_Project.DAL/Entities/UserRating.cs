using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class UserRating
{
    [Key]
    public long RatingId { get; set; }

    public long? RaterId { get; set; }

    public long? RatedUserId { get; set; }

    public int? Score { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("RatedUserId")]
    [InverseProperty("UserRatingRatedUsers")]
    public virtual User? RatedUser { get; set; }

    [ForeignKey("RaterId")]
    [InverseProperty("UserRatingRaters")]
    public virtual User? Rater { get; set; }
}
