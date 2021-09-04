using System;
using Microsoft.AspNetCore.Identity;

namespace net_backEnd.models
{
    public class ApplicationUser : IdentityUser
    {
        public string Country { get; set; }
    }
}
