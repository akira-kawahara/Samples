using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CertAndJWTSample.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> logger_;
        private readonly IConfiguration configuration_;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IConfiguration configuration)
        {
            logger_ = logger;
            configuration_ = configuration;
        }
        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var usr = HttpContext.User;

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
        //ログインのみクライアント証明書認証を行います
        [Authorize(AuthenticationSchemes = CertificateAuthenticationDefaults.AuthenticationScheme)]
        [HttpPost]
        public IActionResult Login([FromBody]UserCredential userCredential)
        {
            //このサンプルでは、ユーザの情報を固定で判定しています
            var user = new User
            {
                Name = "test",
                Password = "test",
                Rol = "Admin"
            };
            if (userCredential.Name != user.Name || userCredential.Password != user.Password)
            {
                return Forbid();
            }

            var token = CreateToken(user);

            //JWTトークンをJSONで返します
            return Ok(new { token = token });
        }

        protected string CreateToken(User user)
        {
            var token = new JwtSecurityToken(
                issuer: configuration_["Jwt:Issuer"],
                audience: configuration_["Jwt:Audience"],
                claims: new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Name),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, user.Rol)
                },
                expires: DateTime.UtcNow.AddDays(1),   //このサンプルでは、トークンンの有効期限を1日とします
                notBefore: DateTime.UtcNow,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration_["Jwt:Key"])),
                    SecurityAlgorithms.HmacSha256)
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    //クライアントから送られてくるユーザの情報
    public class UserCredential
    {
        public string Name { get; set; }
        public string  Password { get; set; }
    }

    //サーバに保存されているユーザの情報。
    public class User
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public string Rol { get; set; }
    }
}
