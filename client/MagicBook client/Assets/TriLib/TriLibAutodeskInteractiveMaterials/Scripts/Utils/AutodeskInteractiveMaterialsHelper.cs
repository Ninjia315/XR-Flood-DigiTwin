using TriLibCore.Mappers;
using UnityEngine;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Represents a series of AutodeskInteractiveStandardMaterialMapper helper methods.
    /// </summary>
    [CreateAssetMenu(menuName = "TriLib/MaterialsHelper/Autodesk Interactive Materials Helper", fileName = "AutodeskInteractiveMaterialsHelper")]
    public class AutodeskInteractiveMaterialsHelper : MaterialsHelper
    {   
        /// <summary>
        /// Configures the AssetLoaderOptions to use the AutodeskInteractiveStandardMaterialMapper.
        /// </summary>
        /// <param name="assetLoaderOptions">The options to use when loading the Model.</param>
        public static void SetupStatic(ref AssetLoaderOptions assetLoaderOptions)
        {
            CreateInstance<AutodeskInteractiveMaterialsHelper>().Setup(ref assetLoaderOptions);
        }

        /// <summary>
        /// Configures the AssetLoaderOptions to use the AutodeskInteractiveStandardMaterialMapper.
        /// </summary>
        /// <param name="assetLoaderOptions">The options to use when loading the Model.</param>
        public override void Setup(ref AssetLoaderOptions assetLoaderOptions)
        {
            var autodeskInteractiveMaterialMapper = ScriptableObject.CreateInstance<AutodeskInteractiveStandardMaterialMapper>();
            if (autodeskInteractiveMaterialMapper != null)
            {
                if (assetLoaderOptions == null)
                {
                    assetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
                }
                if (assetLoaderOptions.MaterialMappers == null)
                {
                    assetLoaderOptions.MaterialMappers = new MaterialMapper[] { autodeskInteractiveMaterialMapper };
                }
                else
                {
                    ArrayUtils.Add(ref assetLoaderOptions.MaterialMappers, autodeskInteractiveMaterialMapper);
                }
                assetLoaderOptions.CreateMaterialsForAllModels = true;
                assetLoaderOptions.SetUnusedTexturePropertiesToNull = false;
                assetLoaderOptions.ConvertMaterialTextures = false;
                assetLoaderOptions.LoadDisplacementTextures = true;
                autodeskInteractiveMaterialMapper.CheckingOrder = 1;
            }
        }
    }
}