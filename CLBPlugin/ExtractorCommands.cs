using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;


namespace TestLMVExtractor
{
    public class ExtractorCommands
    {
        private ObjectId solidId;      
        static readonly string RegAppName = "CARBON_NEGATIVE";
        /// <summary>
        ///  Function: Finds a solid entity by its handle, removes built-in properties, and adds custom properties.
        ///  Sample: This example works on a provided "House" drawing containing a solid entity with handle "14A37" and a registered application named "CarbonNegative."
        ///  Objective: The objective of this sample is to extract data from the solid entity and add custom properties to it in the form of XData.
        ///  Execution: The logic for this bundle is executed within a Design Automation pipeline.
        ///  Output: The extracted data is packaged into a collaboration file (.collaboration).
        /// </summary>
        [CommandMethod("EXTRACTDATA")]
        public void ExtractData()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if(doc == null) return;
            string assetName = doc.IsNamedDrawing ? Path.GetFileNameWithoutExtension(doc.Name) : "House";

            var db = doc.Database;
            var ed = doc.Editor;          
            long handle = Convert.ToInt64("14A37", 16);
            Handle h = new Handle(handle);



            //1. Fetch House wall solid from handle.

            if (!db.TryGetObjectId(h, out solidId)) {
                ed.WriteMessage($"\nEntity Not Found for given{h.Value}");
                return; 
            }

            using (Transaction t = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)t.GetObject(db.BlockTableId, OpenMode.ForRead, false);
                var btr = (BlockTableRecord)t.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false);
                var layoutId = btr.LayoutId;
                var ent = t.GetObject(solidId, OpenMode.ForWrite) as Solid3d;
                RegAppTable tbl = (RegAppTable)t.GetObject(db.RegAppTableId, OpenMode.ForWrite, false);
                if (!tbl.Has(RegAppName))
                {
                    RegAppTableRecord app = new RegAppTableRecord
                    {
                        Name = RegAppName
                    };
                    tbl.Add(app);
                    t.AddNewlyCreatedDBObject(app, true);
                }      
                if(ent.GetXDataForApplication(RegAppName) == null)
                {
                    ent.XData = new ResultBuffer(
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, @"Using 40% fly ash – fine glass powder made primarily of iron,
                                                                          silica, and alumina – helps cut embodied carbon in conventional bricks.")
                    );
                   
                }
                t.Commit();
            }

            // 2. Load LMVExport.crx
            ed.Command("_ARX", "_L", "ACLMVEXPORT");
            // 3. Hook up the events
            // Wraps the AcDwgExtractor::ExtractorReactor::endExtraction() ObjectARX function.
            Application.EndExtraction += Application_EndExtraction;          
            var folder = Directory.GetCurrentDirectory();           
            var collabFile = Path.Combine(folder, $"{assetName}.collaboration");

            //4. Call LMVEXPORT command to use PDF based Extractor and for F2D based extraction use DWGEXTRACTOR (legacy not recommended)
            //5. Feed the `filter.json`, which tells what properties to extract

            /*Example :
            
            {
            "AcDbObject": [
                null,
               "Handle"
              ]
            
            }
            AcDbObject have two properties Handle and Annotative
            Only Handle property is extracted from the solid entity            
            */

            //Check if the command is running in Design Automation
            var workItemId = Environment.GetEnvironmentVariable("DAS_WORKITEM_ID");
            if(workItemId != null)
            {
                var filter = HostApplicationServices.Current.FindFile("filter.json", db, FindFileHint.Default);
                ed.Command("_LMVEXPORT", folder, filter);
            }
            else
            {
                var filter = @"filter.json"; //Path to the filter.json file
                ed.Command("_LMVEXPORT", folder, Path.Combine(folder, filter));
            }
            //6. Call NETLOAD to load AcShareViewPropsCore.dll
            ed.Command("_NETLOAD", "AcShareViewPropsCore.dll");
            //7. Call _CREATESIMPLESHAREPACKAGE to create a *.collobration package
           
            ed.Command("_CREATESIMPLESHAREPACKAGE", folder, collabFile);
            //8. Unhook the events
            Application.EndExtraction -= Application_EndExtraction;
        }

    

        private void Application_EndExtraction(object sender, EndExtractionEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;
            ed.WriteMessage("---Extraction End----");
            using (Transaction t = db.TransactionManager.StartTransaction())
            {
                var ent = t.GetObject(solidId, OpenMode.ForRead);
                var xdata = ent.GetXDataForApplication(RegAppName);
                if (xdata == null) return;
                
                //add xdata property
                var data = xdata.AsArray();
                e.AddProperty(solidId, "CarbonNegative", "Reason", data[1].Value, "", false);

                //add some other sustainability properties
                e.AddProperty(solidId, "CarbonNegative", "BrickType", "CarbiCrete", "", false);
                e.AddProperty(solidId, "CarbonNegative", "Density (kg/m3)", "2250 CMU", "", false);
                e.AddProperty(solidId, "CarbonNegative", "Water Absorption(%)", "6.0", "", false);
                e.AddProperty(solidId, "CarbonNegative", "Compressive Strength (MPa)", "26", "", false);
                e.AddProperty(solidId, "CarbonNegative", "Moisture(%)", "1.5", "", false);
                e.AddProperty(solidId, "CarbonNegative", "Fire Rating(hrs)", "2", "", false);
                t.Commit();
            }

        }
       
    }
}


