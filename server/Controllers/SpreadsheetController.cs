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
                {
                    // Get the file ID for the requested file name
                    var errorResponse = new ErrorResponse 
                    { 
                        Message = "File not found in Google Drive.",
                        Error = "NotFound"
                    };
                    return NotFound(errorResponse);
                }
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
                var errorResponse = new ErrorResponse
                {
                    Message = "Error occurred while processing the file.",
                    Error = ex.Message
                };
                return BadRequest(errorResponse);
            }
        }

        // Class to store FileOptions
        public class FileOptions
        {
            public string FileName { get; set; } = string.Empty;
            public string Extension { get; set; } = string.Empty;
        }

        // Error response model
        public class ErrorResponse
        {
            public string Message { get; set; }
            public string Error { get; set; }
        }

        [HttpPost]
        [Route("SaveExcelToGoogleDrive")]
        public async Task<IActionResult> SaveExcelToGoogleDrive([FromForm] SaveSettings saveSettings)
        {
            try
            {
                // Generate Excel stream from Syncfusion JSON
                Stream generatedStream = Workbook.Save<Stream>(saveSettings);

                using MemoryStream excelStream = new MemoryStream();
                await generatedStream.CopyToAsync(excelStream);
                excelStream.Position = 0;

                // Prepare file name
                string fileName = saveSettings.FileName + "." + saveSettings.SaveType.ToString().ToLower();

                // Validate credential file
                if (!System.IO.File.Exists(credentialPath))
                    throw new FileNotFoundException($"Service account key file not found at {credentialPath}");

                // Authenticate Google Drive
                GoogleCredential credential;
                using (var streamKey = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(streamKey)
                        .CreateScoped(DriveService.Scope.Drive);
                }

                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = applicationName,
                });

                // Determine MIME type
                string mimeType = saveSettings.SaveType switch
                {
                    SaveType.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    SaveType.Xls => "application/vnd.ms-excel",
                    SaveType.Csv => "text/csv",
                    _ => "application/octet-stream"
                };

                // Check if file exists in the specific folder
                var listRequest = service.Files.List();
                listRequest.Q = $"name='{fileName}' and '{folderId}' in parents and trashed=false";
                listRequest.Fields = "files(id, name)";
                var files = await listRequest.ExecuteAsync();

                if (files.Files.Any())
                {
                    // Update existing file (DO NOT set Parents here)
                    var fileId = files.Files[0].Id;

                    excelStream.Position = 0;

                    var updateRequest = service.Files.Update(
                        new Google.Apis.Drive.v3.Data.File(),
                        fileId,
                        excelStream,
                        mimeType
                    );

                    updateRequest.Fields = "id";
                    await updateRequest.UploadAsync();

                    return Ok("File updated successfully in Google Drive.");
                }
                else
                {
                    // Create new file in folder
                    var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = fileName,
                        Parents = new List<string> { folderId }
                    };

                    excelStream.Position = 0;

                    var createRequest = service.Files.Create(fileMetadata, excelStream, mimeType);
                    createRequest.Fields = "id";
                    await createRequest.UploadAsync();

                    return Ok("File created successfully in Google Drive.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "Error saving file to Google Drive",
                    Error = ex.Message
                });
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
