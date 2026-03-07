using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class ListingComment
{
    [Key]
    public long CommentId { get; set; }

    public long ListingId { get; set; }

    public long UserId { get; set; }

    public long? ParentId { get; set; }

    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    [Column(TypeName = "timestamp with time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ListingId))]
    [InverseProperty("ListingComments")]
    public virtual Listing? Listing { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty("ListingComments")]
    public virtual User? User { get; set; }

    [ForeignKey(nameof(ParentId))]
    [InverseProperty(nameof(ListingComment.Replies))]
    public virtual ListingComment? ParentComment { get; set; }

    [InverseProperty(nameof(ListingComment.ParentComment))]
    public virtual ICollection<ListingComment> Replies { get; set; } = new List<ListingComment>();
}
