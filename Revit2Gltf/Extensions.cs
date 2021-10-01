using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf
{
    public static class Extensions
    {
        public static Dictionary<string,string> ToFilenamePathDictionarySafe(this IEnumerable<string> fullPaths)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            foreach(var p in fullPaths)
            {
                var fn = Path.GetFileNameWithoutExtension(p);
                if (output.ContainsKey(fn)) { continue; }
                output.Add(fn, p);
            }
            return output;
        }

        public static void WriteToFile(this string file, string path)
        {
            System.IO.File.WriteAllText(path, file);
        }

        public static Dictionary<string, Dictionary<string, string>> Combine(this IEnumerable<Dictionary<string,Dictionary<string,string>>> set)
        {
            var output = new Dictionary<string, Dictionary<string,string>>();

            foreach(var d in set)
            {
                foreach(var kv in d)
                {
                    if (!output.ContainsKey(kv.Key))
                    {
                        output.Add(kv.Key, kv.Value);
                    }
                }

            }

            return output;
        }

        public static FilteredElementCollector ExportableElements(this Document doc)
        {
            var catsToExport = 
                ConfigurationManager.ActiveConfiguration.ExportedCategories;

            List<ElementId> ids
          = new List<BuiltInCategory>(catsToExport)
            .ConvertAll<ElementId>(c
             => new ElementId((int)c));

            FilterCategoryRule r
              = new FilterCategoryRule(ids);

            ElementParameterFilter f
              = new ElementParameterFilter(r, true);

            // Use a logical OR of category filters

            IList<ElementFilter> a
              = new List<ElementFilter>(catsToExport.Count);

            foreach (BuiltInCategory bic in catsToExport)
            {
                a.Add(new ElementCategoryFilter(bic));
            }

            LogicalOrFilter categoryFilter
              = new LogicalOrFilter(a);

            // Run the collector

            FilteredElementCollector els
              = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .WherePasses(categoryFilter);

            return els;
        }

        public static Options DefaultExportGeometryOptions = new Options() 
        { 
            DetailLevel = ViewDetailLevel.Medium, 
            IncludeNonVisibleObjects = true 
        };

        /// <summary>
        /// Return parameter data for all  
        /// elements of all the given categories
        /// </summary>
        public static Dictionary<string, ElementParameters>
          SiphonElementParamValues(
            this Document doc,
            bool calculateCentroids = true,
            List<BuiltInCategory> cats = null)
        {
            if(cats == null)
            {
                cats = ConfigurationManager.ActiveConfiguration.ExportedCategories;
            }

            var output = new Dictionary<string, ElementParameters>();


            // Collect all required elements
            var els = 
                doc.ExportableElements()
                .Where(e => e.Category != null);

            foreach (Element e in els)
            {
                try
                {
                    BuiltInCategory bic = (BuiltInCategory)
                      (e.Category.Id.IntegerValue);
                    ElementParameters eParams = new ElementParameters(e, calculateCentroids);
                    output.Add(e.UniqueId, eParams);
                }
                catch(Exception ex)
                {
                    // Log exception
                }
            }



            // Grab rooms
            RoomFilter filter = new RoomFilter();

            // Apply the filter to the elements in the active document
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            IList<Element> rooms = collector.WherePasses(filter).ToElements();
            foreach(var r in rooms)
            {
                if (output.ContainsKey(r.UniqueId)) { continue; }
                try
                {
                    output.Add(r.UniqueId, new ElementParameters(r, true));
                }
                catch (Exception ex)
                {
                    // Log exception
                }
            }

            // Grab levels
            FilteredElementCollector level_coll = new FilteredElementCollector(doc);
            ICollection<Element> levels = level_coll.OfClass(typeof(Level)).ToElements();

            foreach(var l in levels)
            {
                if (output.ContainsKey(l.UniqueId)) { continue; }
                try
                {
                    output.Add(l.UniqueId, new ElementParameters(l, false));
                }
                catch (Exception ex)
                {
                    // Log exception
                }
            }

            return output;
        }

        public static string Description(
          this BuiltInCategory bic)
        {
            //string s = bic.ToString().ToLower();
            //s = s.Substring(4);
            //s = s.Substring(0, s.Length - 1);
            //return s;
            return bic.ToString().ToLower();
        }

        public static View3D GetOrCreateDefault3DView(this Document doc)
        {
            var view3d = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .Where(v => v.Name.ToUpper() == "{3D}")
                .FirstOrDefault();

            if (view3d == null)
            {
                // Create a new 3d view (rare)
                // Grab the view family type.
                var vft = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .Where(v => v.ViewFamily == ViewFamily.ThreeDimensional)
                    .FirstOrDefault();

                using (var tr = new Transaction(doc))
                {
                    tr.Start("Create 3D view for export to gltf");
                    try
                    {
                        view3d = View3D.CreateIsometric(doc, vft.Id);
                        tr.Commit();
                    }
                    catch (Exception e)
                    {
                        tr.RollBack();
                    }
                }
            }

            return view3d;
        }
    }
}
