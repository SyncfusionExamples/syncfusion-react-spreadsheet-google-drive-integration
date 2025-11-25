# syncfusion-react-spreadsheet-google-drive-integration
This repository contains a complete sample demonstrating how to integrate the Syncfusion React Spreadsheet component with an ASP.NET Core Web API connected to Google Drive. It showcases how to open and save Excel files using Google Drive’s API for a secure and seamless cloud-based spreadsheet experience

📁 **Project Structure**

```
├── client/       # React app with Syncfusion Spreadsheet
└── server/       # ASP.NET Core Web API project
```

✨ **Features**

- Open Excel files directly from Google Drive into Syncfusion Spreadsheet.
- Edit spreadsheet data in-browser.
- Save changes back to Google Drive with a single click.
- Dropdown list to select files from Google Drive.

🧩 **Technologies Used**

- React + Syncfusion Spreadsheet
- ASP.NET Core Web API
- Google Drive API for .NET
- Google Drive for cloud storage

🚀 **Getting Started**

1. **Clone the Repository**

   ```bash
   git clone https://github.com/SyncfusionExamples/syncfusion-react-spreadsheet-google-drive-integration
   ```

2. **Setup the Client**

   ```bash
   cd client
   npm install
   npm start
   ```

3. **Setup the Server**

   ```bash
   cd server
   # Open in Visual Studio or VS Code
   # Restore NuGet packages
   ```
   **Update the following in the Server:**

   To configure Google Drive Storage access, update the `appsettings.json` file with the following settings,
   "CredentialPath": "path-to-your-service-account-key.json", 
   "FolderId": "your-google-drive-folder-id", 
   "ApplicationName": "YourAppName" 

4. **Run the Server**
   
   Run the project and test the endpoints directly from the React app

   ```bash
   dotnet run
   ```

📌 **Notes**

- Update the fetch() URLs in the React sample to point to your local backend endpoints for `OpenExcelFromGoogleDrive` and `SaveExcelToGoogleDrive`.
- The React sample includes a dropdown list with three predefined Excel files:

   ```javascript
   const fileList = [
      { name: 'Car Sales Report', extension: '.xlsx' },
      { name: 'Shopping Details', extension: '.xls' },
      { name: 'Price Details', extension: '.csv' },
   ];
   ```
   
- **Tested files used in the dropdown can be found here**: [Tested Excel files](./client/public/Files/) 

- You can update this list to match the actual Excel files stored in your Google Drive storage.
- After selecting a file, click the "Open From Drive" button to load the selected Excel file into the Syncfusion Spreadsheet.
- Once you make edits, click the "Save to Drive" button to save the changes back to the same file in your Google Drive storage.

📄 **License and copyright**

> This is a commercial product and requires a paid license for possession or use. Syncfusion<sup>®</sup> licensed software, including this control, is subject to the terms and conditions of Syncfusion<sup>®</sup> [EULA](https://www.syncfusion.com/eula/es/). To acquire a license for 140+ [JavaScript UI controls](https://www.syncfusion.com/javascript-ui-controls), you can [purchase](https://www.syncfusion.com/sales/products) or [start a free 30-day trial](https://www.syncfusion.com/account/manage-trials/start-trials).

> A [free community license](https://www.syncfusion.com/products/communitylicense) is also available for companies and individuals whose organizations have less than $1 million USD in annual gross revenue and five or fewer developers.

See [LICENSE FILE](https://github.com/syncfusion/ej2-javascript-ui-controls/blob/master/license?utm_source=npm&utm_medium=listing&utm_campaign=javascript-spreadsheet-npm) for more info.

---

Feel free to fork, customize, and contribute to this project!