using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace mypro.Models;

public partial class Section
{
    public int SectionId { get; set; }

    public int CourseId { get; set; }

    public string Title { get; set; } = null!;

    public int? SortOrder { get; set; }
    [ValidateNever]
    public virtual Course Course { get; set; } = null!;
    [ValidateNever]
    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
