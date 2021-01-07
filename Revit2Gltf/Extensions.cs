using Autodesk.Revit.DB;
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

        static Options DefaultExportGeometryOptions = new Options() 
        { 
            DetailLevel = ViewDetailLevel.Medium, 
            IncludeNonVisibleObjects = true 
        };

        /// <summary>
        /// Return parameter data for all  
        /// elements of all the given categories
        /// </summary>
        public static Dictionary<string, Dictionary<string, Dictionary<string,string>>>
          SiphonElementParamValues(
            this Document doc,
            out Dictionary<int, string> legend,
            bool calculateCentroids = true,
            List<BuiltInCategory> cats = null)
        {
            if(cats == null)
            {
                cats = ConfigurationManager.ActiveConfiguration.ExportedCategories;
            }

            // Set up the return value dictionary
            var map_cat_to_uid_to_param_values
                    = new Dictionary<string,
                      Dictionary<string,
                       Dictionary<string, string>>>();

            // One top level dictionary per category
            foreach (BuiltInCategory cat in cats)
            {
                map_cat_to_uid_to_param_values.Add(
                  cat.Description(),
                  new Dictionary<string,
                    Dictionary<string, string>>());
            }

            // Collect all required elements
            var els = doc.ExportableElements();

            // Retrieve parameter data for each element
            var legendReversed = new Dictionary<string, int>();
            legendReversed.Add("Category", 0);
            if (calculateCentroids)
            {
                legendReversed.Add("Centroid", 1);
            }

            foreach (Element e in els)
            {
                Category cat = e.Category;
                if (null == cat)
                {
                    continue;
                }
                Dictionary<string,string> param_values = GetParamValues(e, ref legendReversed);

                if (calculateCentroids && 
                    e.TryGetCentroid(DefaultExportGeometryOptions, out var c))
                {
                    param_values.Add("1", c.Centroid_Str);
                }

                BuiltInCategory bic = (BuiltInCategory)
                  (e.Category.Id.IntegerValue);

                string catkey = bic.Description();
                string uniqueId = e.UniqueId.ToString();

                map_cat_to_uid_to_param_values[catkey].Add(
                  uniqueId, param_values);
            }

            legend = legendReversed.ToDictionary(kv => kv.Value, kv => kv.Key);


            return map_cat_to_uid_to_param_values;
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

        /// <summary>
        /// Return all the parameter values  
        /// deemed relevant for the given element
        /// in string form.
        /// </summary>
        static Dictionary<string,string> GetParamValues(Element e, ref Dictionary<string,int> paramLegend)
        {
            // Two choices: 
            // Element.Parameters property -- Retrieves 
            // a set containing all  the parameters.
            // GetOrderedParameters method -- Gets the 
            // visible parameters in order.

            IList<Parameter> ps = e.GetOrderedParameters();

            List<string> param_values = new List<string>(
              ps.Count);


            var paramList =
                ps
                .Where(p => p.HasValue && !String.IsNullOrEmpty(p.AsValueString()));
            Dictionary<string, string> output = new Dictionary<string, string>();
            output.Add("0", e.Category.Name);

            foreach (var p in paramList)
            {
                var key = GetLegendValue(p,ref paramLegend).ToString();
                if (!output.ContainsKey(key))
                {
                    output.Add(key, p.AsValueString());
                }
            }
            return output;

            int GetLegendValue(Parameter p, ref Dictionary<string,int> lgd)
            {
                string n = p.Definition.Name;
                if (!lgd.ContainsKey(n))
                {
                    int c = lgd.Count;
                    lgd.Add(n, c);
                    return c;
                }
                return lgd[n];                              
            }
           
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
