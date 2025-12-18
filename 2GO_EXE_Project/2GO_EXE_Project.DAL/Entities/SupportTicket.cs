using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class SupportTicket
{
    [Key]
    public long TicketId { get; set; }

    public long? UserId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Topic { get; set; }

    public string? Description { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("SupportTickets")]
    public virtual User? User { get; set; }
}
