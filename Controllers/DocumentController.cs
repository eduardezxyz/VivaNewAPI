using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NewVivaApi.Data;
using NewVivaApi.Models;
// using NewVivaApi.Services;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using System.Security.Claims;
using System.IO;
using System.Configuration;
using Microsoft.Extensions.Logging;


namespace NewVivaApi.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        //private readonly EmailService _emailService;
        private readonly ILogger<DocumentController> _logger;


        public DocumentController(AppDbContext context, ILogger<DocumentController> logger, IConfiguration configuration) // EmailService emailService
        {
            _context = context;
            _configuration = configuration;
            //_emailService = emailService;
            _logger = logger;
        }

        [HttpPost("Upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(int documentType, int keyID, List<IFormFile> files)
        {
            // if (User.Identity?.IsServiceUser() == true)
            // {
            //     return BadRequest();
            // }

            // Check if we have a proper form with files
            if (!Request.HasFormContentType)
            {
                return BadRequest("Request must have multipart/form-data content type");
            }

            Console.WriteLine($"DocumentType: {documentType}, KeyID: {keyID}");

            // Use parameter files first, then fallback to Request.Form.Files
            var uploadFiles = files?.Any() == true ? files : Request.Form.Files.ToList();

            if (!uploadFiles.Any())
            {
                return BadRequest("No files uploaded");
            }
            Console.WriteLine($"Files count: {uploadFiles.Count}");

            foreach (var postedFile in uploadFiles)
            {
                if (postedFile.Length == 0)
                    continue;

                string newFileName = Guid.NewGuid().ToString();
                var fileExtension = Path.GetExtension(postedFile.FileName);
                if (!string.IsNullOrEmpty(fileExtension))
                {
                    newFileName = newFileName + fileExtension;
                }

                Console.WriteLine($"Processing file: {postedFile.FileName}, New File Name: {newFileName}");

                // Upload document to Cloud
                var bucketName = _configuration["S3:BucketName"];
                var filePath = _configuration["S3:FilePath"];

                Console.WriteLine($"Configuration values - BucketName: '{bucketName}', FilePath: '{filePath}'");

                _logger.LogInformation("Configuration values - BucketName: '{BucketName}', FilePath: '{FilePath}'",
                    bucketName ?? "NULL", filePath ?? "NULL");

                if (string.IsNullOrEmpty(bucketName))
                {
                    return BadRequest("S3 BucketName configuration is missing or empty");
                }

                // Upload document to Cloud
                var upObj = new S3UploadRequest
                {
                    BucketName = bucketName,
                    FilePath = filePath ?? "",
                    Key = newFileName,
                    UploadedFile = postedFile,
                };

                Console.WriteLine($"Uploading to S3: Bucket: {upObj.BucketName}, Key: {upObj.Key}");
                _logger.LogInformation("Uploading file to S3: Bucket: {BucketName}, Key: {Key}",
                    upObj.BucketName, upObj.Key);

                try
                {
                    var accessKey = _configuration["AWS:AccessKey"];
                    var secretKey = _configuration["AWS:SecretAccessKey"];

                    if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                    {
                        return BadRequest("AWS credentials are missing from configuration");
                    }

                    _logger.LogInformation("About to create S3Upload with credentials...");
                    var s3Uploader = new S3Upload(upObj, accessKey, secretKey);
                    _logger.LogInformation("S3Upload created successfully, about to upload...");
                    await s3Uploader.Upload();
                    Console.WriteLine("File uploaded successfully to S3.");
                }
                catch (Exception e)
                {
                    var innerMessage = e.InnerException?.Message ?? "No inner exception";
                    return BadRequest($"Error Uploading to S3. Message: {e.Message} - InnerError: {innerMessage}");
                }

                // Upload document to DB
                var uploadedDoc = new Document
                {
                    Bucket = upObj.BucketName,
                    Path = upObj.FilePath,
                    FileName = upObj.Key,
                    DownloadFileName = postedFile.FileName,
                    DocumentTypeId = documentType,
                    CreateByUser = User.Identity?.Name ?? "Unknown",
                    CreateDt = DateTimeOffset.UtcNow
                };

                Console.WriteLine($"Bucket: {uploadedDoc.Bucket}, Path: {uploadedDoc.Path}, FileName: {uploadedDoc.FileName}, DownloadFileName: {uploadedDoc.DownloadFileName}");
                Console.WriteLine($"DocumentTypeId: {uploadedDoc.DocumentTypeId}, Created By: {uploadedDoc.CreateByUser}, Created At: {uploadedDoc.CreateDt}");

                switch (documentType)
                {
                    case 1: // Payment Documents
                        uploadedDoc.PayAppId = keyID;
                        break;
                    case 2: // Lien Documents
                        uploadedDoc.PayAppId = keyID;
                        break;
                    case 3: // Sign Up Forms
                        var subproj = await _context.SubcontractorProjectsVws
                            .FirstOrDefaultAsync(p => p.SubcontractorProjectId == keyID);
                        
                        if (subproj == null)
                        {
                            return BadRequest($"Invalid Subcontractor Project ID: {keyID}. Please verify the project exists and try again.");
                        }
                        
                        // Verify the subcontractor exists
                        var subcontractorExists = await _context.Subcontractors
                            .AnyAsync(s => s.SubcontractorId == subproj.SubcontractorId);
                        
                        if (!subcontractorExists)
                        {
                            return BadRequest($"Data integrity error: Subcontractor (ID: {subproj.SubcontractorId}) associated with this project no longer exists. Please contact support.");
                        }
                        
                        uploadedDoc.SubcontractorProjectId = keyID;
                        uploadedDoc.SubcontractorId = subproj.SubcontractorId;
                        break;
                    case 4: // W9 Documents
                        // Verify the subcontractor exists
                        var w9SubcontractorExists = await _context.Subcontractors
                            .AnyAsync(s => s.SubcontractorId == keyID);
                        
                        if (!w9SubcontractorExists)
                        {
                            return BadRequest($"Invalid Subcontractor ID: {keyID}. Please verify the subcontractor exists and try again.");
                        }
                        
                        uploadedDoc.SubcontractorId = keyID;
                        break;
                    case 5: // Upload Report Documents
                        // Verify the general contractor exists
                        var gcExists = await _context.GeneralContractors
                            .AnyAsync(gc => gc.GeneralContractorId == keyID);
                        
                        if (!gcExists)
                        {
                            return BadRequest($"Invalid General Contractor ID: {keyID}. Please verify the contractor exists and try again.");
                        }
                        
                        uploadedDoc.GeneralContractorId = keyID;
                        break;
                    case 6: // Sample Forms
                        // Verify the subcontractor exists
                        var sampleSubcontractorExists = await _context.Subcontractors
                            .AnyAsync(s => s.SubcontractorId == keyID);
                        
                        if (!sampleSubcontractorExists)
                        {
                            return BadRequest($"Invalid Subcontractor ID: {keyID}. Please verify the subcontractor exists and try again.");
                        }
                        
                        uploadedDoc.SubcontractorId = keyID;
                        break;
                    default:
                        return BadRequest($"Invalid document type: {documentType}. Supported types are 1-6.");
                }

                try
                {
                    _context.Documents.Add(uploadedDoc);
                    await _context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    var innerMessage = e.InnerException?.Message ?? "No inner exception";
                    return BadRequest($"Error Saving Document to Database. Message: {e.Message} - InnerError: {innerMessage}");
                }

                var nd = await _context.DocumentsVws
                    .FirstOrDefaultAsync(d => d.DocumentId == uploadedDoc.DocumentId);


                //Email service
                // if (nd != null)
                // {
                //     var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                //     if (nd.GeneralContractorId != null && userId != null)
                //         await _emailService.SendGCEmailReportsAvailable(userId, nd.GeneralContractorId.Value);

                //     if (documentType == 2 && nd.PayAppId != null && userId != null)
                //     {
                //         await _emailService.SendLienReleaseSubmittedEmail(userId, nd.PayAppId.Value);
                //     }
                //     else if (documentType == 3 && userId != null)
                //     {
                //         await _emailService.SendSCNewSignupForm(userId, uploadedDoc.DocumentId);
                //     }
                // }
            }

            return Ok();
        }

        [HttpGet("GetDocument")]
        public IActionResult GetDocument(string key)
        {
            // if (User.Identity?.IsServiceUser() == true)
            // {
            //     return BadRequest();
            // }
            var accessKey = _configuration["AWS:AccessKey"];
            var secretKey = _configuration["AWS:SecretAccessKey"];

            using var client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.USWest2);
            string urlString = "";

            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _configuration["S3:BucketName"],
                    Key = key,
                    Expires = DateTime.UtcNow.AddMinutes(5)
                };
                urlString = client.GetPreSignedURL(request);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok(urlString);
        }
    }
}