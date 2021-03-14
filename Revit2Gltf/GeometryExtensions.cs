using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf
{
    public static class GeometryExtensions
    {
        public static bool TryGetCentroidVolume(this Solid s, out CentroidVolume cv)
        {
            cv = null;
            try
            {
                if (null != s
                    && 0 < s.Faces.Size
                    && SolidUtils.IsValidForTessellation(s)
                    && (null != (cv = new CentroidVolume()
                    {
                        Centroid = s.ComputeCentroid(),
                        Volume = s.Volume
                    }
                )))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
            }
            return false;
        }

        /// <summary>
        /// Calculate centroid for all non-empty solids 
        /// found for the given element. Family instances 
        /// may have their own non-empty solids, in which 
        /// case those are used, otherwise the symbol geometry.
        /// The symbol geometry could keep track of the 
        /// instance transform to map it to the actual 
        /// project location. Instead, we ask for 
        /// transformed geometry to be returned, so the 
        /// resulting solids are already in place.
        /// </summary>
        public static bool TryGetCentroid(
          this Element e,
          Options opt,
          out CentroidVolume cvol)
        {
            cvol = null;
            GeometryElement geo = e.get_Geometry(opt);
            CentroidVolume combined = new CentroidVolume();

            if (null == geo) { return false; }

            // List of pairs of centroid, volume for each solid

            List<CentroidVolume> a
                = new List<CentroidVolume>();

            if (e is FamilyInstance)
            {
                geo = geo.GetTransformed(
                    Transform.Identity);
            }

            GeometryInstance inst = null;

            foreach (GeometryObject obj in geo)
            {
                if (!TryGetCentroidVolume(obj as Solid, out var cv)) { continue; }
                a.Add(cv);
                inst = obj as GeometryInstance;
            }

            if (0 == a.Count && null != inst)
            {
                geo = inst.GetSymbolGeometry();

                foreach (GeometryObject obj in geo)
                {
                    if (!TryGetCentroidVolume(obj as Solid, out var cv)) { continue; }
                    a.Add(cv);
                }
            }

            // Get the total centroid from the partial
            // contributions. Each contribution is weighted
            // with its associated volume, which needs to 
            // be factored out again at the end.

            try
            {
                if (0 < a.Count)
                {
                    combined = new CentroidVolume();
                    bool unweighted = false;

                    // Revit may give us volumes of 0.
                    // In which case we will not do a 
                    // weighted calculation, which will
                    // throw a divide by zero exception.
                    foreach (CentroidVolume cv2 in a)
                    {
                        if (cv2.Volume == 0)
                        {
                            unweighted = true;
                            break;
                        }
                    }
                    if (unweighted)
                    {
                        foreach (var cv2 in a)
                        {
                            combined.Centroid += cv2.Centroid;
                        }
                        combined.Centroid /= a.Count;
                    }
                    else
                    {
                        foreach (var cv2 in a)
                        {
                            combined.Centroid += cv2.Volume * cv2.Centroid;
                            combined.Volume += cv2.Volume;
                        }
                        combined.Centroid /= (a.Count == 0 ? 1 : a.Count) * (combined.Volume == 0 ? 1 : combined.Volume);
                    }
                }
            } catch(Exception ex)
            {
                combined = null;
            }
            cvol = combined;
            return combined != null && combined != default(CentroidVolume);
        }
    }
}
