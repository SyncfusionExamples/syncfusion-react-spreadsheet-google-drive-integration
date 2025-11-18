using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Syncfusion.EJ2.Spreadsheet;
using Syncfusion.XlsIO;
using System.IO;
using System.Net;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpreadsheetController : ControllerBase
    {
        public readonly string folderId;
        public readonly string applicationName;
        public readonly string credentialPath;
        private static readonly string[] Scopes = { DriveService.Scope.DriveFile, DriveService.Scope.DriveReadonly };

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
                MemoryStream stream = new MemoryStream();
                UserCredential credential;

                // Authenticate using OAuth 2.0
                using (var stream1 = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
                {
                    string credPath = "token.json";
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream1).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                }

                // Create Google Drive API service
                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = applicationName,
                });

                // List Excel files in Google Drive folder
                var listRequest = service.Files.List();
                listRequest.Q = $"mimeType='application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' and '{folderId}' in parents and trashed=false";
                listRequest.Fields = "files(id, name)";
                var files = await listRequest.ExecuteAsync();

                // Find the requested file
                string fileIdToDownload = files.Files.FirstOrDefault(f => f.Name == options.FileName + options.Extension)?.Id;
                if (string.IsNullOrEmpty(fileIdToDownload))
                    return NotFound("File not found in Google Drive.");

                // Download the file
                var request = service.Files.Get(fileIdToDownload);
                await request.DownloadAsync(stream);
                stream.Position = 0;

                // Prepare file for Syncfusion Excel processing
                OpenRequest open = new OpenRequest
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

        // Strongly typed model for file details
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
                // Convert spreadsheet JSON to Excel stream using Syncfusion
                Stream fileStream = Workbook.Save<Stream>(saveSettings);
                fileStream.Position = 0; // Reset stream position

                // Define filename for Google Drive
                string fileName = saveSettings.FileName + "." + saveSettings.SaveType.ToString().ToLower();

                // Authenticate using OAuth 2.0
                UserCredential credential;
                using (var memStream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
                {
                    string credPath = "token.json";
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(memStream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                }

                // Create Google Drive API service
                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = applicationName,
                });

                // Prepare file metadata
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = fileName,
                    Parents = new List<string> { folderId }
                };

                // Upload Excel file to Google Drive
                FilesResource.CreateMediaUpload request;
                request = service.Files.Create(fileMetadata, fileStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                request.Fields = "id";

                await request.UploadAsync();

                return Ok("Excel file successfully saved to Google Drive.");
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
        }

        [HttpPost]
        [Route("Save")]
        public IActionResult Save([FromForm] SaveSettings saveSettings)
        {
            return Workbook.Save(saveSettings);
        }
    }
}
