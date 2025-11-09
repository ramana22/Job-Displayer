using HiringCafeTracker.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiringCafeTracker.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public CompaniesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetCompanies(CancellationToken cancellationToken)
    {
        var companies = await _dbContext.Jobs.AsNoTracking()
            .Where(j => j.Company != null && j.ApplyLink != null)
            .GroupBy(j => j.Company!)
            .Select(g => new
            {
                CompanyName = g.Key,
                CareerSiteUrl = g.Select(j => j.ApplyLink!).First()
            })
            .OrderBy(c => c.CompanyName)
            .ToListAsync(cancellationToken);

        return Ok(companies);
    }
}
