using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Models;

namespace project_lifecycle.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<HumanResource> HumanResources { get; set; }
        public DbSet<DepartmentHead> DepartmentHeads { get; set; }
        public DbSet<ProjectManager> ProjectManagers { get; set; }
        public DbSet<Executive> Executives { get; set; }
        public DbSet<ProjectProposal> ProjectProposals { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
