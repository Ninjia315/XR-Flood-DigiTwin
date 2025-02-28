using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TMRI.Core
{
    public class SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(float _x, float _y, float _z)
        {
            x = _x; y = _y; z = _z;
        }
        public override string ToString()
        {
            return $"(x:{x}, y:{y}, z:{z})";
        }
        public static explicit operator SerializableVector3(Vector3 v) => new SerializableVector3(v.x, v.y, v.z);
        public static implicit operator Vector3(SerializableVector3 v) => new Vector3(v?.x??0f, v?.y??0f, v?.z??0f);
    }

    public class SerializableQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public SerializableQuaternion(float _x, float _y, float _z, float _w)
        {
            x = _x; y = _y; z = _z; w = _w;
        }
        public static explicit operator SerializableQuaternion(Quaternion v) => new SerializableQuaternion(v.x, v.y, v.z, v.w);
        public static implicit operator Quaternion(SerializableQuaternion v) => new Quaternion(v.x, v.y, v.z, v.w);
    }

    public class SerializablePose
    {
        public SerializableVector3 Position;
        public SerializableQuaternion Rotation;
        public XRMode Mode;
        public int State;
    }

    public class WebsocketMessage
    {
        public string ID;
        public string imageTarget;
        public string type;
        public string data;
    }

    public enum XRMode
    {
        AR,
        VR
    }

    public class HitData
    {
        public string playerID;
	}

    public class LineDrawing
    {
        public string ID;
        public int Index;
        public List<SerializableVector3> Positions;
    }

    public class ClientInfo
    {
        public string DLink;
        public string ID;
        public string imageTarget;
        public int team;
        public List<LineDrawing> lines;
    }

    public class SerializableLandmark
    {
        public string label;
        public List<int> icons = new();
        public SerializableVector3 position;
        public int level;
        public string referenceAssetId;
    }

    public class SerializableLandmarkType
    {
        public int id;
        public string name;
        public string iconUrl;
    }

    public class Landmark
    {
        public string label;
        public Dictionary<int, Texture2D> icons = new();
        public List<SerializableLandmarkType> iconTypes = new();
        public Vector3 position;
        public int level;
        public string referenceAssetId;
    }

    public class ModelLOD
    {
        public string url;
        public float screenVisibleFraction;
        public SerializableVector3 originLatLonAlt;
        public int lodLevel;
        public int triangles;
    }

    public class SerializablePanorama
    {
        public string multiplePanoramasUrl;
        public SerializableVector3 position;
        public string spherical360ImageUrl;
        public string depthImageUrl;
        public float rotationFromNorthDegrees;
        public SerializableVector3 offsetInVR;
        public string modelUrl;
        public bool invertDepth;
        public bool HasMultiplePanoramas => !string.IsNullOrEmpty(multiplePanoramasUrl);
    }

    public class SerializableAsset
    {
        public string id;
        public float initialScale;
        public float conversionValue = 1f; //for conversion relation to Unity units e.g. Scale: 1 (unity unit = 1m in reality): x(meters in the model)
        public float lodMultiplier = 1f;
        public List<ModelLOD> models;
        public SerializableVector3 originLatLonAlt;
        public SerializableVector3 minLatLonAlt;
        public SerializableVector3 maxLatLonAlt;
        public List<AssetPlatformLink> assetBundles;
        public List<SerializableLandmark> landmarks;
        public List<SerializablePanorama> panoramas;
        public List<SerializableGeoImage> geoImages;
        public GeoData geoData;
        public string landmarkListUrl;

        public bool HasModelLODs => models != null && models.Count > 0;
    }

    public class SerializableGeoImage
    {
        public string id;
        public SerializableVector3 minLatLon;
        public SerializableVector3 maxLatLon;
        public string imageUrl;
        public string legendImageUrl;
        public float opacity;
    }

    public class SerializableTrackingmarker
    {   
        public string name;
        public string defaultToLoad;
        public string link;
        public float printwidth;
    }

    public class SerializableConfigFile
    {
        public string ipAddress;
        public SerializableVector3 display_size;
        public List<SerializableTrackingmarker> markers;
        public List<SerializableAsset> assets;
        public List<SerializableLandmarkType> landmarkTypes = new();
    }

    public class AssetPlatformLink
    {
        public List<string> platforms;
        public string url;
        public uint crc;
        public string hash;
    }

    public class GeoData
    {
        public List<string> dataUrls = new();
        public int numFiles = 1;
        public int fileStartIndex = 0;
        public float scale = 1.0f;
        public string filenameFormat = "BP066_{0:000#}0m.csv";
        public char delimiter = ',';
        public int numColumns;
        public int dataValueColumnIndex;
        public int latLngShapeStartColumnIndex;
        public int elevationColumnIndex;
        public float xDegreesOffset;
    }

    public enum FloodVisualizationType
    {
        Realistic,
        Depth,
        Speed,
        Realistic_Alt,
        Depth_Alt,
    }

    public class FloodVisualizationData
    {
        public FloodVisualizationType FloodType;
        public bool ShowAffectedBuildings;
        public int AnimationStep;
        public float AnimationSpeedSeconds;
        public bool IsPlaying;
        public int MaxAnimationStep;
    }

    public class GeoImageVisualizationData
    {
        public string ShowGeoImageId;
        public float GeoImageOpacity;
    }

    [Serializable]
    public class FloodShader
    {
        public FloodVisualizationType FloodType;
        public Material FloodMaterial;
    }

    public class PositionAndFloodInfo
    {
        public SerializableVector3 position;
        public float depth;
        public float speed;
        public float altitude;
        public bool enabled;
    }

} //namespace TMRI.Core
