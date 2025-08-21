using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using NewVivaApi.Models;  
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using NewVivaApi.Data;

namespace NewVivaApi.Services 
{
    public class EmailService
    {
        private readonly AppDbContext _context; 
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        
        private string urlbase => _configuration["AWS:BaseURL"];
        private string emailSystemCD => _configuration["AWS:EmailConfigCD"];
        private string domainUrlBase => _configuration["AWS:NewBaseUrl"];
        private string APIURL => _configuration["AWS:APIURL"];

        public EmailService(AppDbContext context, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
        }

        public string generateEmails(string TemplateName, Dictionary<string, string> keyPairs, string Invitees, string subject, string userName, Document attachment = null)
        {
            string templatePath = Path.Combine(_environment.WebRootPath, "Templates", TemplateName + ".html");
            string templateHtml = File.ReadAllText(templatePath);

            keyPairs.Add("{{siteLink}}", urlbase);

            EmailParameters ep = new EmailParameters();
            ep.templateHTML = templateHtml;
            ep.parmList = keyPairs;
            ep.emails = Invitees;
            ep.subject = subject;
            ep.fromEmail = "support@VivaPayApp.com";
            ep.createdByUser = userName;
            ep.SystemCD = emailSystemCD;
            ep.isUnitTestTF = false;

            EmailService es = new EmailService(_context, _configuration, _environment);
            ep = es.createEmail(ep, attachment);

            if (ep.sentSuccessfulTF == false)
            {
                return "Something went wrong when sending emails: " + ep.resultMessage;
            }

            return "";
        }

        public EmailParameters createEmail(EmailParameters ep, Document attachment = null)
        {
            //Check Params to be valid
            if (ep.templateHTML == null || ep.parmList.Count < 1 || ep.emails == null || ep.subject == null || ep.fromEmail == null || ep.createdByUser == null)
            {
                ep.sentSuccessfulTF = false;
                ep.resultMessage = "One or more of the email parameters is empty or null.";
                return ep;
            }

            ep.emailBodyHTML = ep.templateHTML;

            foreach (KeyValuePair<string, string> p in ep.parmList)
            {
                ep.emailBodyHTML = ep.emailBodyHTML.Replace(p.Key, p.Value);
            }

            if (ep.isUnitTestTF)
            {
                ep.sentSuccessfulTF = true;
                ep.resultMessage = "Email sent successfully.";
                return ep;
            }

            int? refEmailMessageID = null;
            
            // NOTE: You'll need to replace this with your .NET Core data access method
            // The old TableAdapter pattern doesn't exist in .NET Core
            // You might need to create a stored procedure call or use Entity Framework
            
            // Example using Entity Framework (adjust based on your actual implementation):
            try
            {
                // Replace this with your actual email logging implementation
                // This is just a placeholder showing the concept
                /*
                var emailMessage = new EmailMessage
                {
                    Recipients = ep.emails,
                    Subject = ep.subject,
                    CreatedBy = ep.createdByUser,
                    FromEmail = ep.fromEmail,
                    EmailBody = ep.emailBodyHTML,
                    SystemCD = ep.SystemCD,
                    CCAddress = ep.CCAddress,
                    BCCAddress = ep.BCCAddress,
                    CreatedDate = DateTime.UtcNow
                };
                
                if (attachment != null)
                {
                    emailMessage.AttachmentFileName = attachment.DownloadFileName;
                    emailMessage.AttachmentPath = attachment.Path + attachment.FileName;
                    emailMessage.AttachmentBucket = attachment.Bucket;
                }
                
                _context.EmailMessages.Add(emailMessage);
                _context.SaveChanges();
                refEmailMessageID = emailMessage.Id;
                */
                
                // Temporary placeholder - you'll need to implement your email storage logic
                refEmailMessageID = 1; // Replace with actual implementation
            }
            catch (Exception ex)
            {
                ep.sentSuccessfulTF = false;
                ep.resultMessage = $"Error saving email: {ex.Message}";
                return ep;
            }

            if (refEmailMessageID == null || refEmailMessageID < 1)
            {
                ep.sentSuccessfulTF = false;
                ep.resultMessage = "Something went wrong when inserting email into database.";
                return ep;
            }

            ep.emailMessageID = refEmailMessageID;
            ep.sentSuccessfulTF = true;
            ep.resultMessage = "Email sent successfully.";
            return ep;
        }

