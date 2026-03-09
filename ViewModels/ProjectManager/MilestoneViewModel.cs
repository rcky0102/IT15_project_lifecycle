using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace project_lifecycle.ViewModels.ProjectManager
{
    public class MilestoneViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int ProjectMilestoneId { get; set; }
        public int MilestoneId { get; set; }
        public string MilestoneName { get; set; } = string.Empty;
        public int SequenceOrder { get; set; }
        public string Status { get; set; } = string.Empty;

        public List<ProjectTaskItemViewModel> Tasks { get; set; } = new List<ProjectTaskItemViewModel>();
        public List<SelectListItem> AvailableMembers { get; set; } = new List<SelectListItem>();
    }

    public class ProjectTaskItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? AssignedMemberName { get; set; }
        public List<MemberViewModel> AssignedMembers { get; set; } = new List<MemberViewModel>();
        public bool IsArchived { get; set; }
    }
}
