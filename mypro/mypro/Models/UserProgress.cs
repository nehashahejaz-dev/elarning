using System;
using System.Collections.Generic;

namespace mypro.Models;

public partial class UserProgress
{
    public int ProgressId { get; set; }

    public int EnrollmentId { get; set; }

    public int LessonId { get; set; }

    public bool? IsCompleted { get; set; }

    public DateTime? CompletedDate { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual Lesson Lesson { get; set; } = null!;
}
