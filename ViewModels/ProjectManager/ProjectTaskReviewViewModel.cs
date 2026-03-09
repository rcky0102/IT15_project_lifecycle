using System;

namespace project_lifecycle.ViewModels.ProjectManager
{
    public class ProjectTaskReviewViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int ProjectMilestoneId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string? EmployeeInput { get; set; }
        public string? AssignedMemberName { get; set; }
        public System.Collections.Generic.List<MemberViewModel> AssignedMembers { get; set; } = new System.Collections.Generic.List<MemberViewModel>();
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
