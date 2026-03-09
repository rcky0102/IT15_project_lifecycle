using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace project_lifecycle.ViewModels.ProjectManager
{
    public class ProjectTaskEditViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int ProjectMilestoneId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<int> AssignedMemberIds { get; set; } = new List<int>();
        public List<SelectListItem> AvailableMembers { get; set; } = new List<SelectListItem>();
        /// <summary>Current TaskMember records (both active and archived) for this task.</summary>
        public List<TaskMemberItemViewModel> CurrentTaskMembers { get; set; } = new List<TaskMemberItemViewModel>();
    }

    public class TaskMemberItemViewModel
    {
        public int TaskMemberId { get; set; }
        public int MemberId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string ProjectRoleName { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
    }
}
