using TriLibCore.Mappers;
using TriLibCore.Samples;
using UnityEngine;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Represents a series of GltfMaterialsMapper helper methods.
    /// </summary>
    [CreateAssetMenu(menuName = "TriLib/MaterialsHelper/Gltf Materials Helper", fileName = "GltfMaterialsHelper")]
    public class GltfMaterialsHelper : MaterialsHelper
    {
        /// <summary>
        /// Configures the AssetLoaderOptions to use the AutodeskInteractiveStandardMaterialMapper.
        /// </summary>
        /// <param name="assetLoaderOptions">The options to use when loading the Model.</param>
        public static void SetupStatic(ref AssetLoaderOptions assetLoaderOptions)
        {
            CreateInstance<GltfMaterialsHelper>().Setup(ref assetLoaderOptions);
        }

        /// <summary>
        /// Configures the AssetLoaderOptions to use the glTF2StandardMaterialMapper.
        /// </summary>
        /// <param name="assetLoaderOptions">The options to use when loading the Model.</param>
        public override void Setup(ref AssetLoaderOptions assetLoaderOptions)
        {
            var glTF2MaterialMapper = ScriptableObject.CreateInstance<glTF2StandardMaterialMapper>();
            if (glTF2MaterialMapper != null)
            {
                if (assetLoaderOptions == null)
                {
                    assetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
                }
                if (assetLoaderOptions.MaterialMappers == null)
                {
                    assetLoaderOptions.MaterialMappers = new MaterialMapper[] { glTF2MaterialMapper };
                }
                else
                {
                    ArrayUtils.Add(ref assetLoaderOptions.MaterialMappers, glTF2MaterialMapper);
                }
                assetLoaderOptions.CreateMaterialsForAllModels = true;
                assetLoaderOptions.SetUnusedTexturePropertiesToNull = false;
                assetLoaderOptions.ConvertMaterialTextures = false;
                glTF2MaterialMapper.CheckingOrder = 2;
            }
        }
    }
}