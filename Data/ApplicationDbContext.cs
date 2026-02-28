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
        public DbSet<ProjectTaskVersion> ProjectTaskVersions { get; set; }
        public DbSet<TaskNoteVersion> TaskNoteVersions { get; set; }
        public DbSet<Executive> Executives { get; set; }
        public DbSet<ProjectProposal> ProjectProposals { get; set; }
        public DbSet<ProjectProposalVersion> ProjectProposalVersions { get; set; }
        public DbSet<ProposalNoteVersion> ProposalNoteVersions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentCollaborator> DocumentCollaborators { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

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

            builder.Entity<ProjectProposalVersion>()
                .HasOne(ppv => ppv.ProjectProposal)
                .WithMany()
                .HasForeignKey(ppv => ppv.ProjectProposalId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ProjectProposalVersion>()
                .HasOne(ppv => ppv.Employee)
                .WithMany()
                .HasForeignKey(ppv => ppv.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ProposalNoteVersion>()
                .HasOne(pnv => pnv.ProjectProposal)
                .WithMany()
                .HasForeignKey(pnv => pnv.ProjectProposalId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ProposalNoteVersion>()
                .HasOne(pnv => pnv.DepartmentHead)
                .WithMany()
                .HasForeignKey(pnv => pnv.DepartmentHeadId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ProjectTaskVersion>()
                .HasOne(ptv => ptv.ProjectTask)
                .WithMany()
                .HasForeignKey(ptv => ptv.ProjectTaskId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ProjectTaskVersion>()
                .HasOne(ptv => ptv.TaskMember)
                .WithMany()
                .HasForeignKey(ptv => ptv.TaskMemberId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TaskNoteVersion>()
                .HasOne(tnv => tnv.ProjectTask)
                .WithMany()
                .HasForeignKey(tnv => tnv.ProjectTaskId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TaskNoteVersion>()
                .HasOne(tnv => tnv.ProjectManager)
                .WithMany()
                .HasForeignKey(tnv => tnv.ProjectManagerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Document>()
                .HasOne(d => d.OwnerEmployee)
                .WithMany()
                .HasForeignKey(d => d.OwnerEmployeeId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<DocumentCollaborator>()
                .HasOne(dc => dc.Document)
                .WithMany()
                .HasForeignKey(dc => dc.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DocumentCollaborator>()
                .HasOne(dc => dc.Employee)
                .WithMany()
                .HasForeignKey(dc => dc.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<DocumentVersion>()
                .HasOne(dv => dv.Document)
                .WithMany()
                .HasForeignKey(dv => dv.DocumentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<DocumentVersion>()
                .HasOne(dv => dv.Employee)
                .WithMany()
                .HasForeignKey(dv => dv.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Messaging ──
            builder.Entity<ConversationParticipant>()
                .HasOne(cp => cp.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ConversationParticipant>()
                .HasOne(cp => cp.User)
                .WithMany()
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ChatMessage>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
