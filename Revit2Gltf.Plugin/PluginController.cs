using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Revit2Gltf.Plugin
{
    internal class PluginController
    {
        private const string RibbonTabName = "Cityzenith";
        private const string SWPRibbonPanelName = "Revit To Gltf";
        private const string ExportButtonName = "       Export To Gltf       ";

        private static string _thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

        /// <summary>
        /// The export button
        /// </summary>
        PushButton Btn_Export { get; set; }


        public PluginController(UIControlledApplication application)
        {
            CreateUI(application);
        }

        private void CreateUI(UIControlledApplication application)
        {
            application.ViewActivated += Application_ViewActivated;

            try
            {
                application.CreateRibbonTab(RibbonTabName);
            }
            // This exception is usually thrown because the
            // ribbontab of the same name already exists.
            catch { }

            RibbonPanel panel = application.CreateRibbonPanel(RibbonTabName, SWPRibbonPanelName);

            AddExportButton();

            #region Button Fns
            void AddExportButton()
            {
                PushButtonData pbd = new PushButtonData(
                    "ExportToGltf",
                    ExportButtonName,
                    _thisAssemblyPath,
                    "Revit2Gltf.Plugin.Commands.ExportToGltfCommand");
                Btn_Export = panel.AddItem(pbd) as PushButton;
                Btn_Export.ToolTip = "Exports the 3D view to GLTF";
                Btn_Export.LargeImage = new BitmapImage(new Uri("pack://application:,,,/Revit2Gltf.Plugin;component/Resources/icons8-upload-to-cloud-32.png"));
                Btn_Export.Enabled = true;
            }
            #endregion
        }

        private void Application_ViewActivated(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
        {
            Btn_Export.Enabled = e.CurrentActiveView is View3D;
        }


    }
}
