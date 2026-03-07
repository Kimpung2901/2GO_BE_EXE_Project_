using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class ShippingRequest
{
    [Key]
    public long ShipId { get; set; }

    public long? OrderId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Provider { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? TrackingCode { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? PickupAddress { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? DeliveryAddress { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? LabelA5Url { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? Label80x80Url { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? Label52x70Url { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("ShippingRequests")]
    public virtual Order? Order { get; set; }
}
