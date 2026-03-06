using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace project_lifecycle.ViewModels.DepartmentHead
{
    public class PmPickerItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
    }

    public class DepartmentHeadProjectIndexViewModel
    {
        public List<DepartmentHeadProjectListItemViewModel> Projects { get; set; } = new();
        public CreateDepartmentHeadProjectViewModel CreateProject { get; set; } = new();

        public List<SelectListItem> AvailableProposals { get; set; } = new();
        public List<SelectListItem> AvailableProjectManagers { get; set; } = new();
        public List<PmPickerItem> AvailableProjectManagersPicker { get; set; } = new();
        public List<SelectListItem> AvailableEmployees { get; set; } = new();
        public List<SelectListItem> AvailableProjectRoles { get; set; } = new();
    }

    public class DepartmentHeadProjectListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProposalTitle { get; set; } = string.Empty;
        public string ProjectManagerName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime DateCreated { get; set; }
        public int MemberCount { get; set; }
        public List<MemberViewModel> Members { get; set; } = new();
        public List<ProjectMilestoneViewModel> Milestones { get; set; } = new();
        public string Status { get; set; } = "Unfinished";
        public bool IsArchived { get; set; } = false;
    }

    public class MemberViewModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public int ProjectRoleId { get; set; }
        public string ProjectRoleName { get; set; } = string.Empty;
    }

    public class ProjectMilestoneViewModel
    {
        public int Id { get; set; }
        public int ProjectMilestoneId { get; set; }
        public string MilestoneName { get; set; } = string.Empty;
        public int SequenceOrder { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateDepartmentHeadProjectViewModel
    {
        [Required]
        [Display(Name = "Project Name")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Project Proposal")]
        public int ProjectProposalId { get; set; }

        [Required]
        [Display(Name = "Project Manager")]
        public int ProjectManagerId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

        public List<int> MemberEmployeeIds { get; set; } = new();
        public List<int> MemberProjectRoleIds { get; set; } = new();
    }
}
