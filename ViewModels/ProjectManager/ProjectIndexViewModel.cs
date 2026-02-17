using System;
using System.Collections.Generic;

namespace project_lifecycle.ViewModels.ProjectManager
{
    public class ProjectIndexViewModel
    {
        public List<ProjectDetailViewModel> Projects { get; set; } = new List<ProjectDetailViewModel>();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailableEmployees { get; set; } = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailableProjectRoles { get; set; } = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailableMilestones { get; set; } = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
    }

    public class ProjectDetailViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProposalTitle { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<MemberViewModel> Members { get; set; } = new List<MemberViewModel>();
        public List<ProjectMilestoneViewModel> Milestones { get; set; } = new List<ProjectMilestoneViewModel>();
    }

    public class MemberViewModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int ProjectRoleId { get; set; }
        public string ProjectRoleName { get; set; } = string.Empty;
    }

    public class ProjectMilestoneViewModel
    {
        public int Id { get; set; }
        public int MilestoneId { get; set; }
        public string MilestoneName { get; set; } = string.Empty;
        public int SequenceOrder { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
