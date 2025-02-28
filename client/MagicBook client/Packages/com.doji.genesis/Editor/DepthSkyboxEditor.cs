using System;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using SR = Genesis.Editor.StringResources;

namespace Genesis.Editor {

    [CustomEditor(typeof(DepthSkybox))]
    public class DepthSkyboxEditor : UnityEditor.Editor {

        private DepthSkybox _depthSkybox;
        private GameObject gameObject;

        private void OnEnable() {
            _depthSkybox = (DepthSkybox)target;
            gameObject = _depthSkybox.gameObject;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (GUILayout.Button(SR.ExtractMeshButton))
            {
                _depthSkybox.ExtractMesh();
            }

            if (GUILayout.Button(SR.GenerateButton)) {
                var name = _depthSkybox.gameObject.name;
                var renderer = gameObject.GetComponent<MeshRenderer>();
                var skybox = (Texture2D)renderer.sharedMaterial.GetTexture("_MainTex");
                var skyboxDepth = (Texture2D)renderer.sharedMaterial.GetTexture("_Depth");
                var assetPath = Path.Combine(IOUtils.StagingAreaPath, $"{name}");
                var range = new Vector2(0f, renderer.sharedMaterial.GetFloat("_Max"));
                var invert = renderer.sharedMaterial.GetFloat("_InverseDepth");
                var scale = renderer.sharedMaterial.GetFloat("_Scale");

                //DepthSkyboxPrefabUtility.CreateSkyboxPrefab(skybox, assetPath, name, range, skyboxDepth, invert > .5f, scale);
                //return;

                var go = _depthSkybox.ExtractMesh();
                var mesh = go.GetComponent<MeshFilter>().sharedMesh;
                
                Directory.CreateDirectory(assetPath);

                var m = go.GetComponent<MeshRenderer>().sharedMaterial;
                var matPath = Path.Combine(assetPath, $"{name}_mat.mat");
                //var texPath = Path.Combine(assetPath, $"{go.name}.mat");
                AssetDatabase.CreateAsset(skybox, Path.Combine(assetPath, $"{name}_color.asset"));
                AssetDatabase.CreateAsset(skyboxDepth, Path.Combine(assetPath, $"{name}_depth.asset"));
                AssetDatabase.CreateAsset(mesh, Path.Combine(assetPath, $"{name}_mesh.asset"));

                var existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (existingMat != null)
                {
                    AssetDatabase.DeleteAsset(matPath);
                }

                AssetDatabase.CreateAsset(m, matPath);

                string prefabPath = Path.Combine(assetPath, $"{name}.prefab");
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (existing != null)
                {
                    AssetDatabase.DeleteAsset(prefabPath);
                }
                GameObject variant = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                //Object.DestroyImmediate(instance);
                //PrefabUtility.InstantiatePrefab(variant);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        

        private float SampleBilinear(NativeArray<float> depthData, Vector2 uv, int width, int height) {
            float i = Mathf.Lerp(0f, width - 1, uv.x);
            int i0 = Mathf.FloorToInt(i);

            int i1 = i0 < width - 1 ? i0 + 1 : i0;
            float j = Mathf.Lerp(0f, height - 1, uv.y);
            int j0 = Mathf.FloorToInt(j);
            int j1 = j0 < height - 1 ? j0 + 1 : j0;

            float q11 = depthData[(i0 * width) + j0];
            float q21 = depthData[(i1 * width) + j0];
            float q12 = depthData[(i0 * width) + j1];
            float q22 = depthData[(i1 * width) + j1];

            float dx = i - i0;
            float dy = j - j0;

            float v1 = q11 + dx * (q21 - q11);
            float v2 = q12 + dx * (q22 - q12);

            return v1 + dy * (v2 - v1);
        }
    }
}