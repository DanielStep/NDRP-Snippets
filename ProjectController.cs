// Copyright Daniel Stepanenko, Arpan Jain, Jordan Andrews

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using NDRP_solution.Models;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using NDRP_solution.ViewModels.Site;
using NDRP_solution.ViewModels.Project;
using System.IO;
using NDRP_solution.Storage;
using System.Globalization;
using SharedVM = NDRP_solution.ViewModels.SharedViewModels;
using NDRP_solution.Business;

namespace NDRP_solution.Controllers
{
    [Session.SessionHandler]
    public class ProjectController : Controller
    {
        private NDREntities db = new NDREntities();

        /// <summary>
        /// Controller Action function routed to Project/Index/id that queries corresponding Project, 
        /// and associated Sites and Documents for return to client View as view model.
        /// </summary>
        /// <param name="id">Project's projectId</param>
        /// <returns>ProjectIndexViewModel object</returns>
        public ActionResult Index(int id)
        {
            //build up viewmodel with 2 queries. Could be done with 1 query but not important for now.
            //get project details
            var projectInfo = (from p in db.Projects
                                    where p.ProjectId == id
                                    select new ProjectInfo
                                    {
                                        Name = p.Name,
                                        Description = p.Description,
                                        RefNumber = p.ProjectCode,
                                        Id = p.ProjectId,
                                        ImagePath = p.ImagePath,
                                        DocumentContainerId = p.DocumentContainerId
                                    }).FirstOrDefault();

            //get sites list
            var sites = (from dbSite in db.Sites
                            where dbSite.ProjectId == id
                            select new SiteInfo
                            {
                                Name = dbSite.Name,
                                Id = dbSite.SiteId,
                                Description = dbSite.Description,
                                Address = dbSite.Address,
                                ImagePath = dbSite.ImagePath,
                                ProjectId = dbSite.ProjectId,
                                NotificationNumber = 0
                            }).OrderBy(x=>x.Name).ToList();

            //Count how many items are high risk designated
            foreach(var site in sites)
            {
                var audits = db.Audits.Where(x => x.SiteId == site.Id).ToList();
                site.NotificationNumber = audits.Sum(x => x.GetCountOfHighRisk());
            }

            //Query for and build list of documents
            List<SharedVM.DocumentInfo> documentList = new List<SharedVM.DocumentInfo>();
            if (projectInfo.DocumentContainerId != null)
            {
                documentList = db.DocumentInfoes
                    .Where(di => di.DocumentContainerId == projectInfo.DocumentContainerId)
                    .Select(di => new SharedVM.DocumentInfo
                    {
                        DocumentInfoId = di.DocumentInfoId,
                        DocumentVersions = di.DocumentVersions
                        .Select(dv => new SharedVM.DocumentVersion
                        {
                            DocumentVersionId = dv.DocumentVersionId,
                            Name = dv.Name,
                            VersionNumber = dv.VersionNumber,
                            Description = dv.Description,
                            Url = dv.Url,
                            UploadDate = dv.UploadDate,
                            UploadedBy = dv.UploadedBy
                        })
                        .OrderByDescending(dv => dv.DocumentVersionId).ToList()
                    }).ToList();
            }

            //build viewmodel and send to view
            var viewModel = new ProjectIndexViewModel
            {
                ProjectDetails = projectInfo,
                Sites = sites,
                Documents = documentList
            };
            return View(viewModel);
        }

