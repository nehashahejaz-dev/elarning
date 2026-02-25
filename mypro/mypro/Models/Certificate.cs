using System;
using System.Collections.Generic;

namespace mypro.Models;

public partial class Certificate
{
    public int CertificateId { get; set; }

    public int UserId { get; set; }

    public int CourseId { get; set; }

    public string? CertificateCode { get; set; }

    public DateTime? IssuedDate { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
