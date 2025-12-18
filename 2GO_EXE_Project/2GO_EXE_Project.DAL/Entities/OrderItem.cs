using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class OrderItem
{
    [Key]
    public long OrderItemId { get; set; }

    public long? OrderId { get; set; }

    public long? ListingId { get; set; }

    [Column(TypeName = "decimal(15, 2)")]
    public decimal? Price { get; set; }

    [ForeignKey("ListingId")]
    [InverseProperty("OrderItems")]
    public virtual Listing? Listing { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("OrderItems")]
    public virtual Order? Order { get; set; }
}
