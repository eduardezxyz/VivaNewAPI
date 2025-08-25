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
using Microsoft.Data.SqlClient;
using System.Data;

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
            string templatePath = Path.Combine(_environment.ContentRootPath, "Templates", TemplateName + ".html");
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

            ep = createEmail(ep, attachment);

            if (ep.sentSuccessfulTF == false)
            {
                return "Something went wrong when sending emails: " + ep.resultMessage;
            }

            return "";
        }

        public EmailParameters createEmail(EmailParameters ep, Document attachment = null)
        {
            Console.WriteLine("=== createEmail method started ===");
            Console.WriteLine($"Email parameters: Recipients={ep.emails}, Subject={ep.subject}, CreatedBy={ep.createdByUser}, SystemCD={ep.SystemCD}");

            //Check Params to be valid
            if (ep.templateHTML == null || ep.parmList.Count < 1 || ep.emails == null || ep.subject == null || ep.fromEmail == null || ep.createdByUser == null)
            {
                Console.WriteLine("Email validation FAILED - missing required parameters");
                Console.WriteLine($"Validation details: templateHTML={ep.templateHTML != null}, parmListCount={ep.parmList?.Count ?? 0}, emails={ep.emails}, subject={ep.subject}, fromEmail={ep.fromEmail}, createdByUser={ep.createdByUser}");

                ep.sentSuccessfulTF = false;
                ep.resultMessage = "One or more of the email parameters is empty or null.";
                return ep;
            }

            Console.WriteLine("Email validation PASSED");

            ep.emailBodyHTML = ep.templateHTML;
            Console.WriteLine($"Starting template parameter replacement with {ep.parmList.Count} parameters");

            foreach (KeyValuePair<string, string> p in ep.parmList)
            {
                Console.WriteLine($"Replacing parameter {p.Key} with value: {p.Value}");
                ep.emailBodyHTML = ep.emailBodyHTML.Replace(p.Key, p.Value);
            }

            Console.WriteLine("Template parameter replacement completed");

            if (ep.isUnitTestTF)
            {
                Console.WriteLine("Unit test mode - skipping actual email insertion");
                ep.sentSuccessfulTF = true;
                ep.resultMessage = "Email sent successfully.";
                return ep;
            }

            int? refEmailMessageID = null;
            Console.WriteLine("Starting database operations for email insertion");

            try
            {
                var connectionString = _configuration.GetConnectionString("UtilityConnectionString");
                Console.WriteLine($"Using connection string from configuration: UtilityConnectionString");

                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("ERROR: UtilityConnectionString is null or empty in configuration");
                    throw new InvalidOperationException("UtilityConnectionString not found in configuration");
                }

                Console.WriteLine("Opening database connection...");
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                Console.WriteLine("Database connection opened successfully");

                if (attachment == null)
                {
                    Console.WriteLine("Processing email WITHOUT attachment using UE_EmailMessage_I stored procedure");

                    // Call UE_EmailMessage_I stored procedure
                    using var command = new SqlCommand("dbo.UE_EmailMessage_I", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    Console.WriteLine("Creating stored procedure parameters...");

                    var emailIdParam = new SqlParameter("@EmailMessageID", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.InputOutput,
                        Value = DBNull.Value
                    };
                    command.Parameters.Add(emailIdParam);

                    command.Parameters.AddWithValue("@ToAddress", ep.emails ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@MsgSubject", ep.subject ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CreatedUser", ep.createdByUser ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@FromAddress", ep.fromEmail ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@FromDisplayName", ep.createdByUser ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@BodyHTML", ep.emailBodyHTML ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@BodyURL", DBNull.Value);
                    command.Parameters.AddWithValue("@SystemCD", ep.SystemCD ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CCAddress", ep.CCAddress ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@BCCAddress", ep.BCCAddress ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ImportanceID", 1);
                    command.Parameters.AddWithValue("@EmbedBodyURLImagesTF", false);
                    command.Parameters.AddWithValue("@TestModeTF", false);
                    command.Parameters.AddWithValue("@BatchPriority", 50);
                    command.Parameters.AddWithValue("@SentDT", DBNull.Value);
                    command.Parameters.AddWithValue("@ReplyTo", DBNull.Value);

                    Console.WriteLine("🚀 Executing UE_EmailMessage_I stored procedure...");
                    var rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"Stored procedure executed, rows affected: {rowsAffected}");

                    refEmailMessageID = emailIdParam.Value as int?;
                    Console.WriteLine($"Email inserted successfully with ID: {refEmailMessageID}");
                }
                else
                {
                    Console.WriteLine("Processing email WITH attachment using UE_EmailMessageWithAttachment_I stored procedure");
                    Console.WriteLine($"Attachment details: FileName={attachment.DownloadFileName}, Path={attachment.Path}, Bucket={attachment.Bucket}");

                    // Call UE_EmailMessageWithAttachment_I stored procedure
                    using var command = new SqlCommand("dbo.UE_EmailMessageWithAttachment_I", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    Console.WriteLine("Creating stored procedure parameters for attachment email...");

                    var emailIdParam = new SqlParameter("@EmailMessageID", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.InputOutput,
                        Value = DBNull.Value
                    };
                    command.Parameters.Add(emailIdParam);

                    // Basic email parameters
                    command.Parameters.AddWithValue("@ToAddress", ep.emails ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@MsgSubject", ep.subject ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CreatedUser", ep.createdByUser ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@FromAddress", ep.fromEmail ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@FromDisplayName", ep.createdByUser ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@BodyHTML", ep.emailBodyHTML ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@BodyURL", DBNull.Value);
                    command.Parameters.AddWithValue("@SystemCD", ep.SystemCD ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CCAddress", ep.CCAddress ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@BCCAddress", ep.BCCAddress ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ImportanceID", 1);
                    command.Parameters.AddWithValue("@EmbedBodyURLImagesTF", false);
                    command.Parameters.AddWithValue("@TestModeTF", false);

                    // Attachment parameters
                    command.Parameters.AddWithValue("@AttachmentFileName", attachment.DownloadFileName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@AttachmentURL", DBNull.Value);
                    command.Parameters.AddWithValue("@AttachmentFilePath", (attachment.Path + attachment.FileName) ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@BatchPriority", 50);
                    command.Parameters.AddWithValue("@s3_FileName", attachment.FileName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@s3_TF", true);
                    command.Parameters.AddWithValue("@s3_Bucket", attachment.Bucket ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@HtmlToPDF_TF", false);
                    command.Parameters.AddWithValue("@HtmlToPDF_WaitForCallback", false);
                    command.Parameters.AddWithValue("@HtmlToPDF_MinLoadWait_ms", 0);
                    command.Parameters.AddWithValue("@HtmlToPDF_PostData", DBNull.Value);
                    command.Parameters.AddWithValue("@ReplyTo", DBNull.Value);

                    Console.WriteLine("🚀 Executing UE_EmailMessageWithAttachment_I stored procedure...");
                    var result = command.ExecuteScalar();
                    Console.WriteLine($"Stored procedure executed, result: {result}");

                    refEmailMessageID = emailIdParam.Value as int?;
                    Console.WriteLine($"Email with attachment inserted successfully with ID: {refEmailMessageID}");
                }

                Console.WriteLine("🔌 Closing database connection");
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL ERROR occurred while inserting email");
                Console.WriteLine($"   Error Number: {sqlEx.Number}");
                Console.WriteLine($"   Severity: {sqlEx.Class}");
                Console.WriteLine($"   State: {sqlEx.State}");
                Console.WriteLine($"   Message: {sqlEx.Message}");
                Console.WriteLine($"   Stack Trace: {sqlEx.StackTrace}");

                ep.sentSuccessfulTF = false;
                ep.resultMessage = $"Database error saving email: {sqlEx.Message}";
                return ep;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UNEXPECTED ERROR occurred while saving email");
                Console.WriteLine($"   Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   Stack Trace: {ex.StackTrace}");

                ep.sentSuccessfulTF = false;
                ep.resultMessage = $"Error saving email: {ex.Message}";
                return ep;
            }

            if (refEmailMessageID == null || refEmailMessageID < 1)
            {
                Console.WriteLine($"Email insertion FAILED - EmailMessageID is null or invalid: {refEmailMessageID}");
                ep.sentSuccessfulTF = false;
                ep.resultMessage = "Something went wrong when inserting email into UE_EmailMessage table.";
                return ep;
            }

            Console.WriteLine($"Email successfully queued for sending with ID: {refEmailMessageID}");
            ep.emailMessageID = refEmailMessageID;
            ep.sentSuccessfulTF = true;
            ep.resultMessage = "Email sent successfully.";

            Console.WriteLine("=== createEmail method completed successfully ===");
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
            Console.WriteLine($"=== STARTING sendAdminEmailNewSubcontractor ===");
            Console.WriteLine($"Input Parameters - UserID: {UserID}, SubcontractorID: {SubcontractorID}");

            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);
            if (userProfileRecord == null)
            {
                Console.WriteLine($"ERROR: User profile not found for UserID: {UserID}");
                return;
            }
            else
            {
                Console.WriteLine($"Found user: {userProfileRecord.UserName} - {userProfileRecord.FirstName} {userProfileRecord.LastName}");
            }

            Console.WriteLine($"Looking up SubContractor for ID: {SubcontractorID}");
            SubcontractorsVw subcontractorRecord = _context.SubcontractorsVws.FirstOrDefault(sc => sc.SubcontractorId == SubcontractorID);

            if (subcontractorRecord == null)
            {
                Console.WriteLine($"ERROR: SubContractor not found for ID: {SubcontractorID}");
                return;
            }
            else
            {
                Console.WriteLine($"Found SC: {subcontractorRecord.SubcontractorName}");
            }

            Console.WriteLine($"Getting admin email list...");
            string emailList = listOfAdminsToNotify();
            Console.WriteLine($"Admin email list: {emailList}");

            if (string.IsNullOrEmpty(emailList))
            {
                Console.WriteLine($"WARNING: No admin emails found!");
                return;
            }

            string templateHTML = "AdminNewSC";
            Console.WriteLine($"Using email template: {templateHTML}");

            Dictionary<string, string> keyPairs = new Dictionary<string, string>();
            keyPairs.Add("{{SubcontractorName}}", subcontractorRecord.SubcontractorName);
            keyPairs.Add("{{UserName}}", userProfileRecord.UserName);

            Console.WriteLine($"Email placeholders:");
            foreach (var kvp in keyPairs)
            {
                Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
            }

            Console.WriteLine($"Calling generateEmails...");
            Console.WriteLine($"  Template: {templateHTML}");
            Console.WriteLine($"  Recipients: {emailList}");
            Console.WriteLine($"  Subject: New General Contractor Added");
            Console.WriteLine($"  From User: {userProfileRecord.UserName}");

            string result = generateEmails(templateHTML, keyPairs, emailList, "New Subcontractor Added", userProfileRecord.UserName);

            Console.WriteLine($"generateEmails result: {result}");
            Console.WriteLine($"=== COMPLETED sendAdminEmailNewSubcontractor ===");
        }

        public void sendAdminEmailNewGeneralContractor(string UserID, int GeneralContractorID)
        {
            Console.WriteLine($"=== STARTING sendAdminEmailNewGeneralContractor ===");
            Console.WriteLine($"Input Parameters - UserID: {UserID}, GeneralContractorID: {GeneralContractorID}");

            UserProfilesVw userProfileRecord = _context.UserProfilesVws.FirstOrDefault(up => up.UserId == UserID);

            if (userProfileRecord == null)
            {
                Console.WriteLine($"ERROR: User profile not found for UserID: {UserID}");
                return;
            }
            else
            {
                Console.WriteLine($"Found user: {userProfileRecord.UserName} - {userProfileRecord.FirstName} {userProfileRecord.LastName}");
            }

            Console.WriteLine($"Looking up General Contractor for ID: {GeneralContractorID}");
            GeneralContractorsVw generalContractorsRecord = _context.GeneralContractorsVws.FirstOrDefault(gc => gc.GeneralContractorId == GeneralContractorID);

            if (generalContractorsRecord == null)
            {
                Console.WriteLine($"ERROR: General Contractor not found for ID: {GeneralContractorID}");
                return;
            }
            else
            {
                Console.WriteLine($"Found GC: {generalContractorsRecord.GeneralContractorName}");
            }

            Console.WriteLine($"Getting admin email list...");
            string emailList = listOfAdminsToNotify();
            Console.WriteLine($"Admin email list: {emailList}");

            if (string.IsNullOrEmpty(emailList))
            {
                Console.WriteLine($"WARNING: No admin emails found!");
                return;
            }

            string templateHTML = "AdminNewGC";
            Console.WriteLine($"Using email template: {templateHTML}");

            Dictionary<string, string> keyPairs = new Dictionary<string, string>();
            keyPairs.Add("{{GeneralContractorName}}", generalContractorsRecord.GeneralContractorName);
            keyPairs.Add("{{UserName}}", userProfileRecord.UserName);

            Console.WriteLine($"Email placeholders:");
            foreach (var kvp in keyPairs)
            {
                Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
            }

            Console.WriteLine($"Calling generateEmails...");
            Console.WriteLine($"  Template: {templateHTML}");
            Console.WriteLine($"  Recipients: {emailList}");
            Console.WriteLine($"  Subject: New General Contractor Added");
            Console.WriteLine($"  From User: {userProfileRecord.UserName}");

            string result = generateEmails(templateHTML, keyPairs, emailList, "New General Contractor Added", userProfileRecord.UserName);

            Console.WriteLine($"generateEmails result: {result}");
            Console.WriteLine($"=== COMPLETED sendAdminEmailNewGeneralContractor ===");
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

            if (generalContractorRecord.DommainName != null)
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



