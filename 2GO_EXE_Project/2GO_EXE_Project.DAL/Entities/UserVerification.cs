using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

[Table("UserVerification")]
public partial class UserVerification
{
    [Key]
    public long VerificationId { get; set; }

    public long? UserId { get; set; }

    public bool? PhoneVerified { get; set; }

    public bool? EmailVerified { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? IdCardFrontUrl { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? IdCardBackUrl { get; set; }

    public DateTime? VerifiedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserVerifications")]
    public virtual User? User { get; set; }
}
