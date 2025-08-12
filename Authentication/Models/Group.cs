using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewVivaApi.Authentication.Models;
public partial class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime? DeleteDate { get; set; }
}

public partial class User_Group_Xref
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid UserId { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid GroupId { get; set; }
}