import { createRoot } from 'react-dom/client';
import './index.css';
import * as React from 'react';
import { useState } from 'react';
import { DropDownListComponent } from '@syncfusion/ej2-react-dropdowns';
import { SpreadsheetComponent } from '@syncfusion/ej2-react-spreadsheet';

/**
 * Default Spreadsheet sample
 */
function Default() {
    let spreadsheet;
    const fileList = [
        { name: 'Car Sales Report', extension: '.xlsx' },
        { name: 'Shopping Details', extension: '.xls' },
        { name: 'Price Details', extension: '.csv' }
    ];

    const fields = { text: 'name' };

    const [fileInfo, setFileInfo] = useState(fileList[0]);
    const [loadedFileInfo, setLoadedFileInfo] = useState(null);
    const openFromGoogleDrive = () => {
        spreadsheet.showSpinner();
        // Make a POST request to the backend API to open the file from Google Drive.
        // Replace the URL with your local or hosted endpoint URL.
        fetch('https://localhost:your_port_number/api/spreadsheet/OpenExcelFromGoogleDrive', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                FileName: fileInfo.name,       // Name of the file to open
                Extension: fileInfo.extension, // File extension (.xlsx)
            }),
        })
            .then((response) => response.json()) // Parse the response as JSON
            .then((data) => {
                spreadsheet.hideSpinner();
                // Load the spreadsheet data into the UI
                spreadsheet.openFromJson({ file: data, triggerEvent: true });
            })
            .catch((error) => {
                spreadsheet.hideSpinner();
                window.alert('Error importing file from Google Drive: ' + error);
            });
    };

    // Save the current spreadsheet to Google Drive
    const saveToGoogleDrive = () => {
        // Convert spreadsheet data to JSON
        spreadsheet.saveAsJson().then((json) => {
            const formData = new FormData();
            // Append required fields for backend API
            formData.append('FileName', loadedFileInfo.fileName);   // File name
            formData.append('SaveType', loadedFileInfo.saveType);   // Format type (Xlsx, Xls, Csv)
            formData.append('JSONData', JSON.stringify(json.jsonObject.Workbook)); // Spreadsheet data
            formData.append('PdfLayoutSettings', JSON.stringify({ FitSheetOnOnePage: false })); // PDF settings
            // Make a POST request to the backend API to save the file to Google Drive.
            // Replace the URL with your local or hosted endpoint URL.
            fetch('https://localhost:your_port_number/api/spreadsheet/SaveExcelToGoogleDrive', {
                method: 'POST',
                body: formData,
            })
                .then((response) => {
                    if (!response.ok) throw new Error(`Save failed: ${response.status}`);
                    window.alert('Workbook saved successfully to Google Drive.');
                })
                .catch((error) => {
                    window.alert('Error saving to Google Drive: ' + error);
                });
        });
    };
    const onCreated = () => { };
    const beforeSave = (args) => {
        args.isFullPost = false;
        console.log(args);
    };
    const openComplete = (args) => {
        if (args.response.isOpenFromJson) {
            const saveTypes = { '.xlsx': 'Xlsx', '.xls': 'Xls', '.csv': 'Csv' };
            setLoadedFileInfo({
                fileName: fileInfo.name,
                saveType: saveTypes[fileInfo.extension],
            });
        }
    };
    const fileChangeHandler = (args) => {
        setFileInfo(args.itemData);
    };
    return (
        <div className="control-pane">
            <div className="control-section spreadsheet-control">
                <label for="filename-ddl">Select a file:</label>
                <DropDownListComponent
                    id="filename-ddl"
                    dataSource={fileList}
                    fields={fields}
                    index={0}
                    width={175}
                    change={fileChangeHandler}
                />
                <button
                    className="e-btn"
                    onClick={openFromGoogleDrive}
                    style={{ marginLeft: '10px' }}
                >
                    Open from Drive
                </button>
                <button
                    className="e-btn"
                    onClick={saveToGoogleDrive}
                    style={{ marginLeft: '10px' }}
                    disabled={loadedFileInfo == null}
                >
                    Save to Drive
                </button>
                <SpreadsheetComponent
                    openUrl="https://localhost:your_port_number/api/spreadsheet/Open"
                    saveUrl="https://localhost:your_port_number/api/spreadsheet/Save"
                    ref={(ssObj) => {
                        spreadsheet = ssObj;
                    }}
                    created={onCreated}
                    beforeSave={beforeSave}
                    openComplete={openComplete}
                ></SpreadsheetComponent>
            </div>
        </div>
    );
}
export default Default;

const root = createRoot(document.getElementById('sample'));
root.render(<Default />);
