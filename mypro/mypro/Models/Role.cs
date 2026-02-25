using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace mypro.Models;

public partial class Role
{
    public int RoleId { get; set; }

    [Required(ErrorMessage = "Role Name is required")]
    public string RoleName { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
