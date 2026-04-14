using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        string configUserCredentials = config.GetValue<String>("AdminCredentials:Username");
        string configUserHashedPassword = config.GetValue<String>("AdminCredentials:Password");
        bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(req.Password, configUserHashedPassword);

        if (isPasswordCorrect && req.Username == configUserCredentials)
        {
            HttpContext.Session.SetString("auth", "1");
            return Ok();
        }
        return Unauthorized();
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("auth");
        return Ok();
    }
}