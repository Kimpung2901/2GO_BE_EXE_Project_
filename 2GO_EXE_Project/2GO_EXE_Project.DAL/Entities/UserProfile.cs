using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class UserProfile
{
    [Key]
    public long ProfileId { get; set; }

    public long? UserId { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? FullName { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Gender { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? AddressLine { get; set; }

    public int? CityId { get; set; }

    public int? DistrictId { get; set; }

    public int? WardId { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserProfiles")]
    public virtual User? User { get; set; }
}
