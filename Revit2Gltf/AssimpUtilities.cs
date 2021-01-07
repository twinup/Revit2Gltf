using Assimp;
using Assimp.Configs;
using Autodesk.Revit.DB;
using Autodesk.Revit.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RevitMaterial = Autodesk.Revit.DB.Material;

namespace Revit2Gltf
{
    public static class AssimpUtilities
    {
        public static float GetOpacity(this RevitMaterial mat)
        {
            return (Math.Abs(mat.Transparency - 100) * 1f / 100f);
        }

        public static Color4D ToColor4D(this RevitMaterial mat)
        {
            var color = mat.Color;
            return new Color4D(color.Red * 1f / 255f, color.Green * 1f / 255f, color.Blue * 1f / 255f, mat.GetOpacity());
        }

        public static Assimp.Material ConvertToAssimpMaterial(TextureBundle bundle, Document doc)
        {
            // Create new material with base props
            // from the Revit material
            var newmat = new Assimp.Material()
            {
                Opacity = bundle.Material.GetOpacity(),
                Reflectivity = 0f,
                Name = bundle.Material.Name,
                ColorDiffuse = bundle.Material.ToColor4D()
            };

            // Extract base properties from revit material
            ElementId appearanceAssetId = bundle.Material.AppearanceAssetId;
            AppearanceAssetElement appearanceAsset = doc.GetElement(appearanceAssetId) as AppearanceAssetElement;
            Asset renderingAsset = appearanceAsset.GetRenderingAsset();
            RenderAppearanceDescriptor rad
                  = new RenderAppearanceDescriptor(renderingAsset);
            PropertyDescriptorCollection collection = rad.GetProperties();
            List<PropertyDescriptor> orderableCollection = new List<PropertyDescriptor>(collection.Count);

            List<string> allPropNames = orderableCollection.Select(f => f.Name).ToList();

            foreach (PropertyDescriptor descr in collection)
            {
                orderableCollection.Add(descr);
                switch (descr.Name)
                {
                    #region Notes

                    // The commented out properties aren't in use yet, 
                    // but do work with revit materials as expected.

                    //case "texture_UScale":
                    //    var uScale = renderingAsset["texture_UScale"] as AssetPropertyDouble;
                    //    break;
                    //case "texture_VScale":
                    //    break;
                    //case "texture_UOffset":
                    //    break;
                    //case "texture_VOffset":
                    //    break;
                    //case "texture_RealWorldScaleX":
                    //    var xScale = renderingAsset["texture_RealWorldScaleX"] as AssetPropertyDistance;
                    //    break;
                    //case "texture_RealWorldScaleY":
                    //    break;

                    #endregion

                    case "generic_diffuse":
                        var prop = renderingAsset["generic_diffuse"] as AssetPropertyDoubleArray4d;
                        newmat.ColorDiffuse = new Color4D(
                            (float)prop.Value.get_Item(0),
                            (float)prop.Value.get_Item(1),
                            (float)prop.Value.get_Item(2),
                            (float)prop.Value.get_Item(3)
                        );
                        break;
                    case "glazing_reflectance":
                        // This is glass, so we should reduce the transparency.
                        var refl = renderingAsset["glazing_reflectance"] as AssetPropertyDouble;
                        if (refl == null)
                        {
                            var reflFloat = renderingAsset["glazing_reflectance"] as AssetPropertyFloat;
                            newmat.Reflectivity = reflFloat?.Value ?? 0f;
                        }
                        else
                        {
                            newmat.Reflectivity = (float)refl.Value;
                        }
                        newmat.Opacity = Math.Abs(0f - newmat.Reflectivity);
                        break;
                    case "common_Tint_color":
                        // Tint shouldn't be used if generic diffuse is set
                        if (renderingAsset["generic_diffuse"] != null) { continue; }
                        var tintProp = renderingAsset["common_Tint_color"] as AssetPropertyDoubleArray4d;
                        newmat.ColorDiffuse = new Color4D(
                            (float)tintProp.Value.get_Item(0),
                            (float)tintProp.Value.get_Item(1),
                            (float)tintProp.Value.get_Item(2),
                            (float)tintProp.Value.get_Item(3)
                        );
                        break;
                    default:
                        break;
                }
            }

            // Set textures
            foreach (var tx in bundle.TexturePaths)
            {
                // Get the filename
                var txFileName = tx.Value.SafeFileName;
                if (tx.Key == RevitTextureType.Color)
                {
                    newmat.TextureDiffuse = new TextureSlot(
                        $"Textures/{txFileName}",
                        TextureType.Diffuse,
                        0, // Texture index in the material
                        TextureMapping.Box,
                        0, // 
                        0.5f, // Blend mode
                        TextureOperation.Add,
                        TextureWrapMode.Clamp,
                        TextureWrapMode.Clamp,
                        0 // Flags,
                                );
                }
                else if (tx.Key == RevitTextureType.Bump)
                {
                    newmat.TextureHeight = new TextureSlot(
                        $"Textures/{txFileName}",
                        TextureType.Diffuse,
                        0, // Texture index in the material
                        TextureMapping.Box,
                        0, // 
                        0.5f, // Blend mode
                        TextureOperation.Add,
                        TextureWrapMode.Clamp,
                        TextureWrapMode.Clamp,
                        0 // Flags,
                                );
                }
            }
            return newmat;
        }

