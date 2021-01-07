using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf
{
    public class CentroidVolume
    {
        public const int CoordinateRoundingDecimals = 3;
        public const int VolumeRoundingDecimals = 3;

        public string Centroid_Str =>
            $"{Math.Round(Centroid.X, CoordinateRoundingDecimals)}," +
            $"{Math.Round(Centroid.Y, CoordinateRoundingDecimals)}," +
            $"{Math.Round(Centroid.Z, CoordinateRoundingDecimals)}";
        public string Volume_Str => Math.Round(Volume, VolumeRoundingDecimals).ToString();

        public XYZ Centroid { get; set; } = XYZ.Zero;
        public double Volume { get; set; } = 0;

        public CentroidVolume() { }
    }
}
