using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.HR;

public class AttendanceLog : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public bool IsFaceVerified { get; set; }
    public string? FaceVerificationImagePath { get; set; } // selfie at clock-in
    public string? ClockInSource { get; set; } // "FaceRecognition", "Manual"
    public string? Notes { get; set; }
    public TimeSpan? TotalHours => ClockOut.HasValue ? ClockOut.Value - ClockIn : null;
}
