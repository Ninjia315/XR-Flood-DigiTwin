using UnityEngine;

namespace TriLibCore.Utils
{
    public abstract class MaterialsHelper : ScriptableObject
    {
        public abstract void Setup(ref AssetLoaderOptions assetLoaderOptions);
    }
}