using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#if REVIT2019
using Autodesk.Revit.DB.Visual;
#else
using Autodesk.Revit.Utility;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf
{
    public static class MaterialUtilities
    {
        /// <summary>
        /// Keywords inside of asset attribute
        /// names which indicate what kind of
        /// texture they are. 
        /// Thank you Revit
        /// for being such a pain in the ass.
        /// </summary>
        public static Dictionary<RevitTextureType, List<string>> TextureTypeKeywords =
            new Dictionary<RevitTextureType, List<string>>()
            {
                    {
                        RevitTextureType.Color, new List<string>()
                        {
                            "color",
                            "diffuse",
                            "unifiedbitmapschema"
                        }
                    },
                    {
                        RevitTextureType.Bump, new List<string>()
                        {
                            "bm_map",
                            "bump",
                            "pattern_map"
                        }
                    }
            };

        public static string TexturesPathRoot =
            $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}" +
            @"\Common Files\Autodesk Shared\Materials\Textures".Replace("\\\\", "\\");

        public static Dictionary<string, string> RevitTextures =
            Directory.GetFiles(TexturesPathRoot, "*", SearchOption.AllDirectories)
                     .ToFilenamePathDictionarySafe();

        private static Dictionary<string, List<TextureBundle>> _bundleCache =
            new Dictionary<string, List<TextureBundle>>();
        private static Dictionary<string, List<string>> _texturePathCache =
            new Dictionary<string, List<string>>();

        public static bool TryGetTextureTypeFromAssetName(string assetName, out RevitTextureType t)
        {
            t = RevitTextureType.Unknown;
            string lowerCaseName = assetName.ToLower();
            foreach (var kv in TextureTypeKeywords)
            {
                foreach (var val in kv.Value)
                {
                    if (lowerCaseName.Contains(val))
                    {
                        t = kv.Key;
                        return true;
                    }
                }
            }
            return false;
        }

        public static List<TextureBundle> GetTextureBundles(Document doc, out List<string> paths)
        {

            if (_bundleCache.ContainsKey(doc.PathName))
            {
                paths = _texturePathCache[doc.PathName];
                return _bundleCache[doc.PathName];
            }
            _texturePathCache.Add(doc.PathName, new List<string>());

            // Find materials
            FilteredElementCollector fec = new FilteredElementCollector(doc).OfClass(typeof(Material));

            // Convert materials to bundles
            List<TextureBundle> bundles = new List<TextureBundle>();
            foreach (var m in fec.Cast<Material>())
            {
                try
                {
                    var bundle = new TextureBundle(m);

                    ElementId appearanceAssetId = m.AppearanceAssetId;
                    AppearanceAssetElement appearanceAssetElem
                      = doc.GetElement(appearanceAssetId)
                        as AppearanceAssetElement;

                    if (appearanceAssetElem == null) { continue; }

                    Asset asset = appearanceAssetElem
                      .GetRenderingAsset();

                    if (asset == null) { continue; }

                    for (int assetIdx = 0; assetIdx < asset.Size; assetIdx++)
                    {
                        AssetProperty aProperty = asset[assetIdx];
                        if (aProperty.NumberOfConnectedProperties < 1) { continue; }

                        Asset connectedAsset = aProperty
                            .GetConnectedProperty(0) as Asset;

                        // See if there is a path associated.
#if REVIT2018 || REVIT2019 || REVIT2020
                        // This line is 2018.1 & up because of the 
                        // property reference to UnifiedBitmap
                        // .UnifiedbitmapBitmap.  In earlier versions,
                        // you can still reference the string name 
                        // instead: "unifiedbitmap_Bitmap"
                        AssetPropertyString path = connectedAsset[
                          UnifiedBitmap.UnifiedbitmapBitmap]
                            as AssetPropertyString;
#else
                        AssetPropertyString path =
                            connectedAsset["unifiedbitmap_Bitmap"] as AssetPropertyString;
#endif
                        // If there is no asset path, nothing to pursue (Empty field)
                        if (path == null || String.IsNullOrEmpty(path.Value)) { continue; }

                        // See what kind of texture it is.
                        if (TryGetTextureTypeFromAssetName(connectedAsset.Name, out var t))
                        {
                            // This will be a relative path to the 
                            // built -in materials folder, addiitonal 
                            // render appearance folder, or an 
                            // absolute path.
                            string assetName = Path.GetFileNameWithoutExtension(path.Value);

                            // Ensure that we have a valid texture path.
                            if (RevitTextures.ContainsKey(assetName))
                            {
                                bundle.TexturePaths.Add(t, new SafenedFilename(RevitTextures[assetName]));
                            }
                            else
                            {
                                //Logger.LogError(
                                //    $"Found asset outisde of Revit material lib: {path.Value}. Could not add to export"
                                //    );
                            }
                        }
                    }

                    // Return the bundle we created.
                    bundles.Add(bundle);
                }
                catch (Exception e)
                {
                    //Logger.LogException("Error in bundle creation: ", e);
                }
            }

            var bundleList = bundles.Where(b => b != null).ToList();
            _bundleCache.Add(doc.PathName, bundleList);
            paths = _texturePathCache[doc.PathName];

            return bundleList;
        }
    }
}