        public string listOfAdminsToNotify()
        {
            List<UserProfilesVw> listOfAdminsToNotify = _context.UserProfilesVws.Where(up => up.UserType == "Viva").ToList();
            string listOfAdminsToNotifyString = "";
            for (var i = 0; i < listOfAdminsToNotify.Count; i++)
            {
                listOfAdminsToNotifyString = listOfAdminsToNotifyString + listOfAdminsToNotify[i].UserName + ", ";
            }
            return listOfAdminsToNotifyString;
        }

        public string listOfGCUsersToNotify(int GeneralContractorID)
        {
            List<GeneralContractorUser> listOfGCUsersToNotify = _context.GeneralContractorUsers.Where(up => up.GeneralContractorId == GeneralContractorID).ToList();
            string listOfGCUsersToNotifyString = "";
            for (var i = 0; i < listOfGCUsersToNotify.Count; i++)
            {
                string userID = listOfGCUsersToNotify[i].UserId;
                UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == userID);
                listOfGCUsersToNotifyString = listOfGCUsersToNotifyString + userProfileRecord.UserName + ", ";
            }
            return listOfGCUsersToNotifyString;
        }

        public string listOfSCUsersToNotify(int SubcontractorID)
        {
            List<SubcontractorUser> listOfSCUsersToNotify = _context.SubcontractorUsers.Where(up => up.SubcontractorId == SubcontractorID).ToList();
            string listOfSCUsersToNotifyString = "";
            for (var i = 0; i < listOfSCUsersToNotify.Count; i++)
            {
                string userID = listOfSCUsersToNotify[i].UserId;
                UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == userID);
                listOfSCUsersToNotifyString = listOfSCUsersToNotifyString + userProfileRecord.UserName + ", ";
            }
            return listOfSCUsersToNotifyString;
        }

        public string sendNewSubcontractorEmail(string UserID, int SubcontractorID, string TmpPassword)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            SubcontractorsVw subcontractorRecord = _context.SubcontractorsVws.FirstOrDefault(sc => sc.SubcontractorId == SubcontractorID);

            string templateHTML = "NewSCAdded";

            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            JObject jsonAttributes = JObject.Parse(subcontractorRecord.JsonAttributes);
            keyPairs.Add("{{ContactName}}", userProfileRecord.FirstName);
            keyPairs.Add("{{SubcontractorName}}", subcontractorRecord.SubcontractorName);
            keyPairs.Add("{{Password}}", TmpPassword);
                        
            generateEmails(templateHTML, keyPairs, userProfileRecord.UserName, "New Subcontractor User Added", userProfileRecord.UserName);

            return "";
        }

        public void sendNewGeneralContractorEmail(string UserID, int GeneralContractorID, string TmpPassword)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            GeneralContractorsVw generalContractorRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == GeneralContractorID);

            string templateHTML = "NewGCAdded";

            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            JObject jsonAttributes = JObject.Parse(generalContractorRecord.JsonAttributes);
            keyPairs.Add("{{ContactName}}", userProfileRecord.FirstName);
            keyPairs.Add("{{GeneralContractorName}}", generalContractorRecord.GeneralContractorName);
            keyPairs.Add("{{Password}}", TmpPassword);

            generateEmails(templateHTML, keyPairs, userProfileRecord.UserName, "New General Contractor User Added", userProfileRecord.UserName);
        }

        public void sendNewAdminEmail(string UserID, string TmpPassword)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            string emailList = listOfAdminsToNotify();

            string templateHTML = "NewAdminAdded";

            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            keyPairs.Add("{{UserName}}", userProfileRecord.UserName);
            keyPairs.Add("{{Password}}", TmpPassword);

            generateEmails(templateHTML, keyPairs, emailList, "New Admin Added", userProfileRecord.UserName);
        }

        public void sendPasswordResetLinkEmail(string Email, string Name, string UserName, string resetToken, string domain)
        {
            string templateHTML = "ResetPassword";
            string resetLink = String.Concat(domain, "reset/", resetToken);

            Dictionary<string, string> keyPairs = new Dictionary<string, string>();
            keyPairs.Add("{{Name}}", Name);
            keyPairs.Add("{{ResetLink}}", resetLink);

            generateEmails(templateHTML, keyPairs, Email, "Reset password instructions", UserName);
        }

        public void sendPasswordChangedEmail(string Email, string Name)
        {
            string templateHTML = "PasswordChangeNotification";

            Dictionary<string, string> keyPairs = new Dictionary<string, string>();
            keyPairs.Add("{{Name}}", Name);            

            generateEmails(templateHTML, keyPairs, Email, "Password changed", Email);
        }

        public void sendPayAppToApproveEmail(string UserID, int GeneralContractorID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            GeneralContractorsVw generalContractorRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == GeneralContractorID);
            JObject jsonAttributes = JObject.Parse(generalContractorRecord.JsonAttributes);
            string emailList = listOfGCUsersToNotify(GeneralContractorID);

            string templateHTML = "GCPayAppsToApprove";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>(); 

            if (generalContractorRecord.GeneralContractorName != null)
            {
                keyPairs.Add("{{GeneralContractorName}}", generalContractorRecord.GeneralContractorName);
            }
            else
            {
                keyPairs.Add("{{GeneralContractorName}}", "Viva");
            }

            if (generalContractorRecord.DommainName != null)
            {
                keyPairs.Add("{{DomainName}}", "https://" + generalContractorRecord.DommainName + domainUrlBase);
            }
            else
            {
                keyPairs.Add("{{DomainName}}", urlbase);
            }

            generateEmails(templateHTML, keyPairs, emailList, "New PayApp to Approve", userProfileRecord.UserName);
        }

        public void sendVivaNotificationNewPayApp(string UserID, int ProjectID, int GeneralContractorID, int PayAppID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            GeneralContractorsVw generalContractorRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == GeneralContractorID);
            ProjectsVw projectIDRecord = _context.ProjectsVws.FirstOrDefault(pn => pn.ProjectId == ProjectID);
            PayAppsVw payAppID = _context.PayAppsVws.FirstOrDefault(id => id.PayAppId == PayAppID);
            string emailList = listOfAdminsToNotify();

            string templateHTML = "AdminPayAppNew";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            keyPairs.Add("{{GeneralContractorName}}", generalContractorRecord.GeneralContractorName);
            keyPairs.Add("{{SubcontractorName}}", userProfileRecord.CompanyName);
            keyPairs.Add("{{ProjectName}}", projectIDRecord.ProjectName);
            keyPairs.Add("{{PayAppID}}", payAppID.VivaPayAppId);    

            generateEmails(templateHTML, keyPairs, emailList, "New PayApp to Approve", userProfileRecord.UserName);
        }

        public void sendAdminEmailNewSubcontractor(string UserID, int SubcontractorID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            SubcontractorsVw subcontractorRecord = _context.SubcontractorsVws.FirstOrDefault(sc => sc.SubcontractorId == SubcontractorID);
            string emailList = listOfAdminsToNotify();

            string templateHTML = "AdminNewSC";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            keyPairs.Add("{{SubcontractorName}}", subcontractorRecord.SubcontractorName);
            keyPairs.Add("{{UserName}}", userProfileRecord.UserName);

            generateEmails(templateHTML, keyPairs, emailList, "New Subcontractor Added", userProfileRecord.UserName);
        }

        public void sendAdminEmailNewGeneralContractor(string UserID, int GeneralContractorID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            GeneralContractorsVw generalContractorsRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == GeneralContractorID);
            string emailList = listOfAdminsToNotify();

            string templateHTML = "AdminNewGC";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            keyPairs.Add("{{GeneralContractorName}}", generalContractorsRecord.GeneralContractorName);
            keyPairs.Add("{{UserName}}", userProfileRecord.UserName);      

            generateEmails(templateHTML, keyPairs, emailList, "New General Contractor Added", userProfileRecord.UserName);
        }

        public void sendAdminEmailNewSubcontractorUser(string UserID, int SubcontractorID, string creatorUserName)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            SubcontractorsVw subcontractorRecord = _context.SubcontractorsVws.FirstOrDefault(sc => sc.SubcontractorId == SubcontractorID);
            string emailList = listOfAdminsToNotify();

            string templateHTML = "AdminNewSCUser";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            JObject jsonAttributes = JObject.Parse(subcontractorRecord.JsonAttributes);
            keyPairs.Add("{{ContactName}}", userProfileRecord.UserName);
            keyPairs.Add("{{SubcontractorName}}", subcontractorRecord.SubcontractorName);
            keyPairs.Add("{{UserName}}", creatorUserName);      

            generateEmails(templateHTML, keyPairs, emailList, "New Subcontractor User", userProfileRecord.UserName);
        }

        public void sendAdminEmailNewGeneralContractorUser(string newUserID, int GeneralContractorID, string creatorUserName)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == newUserID);
            GeneralContractorsVw generalContractorsRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == GeneralContractorID);

            string emailList = listOfAdminsToNotify();

            string templateHTML = "AdminNewGCUser";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            JObject jsonAttributes = JObject.Parse(generalContractorsRecord.JsonAttributes);
            keyPairs.Add("{{ContactName}}", userProfileRecord.UserName);
            keyPairs.Add("{{GeneralContractorName}}", generalContractorsRecord.GeneralContractorName);
            keyPairs.Add("{{UserName}}", creatorUserName);     

            generateEmails(templateHTML, keyPairs, emailList, "New General Contractor User", userProfileRecord.UserName);
        }

        public void sendAdminEmailNewAdmin(string UserID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            string emailList = listOfAdminsToNotify();

            string templateHTML = "AdminNewAdmin";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();
            
            keyPairs.Add("{{UserName}}", userProfileRecord.UserName);

            generateEmails(templateHTML, keyPairs, emailList, "New Admin User", userProfileRecord.UserName);
        }

        public void sendGCEmailReportsAvailable(string UserID, int GeneralContractorID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            GeneralContractorsVw generalContractorRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == GeneralContractorID);
            string emailList = listOfGCUsersToNotify(GeneralContractorID);
            JObject jsonAttributes = JObject.Parse(generalContractorRecord.JsonAttributes);

            string templateHTML = "GCReportsAvailable";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            if (generalContractorRecord.GeneralContractorName != null)
            {
                keyPairs.Add("{{GeneralContractorName}}", generalContractorRecord.GeneralContractorName);
            }
            else
            {
                keyPairs.Add("{{GeneralContractorName}}", "Viva");
            }

            if (jsonAttributes["PrimaryColor"] != null)
            {
                keyPairs.Add("primaryColor", jsonAttributes["PrimaryColor"].ToString());
            }
            else
            {
                keyPairs.Add("primaryColor", "#2000d0");
            }

            if(generalContractorRecord.DommainName != null)
            {
                keyPairs.Add("{{DomainName}}", "https://" + generalContractorRecord.DommainName + domainUrlBase);
            }
            else
            {
                keyPairs.Add("{{DomainName}}", urlbase);
            }

            generateEmails(templateHTML, keyPairs, emailList, "Reports Available for Your Review", userProfileRecord.UserName);
        }

        public void sendSCAddedToProject(string UserID, int SubcontractorID, string ProjectName)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            SubcontractorsVw subcontractorRecord = _context.SubcontractorsVws.FirstOrDefault(sc => sc.SubcontractorId == SubcontractorID);
            ProjectsVw projectNameRecord = _context.ProjectsVws.FirstOrDefault(pn => pn.ProjectName == ProjectName);
            GeneralContractorsVw generalContractorRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == projectNameRecord.GeneralContractorId);

            string emailList = listOfSCUsersToNotify(SubcontractorID);

            JObject jsonAttributes = JObject.Parse(generalContractorRecord.JsonAttributes);

            string templateHTML = "SCAddedToProject";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            keyPairs.Add("{{SubcontractorName}}", subcontractorRecord.SubcontractorName);
            keyPairs.Add("{{ProjectName}}", projectNameRecord.ProjectName);
            keyPairs.Add("{{UserName}}", userProfileRecord.UserName);
                        
            if (generalContractorRecord.GeneralContractorName != null)
            {
                keyPairs.Add("{{GeneralContractorName}}", generalContractorRecord.GeneralContractorName);
            }
            else
            {
                keyPairs.Add("{{GeneralContractorName}}", "Viva");
            }

            if (jsonAttributes["PrimaryColor"] != null)
            {
                keyPairs.Add("primaryColor", jsonAttributes["PrimaryColor"].ToString());
            }
            else
            {
                keyPairs.Add("primaryColor", "#2000d0");
            }

            if (generalContractorRecord.DommainName != null)
            {
                keyPairs.Add("{{DomainName}}", "https://" + generalContractorRecord.DommainName + domainUrlBase);
            }
            else
            {
                keyPairs.Add("{{DomainName}}", urlbase);
            }

            generateEmails(templateHTML, keyPairs, emailList, "Subcontractor Added to Project", userProfileRecord.UserName);
        }

        public void sendSCNewSignupForm(string UserID, int documentID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            Document attachment = _context.Documents.FirstOrDefault(d => d.DocumentId == documentID);
            SubcontractorsVw subcontractorRecord = _context.SubcontractorsVws.FirstOrDefault(sc => sc.SubcontractorId == attachment.SubcontractorId);
            SubcontractorProjectsVw subProjRecord = _context.SubcontractorProjectsVws.FirstOrDefault(sp => sp.SubcontractorProjectId == attachment.SubcontractorProjectId);
            ProjectsVw projectNameRecord = _context.ProjectsVws.FirstOrDefault(pn => pn.ProjectId == subProjRecord.ProjectId);
            GeneralContractorsVw generalContractorRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == projectNameRecord.GeneralContractorId);

            string emailList = listOfSCUsersToNotify((int)attachment.SubcontractorId);

            JObject jsonAttributes = JObject.Parse(generalContractorRecord.JsonAttributes);

            string templateHTML = "SCNewSignUpForm";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            keyPairs.Add("{{SubcontractorName}}", subcontractorRecord.SubcontractorName);
            keyPairs.Add("{{ProjectName}}", projectNameRecord.ProjectName);
            keyPairs.Add("{{UserName}}", userProfileRecord.UserName);   

            if (generalContractorRecord.GeneralContractorName != null)
            {
                keyPairs.Add("{{GeneralContractorName}}", generalContractorRecord.GeneralContractorName);
            }
            else
            {
                keyPairs.Add("{{GeneralContractorName}}", "Viva");
            }

            if (jsonAttributes["PrimaryColor"] != null)
            {
                keyPairs.Add("primaryColor", jsonAttributes["PrimaryColor"].ToString());
            }
            else
            {
                keyPairs.Add("primaryColor", "#2000d0");
            }

            if (generalContractorRecord.DommainName != null)
            {
                keyPairs.Add("{{DomainName}}", "https://" + generalContractorRecord.DommainName + domainUrlBase);
            }
            else
            {
                keyPairs.Add("{{DomainName}}", urlbase);
            }

            generateEmails(templateHTML, keyPairs, emailList, "New Sign-Up Form Added to Project", userProfileRecord.UserName, attachment);
        }

        public void sendAdminPayAppApproved(string UserID, int ProjectID, int SubcontractorID, int PayAppID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            SubcontractorsVw subcontractorRecord = _context.SubcontractorsVws.FirstOrDefault(sc => sc.SubcontractorId == SubcontractorID);
            ProjectsVw projectIDRecord = _context.ProjectsVws.FirstOrDefault(pn => pn.ProjectId == ProjectID);
            PayAppsVw payAppRecord = _context.PayAppsVws.FirstOrDefault(id => id.PayAppId == PayAppID);
            GeneralContractorsVw generalcontractorRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == projectIDRecord.GeneralContractorId);
            JObject jsonAttributes = JObject.Parse(generalcontractorRecord.JsonAttributes);
            string emailList = listOfAdminsToNotify();

            dynamic jsonDollar = JsonConvert.DeserializeObject<dynamic>(payAppRecord.JsonAttributes);
            decimal dollaramount = jsonDollar.DueToSub;

            string templateHTML = "PayAppApproved";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            keyPairs.Add("{{PayAppID}}", payAppRecord.VivaPayAppId);
            keyPairs.Add("{{ProjectName}}", projectIDRecord.ProjectName);
            keyPairs.Add("{{SubcontractorName}}", subcontractorRecord.SubcontractorName);
            keyPairs.Add("{{DollarAmount}}", dollaramount.ToString("C2", CultureInfo.CreateSpecificCulture("en-US")));        

            if (generalcontractorRecord.GeneralContractorName != null)
            {
                keyPairs.Add("{{GeneralContractorName}}", generalcontractorRecord.GeneralContractorName);
            }
            else
            {
                keyPairs.Add("{{GeneralContractorName}}", "Viva");
            }

            if (jsonAttributes["PrimaryColor"] != null)
            {
                keyPairs.Add("primaryColor", jsonAttributes["PrimaryColor"].ToString());
            }
            else
            {
                keyPairs.Add("primaryColor", "#2000d0");
            }

            if (generalcontractorRecord.DommainName != null)
            {
                keyPairs.Add("{{DomainName}}", "https://" + generalcontractorRecord.DommainName + domainUrlBase);
            }
            else
            {
                keyPairs.Add("{{DomainName}}", urlbase);
            }

            generateEmails(templateHTML, keyPairs, emailList, "PayApp Approved", userProfileRecord.UserName);
        }

        public void sendSCPayAppApproved(string UserID, int ProjectID, int SubcontractorID, int PayAppID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            SubcontractorsVw subcontractorRecord = _context.SubcontractorsVws.FirstOrDefault(sc => sc.SubcontractorId == SubcontractorID);
            ProjectsVw projectIDRecord = _context.ProjectsVws.FirstOrDefault(pn => pn.ProjectId == ProjectID);
            PayAppsVw payAppRecord = _context.PayAppsVws.FirstOrDefault(id => id.PayAppId == PayAppID);
            GeneralContractorsVw generalcontractorRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == projectIDRecord.GeneralContractorId);
            JObject jsonAttributes = JObject.Parse(generalcontractorRecord.JsonAttributes);
            string emailList = listOfSCUsersToNotify(SubcontractorID);

            dynamic jsonDollar = JsonConvert.DeserializeObject<dynamic>(payAppRecord.JsonAttributes);
            decimal dollaramount = jsonDollar.DueToSub;

            string templateHTML = "PayAppApproved";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            keyPairs.Add("{{PayAppID}}", payAppRecord.VivaPayAppId);
            keyPairs.Add("{{ProjectName}}", projectIDRecord.ProjectName);
            keyPairs.Add("{{SubcontractorName}}", subcontractorRecord.SubcontractorName);
            keyPairs.Add("{{DollarAmount}}", dollaramount.ToString("C2", CultureInfo.CreateSpecificCulture("en-US")));

            if (generalcontractorRecord.GeneralContractorName != null)
            {
                keyPairs.Add("{{GeneralContractorName}}", generalcontractorRecord.GeneralContractorName);
            }
            else
            {
                keyPairs.Add("{{GeneralContractorName}}", "Viva");
            }

            if (jsonAttributes["PrimaryColor"] != null)
            {
                keyPairs.Add("primaryColor", jsonAttributes["PrimaryColor"].ToString());
            }
            else
            {
                keyPairs.Add("primaryColor", "#2000d0");
            }

            if (generalcontractorRecord.DommainName != null)
            {
                keyPairs.Add("{{DomainName}}", "https://" + generalcontractorRecord.DommainName + domainUrlBase);
            }
            else
            {
                keyPairs.Add("{{DomainName}}", urlbase);
            }
            generateEmails(templateHTML, keyPairs, emailList, "PayApp Approved", userProfileRecord.UserName);

        }

        public void sendSCNeedLienRelease(string UserID, int ProjectID, int SubcontractorID, int PayAppID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            SubcontractorsVw subcontractorRecord = _context.SubcontractorsVws.FirstOrDefault(sc => sc.SubcontractorId == SubcontractorID);
            ProjectsVw projectIDRecord = _context.ProjectsVws.FirstOrDefault(pn => pn.ProjectId == ProjectID);
            PayAppsVw payAppRecord = _context.PayAppsVws.FirstOrDefault(id => id.PayAppId == PayAppID);
            GeneralContractorsVw generalcontractorRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == projectIDRecord.GeneralContractorId);
            JObject jsonAttributes = JObject.Parse(generalcontractorRecord.JsonAttributes);
            string emailList = listOfSCUsersToNotify(SubcontractorID);

            string templateHTML = "SCLienReleaseNeeded";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            keyPairs.Add("{{PayAppID}}", payAppRecord.VivaPayAppId);
            keyPairs.Add("{{ProjectName}}", projectIDRecord.ProjectName);
            keyPairs.Add("{{SubcontractorName}}", subcontractorRecord.SubcontractorName);

            if (generalcontractorRecord.GeneralContractorName != null)
            {
                keyPairs.Add("{{GeneralContractorName}}", generalcontractorRecord.GeneralContractorName);
            }
            else
            {
                keyPairs.Add("{{GeneralContractorName}}", "Viva");
            }

            if (jsonAttributes["PrimaryColor"] != null)
            {
                keyPairs.Add("primaryColor", jsonAttributes["PrimaryColor"].ToString());
            }
            else
            {
                keyPairs.Add("primaryColor", "#2000d0");
            }

            if (generalcontractorRecord.DommainName != null)
            {
                keyPairs.Add("{{DomainName}}", "https://" + generalcontractorRecord.DommainName + domainUrlBase);
            }
            else
            {
                keyPairs.Add("{{DomainName}}", urlbase);
            }

            generateEmails(templateHTML, keyPairs, emailList, "Lien Release Needed", userProfileRecord.UserName);
        }

        public void sendSCPaymentInfo(string UserID, int ProjectID, int SubcontractorID, int PayAppID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            SubcontractorsVw subcontractorRecord = _context.SubcontractorsVws.FirstOrDefault(sc => sc.SubcontractorId == SubcontractorID);
            ProjectsVw projectIDRecord = _context.ProjectsVws.FirstOrDefault(pn => pn.ProjectId == ProjectID);
            PayAppsVw payAppRecord = _context.PayAppsVws.FirstOrDefault(id => id.PayAppId == PayAppID);
            GeneralContractorsVw generalcontractorRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == projectIDRecord.GeneralContractorId);
            JObject jsonAttributes = JObject.Parse(generalcontractorRecord.JsonAttributes);
            string emailList = listOfSCUsersToNotify(SubcontractorID);

            dynamic jsonDollar = JsonConvert.DeserializeObject<dynamic>(payAppRecord.JsonAttributes);
            var dollaramount = jsonDollar.DueToSub;

            string templateHTML = "SCPayAppPaid";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            keyPairs.Add("{{SubcontractorName}}", subcontractorRecord.SubcontractorName);
            keyPairs.Add("{{PayAppID}}", payAppRecord.VivaPayAppId);
            keyPairs.Add("{{ProjectName}}", projectIDRecord.ProjectName);
            keyPairs.Add("{{DollarAmount}}", dollaramount.ToString("C2", CultureInfo.CreateSpecificCulture("en-US")));          

            if (generalcontractorRecord.GeneralContractorName != null)
            {
                keyPairs.Add("{{GeneralContractorName}}", generalcontractorRecord.GeneralContractorName);
            }
            else
            {
                keyPairs.Add("{{GeneralContractorName}}", "Viva");
            }

            if (jsonAttributes["PrimaryColor"] != null)
            {
                keyPairs.Add("primaryColor", jsonAttributes["PrimaryColor"].ToString());
            }
            else
            {
                keyPairs.Add("primaryColor", "#2000d0");
            }

            if (generalcontractorRecord.DommainName != null)
            {
                keyPairs.Add("{{DomainName}}", "https://" + generalcontractorRecord.DommainName + domainUrlBase);
            }
            else
            {
                keyPairs.Add("{{DomainName}}", urlbase);
            }

            generateEmails(templateHTML, keyPairs, emailList, "Pay App Paid", userProfileRecord.UserName);
        }

        public void sendSCPaymentInfoForServiceUser(string UserID, int ProjectID, int SubcontractorID, int PayAppID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            SubcontractorsVw subcontractorRecord = _context.SubcontractorsVws.FirstOrDefault(sc => sc.SubcontractorId == SubcontractorID);
            ProjectsVw projectIDRecord = _context.ProjectsVws.FirstOrDefault(pn => pn.ProjectId == ProjectID);
            PayAppsVw payAppRecord = _context.PayAppsVws.FirstOrDefault(id => id.PayAppId == PayAppID);
            GeneralContractorsVw generalcontractorRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == projectIDRecord.GeneralContractorId);
            JObject jsonAttributes = JObject.Parse(generalcontractorRecord.JsonAttributes);
            string emailList = listOfAdminsToNotify();

            dynamic jsonDollar = JsonConvert.DeserializeObject<dynamic>(payAppRecord.JsonAttributes);
            var dollaramount = jsonDollar.DueToSub;

            string templateHTML = "SCPayAppPaid";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            keyPairs.Add("{{SubcontractorName}}", subcontractorRecord.SubcontractorName);
            keyPairs.Add("{{PayAppID}}", payAppRecord.VivaPayAppId);
            keyPairs.Add("{{ProjectName}}", projectIDRecord.ProjectName);
            keyPairs.Add("{{DollarAmount}}", dollaramount.ToString("C2", CultureInfo.CreateSpecificCulture("en-US")));     

            if (generalcontractorRecord.GeneralContractorName != null)
            {
                keyPairs.Add("{{GeneralContractorName}}", generalcontractorRecord.GeneralContractorName);
            }
            else
            {
                keyPairs.Add("{{GeneralContractorName}}", "Viva");
            }

            if (jsonAttributes["PrimaryColor"] != null)
            {
                keyPairs.Add("primaryColor", jsonAttributes["PrimaryColor"].ToString());
            }
            else
            {
                keyPairs.Add("primaryColor", "#2000d0");
            }

            if (generalcontractorRecord.DommainName != null)
            {
                keyPairs.Add("{{DomainName}}", "https://" + generalcontractorRecord.DommainName + domainUrlBase);
            }
            else
            {
                keyPairs.Add("{{DomainName}}", urlbase);
            }

            generateEmails(templateHTML, keyPairs, emailList, "Pay App Paid", userProfileRecord.UserName);
        }

        public void sendLeinReleaseSubmittedEmail(string UserID, int PayAppID)
        {
            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            PayAppsVw payAppRecord = _context.PayAppsVws.FirstOrDefault(id => id.PayAppId == PayAppID);
            SubcontractorsVw subcontractorRecord = _context.SubcontractorsVws.FirstOrDefault(sc => sc.SubcontractorId == payAppRecord.SubcontractorId);
            ProjectsVw projectIDRecord = _context.ProjectsVws.FirstOrDefault(pn => pn.ProjectId == payAppRecord.ProjectId);
            GeneralContractorsVw generalContractorRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == projectIDRecord.GeneralContractorId);
            JObject jsonAttributes = JObject.Parse(generalContractorRecord.JsonAttributes);

            string emailList = listOfAdminsToNotify();
            emailList += ", " + listOfGCUsersToNotify(projectIDRecord.GeneralContractorId);

            string templateHTML = "AdminLienReleaseUploaded";
            Dictionary<string, string> keyPairs = new Dictionary<string, string>();

            keyPairs.Add("{{SubcontractorName}}", subcontractorRecord.SubcontractorName);
            keyPairs.Add("{{PayAppID}}", payAppRecord.VivaPayAppId);
            keyPairs.Add("{{ProjectName}}", projectIDRecord.ProjectName);           

            if (generalContractorRecord.GeneralContractorName != null)
            {
                keyPairs.Add("{{GeneralContractorName}}", generalContractorRecord.GeneralContractorName);
            }
            else
            {
                keyPairs.Add("{{GeneralContractorName}}", "Viva");
            }

            if (jsonAttributes["PrimaryColor"] != null)
            {
                keyPairs.Add("primaryColor", jsonAttributes["PrimaryColor"].ToString());
            }
            else
            {
                keyPairs.Add("primaryColor", "#2000d0");
            }

            if (generalContractorRecord.DommainName != null)
            {
                keyPairs.Add("{{DomainName}}", "https://" + generalContractorRecord.DommainName + domainUrlBase);
            }
            else
            {
                keyPairs.Add("{{DomainName}}", urlbase);
            }

            generateEmails(templateHTML, keyPairs, emailList, "Lein Release Submitted", userProfileRecord.UserName);
        }
    }

    // Helper classes - you'll need to create these based on your data models
    public class EmailParameters
    {
        public string templateHTML { get; set; }
        public Dictionary<string, string> parmList { get; set; }
        public string emails { get; set; }
        public string subject { get; set; }
        public string fromEmail { get; set; }
        public string createdByUser { get; set; }
        public string SystemCD { get; set; }
        public bool isUnitTestTF { get; set; }
        public string emailBodyHTML { get; set; }
        public bool sentSuccessfulTF { get; set; }
        public string resultMessage { get; set; }
        public int? emailMessageID { get; set; }
        public string CCAddress { get; set; }
        public string BCCAddress { get; set; }
    }
}

            

        