using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Example.Oauth.Server.Models
{
    public class ApplicationContext : DbContext
    {
        public DbSet<Application> Applications { get; set; }
    }
}
