using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace mypro.Models;

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;


public partial class User
{
    public int UserId { get; set; }


    [Required(ErrorMessage = "Full Name is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 to 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Use only letters and spaces")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must have 1 uppercase, 1 lowercase and 1 number")]
    public string PasswordHash { get; set; } = null!;


    [Required(ErrorMessage = "Please select a role")]
    public int RoleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ValidateNever] // Isse validation skip ho jayegi
    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    [ValidateNever]
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    [ValidateNever]
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    [ValidateNever]
    public virtual Role Role { get; set; } = null!;

    [ValidateNever]
    public virtual ICollection<StudentAttempt> StudentAttempts { get; set; } = new List<StudentAttempt>();
}
