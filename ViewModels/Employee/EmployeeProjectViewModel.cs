using System;
using System.Collections.Generic;

namespace project_lifecycle.ViewModels.Employee
{
    public class EmployeeProjectViewModel
    {
        public project_lifecycle.ViewModels.ProjectManager.ProjectDetailViewModel Project { get; set; } = new project_lifecycle.ViewModels.ProjectManager.ProjectDetailViewModel();
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        public class TaskItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string MilestoneName { get; set; } = string.Empty;
            public bool IsArchived { get; set; } = false;
        }
    }
}
