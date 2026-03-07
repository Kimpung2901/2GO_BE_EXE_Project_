using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class Cart
{
    [Key]
    public long CartId { get; set; }

    public long? UserId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Carts")]
    public virtual User? User { get; set; }

    [InverseProperty("Cart")]
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
