using System;
using System.Collections.Generic;

namespace mypro.Models;

public partial class StudentAttempt
{
    public int AttemptId { get; set; }

    public int UserId { get; set; }

    public int QuizId { get; set; }

    public int? ScoreObtained { get; set; }

    public bool? IsPassed { get; set; }

    public DateTime? AttemptDate { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
