using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace project_lifecycle.ViewModels.ProjectManager
{
    public class EmployeePickerItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
    }

    public class ProjectManageViewModel
    {
        public ProjectDetailViewModel Project { get; set; } = new ProjectDetailViewModel();
        public List<SelectListItem> AvailableEmployees { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableProjectRoles { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableMilestones { get; set; } = new List<SelectListItem>();
        public List<EmployeePickerItem> AvailableEmployeePicker { get; set; } = new List<EmployeePickerItem>();
    }
}
