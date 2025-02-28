using System.Collections.Generic;
using System.Linq;
using TMRI.Core;
using UnityEngine;

public class LODMesh : MonoBehaviour
{
    public int NumLODs;

    public LODGroup lodGroup { get; private set; }

    public List<Collider> overlappingColliders;

    private BoxCollider col;
    private bool initialized;
    private AssetHandler.ModelLODBehaviour myLOD;
    public float overlapCastingMultiplier = 0.5f;
    private LODMesh[] otherLODMeshes;

    Collider[] GetOverlappingColliders => Physics.OverlapBox(
            transform.position,
            new Vector3(col.size.x * col.transform.lossyScale.x * overlapCastingMultiplier, 999f, col.size.z * col.transform.lossyScale.z * overlapCastingMultiplier),
            transform.rotation,
            LayerMask.GetMask("3D Model"));

    public bool IsPartOfLOD(MeshRenderer mr)
    {
        if (lodGroup == null)
            return false;

        return lodGroup.GetLODs().Any(lod => lod.renderers.Any(r => r == mr));
    }

    public bool RemoveFromLOD(MeshRenderer mr)
    {
        if (lodGroup == null)
            return false;

        var removed = false;
        var lods = lodGroup.GetLODs();
        for(int i =0; i<lods.Length; i++)
        {
            var r = lods[i].renderers.ToList();
            if (r.Remove(mr))
            {
                lods[i].renderers = r.ToArray();
                removed = true;
            }
        }
        return removed;
    }

    void Start()
    {
        col = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        if (col == null && !TryGetComponent<BoxCollider>(out col))
            return;

        //if (!initialized)
        //    AddLODGroup();
    }

    public void AddLODGroup()
    {
        myLOD = GetComponentInParent<AssetHandler.ModelLODBehaviour>();

        if (myLOD == null)
            return;

        var overlappingColliders = GetOverlappingColliders;

        if (overlappingColliders.Count() < NumLODs)
            return;

        otherLODMeshes = FindObjectsByType<LODMesh>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (lodGroup == null)
        {
            var lodTargetGO = new GameObject("LOD target");
            lodTargetGO.transform.parent = transform;
            lodTargetGO.transform.localScale = Vector3.one;
            lodTargetGO.transform.localPosition = col.center;

            lodGroup = lodTargetGO.AddComponent<LODGroup>();
        }

        Dictionary<int, List<MeshRenderer>> lodGroupsRenderers = new();
        Dictionary<int, float> lodScreenVisibleFractions = new();

        foreach (var overlappingCol in overlappingColliders)
        {
            if (!overlappingCol.TryGetComponent(out MeshRenderer mr))
                continue;

            var lodBehaviour = overlappingCol.GetComponentInParent<AssetHandler.ModelLODBehaviour>();
            if (lodBehaviour == null || lodBehaviour.lodIndex == myLOD.lodIndex)
                continue;

            var shouldSkip = false;
            var myOverlap = ComputeOverlapVolume(overlappingCol);//overlappingCol.bounds);
            foreach (var duplicate in otherLODMeshes)//otherLODMeshes.Where(otherLOD => otherLOD.IsPartOfLOD(mr)))
            {
                if (duplicate.ComputeOverlapVolume(overlappingCol) > myOverlap)
                {
                    shouldSkip = true;
                    break;
                }
            }

            if (shouldSkip)
                continue;

            if (otherLODMeshes.Any(o => o.IsPartOfLOD(mr)))
                continue;

            var i = lodBehaviour.lodIndex;

            if (!lodGroupsRenderers.ContainsKey(i))
                lodGroupsRenderers[i] = new List<MeshRenderer>(new[] { mr });
            else
                lodGroupsRenderers[i].Add(mr);

            lodScreenVisibleFractions[i] = lodBehaviour.screenVisibleFraction;// == 0f ? 1f - (1f / NumLODs * (i + 1)) : lodBehaviour.screenVisibleFraction;
        }

        var lods = new LOD[lodScreenVisibleFractions.Count+1];
        lods[lodScreenVisibleFractions.Count] = new LOD(0, new[] { GetComponent<MeshRenderer>() });
        
        var orderedSV = lodScreenVisibleFractions.OrderBy(item => item.Key).Select(item => item.Value).ToList();
        var orderedGR = lodGroupsRenderers.OrderBy(item => item.Key).Select(item => item.Value).ToList();
        for (int i = 0; i < lodScreenVisibleFractions.Count; i++)
        {
            lods[i] = new LOD(orderedSV[i] == 0f ? 1f - (1f / lods.Length * i) : orderedSV[i], orderedGR[i].ToArray());
        }

        lodGroup.SetLODs(lods);
        ResetLODSize(transform.lossyScale.x);

        initialized = true;
    }

