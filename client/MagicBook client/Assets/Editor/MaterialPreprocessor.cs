using UnityEditor;
using UnityEngine;

public class MaterialPreprocessor : EditorWindow
{
    [MenuItem("Tools/Material Preprocessor For ClipBox")]
    public static void ShowWindow()
    {
        Debug.Log("Material Preprocessor Window Opening...");
        EditorWindow window = GetWindow<MaterialPreprocessor>("Material Preprocessor For ClipBox");
        window.position = new Rect(100, 100, 400, 300);
        window.Show();
    }

    private GameObject modelPrefab;
    private Shader shader;

    void OnEnable()
    {
        // Initialize the shader with the default value
        shader = Shader.Find("Custom/ClipBox");
        if (shader == null)
        {
            Debug.LogError("Default shader 'Custom/ClipBox' not found!");
        }
    }

    void OnGUI()
    {
        GUILayout.Label("Material Preprocessor For ClipBox", EditorStyles.boldLabel);
        modelPrefab = (GameObject)EditorGUILayout.ObjectField("Model Prefab", modelPrefab, typeof(GameObject), false);
        shader = (Shader)EditorGUILayout.ObjectField("ClipBox Shader", shader, typeof(Shader), false);
        if (GUILayout.Button("Process and Save"))
        {
            Debug.Log("Process and Save button clicked.");
            if (modelPrefab != null && shader != null)
            {
                ProcessAndSavePrefab();
            }
            else
            {
                Debug.LogError("Model Prefab and Shader must be assigned!");
            }
        }
    }
    void ProcessAndSavePrefab()
    {
        if (modelPrefab == null)
        {
            Debug.LogError("Prefab is null.");
            return;
        }

        // Load the prefab
        string path = AssetDatabase.GetAssetPath(modelPrefab);
        Debug.Log("Prefab path: " + path);

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Failed to get asset path.");
            return;
        }

        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);

        if (modelInstance == null)
        {
            Debug.LogError("Failed to instantiate prefab.");
            return;
        }

        // Process materials
        MeshRenderer[] renderers = modelInstance.GetComponentsInChildren<MeshRenderer>();
        Debug.Log("Found " + renderers.Length + " MeshRenderers.");

        foreach (var renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null)
                {
                    Debug.LogError("Material is null on " + renderer.gameObject.name);
                    continue;
                }

                materials[i].shader = shader;
                Debug.Log("Assigned shader to material on " + renderer.gameObject.name);
            }

            renderer.sharedMaterials = materials;
        }

        // Save the modified prefab
        PrefabUtility.SaveAsPrefabAsset(modelInstance, path);
        Debug.Log("Prefab saved at: " + path);

        // Clean up the instance
        DestroyImmediate(modelInstance);
        Debug.Log("Cleaned up instantiated prefab instance.");

        Debug.Log("Materials processed and prefab saved.");
    }
}
