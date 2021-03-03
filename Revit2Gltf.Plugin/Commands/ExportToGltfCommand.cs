using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf.Plugin.Commands
{
    /// <summary>
    /// 
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    class ExportToGltfCommand : IExternalCommand
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandData"></param>
        /// <param name="message"></param>
        /// <param name="elements"></param>
        /// <returns></returns>
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!TryGetActiveDocument(commandData, out Autodesk.Revit.DB.Document docToExport)) { return Result.Failed; }
            if (!TryGetDefaultView(docToExport, out View viewToExport)) { return Result.Failed; }


            // Grab doc name
            string docName = Path.GetFileNameWithoutExtension(docToExport.Title);

            // Prep FBX export
            var vs = new ViewSet();
            vs.Insert(viewToExport);

            // Grab configuration
            var configuration = ConfigurationManager.ActiveConfiguration;

            // Create a new folder for the export
            configuration.ExportFilePathRoot = $"C:/Cityzenith/{docName}";
            if (!Directory.Exists(configuration.ExportFilePathRoot))
            {
                Directory.CreateDirectory(configuration.ExportFilePathRoot);
            }

            // Export FBX file
            docToExport.Export(
                configuration.ExportFilePathRoot,
                docName,
                vs,
                new FBXExportOptions()
                {
                    LevelsOfDetailValue = 15,
                    UseLevelsOfDetail = true,
                    WithoutBoundaryEdges = true
                }
             );



            // Check that file was created successfully
            var fbxFileName = configuration.ExportFilePathRoot + "/" + docName + ".fbx";
            if (!System.IO.File.Exists(fbxFileName)) { return Result.Failed; }
            var fbxModel = AssimpUtilities.LoadFbx(fbxFileName);

            // Get conversions between local ids
            var localToUniqueIdMap = docToExport.ExportableElements()
                .ToDictionary(e => e.Id.ToString(), e => e.UniqueId.ToString());

            // Replace auto-generated element names in Fbx with unqiue ids from revit doc
            AssimpUtilities.ReplaceNamesWithUniqueIds(fbxModel, localToUniqueIdMap);

            // Create textures subfolder
            string textureDirPath = configuration.ExportFilePathRoot + '/' + "Textures";
            if (!Directory.Exists(textureDirPath)) { Directory.CreateDirectory(textureDirPath); }

            var bundles = MaterialUtilities.GetTextureBundles(docToExport, out var paths);
            foreach (var b in bundles)
            {
                // Create material
                var assimpMaterial = AssimpUtilities.ConvertToAssimpMaterial(b, docToExport);

                // Add material to model and assign
                AssimpUtilities.AddAndAssignMaterial(
                    fbxModel,
                    assimpMaterial,
                    docToExport.ExportableElements().Where(e =>
                    {
                        var id = e.GetMaterialIds(false).FirstOrDefault();
                        if (id != null && id == b.Material.Id) { return true; }
                        return false;
                    })
                    .Select(e => e.UniqueId.ToString())
                    .ToHashSet()
                , out bool utilized);

                if (!utilized) { continue; }

                // Copy textures into textures folder
                foreach (var path in b.TexturePaths.Values)
                {
                    string destination = $"{textureDirPath}/{path.SafeFileName}";
                    try
                    {
                        File.Copy(path.FileLocation, destination, true);
                    }
                    catch (Exception e)
                    {
                        // This is likely due to duplicate materials copied in.
                        // This could also be an access issue, but less commonly.
                        //Logger.LogException("Error in copying textures: ", e);
                    }
                }
            }

            // Grab all element data
            var paramData = docToExport.SiphonElementParamValues(out var legend);
            var combined = paramData.Values.Combine();
            JsonConvert.SerializeObject(combined).WriteToFile($"{configuration.ExportFilePathRoot}/Params.json");
            JsonConvert.SerializeObject(legend).WriteToFile($"{configuration.ExportFilePathRoot}/Legend.json");

            // Write out gltf
            AssimpUtilities.SaveToGltf(fbxModel, $"{configuration.ExportFilePathRoot}", docName);

            // Delete .FBX
            File.Delete(fbxFileName);

            // Let em know!
            TaskDialog dlg = new TaskDialog("Export Successful");
            dlg.MainInstruction = $"Gltf file exported successfully: \n\n {configuration.ExportFilePathRoot}";
            dlg.Show();

            Process.Start(configuration.ExportFilePathRoot);

            return Result.Succeeded;
        }

        private bool TryGetDefaultView(Document docToExport, out View viewToExport)
        {
            // view to export
            viewToExport = null;
#if !REVIT2020
            viewToExport = docToExport.GetOrCreateDefault3DView();
#else
            viewToExport = AddinApplication.UIController.GetCurrentView(docToExport);
#endif
            if (viewToExport == null)
            {
                return false;
            }

            // Set view settings
            using (var tr = new Transaction(docToExport))
            {
                tr.Start("Changing view settings to realistic");
                viewToExport.DetailLevel = ViewDetailLevel.Fine;
                viewToExport.DisplayStyle = DisplayStyle.Realistic;
                tr.Commit();
            }

            return true;
        }

        private bool TryGetActiveDocument(ExternalCommandData commandData, out Document docToExport)
        {
            // document to export
            docToExport = commandData.Application.ActiveUIDocument.Document;
            if (docToExport == null)
            {
                return false;
            }
            return true;
        }
    }
}