        /// <summary>
        /// Controller function received a document in Post and uploads as blob into Azure storage into the parent entity type
        /// (projects, sites, audits) using parent entity's id as path. DocumentInfo is written to corresponding entity's db entry.
        /// If not already existing, a new DocumentContainer entry to created in db table to map the entity type to the DocumentInfo
        /// </summary>
        /// <param name="filename">name of file</param>
        /// <param name="id">document's parent entity's id</param>
        /// <param name="overwrite">'overwrite' explicitly to false so that each document is a new one</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult UploadDocument(String filename, int id, bool overwrite = false)
        {
            // Setting 'overwrite' explicitly to false so that each document is a new one
            // Version control currently not supported and overwrite not allowed
            overwrite = false;

            // Retrieve the file
            var file = Request.Files[filename];

            // Create a virtual directory within the filename
            string newFilename = filename;
            string newFilepath = id + "/" + newFilename;

            // Upload file to the blob storage
            AzureStorage storage = new AzureStorage();

            if (!overwrite)
            {
                // Retrieve the filename and the extension
                string filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
                string extension = Path.GetExtension(filename);

                // Check if the document already exists
                int index = 1;
                while (storage.BlobExists(AzureStorage.Containers.projects, newFilepath))
                {
                    newFilename = filenameWithoutExtension + "(" + index + ")" + extension;
                    newFilepath = id + "/" + newFilename;
                    index++;
                }
            }

            // Upload file to the blob storage
            string newPath = storage.UploadBlob(AzureStorage.Containers.projects, newFilepath, file.InputStream, file.ContentType);

            // Retrieve the project
            var project = (from p in db.Projects
                        where p.ProjectId == id
                           select p).FirstOrDefault();

            // Check if the site already has a container reference, otherwise assign a new one
            if (project.DocumentContainerId == null)
            {
                // Create and save a new container reference
                DocumentContainer container = new DocumentContainer();
                db.DocumentContainers.Add(container);
                db.SaveChanges();

                // Assign the new container reference to the site
                project.DocumentContainerId = container.DocumentContainerId;
                db.SaveChanges();
            }

            // Create a new document for the site
            DocumentInfo dInfo = new DocumentInfo { DocumentContainerId = (int)project.DocumentContainerId };
            db.DocumentInfoes.Add(dInfo);
            db.SaveChanges();

            DocumentVersion dVersion = new DocumentVersion
            {
                Name = newFilename,
                DocumentInfoId = dInfo.DocumentInfoId,
                Url = newPath
            };
            db.DocumentVersions.Add(dVersion);
            db.SaveChanges();


            // return the path name back
            return Json(new { success = true, path = newPath, name = newFilename, docid = dInfo.DocumentInfoId, versionid = dVersion.DocumentVersionId }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Controller function receives an entity image by Post, and saves it as blob in Azure under cardimages container
        /// and saves file path to entity's db entry.
        /// </summary>
        /// <param name="filename">image file name</param>
        /// <param name="id">entity id</param>
        /// <returns>JSON object containing the blob file path</returns>
        [HttpPost]
        public JsonResult UploadImage(String filename, int id)
        {
            /*Bypass to UploadDocument*/
            //UploadDocument(filename, id);

            // Retrieve the file and check for content type
            var file = Request.Files[filename];

            if(!file.ContentType.StartsWith("image", StringComparison.CurrentCultureIgnoreCase))
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            // Create new filename to be used as blob name
            //keep trying to upload until we don't override another file.
            String newFilename = filename;
            int tryNum = 0;
            while (CheckIfFilenameExists(newFilename))
            {
                newFilename = Path.GetFileNameWithoutExtension(filename) + tryNum + Path.GetExtension(filename);
            }

            // Upload file to the blob storage
            AzureStorage storage = new AzureStorage();
            string newPath = storage.UploadBlob(AzureStorage.Containers.cardimages, newFilename, file.InputStream, file.ContentType);

            // return image name (blob name) back
            return Json(new { success = true, path = newPath }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Controller function queries appropriate Project and updates db with values of received parameters
        /// </summary>
        /// <param name="id">ProjectId</param>
        /// <param name="name">Project Name</param>
        /// <param name="description">Project Description</param>
        /// <param name="refNumber">Project Reference Number/Code</param>
        /// <param name="imagePath">Project Image</param>
        /// <returns>JSON success response</returns>
        [HttpPost]
        public JsonResult SaveProjectDetails(int id,String name, String description, String refNumber, String imagePath)
        {
            var project = (from p in db.Projects
                           where p.ProjectId == id
                           select p).FirstOrDefault();

            project.Name = name;
            project.Description = description;
            project.ProjectCode = refNumber;
            string curPath = project.ImagePath;

            // Update file name (blob name) in the database
            project.ImagePath = imagePath;
            db.SaveChanges();

            return Json(new { success = true });
        }

        /// <summary>
        /// Controller Action function provides view model for a new Project
        /// </summary>
        /// <returns>Project View Model</returns>
        public ActionResult Create()
        {
            var viewModel = new ProjectIndexViewModel
            {
                ProjectDetails = new ProjectInfo (),
                Sites = new List<SiteInfo>()
            };

            return View(viewModel);
        }

        /// <summary>
        /// Controller Action Function receives JSON data from form to create new Project
        /// Added to database and projectId returned in success response.
        /// </summary>
        /// <param name="Name">Project Name</param>
        /// <param name="ProjectCode">Project Reference Number/Code</param>
        /// <param name="Description">Project Description</param>
        /// <param name="StartDate">Project Start Date</param>
        /// <param name="EstimatedCompletionDate">Project Estimated Completion Date</param>
        /// <param name="ActualCompletionDate">Project Actual Completion Date</param>
        /// <param name="CreatedBy">Project Author</param>
        /// <param name="LastModifiedBy">Project Modifier</param>
        /// <returns>Project object</returns>
        [HttpPost]
        public ActionResult Create(string Name, string ProjectCode, string Description, string StartDate, string EstimatedCompletionDate, string ActualCompletionDate, string CreatedBy, string LastModifiedBy)
        {
            DateTime curDate = DateTime.Now;
            DateTime sDate, eDate, aDate;
            CultureInfo cInfo = new CultureInfo("en-US");

            //Build Project model from JSON post data
            Project project = new Project()
            {
                Name = Name,
                ProjectCode = ProjectCode,
                Description = Description,
                CreatedBy = CreatedBy,
                LastModifiedBy = LastModifiedBy,
                CreatedDate = curDate,
                LastModifiedDate = curDate,
                StartDate = DateTime.TryParse(StartDate, cInfo, DateTimeStyles.None, out sDate) ? sDate : (DateTime?)null,
                EstimatedCompletionDate = DateTime.TryParse(EstimatedCompletionDate, cInfo, DateTimeStyles.None, out eDate) ? eDate : (DateTime?)null,
                ActualCompletionDate = DateTime.TryParse(ActualCompletionDate, cInfo, DateTimeStyles.None, out aDate) ? aDate : (DateTime?)null
            };

            if (ModelState.IsValid)
            {
                db.Projects.Add(project);
                db.SaveChanges();
                return Json(new { success = true, projectId = project.ProjectId }, JsonRequestBehavior.AllowGet);
            }

            return View(project);
        }
    }
}