    public float ComputeOverlapVolume(Collider colA)//Bounds boxA)
    {
        var boxA = colA.bounds;
        var boxB = col.bounds;
        if (!boxA.Intersects(boxB))
            return 0f;

        float xOverlap = Mathf.Max(0, Mathf.Min(boxA.max.x, boxB.max.x) - Mathf.Max(boxA.min.x, boxB.min.x));
        //float yOverlap = Mathf.Max(0, Mathf.Min(boxA.max.y, boxB.max.y) - Mathf.Max(boxA.min.y, boxB.min.y));
        float zOverlap = Mathf.Max(0, Mathf.Min(boxA.max.z, boxB.max.z) - Mathf.Max(boxA.min.z, boxB.min.z));

        return xOverlap * zOverlap;
    }

    public void ResetLODSize(float modelScale, float multiplier=1f)
    {
        if (lodGroup == null)
            return;

        var camFovFraction = (Application.isEditor ? Camera.current.fieldOfView : Camera.main.fieldOfView) / 90f;

        lodGroup.RecalculateBounds();
        //lodGroup.size *= modelScale * largestWorldSize * 2;
        lodGroup.size *= camFovFraction * 0.5f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (initialized)
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(col.size.x * col.transform.lossyScale.x * overlapCastingMultiplier * 2, 999f, col.size.z * col.transform.lossyScale.z * overlapCastingMultiplier * 2));
            overlappingColliders = GetOverlappingColliders.ToList();
            foreach (var overlappingCol in overlappingColliders)
            {
                if (!overlappingCol.TryGetComponent(out MeshRenderer mr))
                    continue;
                var lodBehaviour = overlappingCol.GetComponentInParent<AssetHandler.ModelLODBehaviour>();
                if (lodBehaviour == null || lodBehaviour.lodIndex == myLOD.lodIndex)
                    continue;

                Gizmos.matrix = overlappingCol.transform.localToWorldMatrix;

                var dontDraw = false;
                var myOverlap = ComputeOverlapVolume(overlappingCol);
                foreach (var duplicate in otherLODMeshes)//.Where(otherLOD => otherLOD.IsPartOfLOD(mr)))
                {
                    var overlap = duplicate.ComputeOverlapVolume(overlappingCol);
                    if (overlap > myOverlap)
                    {
                        UnityEditor.Handles.Label(overlappingCol.transform.TransformPoint(((BoxCollider)overlappingCol).center), $"Mine:{myOverlap} col:{overlap}");
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(((BoxCollider)overlappingCol).center, ((BoxCollider)overlappingCol).size);
                        dontDraw = true;
                        break;
                    }
                }

                //Debug.DrawLine(overlappingCol.transform.position, overlappingCol.transform.position + overlappingCol.transform.up * 0.1f, Color.red);
                if (!dontDraw)
                {
                    Gizmos.color = Color.Lerp(Color.green, Color.blue, lodBehaviour.lodIndex / ((float)NumLODs - 1f));
                    Gizmos.DrawWireCube(((BoxCollider)overlappingCol).center, ((BoxCollider)overlappingCol).size);
                }
            }
        }
    }
#endif
}
