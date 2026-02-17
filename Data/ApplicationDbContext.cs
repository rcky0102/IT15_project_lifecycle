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
        public DbSet<Project> Projects { get; set; }
        public DbSet<Milestone> Milestones { get; set; }
        public DbSet<ProjectMilestone> ProjectMilestones { get; set; }
        public DbSet<ProjectRole> ProjectRoles { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<TaskMember> TaskMembers { get; set; }
        public DbSet<Executive> Executives { get; set; }
        public DbSet<ProjectProposal> ProjectProposals { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Project>()
                .HasOne(p => p.ProjectProposal)
                .WithMany()
                .HasForeignKey(p => p.ProjectProposalId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Project>()
                .HasOne(p => p.ProjectManager)
                .WithMany()
                .HasForeignKey(p => p.ProjectManagerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Member>()
                .HasOne(m => m.Project)
                .WithMany()
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Member>()
                .HasOne(m => m.Employee)
                .WithMany()
                .HasForeignKey(m => m.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Member>()
                .HasOne(m => m.ProjectRole)
                .WithMany()
                .HasForeignKey(m => m.ProjectRoleId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ProjectMilestone>()
                .HasOne(pm => pm.Project)
                .WithMany()
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ProjectMilestone>()
                .HasOne(pm => pm.Milestone)
                .WithMany()
                .HasForeignKey(pm => pm.MilestoneId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ProjectTask>()
                .HasOne(t => t.ProjectMilestone)
                .WithMany()
                .HasForeignKey(t => t.ProjectMilestoneId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ProjectTask>()
                .HasOne(t => t.ProjectManager)
                .WithMany()
                .HasForeignKey(t => t.ProjectManagerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TaskMember>()
                .HasOne(tm => tm.ProjectTask)
                .WithMany()
                .HasForeignKey(tm => tm.ProjectTaskId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TaskMember>()
                .HasOne(tm => tm.Member)
                .WithMany()
                .HasForeignKey(tm => tm.MemberId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
