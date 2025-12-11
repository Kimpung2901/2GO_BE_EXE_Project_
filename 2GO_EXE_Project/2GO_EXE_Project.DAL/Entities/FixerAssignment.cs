using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class FixerAssignment
{
    [Key]
    public long AssignmentId { get; set; }

    public long? RequestId { get; set; }

    public long? FixerUserId { get; set; }

    public DateTime? AssignedAt { get; set; }

    [ForeignKey("FixerUserId")]
    [InverseProperty("FixerAssignments")]
    public virtual User? FixerUser { get; set; }

    [ForeignKey("RequestId")]
    [InverseProperty("FixerAssignments")]
    public virtual FixerRequest? Request { get; set; }
}
