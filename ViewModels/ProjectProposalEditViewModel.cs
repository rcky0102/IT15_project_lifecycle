using System.Collections.Generic;
using project_lifecycle.Models;

namespace project_lifecycle.ViewModels
{
    public class ProjectProposalEditViewModel
    {
        public ProjectProposal Proposal { get; set; } = new ProjectProposal();
        public List<ProjectProposalVersion> Versions { get; set; } = new List<ProjectProposalVersion>();
        public int NextVersionNumber { get; set; }
    }
}
