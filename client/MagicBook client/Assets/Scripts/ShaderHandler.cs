using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GLTFast;
using UnityEngine;

public class ShaderHandler : MonoBehaviour, IShaderChangeEventListener
{
    [Serializable]
    public class NamedShader
    {
        public string Name;
        public Shader Shader;
    }

    public List<NamedShader> ShadersToReplace;
    public bool StaticSettings;
    public float DelayTime;
    public GltfAsset OptionalAssetToWaitFor;

    [SerializeField]
    string eventKey;
    public string EventKey { get => eventKey; set => eventKey = value; }

    public void OnChangeToShader(Shader shader)
    {
        var go = gameObject;
        if(OptionalAssetToWaitFor != null)
        {
            go = OptionalAssetToWaitFor.gameObject;
        }

        foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            foreach (var m in mr.materials)
                m.shader = shader;
    }

    // Start is called before the first frame update
    void Start()
    {
        if(StaticSettings)
            return;

        var parentShaderHandler = transform.parent.gameObject.GetComponentInParent<ShaderHandler>();
        if(parentShaderHandler != null && parentShaderHandler.isActiveAndEnabled)
        {
            Debug.Log("Found ShaderHandler in parents so copying their shaders");
            ShadersToReplace = parentShaderHandler.ShadersToReplace;
        }

        StartCoroutine(DelayedExecute());
    }

    IEnumerator DelayedExecute()
    {
        if (OptionalAssetToWaitFor != null)
        {
            while (!OptionalAssetToWaitFor.IsDone)
                yield return null;
        }

        yield return new WaitForSeconds(DelayTime);

        ModifyShaders(OptionalAssetToWaitFor != null ? OptionalAssetToWaitFor.gameObject : gameObject);
    }

    private void ModifyShaders(GameObject asset)
    {
        Debug.Log("ShaderHandler: replacing shaders!");
        MeshRenderer[] rendererArray = asset.GetComponentsInChildren<MeshRenderer>();

        foreach (var renderer in rendererArray)
        {
            //Material[] materials = renderer.sharedMaterials;


            //for (int i = 0; i < materials.Length; i++)
            foreach(var m in renderer.materials)
            {
                //if (materials[i] == null)
                if(m == null)
                {
                    Debug.LogError("Material is null on " + renderer.gameObject.name);
                    continue;
                }

                //if (ShadersToReplace.FirstOrDefault(s => s.Name == materials[i].shader.name) is NamedShader s)
                if (ShadersToReplace.FirstOrDefault(s => s.Name == m.shader.name) is NamedShader s)
                {
                    Debug.Log($"ShaderHandler: replacing  material shader ({m.shader.name}) by {s.Shader?.name}");
                    //materials[i].shader = s.Shader;
                    m.shader = s.Shader;
                }
                else
                {
                    Debug.Log($"ShaderHandler: Did not find a replacement shader for '{m.shader.name}' so skipping.");
                }
                //Debug.Log("Assigned shader to material on " + renderer.gameObject.name);
            }

            //renderer.materials = materials;
        }

    }

}
