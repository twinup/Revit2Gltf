using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf
{
    /// <summary>
    /// Configures the export.
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Categories of Revit elements which will
        /// be exported.
        /// </summary>
        public List<BuiltInCategory> ExportedCategories;

        /// <summary>
        /// Name applied to elements whose name cannot
        /// be ascertained.
        /// </summary>
        public string UnknownObjectName;

        /// <summary>
        /// Where the gltf files will inevitably be exported to.
        /// </summary>
        public string ExportFilePathRoot = "C:/Cityzenith";


        public Configuration()
        {

        }

        public static Configuration Default = new Configuration()
        {
            UnknownObjectName = "Unknown Revit Object",
            ExportedCategories = new List<BuiltInCategory>()
            {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_CurtainWallMullions,
                BuiltInCategory.OST_CurtainWallPanels,
                BuiltInCategory.OST_EdgeSlab,
                BuiltInCategory.OST_Rooms,
                BuiltInCategory.OST_Ceilings,
                BuiltInCategory.OST_Furniture,
                BuiltInCategory.OST_FurnitureSystems,
                BuiltInCategory.OST_Assemblies,
                BuiltInCategory.OST_Columns,
                BuiltInCategory.OST_Casework,
                BuiltInCategory.OST_Site,
                BuiltInCategory.OST_Stairs,
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_Views,
                BuiltInCategory.OST_Truss,
                BuiltInCategory.OST_Levels,
            }
        };
    }
    
    /// <summary>
    /// Light singleton posessing the active
    /// configuration of the exporter.
    /// </summary>
    public static class ConfigurationManager
    {
        public static Configuration ActiveConfiguration = Configuration.Default;
    }
}
