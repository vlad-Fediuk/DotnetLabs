using ECommerce.Shared.Supabase.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/supabase")]
public class SupabaseController : ControllerBase
{
    private readonly Client _supabase;

    public SupabaseController(Client supabase)
    {
        _supabase = supabase;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            await _supabase.InitializeAsync();
            return Ok(new
            {
                service = "ApiGateway",
                supabase = "Connected",
                url = "https://qkvkyiflslckmmnwqkvq.supabase.co"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                service = "ApiGateway",
                supabase = "Error",
                error = ex.Message
            });
        }
    }

    [HttpGet("todos")]
    public async Task<IActionResult> GetTodos()
    {
        try
        {
            await _supabase.InitializeAsync();
            var response = await _supabase.From<Todo>().Get();
            return Ok(new
            {
                count = response.Models.Count,
                items = response.Models
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = ex.Message
            });
        }
    }

    [HttpPost("todos")]
    public async Task<IActionResult> CreateTodo([FromBody] Todo request)
    {
        try
        {
            await _supabase.InitializeAsync();
            var response = await _supabase.From<Todo>().Insert(request);
            return StatusCode(StatusCodes.Status201Created, response.Models.FirstOrDefault());
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = ex.Message
            });
        }
    }
}
