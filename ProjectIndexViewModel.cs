using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NDRP_solution.ViewModels.Site;
using NDRP_solution.ViewModels.SharedViewModels;
using SharedVM = NDRP_solution.ViewModels.SharedViewModels;


namespace NDRP_solution.ViewModels.Project
{
    public class ProjectIndexViewModel
    {
        public ProjectInfo ProjectDetails { get; set; }
        public List<SiteInfo>Sites { get; set; }
        public List<SharedVM.DocumentInfo> Documents { get; set; }
    }

    public class ProjectInfo
    {
        public int Id { get; set; }
        public String RefNumber { get; set; }
        public String Name { get; set; }
        public String Description { get; set; }
        public String EstimatedCompletionDate { get; set; }
        public String ActualCompletionDate { get; set; }
        public String ImagePath { get; set; }
        public int? DocumentContainerId { get; set; }
        public int NotificationNumber { get; set; }
    }
}