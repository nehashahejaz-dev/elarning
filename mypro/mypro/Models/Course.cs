using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Validation ke liye zaroori hai
using System.ComponentModel.DataAnnotations.Schema;

namespace mypro.Models;

public partial class Course
{
    [Key]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Course title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    [Display(Name = "Course Title")]
    public string Title { get; set; } = null!;

    [DataType(DataType.MultilineText)]
    public string? Description { get; set; } // Optional (Non-Required)

    [Required(ErrorMessage = "Please enter the course price")]
    [Range(0, 100000, ErrorMessage = "Price must be between 0 and 100,000")]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Price { get; set; }

    [Display(Name = "Course Thumbnail")]
    public string? ThumbnailUrl { get; set; } // Optional (Non-Required)

    [Required(ErrorMessage = "Please select an instructor")]
    public int InstructorId { get; set; }

    [Required(ErrorMessage = "Please select a category")]
    public int CategoryId { get; set; }

    // Navigation Properties (Hamesha non-required hoti hain form submission ke liye)
    public virtual Category? Category { get; set; }

    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual User? Instructor { get; set; }

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
}