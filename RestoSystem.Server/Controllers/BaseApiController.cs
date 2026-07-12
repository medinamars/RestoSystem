using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoSystem.Server.Data;

namespace RestoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseApiController : ControllerBase
{
    protected readonly AppDbContext _db;

    public BaseApiController(AppDbContext db)
    {
        _db = db;
    }
}
