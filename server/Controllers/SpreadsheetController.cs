using Microsoft.Extensions.Caching.Memory;
using Syncfusion.XlsIO;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.Mvc;
using Syncfusion.EJ2.Spreadsheet;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpreadsheetController : ControllerBase
    {
        //variables for storing GDrive folderId, ApplicationName and Service-Accountkey credentials
        public readonly string folderId;
        public readonly string applicationName;
        public readonly string credentialPath;

        //constructor for assigning credentials
        public SpreadsheetController(IConfiguration configuration)
        {
            folderId = configuration.GetValue<string>("FolderId");
            credentialPath = configuration.GetValue<string>("CredentialPath");
            applicationName = configuration.GetValue<string>("ApplicationName");
        }

        [HttpPost]
        [Route("OpenExcelFromGoogleDrive")]
        public async Task<IActionResult> OpenExcelFromGoogleDrive([FromBody] FileOptions options)
        {
            try
            {
                // Create a memory stream to store file data
                MemoryStream stream = new MemoryStream();

                // Authenticate using Service Account
                GoogleCredential credential;
                // Load Google service account credentials
                using (var streamKey = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(streamKey)
                        .CreateScoped(DriveService.Scope.Drive);
                }

                // Create Google Drive API service
                var service = new DriveService(new BaseClientService.Initializer()
                // Initialize Google Drive API client
                {
                    HttpClientInitializer = credential,
                    ApplicationName = applicationName,
                });

                // List Excel files in Google Drive folder
                var listRequest = service.Files.List();
                // Query Google Drive for Excel, CSV files in the specified folder
                listRequest.Q = $"(mimeType='application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' or mimeType='application/vnd.ms-excel' or mimeType='text/csv') and '{folderId}' in parents and trashed=false";
                listRequest.Fields = "files(id, name)";
                var files = await listRequest.ExecuteAsync();
                // Find the requested file
                string fileIdToDownload = files.Files.FirstOrDefault(f => f.Name == options.FileName + options.Extension)?.Id;
                // Get the file ID for the requested file name
                if (string.IsNullOrEmpty(fileIdToDownload))
                    // Get the file ID for the requested file name
                    return NotFound("File not found in Google Drive.");
                // Download the file
                var request = service.Files.Get(fileIdToDownload);
                await request.DownloadAsync(stream);
                // Download file content into memory stream
                stream.Position = 0;
                // Prepare file for Syncfusion Excel processing
                OpenRequest open = new OpenRequest
                // Wrap downloaded stream as FormFile for Syncfusion processing
                {
                    File = new FormFile(stream, 0, stream.Length, options.FileName, options.FileName + options.Extension)
                };

                // Convert Excel file to JSON using Syncfusion XlsIO
                var result = Workbook.Open(open);
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest("Error occurred while processing the file: " + ex.Message);
            }
        }

        // Class to store FileOptions
        public class FileOptions
        {
            public string FileName { get; set; } = string.Empty;
            public string Extension { get; set; } = string.Empty;
        }

        [HttpPost]
        [Route("SaveExcelToGoogleDrive")]
        public async Task<IActionResult> SaveExcelToGoogleDrive([FromForm] SaveSettings saveSettings)
        {
            try
            {
                 //Generate Excel file stream using Syncfusion
                Stream generatedStream = Workbook.Save<Stream>(saveSettings);
                //Copy to MemoryStream to ensure full content is flushed and seekable
                MemoryStream excelStream = new MemoryStream();
                // Copy generated stream to MemoryStream for upload
                await generatedStream.CopyToAsync(excelStream);
                excelStream.Position = 0; // Reset position for upload

                // Prepare file name with extension based on SaveType
                string fileName = saveSettings.FileName + "." + saveSettings.SaveType.ToString().ToLower();

                // Validate service account credential file
                if (!System.IO.File.Exists(credentialPath))
                    throw new FileNotFoundException($"Service account key file not found at {credentialPath}");

                //Authenticate using Service Account credentials
                GoogleCredential credential;
                // Load Google service account credentials
                using (var streamKey = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(streamKey)
                        .CreateScoped(DriveService.Scope.Drive);
                }

                //Initialize Google Drive API service
                var service = new DriveService(new BaseClientService.Initializer()
                // Initialize Google Drive API client
                {
                    HttpClientInitializer = credential,
                    ApplicationName = applicationName,
                });

                //Prepare file metadata
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = fileName
                };

                //Check if file already exists in the specified folder
                var listRequest = service.Files.List();
                listRequest.Q = $"name='{fileName}' and trashed=false";

                // Query Google Drive for Excel, CSV files in the specified folder
                listRequest.Fields = "files(id)";
                var files = await listRequest.ExecuteAsync();

                // Reset stream position before upload (important for both update and create)
                excelStream.Position = 0;

                // Set MIME type dynamically based on SaveType
                 string mimeType = saveSettings.SaveType switch
                 {
                    SaveType.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    SaveType.Xls => "application/vnd.ms-excel",
                    SaveType.Csv => "text/csv",
                 };

                if (files.Files.Any())
                {
                    // If File exists Update in the existing file
                    var updateRequest = service.Files.Update(fileMetadata, files.Files[0].Id, excelStream,
                        mimeType);
                    updateRequest.Fields = "id";
                    await updateRequest.UploadAsync();
                }
                else
                {
                    // If File does not exist, Create new file
                    var createRequest = service.Files.Create(fileMetadata, excelStream,mimeType);
                    createRequest.Fields = "id";
                    await createRequest.UploadAsync();
                }

                return Ok("Excel file successfully saved/updated in Google Drive.");
            }
            catch (Exception ex)
            {
                return BadRequest("Error saving file to Google Drive: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("Open")]
        public IActionResult Open([FromForm] IFormCollection openRequest)
        {
            OpenRequest open = new OpenRequest();
            // Wrap downloaded stream as FormFile for Syncfusion processing
            if (openRequest.Files.Count != 0)
            {
                open.File = openRequest.Files[0];
                if (openRequest.ContainsKey("IsManualCalculationEnabled") && bool.TryParse(openRequest["IsManualCalculationEnabled"].ToString(), out bool flag))
                {
                    open.IsManualCalculationEnabled = flag;
                }
            }
            open.Password = openRequest["Password"];
            if (openRequest["SheetIndex"].Count != 0)
            {
                open.SheetIndex = int.Parse(openRequest["SheetIndex"].ToString());
            }
            open.SheetPassword = openRequest["SheetPassword"];
            return Content(Workbook.Open(open));
            // Convert Excel file to JSON for rendering in Syncfusion Spreadsheet
        }

        [HttpPost]
        [Route("Save")]
        public IActionResult Save([FromForm] SaveSettings saveSettings)
        {
            return Workbook.Save(saveSettings);
        }
    }
}
