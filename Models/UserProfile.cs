using System;
using System.Collections.Generic;
using NewVivaApi.Authentication.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NewVivaApi.Authentication;

namespace NewVivaApi.Models;

public partial class UserProfile
{
    [Key] // UserId is the primary key
    [ForeignKey(nameof(User))] // And also the foreign key to ApplicationUser
    public string UserId { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public DateTimeOffset CreateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTimeOffset LastUpdateDt { get; set; }

    public DateTimeOffset? DeleteDt { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}
