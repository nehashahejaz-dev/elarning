using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace mypro.Models;

public partial class Lesson
{
    public int LessonId { get; set; }

    public int SectionId { get; set; }

    public string Title { get; set; } = null!;

    public string? ContentUrl { get; set; }

    public TimeOnly? Duration { get; set; }

    public bool IsFree { get; set; }
    [ValidateNever]
    public virtual Section Section { get; set; } = null!;
    [ValidateNever]
    public virtual ICollection<UserProgress> UserProgresses { get; set; } = new List<UserProgress>();
}
