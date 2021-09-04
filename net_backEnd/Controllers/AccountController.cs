using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using net_backEnd.models;
using net_backEnd.modelViews;
using net_backEnd.services;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace net_backEnd.Controllers
{
    [Route("[controller]")]
    [ApiController]

    public class AccountController : Controller
    {
        private readonly ApplicationDb _db;
        private readonly UserManager<ApplicationUser> _manger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public AccountController(ApplicationDb db, UserManager<ApplicationUser> mange,
                                 SignInManager<ApplicationUser> signInManager,
                                 RoleManager<ApplicationRole> roleManager)
        {
            _db = db;
            _manger = mange;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (model == null)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                if (EmailExistes(model.Email))
                {
                    return BadRequest("Email allready avaliable");
                }

                if (IsValidEmail(model.Email) == false)
                {
                    return BadRequest("please add valid Email");
                }

                var user = new ApplicationUser
                {
                    Email = model.Email,
                    UserName = model.UserName
                };

                var result = await _manger.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // genrate email confirmation
                    var token = await _manger.GenerateEmailConfirmationTokenAsync(user);
                    //var confirmLinkAsp = Url.Action("RegisterationConfirm", "Account",
                    //    new { ID = user.Id, token = HttpUtility.UrlEncode(token) }, Request.Scheme
                    //    );
                    var encodeToken = Encoding.UTF8.GetBytes(token);
                    var newToken = WebEncoders.Base64UrlEncode(encodeToken);

                    var confirmLink = $"http://localhost:4200/registerConfirm?ID={user.Id}&Token={newToken}";
                    var text = "Please confirm your registration email at our website";
                    var activeLink = "<a href=\"" + confirmLink + "\">Confirm link</a>";
                    var title = "registration confirm";
                    if (await SendGridAPI.Execute(user.Email, user.UserName, text, activeLink, title)) // 
                    {
                        return StatusCode(StatusCodes.Status200OK);
                    }


                }
                else
                {
                    return BadRequest(result.Errors);
                }
            }
            return StatusCode(StatusCodes.Status400BadRequest);

        }

        private bool EmailExistes(string email)
        {
            return _db.Users.Any(x => x.Email == email);
        }

        public bool IsValidEmail(string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        [HttpGet]
        [Route("RegisterationConfirm")]
        public async Task<IActionResult> RegisterationConfirm(string ID, string token)
        {
            if (string.IsNullOrEmpty(ID) || string.IsNullOrEmpty(token))
                return NotFound();
            var user = await _manger.FindByIdAsync(ID);
            if (user == null)
                return NotFound();

            var newToken = WebEncoders.Base64UrlDecode(token);
            var encodeToken = Encoding.UTF8.GetString(newToken);


            var result = await _manger.ConfirmEmailAsync(user, encodeToken);
            if (result.Succeeded)
            {
                return Ok();
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            await CreateRoles();
            //await CreateAdmin();

            if (model == null)
                return NotFound();
            var user = await _manger.FindByEmailAsync(model.Email);
            if (user == null)
                return NotFound();
            if (!user.EmailConfirmed)
                return Unauthorized("please confirm your email");

            var userName = HttpContext.User.Identity.Name;
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (id != null || userName != null)
            {
                return BadRequest($"user_id : {id} is exists");
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                /// add user role to the user if he dont have
                if (await _roleManager.RoleExistsAsync("User"))
                {
                    if (!await _manger.IsInRoleAsync(user, "User"))
                    {
                        await _manger.AddToRoleAsync(user, "User");
                    }
                }
                var roleName = await GetRoleNameByUserId(user.Id);
                if (roleName != null)
                {
                    AddCookies(user.UserName, roleName, user.Id, model.RememberMe, user.Email);
                }

                return Ok();
            }
            else if (result.IsLockedOut)
            {
                return Unauthorized("please try to login again after 20 second");
            }
            // if the email or password wrong
            return StatusCode(StatusCodes.Status406NotAcceptable);
        }

        private async Task<string> GetRoleNameByUserId(string userId)
        {

            var userRole = await _db.UserRoles.FirstOrDefaultAsync(x => x.UserId == userId);
            if (userRole != null)
            {
                return await _db.Roles.Where(x => x.Id == userRole.RoleId).Select(x => x.Name).FirstOrDefaultAsync();
            }
            return null;
        }

        /// =======================
        /// 


        // =========
        [HttpGet]
        [Authorize]
        [Route("isEmailExists")]
        public async Task<IActionResult> isEmailExists(string email)
        {
            var exist = await _db.Users.AnyAsync(x => x.Email == email);
            if (exist)
            {
                return StatusCode(StatusCodes.Status200OK);
            }
            return StatusCode(StatusCodes.Status400BadRequest);
        }
        // =========
        [HttpGet]
        [Authorize]
        [Route("isUserNameExists")]
        public async Task<IActionResult> isUserNameExists(string username)
        {
            var exist = await _db.Users.AnyAsync(x => x.UserName == username);
            if (exist)
            {
                return StatusCode(StatusCodes.Status200OK);
            }
            return StatusCode(StatusCodes.Status400BadRequest);
        }

        /// =======================

        //private async Task CreateAdmin()
        //{
        //  var admin =  await _manger.FindByNameAsync("admin");
        //    if(admin == null)
        //    {
        //        var user = new ApplicationUser
        //        {
        //            Email = "admin@admin.com",
        //        UserName = "Admin",
        //        EmailConfirmed =true
        //        };
        //      var result =  await _manger.CreateAsync(user, "12345a");
        //        if (result.Succeeded)
        //        {
        //            if(await _roleManager.RoleExistsAsync("Admin"))
        //            {
        //                await _manger.AddToRoleAsync(user, "Admin");
        //            }

        //        }
        //    }
        //}

        private async Task CreateRoles()
        {
            if( _roleManager.Roles.Count() < 1)
            {
                var admin = new ApplicationRole { Name = "Admin" };
                await _roleManager.CreateAsync(admin);

                var user = new ApplicationRole { Name = "User" };
                await _roleManager.CreateAsync(user);
            }
           
        }

        public async void AddCookies(string username,string roleName,string userId, bool remember,string email)
        {
            var claim = new List<Claim>
            {
                new Claim(ClaimTypes.Name,username),
                new Claim(ClaimTypes.Email,email),
                new Claim(ClaimTypes.NameIdentifier,userId),
                new Claim(ClaimTypes.Role,roleName),
            };

            var claimIdentity = new ClaimsIdentity(claim, CookieAuthenticationDefaults.AuthenticationScheme);
            if (remember)
            {
                var authProperties = new AuthenticationProperties {
                    AllowRefresh = true,
                    IsPersistent = remember,
                    ExpiresUtc = DateTime.UtcNow.AddDays(5)
                };

                await HttpContext.SignInAsync
                   (
                    CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimIdentity),
                        authProperties
                );
            }
            else
            {
                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    IsPersistent = remember,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(60)
                };

                await HttpContext.SignInAsync
                   (
                    CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimIdentity),
                        authProperties
                );
            }

        }


        [HttpGet]
        [Route("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(User.FindFirst(ClaimTypes.Email)?.Value);
        }

        [HttpGet]
        [Route("GetRoleName/{email}")]
        public async Task<string> GetRoleName(string email)
        {
            var user = await _manger.FindByEmailAsync(email);
            if(user != null)
            {
                var userRole = await _db.UserRoles.FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (userRole != null)
                {
                    return await _db.Roles.Where(x => x.Id == userRole.RoleId).Select(x => x.Name).FirstOrDefaultAsync();
                }
            }

          
            return null;
        }

        [Authorize]
        [HttpGet]
        [Route("CheckUserClaims/{email}&{role}")]
        public  IActionResult CheckUserClaims(string email,string role)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userEmail != null && userRole != null && id != null)
            {
                if(email == userEmail && role ==  userRole )
                {
                   return StatusCode(StatusCodes.Status200OK);
                }
            }
           return StatusCode(StatusCodes.Status203NonAuthoritative);
        }

        // GET: api/values
        //[HttpGet]
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        //// GET api/values/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/values
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/values/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/values/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
