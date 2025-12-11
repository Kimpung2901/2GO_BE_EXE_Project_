using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class UserDevice
{
    [Key]
    public long DeviceId { get; set; }

    public long? UserId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? DeviceInfo { get; set; }

    [Column("IPAddress")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Ipaddress { get; set; }

    public DateTime? LastActive { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserDevices")]
    public virtual User? User { get; set; }
}
