using GestionFinanciera.API.Core;
using GestionFinanciera.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionFinanciera.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PoliciesController : ControllerBase
    {
        private readonly FinancialDbContext _context;

        public PoliciesController(FinancialDbContext context)
        {
            _context = context;
        }

        // GET: api/policies
        [HttpGet]
        public async Task<IActionResult> GetPolicies()
        {
            var policies = await _context.Policies.ToListAsync();
            return Ok(policies);
        }

        // POST: api/policies
        [HttpPost]
        public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequest request)
        {
            try
            {
                var newPolicy = new Policy(request.PolicyNumber, request.InsuredAmount);

                _context.Policies.Add(newPolicy);

                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPolicies), new 
                { 
                    id = newPolicy.Id
                }, newPolicy);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new 
                { 
                    Error = ex.Message 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                {
                    Error = "An error occurred while saving to the database.", Details = ex.Message
                });
            }
        }
    }

    public class CreatePolicyRequest
    {
        public string PolicyNumber 
        {
            get; set;
        } = string.Empty;
        public decimal InsuredAmount 
        {
            get; set; 
        }
    }
}