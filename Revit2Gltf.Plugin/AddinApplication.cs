using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf.Plugin
{
    class AddinApplication : IExternalApplication
    {
        public static PluginController Controller;

        public Result OnStartup(UIControlledApplication application)
        {
            Controller = new PluginController(application);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
