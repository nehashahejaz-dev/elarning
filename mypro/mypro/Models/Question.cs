using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace mypro.Models;

public partial class Question
{
    public int QuestionId { get; set; }

    public int QuizId { get; set; }

    public string QuestionText { get; set; } = null!;

    public int? Points { get; set; }
    [ValidateNever]
    public virtual ICollection<Option> Options { get; set; } = new List<Option>();
    [ValidateNever]
    public virtual Quiz Quiz { get; set; } = null!;
}
