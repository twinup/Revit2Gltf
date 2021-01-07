using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf
{
    /// <summary>
    /// Combines
    /// </summary>
    public class TextureBundle
    {
        /// <summary>
        /// Full paths to the textures associated with this material.
        /// </summary>
        public Dictionary<RevitTextureType, SafenedFilename> TexturePaths
            = new Dictionary<RevitTextureType, SafenedFilename>();

        /// <summary>
        /// The material.
        /// </summary>
        public readonly Material Material;

        public TextureBundle(Material m)
        {
            Material = m;
        }
    }
}