        public static void AddAndAssignMaterial(
            Scene model,
            Assimp.Material mat,
            HashSet<string> uniqueIdsOfElementsWithMat,
            out bool utilized
            )
        {
            utilized = false;

            // If no elements in the model use this material,
            // don't bother.
            if (uniqueIdsOfElementsWithMat.Count == 0) { return; }

            int matIndex = model.MaterialCount;
            foreach (var mesh in model.Meshes)
            {
                if (uniqueIdsOfElementsWithMat.Contains(mesh.Name))
                {
                    mesh.MaterialIndex = matIndex;
                    utilized = true;
                }
            }
            if (utilized)
            {
                //Logger.LogInfo($"Adding material {mat.Name} to gltf.");
                model.Materials.Add(mat);
            }
            else
            {
                //Logger.LogInfo($"Won't add material {mat.Name} to gltf " +
                //    $"because no objects utilize it.");
            }
        }

        public static void ReplaceNamesWithUniqueIds(Scene model, Dictionary<string, string> localToUniqueIdMap)
        {
            foreach (var node in model.RootNode.GetNodes())
            {
                node.Name = GetUniqueIdFromNodeName(node.Name);
            }
            foreach (var mesh in model.Meshes)
            {
                mesh.Name = GetUniqueIdFromNodeName(mesh.Name);
            }

            string GetUniqueIdFromNodeName(string nodeName)
            {
                // Fragment the node name to get the local id of the element
                // in the brackets
                string[] fragments = nodeName.Split(new char[] { '[', ']' });

                string result = fragments.Length > 1 ?
                    localToUniqueIdMap.ContainsKey(fragments[1]) ?
                        localToUniqueIdMap[fragments[1]] :
                        fragments[1]
                        :
                    fragments.FirstOrDefault() ?? ConfigurationManager.ActiveConfiguration.UnknownObjectName;

                return result;
            }
        }


        public static IEnumerable<Node> GetNodes(this Node root)
        {
            if (!root.HasChildren) { yield break; }
            foreach (var childNode in root.Children)
            {
                yield return childNode;
                if (childNode.HasChildren)
                {
                    foreach (var childChildNode in GetNodes(childNode))
                    {
                        yield return childChildNode;
                    }
                }
            }
            yield break;
        }

        public static Scene LoadFbx(string fileName)
        {
            //Create a new importer
            using (var importer = new AssimpContext())
            {
                //This is how we add a configuration (each config is its own class)
                NormalSmoothingAngleConfig config = new NormalSmoothingAngleConfig(66.0f);
                importer.SetConfig(config);

                //This is how we add a logging callback 
                LogStream logstream = new LogStream(delegate (String msg, String userData) {
                    Console.WriteLine(msg);
                });
                logstream.Attach();

                //Import the model. All configs are set. The model
                //is imported, loaded into managed memory. Then the unmanaged memory is released, and everything is reset.
                Scene model = importer.ImportFile(fileName, PostProcessPreset.TargetRealTimeMaximumQuality);

                return model;
            }
        }

        public static bool TryRemoveFile(string fileName, TimeSpan pause, int tries = 5)
        {
            if (pause == default(TimeSpan))
            {
                // Default to 1 second
                pause = TimeSpan.FromSeconds(1);
            }
            while (tries > 0)
            {
                try
                {
                    File.Delete(fileName);
                    return true;
                }
                catch (Exception e)
                {
                    tries--;
                }
            }
            return false;
        }

        public static bool SaveToGltf(this Scene model, string path, string fileName)
        {
            try
            {
                using (var importer = new AssimpContext())
                {
                    string outputFilePath = $"{path}/{fileName}.gltf";
                    if (importer.ExportFile(model, outputFilePath, "gltf2"))
                    {
                        // Replace the buffer path to a relative one.
                        string gltf = File.ReadAllText(outputFilePath);
                        gltf = gltf.Replace(path + '/', "");
                        File.WriteAllText(outputFilePath, gltf);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                //Logger.LogException("Error in saving gltf: ", e);
                return false;
            }

        }
    }
}
