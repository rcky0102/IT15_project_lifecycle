using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace project_lifecycle.ViewModels.ProjectManager
{
    public class ProjectManageViewModel
    {
        public ProjectDetailViewModel Project { get; set; } = new ProjectDetailViewModel();
        public List<SelectListItem> AvailableEmployees { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableProjectRoles { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableMilestones { get; set; } = new List<SelectListItem>();
    }
}
