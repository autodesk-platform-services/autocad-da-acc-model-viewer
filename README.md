# Generating Collaboration Files in AutoCAD with Design Automation for ACC Viewing

### Using Design Automation for AutoCAD
Design Automation for AutoCAD allows you to generate a collaboration file containing various viewable assets. This file, recognizable by any LMV-based application, can be hosted on Autodesk Construction Cloud for model viewing.

The Model Derivative API translates over 60 file formats into derivatives (output files), including the collaboration format. While the AutoCAD API directly generates collaboration files from 3D drawings, the Design Automation service's API offers greater flexibility. It allows you to not only generate these files but also manipulate the properties of the 3D model within them.

This project focuses on removing generic properties associated with the 3D model. We retain only basic model objects with identifying properties like Name and `Handle` ID. This is achieved by parsing [filter.json](https://git.autodesk.com/moogalm/autocad-da-acc-model-viewer/blob/main/CLBPlugin/filter.json), which contains a list of properties to be extracted.

`Handles` are unique identifiers within a single AutoCAD DWG database. They are 64-bit integers introduced before AutoCAD R13 and persist across sessions. However, handles are not unique across different databases. Since all databases start with the same initial handle value, duplication is almost guaranteed.

Refer [Supported Translation](https://aps.autodesk.com/en/docs/model-derivative/v2/developers_guide/supported-translations/) document.
| COLLABORATION | SVF<br>SVF2<br>Thumbnail |
| :------------- | :------------------------------ |



## Workflow Design

![aps-acc-da-flow](https://media.git.autodesk.com/user/3836/files/f624019d-5184-4ef9-9fd1-f4b0d603a680)

## Workflow Summary

**Objective**: Extract data from a solid entity in an AutoCAD drawing, remove built-in properties, and add custom properties.

1. **Setup**
   
   - Initialize necessary variables and constants.
   - Register a custom application name ("CARBON_NEGATIVE").

2. **Find Solid Entity**
   
   - Open the current document.
   - Retrieve the solid entity by its handle ("14A37").
   - If the entity is not found, log a message and exit.

3. **Transaction Management**
   
   - Start a transaction to modify the drawing database.
   - Open the block table and block table record.
   - Check and register the application in the `RegAppTable`.

4. **Add Custom Properties**
   
   - Check if the solid entity already has custom properties.
   - If not, add custom XData properties to the solid entity.

5. **Load and Hook Events**
   
   - Load `LMVExport.crx` for exporting.
   - Hook the `EndExtraction` event.

6. **Prepare for Extraction**
   
   - Set the current working directory.
   - Determine the output collaboration file path.

7. **Run Export Command**
   
   - Check if running in Design Automation environment.
   - Load the `filter.json` file for property extraction.
   - Run the `LMVEXPORT` command to extract properties based on the filter.

8. **Create Collaboration Package**
   
   - Load `AcShareViewPropsCore.dll`.
   - Create a collaboration package using `_CREATESIMPLESHAREPACKAGE`.

9. **Unhook Events**
   
   - Unhook the `EndExtraction` event.

10. **Handle Extraction End Event**
    
    - Log the end of the extraction process.
    - Start a new transaction to read the entity.
    - Add additional custom properties to the solid entity.

#### Example JSON Filter

```json
{
  "AcDbObject": [
    null,
    "Handle"
  ]
}

```

This JSON filter specifies that only the `Handle` property should be extracted from the solid entity.


## Workflow Demo



https://media.git.autodesk.com/user/3836/files/c6af784c-ddec-47e8-93f0-cca1cc96c30f



## How to Generate a `.collaboration` File from local AcCoreConsole instance.

### Setup

Change directory to where the drawing file is located

```bash
mkdir Collaboration
cd Collaboration
touch filter.json
```

```bash
D:\LMV
│ - House.dwg
└───Collaboration
    - filter.json
```

- [House.dwg](https://git.autodesk.com/moogalm/autocad-da-acc-model-viewer/blob/main/CLBPlugin/filter.json)
- [filter.json](https://git.autodesk.com/moogalm/autocad-da-acc-model-viewer/blob/main/CLBPlugin/House.dwg)

### Process

Steps to produce collaboration file, `DWGEXTRACTOR` will produce a output directory relative to the `index.json`

```bash
cd "AutoCAD 2025"
accoreconsole.exe /i input.dwg
_LMVEXPORT
D:\LMV\Collaboration\filter.json
_NETLOAD
AcShareViewPropsCore.dll
_CREATESIMPLESHAREPACKAGE
D:\LMV\Collaboration
D:\LMV\Collaboration\House.collaboration
```

- `CREATESIMPLESHAREPACKAGE` is a thin AutoCAD command wrapper for the Simple Share Data Wrapper API.
  - Which expects a input folder, which is `output` folder created by `LMVEXPORT` command.
  - Which expects a output file, which is `collaboration` file.
- `filter.json`  a config file to tell `LMVEXPORT` what properties to extract from the seed drawing file.

### Upload

The `.collaboration` file can be uploaded to Viewer or ACC

- NOTE: Alternatively, you can execute [runlocal.bat](https://git.autodesk.com/moogalm/autocad-da-acc-model-viewer/blob/main/CLBPlugin/runlocal.bat)

### Web App Setup

## Prerequisites

1. **APS Account**: Learn how to create a APS Account, activate your subscription, and create an app at [this tutorial](http://aps.autodesk.com/tutorials/#/account/).
2. Add a callback url : `http://localhost:8080/api/auth/callback`
3. **ACC Account**: must be Account Admin to add the app integration. [Learn about provisioning](https://aps.autodesk.com/en/docs/bim360/v1/tutorials/getting-started/manage-access-to-docs/).
4. **Visual Studio**: Either Community 2022 (Windows) or Code (Windows, MacOS).
5. **.NET 8.0** basic knowledge with C#
6. **JavaScript** basic knowledge.

## Running locally

Clone this project or download it. It's recommended to install [GitHub Desktop](https://desktop.github.com/). To clone it via command line, use the following (**Terminal** on MacOSX/Linux, **Windows Terminal** on Windows):

### CLI

Steps to followed in VS 2022 Developer Command Prompt

```bash
git clone https://git.autodesk.com/moogalm/autocad-da-acc-model-viewer.git
cd autocad-da-acc-model-viewer
touch appsettings.user.json
```

- Open `appsettings.user.json` in any text editor and following JSON with your APS Credentials.

```json
{
  "APS_CLIENT_ID": "",
  "APS_CLIENT_SECRET": "",
  "APS_CALLBACK_URL": "http://localhost:8080/api/auth/callback",
  "Forge": {
    "ClientId": "",
    "ClientSecret": ""
  }
}
```

```bash
dotnet restore
dotnet build
dotnet watch --project aps-acc-da
```

#### Sample Build Output

```bash
- execute  `dotnet restore && dotnet build && dotnet watch --project aps-acc-da`

  Determining projects to restore...
  All projects are up-to-date for restore.
  Determining projects to restore...
  All projects are up-to-date for restore.
  aps-acc-da -> D:\Work\Projects\2024\autocad-da-acc-model-viewer\aps-acc-da\bin\Debug\net8.0\aps-acc-da.dll
  LMVExtractor -> D:\Work\Projects\2024\autocad-da-acc-model-viewer\CLBPlugin\bin\Debug\net8.0-windows\LMVExtractor.dll
  Zipping directory "D:\Work\Projects\2024\autocad-da-acc-model-viewer\CLBPlugin\Bundle" to "D:\Work\Projects\2024\autocad-da-acc-model-viewer\aps-acc-da\App_Data\LMVExtractor.bundle.zip".
  -rw-rw-r--  0 0      0         899 Jul 09 02:16 LMVExtractor.bundle/PackageContents.xml
  -rw-rw-r--  0 0      0         915 Jul 09 02:16 LMVExtractor.bundle/Contents/filter.json
  -rw-rw-r--  0 0      0        1788 Jul 09 15:41 LMVExtractor.bundle/Contents/LMVExtractor.deps.json
  -rw-rw-r--  0 0      0        8704 Jul 09 15:41 LMVExtractor.bundle/Contents/LMVExtractor.dll
  -rw-rw-r--  0 0      0       12028 Jul 09 15:41 LMVExtractor.bundle/Contents/LMVExtractor.pdb

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:00.75
dotnet watch 🔥 Hot reload enabled. For a list of supported edits, see https://aka.ms/dotnet/hot-reload.
  💡 Press "Ctrl + R" to restart.
dotnet watch 🔧 Building...
  Determining projects to restore...
  All projects are up-to-date for restore.
  aps-acc-da -> D:\Work\Projects\2024\autocad-da-acc-model-viewer\aps-acc-da\bin\Debug\net8.0\aps-acc-da.dll
dotnet watch 🚀 Started
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:8080
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: D:\Work\Projects\2024\autocad-da-acc-model-viewer\aps-acc-da
```

### Visual Studio

- Download the project or clone from [Github](https://git.autodesk.com/moogalm/autocad-da-acc-model-viewer)

- Open `autocad-da-acc-model-viewer.sln` in Visual Studio
  
  **Open Your Project**:
  
  - Open your project in Visual Studio.

- **Add `appsettings.user.json` File**:
  
  - Right-click on your project in the Solution Explorer.
  - Select **Add** > **New Item**.
  <img width="692" alt="appsettings user" src="https://media.git.autodesk.com/user/3836/files/6af3d6df-3593-4209-8597-b5a5a9d6df85">


- **Choose JSON File Template**:
  
  - In the dialog that appears, select **JSON File**.
  - Name the file `appsettings.user.json`.
  - Click **Add**.

- **Edit `appsettings.user.json` File**:
  
  - Once added, the `appsettings.user.json` file will appear in your Solution Explorer.
  - Double-click the file to open it in the editor.
  - Add your configuration settings in JSON format with APS credentials.
  - Here is an example configuration

```json
{
  "APS_CLIENT_ID": "",
  "APS_CLIENT_SECRET": "",
  "APS_CALLBACK_URL": "http://localhost:8080/api/auth/callback",
  "Forge": {
    "ClientId": "",
    "ClientSecret": ""
  }
}
```

- Make sure the `appsettings.user.json` file is set to **Copy to Output Directory** if needed. Right-click the file, select **Properties**, and set **Copy to Output Directory** to **Copy if newer**.

- **Modify `Startup.cs` to Load Configuration**:

- Ensure your `Startup.cs` file is configured to load the `appsettings.user.json` file

- `appsettings.user.json` serves two purposes
  
  - You can inject the `IConfiguration` service to access the settings in your `appsettings.user.json` file to any `Controller` class.
  
  - `Forge` section in the `appsettings.user.json` file is injected by the APS Design Automation SDK to facilitate DA API requests.

- **Setup Project Profile:**

- Right-click on your project in the Solution Explorer and select "Properties" from the context menu.

- Alternatively, you can select the project and press `Alt+Enter`.

- In the properties window, go to the "Debug" tab. This tab allows you to configure different settings for debugging and running your project.

- In the "Launch" section, you will see different profiles such as "Project", "IIS Express", etc.
  
  - Select the profile you want to configure from the drop-down menu or click on "Create new profile" to set up a custom profile.

#### Build Project

Once the NuGet packages are restored, you can build the project:

1. **Build the Solution:**
   
   - **Using Solution Explorer:**
     - Right-click on the solution or the specific project in Solution Explorer.
     - Select `Build` or `Rebuild`.
   - **Using the Menu:**
     - Go to `Build` on the top menu.
     - Select `Build Solution` (shortcut: `Ctrl+Shift+B`).
     - Alternatively, select `Rebuild Solution` to clean and build the solution from scratch.

2. **Check the Output Window:**
   
   - After starting the build process, you can monitor the progress and see any errors or warnings in the Output window.
   - Go to `View` > `Output` or press `Ctrl+Alt+O` to open the Output window.

#### Troubleshooting Common Issues

- **Missing NuGet Packages:**
  - Ensure that the NuGet package sources are correctly configured.
  - Go to `Tools` > `NuGet Package Manager` > `Package Manager Settings` and check the `Package Sources`.
- **Build Errors:**
  - Review the errors in the Error List window (`View` > `Error List` or `Ctrl+\\` + `Ctrl+E`).
  - Address any missing references, incorrect configurations, or syntax errors as indicated.

#### Additional Tips

- **Clean Solution:**
  
  - If you encounter build issues, try cleaning the solution before rebuilding.
  - Go to `Build` > `Clean Solution`.

- **Project Dependencies:**
  
  - Ensure that project dependencies are correctly configured in Solution Explorer.
  - Right-click the solution, select `Project Dependencies`, and verify the dependencies.

- **Rebuild All Projects:**
  
  - If you have multiple projects, you can rebuild all of them by selecting `Rebuild Solution` instead of building individual projects.

### To Run Plugin Locally

```bash
git clone https://git.autodesk.com/moogalm/autocad-da-acc-model-viewer.git
cd autocad-da-acc-model-viewer
cd CLBPlugin
dotnet build
runlocal.bat
```

https://media.git.autodesk.com/user/3836/files/0c1cb5b4-b2d6-42c2-84fd-9e3c1870c79d



## Use Case

- **Open the Browser:**
  
  - Navigate to [http://localhost:8080](http://localhost:8080) in your web browser.

- **Login:**
  
  - Click the `Login` button.
  - Follow the subsequent steps in the authentication flow.

- **Access the Hubs Browser:**
  
  - In the sidebar, a Tree panel will appear, displaying the hubs browser.

- **Select a Folder:**
  
  - To traverse through the projects, select any folder by clicking on the `+` buttons to expand the tree items.

- **Submit for Design Automation:**
  
  - Click the `submit` button. This action initiates a Design Automation process by submitting a workitem with details such as hub, project, and folder data. This process allows for the upload of a collaboration file created by executing an activity designed to run AutoCAD core commands.

- **Notification:**
  
  - A notification bubble will appear on the selected folder, indicating that an item has been created.

- **View and Copy Logs:**
  
  - You can click the `copy` button to copy the log.
  - Scroll down to view the Design Automation workitem log.

## License

[MIT License](https://git.autodesk.com/moogalm/autocad-da-acc-model-viewer/blob/main/LICENSE)

## Written By

Madhukar Moogala
