using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace net_backEnd.models
{
    public class ApplicationDb : IdentityDbContext<ApplicationUser,ApplicationRole ,string>
    {
        public ApplicationDb(DbContextOptions<ApplicationDb> options):base(options)
        {

        }
    }
}
