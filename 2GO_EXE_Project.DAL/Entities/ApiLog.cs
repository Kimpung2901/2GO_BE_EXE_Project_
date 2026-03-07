using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class ApiLog
{
    [Key]
    public long ApiId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Endpoint { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? RequestMethod { get; set; }

    public string? RequestBody { get; set; }

    public int? ResponseCode { get; set; }

    public DateTime? CreatedAt { get; set; }
}
