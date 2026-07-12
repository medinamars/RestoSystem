using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoSystem.Server.Data;
using RestoSystem.Server.Models.Common;
using RestoSystem.Server.Models.HR;

namespace RestoSystem.Server.Controllers;

public class HRController : BaseApiController
{
    public HRController(AppDbContext db) : base(db) { }

    // === Employees ===
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees([FromQuery] int? branchId, [FromQuery] string? status)
    {
        var query = _db.Employees.AsQueryable();

        if (branchId.HasValue) query = query.Where(e => e.BranchId == branchId);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<EmploymentStatus>(status, out var empStatus))
            query = query.Where(e => e.Status == empStatus);

        var employees = await query.OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToListAsync();

        // Load branch names separately to avoid query filter + navigation issues
        var branchIds = employees.Select(e => e.BranchId).Distinct().ToList();
        var branches = await _db.Branches
            .Where(b => branchIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Name })
            .ToListAsync();
        var branchMap = branches.ToDictionary(b => b.Id, b => b.Name);

        var result = employees.Select(e => new
        {
            e.Id,
            e.EmployeeCode,
            e.FirstName,
            e.LastName,
            e.MiddleName,
            e.ContactNumber,
            e.Email,
            e.Position,
            e.Role,
            e.Status,
            e.HireDate,
            e.BaseSalary,
            BranchId = e.BranchId,
            BranchName = branchMap.GetValueOrDefault(e.BranchId)
        }).ToList();

        return Ok(result);
    }

    [HttpGet("employees/{id}")]
    public async Task<IActionResult> GetEmployee(int id)
    {
        var employee = await _db.Employees
            .Include(e => e.Documents)
            .Include(e => e.Memos)
            .Include(e => e.PerformanceReviews)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null) return NotFound();

        var tenure = DateTime.UtcNow - employee.HireDate;
        var regularizationEligible = employee.Status == EmploymentStatus.Probationary
            && tenure.TotalDays >= 150;

        // Load branch separately to avoid query filter interaction
        var branch = await _db.Branches
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == employee.BranchId);

        return Ok(new
        {
            employee.Id,
            employee.EmployeeCode,
            employee.FirstName,
            employee.LastName,
            employee.MiddleName,
            employee.Nickname,
            employee.ContactNumber,
            employee.Email,
            employee.Address,
            employee.BirthDate,
            employee.GovernmentIds,
            Branch = branch != null ? new { branch.Id, branch.Code, branch.Name } : null,
            employee.Position,
            employee.Role,
            employee.Status,
            employee.HireDate,
            employee.RegularizationDate,
            employee.BaseSalary,
            employee.CommissionRate,
            employee.ReceivesMealAllowance,
            employee.MealAllowanceAmount,
            employee.Notes,
            TenureDays = Math.Floor(tenure.TotalDays),
            IsRegularizationEligible = regularizationEligible,
            employee.Documents,
            employee.Memos,
            employee.PerformanceReviews
        });
    }

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest req)
    {
        var employee = new Employee
        {
            BranchId = req.BranchId,
            FirstName = req.FirstName,
            LastName = req.LastName,
            MiddleName = req.MiddleName,
            Position = req.Position,
            Role = Enum.Parse<UserRole>(req.Role),
            BaseSalary = req.BaseSalary,
            CommissionRate = req.CommissionRate,
            ReceivesMealAllowance = req.ReceivesMealAllowance,
            MealAllowanceAmount = req.MealAllowanceAmount,
            ContactNumber = req.ContactNumber,
            Email = req.Email,
            Address = req.Address,
            HireDate = req.HireDate,
            Status = EmploymentStatus.Probationary,
            EmployeeCode = $"EMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}"
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
    }

    [HttpPut("employees/{id}")]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest req)
    {
        var emp = await _db.Employees.FindAsync(id);
        if (emp == null) return NotFound();

        emp.FirstName = req.FirstName;
        emp.LastName = req.LastName;
        emp.MiddleName = req.MiddleName;
        emp.ContactNumber = req.ContactNumber;
        emp.Email = req.Email;
        emp.Address = req.Address;
        emp.Position = req.Position;
        emp.Role = req.Role;
        emp.BaseSalary = req.BaseSalary;
        emp.CommissionRate = req.CommissionRate;
        emp.BranchId = req.BranchId;
        emp.Status = req.Status;
        emp.ReceivesMealAllowance = req.ReceivesMealAllowance;
        emp.MealAllowanceAmount = req.MealAllowanceAmount;
        emp.Notes = req.Notes;

        await _db.SaveChangesAsync();
        return Ok(emp);
    }

    // === Attendance ===
    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance([FromQuery] int? employeeId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.AttendanceLogs
            .Include(a => a.Employee)
            .AsQueryable();

        if (employeeId.HasValue) query = query.Where(a => a.EmployeeId == employeeId);
        if (from.HasValue) query = query.Where(a => a.ClockIn >= from);
        if (to.HasValue) query = query.Where(a => a.ClockIn <= to);

        return Ok(await query.OrderByDescending(a => a.ClockIn).Take(100).ToListAsync());
    }

    [HttpPost("attendance/clock-in")]
    public async Task<IActionResult> ClockIn([FromBody] ClockInRequest request)
    {
        var employee = await _db.Employees.FindAsync(request.EmployeeId);
        if (employee == null) return NotFound("Employee not found");

        var openLog = await _db.AttendanceLogs
            .Where(a => a.EmployeeId == request.EmployeeId && a.ClockOut == null)
            .FirstOrDefaultAsync();

        if (openLog != null)
            return BadRequest("Employee already clocked in. Please clock out first.");

        var log = new AttendanceLog
        {
            EmployeeId = request.EmployeeId,
            ClockIn = DateTime.UtcNow,
            IsFaceVerified = request.IsFaceVerified,
            FaceVerificationImagePath = request.FaceImagePath,
            ClockInSource = request.IsFaceVerified ? "FaceRecognition" : "Manual"
        };

        _db.AttendanceLogs.Add(log);
        await _db.SaveChangesAsync();
        return Ok(log);
    }

    [HttpPost("attendance/clock-out/{logId}")]
    public async Task<IActionResult> ClockOut(int logId)
    {
        var log = await _db.AttendanceLogs.FindAsync(logId);
        if (log == null) return NotFound();
        if (log.ClockOut != null) return BadRequest("Already clocked out");

        log.ClockOut = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(log);
    }

    // === Payroll ===
    [HttpGet("payroll-periods")]
    public async Task<IActionResult> GetPayrollPeriods() =>
        Ok(await _db.PayrollPeriods.OrderByDescending(p => p.WeekStart).ToListAsync());

    [HttpPost("payroll-periods/generate")]
    public async Task<IActionResult> GeneratePayrollPeriod([FromBody] GeneratePayrollRequest request)
    {
        var existing = await _db.PayrollPeriods
            .Where(p => p.WeekStart == request.WeekStart && p.WeekEnd == request.WeekEnd)
            .FirstOrDefaultAsync();

        if (existing != null)
            return BadRequest("Payroll period already exists");

        var period = new PayrollPeriod
        {
            WeekStart = request.WeekStart,
            WeekEnd = request.WeekEnd,
            PayDate = request.PayDate,
            IsProcessed = false
        };
        _db.PayrollPeriods.Add(period);
        await _db.SaveChangesAsync();

        var employees = await _db.Employees
            .Where(e => e.Status == EmploymentStatus.Active || e.Status == EmploymentStatus.Probationary)
            .ToListAsync();

        foreach (var emp in employees)
        {
            var attendanceLogs = await _db.AttendanceLogs
                .Where(a => a.EmployeeId == emp.Id
                    && a.ClockIn >= request.WeekStart
                    && a.ClockIn <= request.WeekEnd.AddDays(1))
                .ToListAsync();

            var totalHours = attendanceLogs
                .Where(a => a.ClockOut.HasValue)
                .Sum(a => (a.ClockOut!.Value - a.ClockIn).TotalHours);

            var totalShifts = attendanceLogs.Count;
            var basePay = emp.BaseSalary / 52;
            var commissionAmt = totalShifts > 0 ? emp.CommissionRate * basePay / 100 : 0;
            var mealAmt = emp.ReceivesMealAllowance ? emp.MealAllowanceAmount * totalShifts : 0;

            var gross = basePay + commissionAmt + mealAmt;

            _db.Payrolls.Add(new Payroll
            {
                PayrollPeriodId = period.Id,
                EmployeeId = emp.Id,
                GrossPay = gross,
                TotalDeductions = 0,
                NetPay = gross,
                BaseSalaryAmount = basePay,
                CommissionAmount = commissionAmt,
                MealAllowanceAmount = mealAmt,
                TotalHoursWorked = (int)Math.Floor(totalHours),
                TotalShifts = totalShifts
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { PeriodId = period.Id, EmployeesProcessed = employees.Count });
    }

    [HttpGet("payroll")]
    public async Task<IActionResult> GetPayrolls([FromQuery] int? periodId, [FromQuery] int? employeeId)
    {
        var query = _db.Payrolls
            .Include(p => p.Employee)
            .Include(p => p.PayrollPeriod)
            .AsQueryable();

        if (periodId.HasValue) query = query.Where(p => p.PayrollPeriodId == periodId);
        if (employeeId.HasValue) query = query.Where(p => p.EmployeeId == employeeId);

        return Ok(await query.OrderByDescending(p => p.PayrollPeriod.WeekStart).ToListAsync());
    }

    // === Payslips ===
    [HttpGet("payslips")]
    public async Task<IActionResult> GetPayslips([FromQuery] int? payrollId, [FromQuery] bool? undelivered)
    {
        var query = _db.Payslips
            .Include(p => p.Employee)
            .Include(p => p.Payroll).ThenInclude(pr => pr.PayrollPeriod)
            .AsQueryable();

        if (payrollId.HasValue) query = query.Where(p => p.PayrollId == payrollId);
        if (undelivered == true) query = query.Where(p => !p.IsDelivered);

        return Ok(await query.OrderByDescending(p => p.GeneratedDate).ToListAsync());
    }

    [HttpPost("payslips/generate")]
    public async Task<IActionResult> GeneratePayslips([FromQuery] int payrollId)
    {
        var payroll = await _db.Payrolls.FindAsync(payrollId);
        if (payroll == null) return NotFound("Payroll not found");

        var existing = await _db.Payslips.AnyAsync(p => p.PayrollId == payrollId);
        if (existing) return BadRequest("Payslips already generated for this payroll");

        _db.Payslips.Add(new Payslip
        {
            PayrollId = payrollId,
            EmployeeId = payroll.EmployeeId,
            GeneratedDate = DateTime.UtcNow,
            IsDelivered = false
        });

        await _db.SaveChangesAsync();
        return Ok("Payslip generated");
    }

    // === Memos ===
    [HttpGet("memos")]
    public async Task<IActionResult> GetMemos([FromQuery] int? employeeId, [FromQuery] int? branchId)
    {
        var query = _db.Memos.AsQueryable();
        if (employeeId.HasValue) query = query.Where(m => m.EmployeeId == employeeId);
        if (branchId.HasValue) query = query.Where(m => m.BranchId == branchId);
        return Ok(await query.OrderByDescending(m => m.IssueDate).ToListAsync());
    }

    [HttpPost("memos")]
    public async Task<IActionResult> CreateMemo([FromBody] Memo memo)
    {
        _db.Memos.Add(memo);
        await _db.SaveChangesAsync();
        return Ok(memo);
    }

    // === Incidents ===
    [HttpGet("incidents")]
    public async Task<IActionResult> GetIncidents([FromQuery] int? branchId, [FromQuery] string? status)
    {
        var query = _db.IncidentReports.Include(i => i.Employee).AsQueryable();
        if (branchId.HasValue) query = query.Where(i => i.BranchId == branchId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(i => i.Status == status);
        return Ok(await query.OrderByDescending(i => i.IncidentDate).ToListAsync());
    }

    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequest req)
    {
        var incident = new IncidentReport
        {
            EmployeeId = req.EmployeeId,
            BranchId = req.BranchId,
            Title = req.Title,
            Description = req.Description,
            Severity = req.Severity,
            Status = req.Status,
            IncidentDate = req.IncidentDate,
            ReportedBy = req.ReportedBy
        };
        _db.IncidentReports.Add(incident);
        await _db.SaveChangesAsync();
        return Ok(incident);
    }

    // === Meal Entitlements ===
    [HttpGet("meal-entitlements")]
    public async Task<IActionResult> GetMealEntitlements([FromQuery] int? employeeId, [FromQuery] DateTime? date)
    {
        var query = _db.MealEntitlements.Include(m => m.Employee).AsQueryable();
        if (employeeId.HasValue) query = query.Where(m => m.EmployeeId == employeeId);
        if (date.HasValue) query = query.Where(m => m.ShiftDate.Date == date.Value.Date);
        return Ok(await query.OrderByDescending(m => m.ShiftDate).ToListAsync());
    }

    // === Dashboard ===
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var weekStart = now.AddDays(-(int)now.DayOfWeek);

        var totalEmployees = await _db.Employees.CountAsync();
        var activeEmployees = await _db.Employees
            .CountAsync(e => e.Status == EmploymentStatus.Active || e.Status == EmploymentStatus.Probationary);
        var pendingRegularization = await _db.Employees
            .CountAsync(e => e.Status == EmploymentStatus.Probationary);
        var todayShifts = await _db.AttendanceLogs
            .CountAsync(a => a.ClockIn.Date == now.Date);
        var openIncidents = await _db.IncidentReports
            .CountAsync(i => i.Status == "Open" || i.Status == "Investigating");

        return Ok(new
        {
            totalEmployees,
            activeEmployees,
            pendingRegularization,
            todayShifts,
            openIncidents
        });
    }
}

// Request DTOs
public record ClockInRequest(int EmployeeId, bool IsFaceVerified, string? FaceImagePath);
public record GeneratePayrollRequest(DateTime WeekStart, DateTime WeekEnd, DateTime PayDate);

public record CreateEmployeeRequest(
    int BranchId,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Position,
    string Role,
    decimal BaseSalary,
    decimal CommissionRate,
    bool ReceivesMealAllowance,
    decimal MealAllowanceAmount,
    string ContactNumber,
    string Email,
    string Address,
    DateTime HireDate
);

public record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    string ContactNumber,
    string Email,
    string Address,
    string Position,
    UserRole Role,
    decimal BaseSalary,
    decimal CommissionRate,
    int BranchId,
    EmploymentStatus Status,
    bool ReceivesMealAllowance,
    decimal MealAllowanceAmount,
    string? Notes = null
);

public record CreateIncidentRequest(
    int EmployeeId,
    int BranchId,
    string Title,
    string Description,
    string Severity,
    string Status,
    DateTime IncidentDate,
    string? ReportedBy = null
);
