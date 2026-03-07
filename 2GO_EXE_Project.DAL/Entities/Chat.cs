using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _2GO_EXE_Project.DAL.Entities;

public partial class Chat
{
    [Key]
    public long ChatId { get; set; }

    public long? User1Id { get; set; }

    public long? User2Id { get; set; }

    public DateTime? CreatedAt { get; set; }

    [InverseProperty("Chat")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    [ForeignKey("User1Id")]
    [InverseProperty("ChatUser1s")]
    public virtual User? User1 { get; set; }

    [ForeignKey("User2Id")]
    [InverseProperty("ChatUser2s")]
    public virtual User? User2 { get; set; }
}
