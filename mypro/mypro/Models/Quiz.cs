using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace mypro.Models;

public partial class Quiz
{
    public int QuizId { get; set; }

    public int CourseId { get; set; }

    public string? Title { get; set; }

    public int? PassingScore { get; set; }
    [ValidateNever]
    public virtual Course Course { get; set; } = null!;
    [ValidateNever]
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    [ValidateNever]
    public virtual ICollection<StudentAttempt> StudentAttempts { get; set; } = new List<StudentAttempt>();
}
