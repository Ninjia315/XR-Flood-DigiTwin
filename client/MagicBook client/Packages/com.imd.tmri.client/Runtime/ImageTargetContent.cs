using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMRI.Core;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;

namespace TMRI.Client
{
    public class ImageTargetContent : MonoBehaviour
    {
        public ARTrackedImageManager TrackedImageManager;

        [SerializeField]
        Vector3 m_RotationOffset;
        [SerializeField]
        bool ToggleActive;
        [SerializeField]
        bool FrameUpdate;
        [SerializeField]
        string TrackedImageID;
        [SerializeField]
        [Range(0, 1)]
        float Smoothing = 1f;
        [SerializeField]
        bool PersistLocalPose = false;
        [SerializeField]
        UnityEvent OnAdded;
        [SerializeField]
        UnityEvent<string> OnTracked;

        public void RotationX(float value) => m_RotationOffset.x = value;
        public void RotationY(float value) => m_RotationOffset.y = value;
        public void RotationZ(float value) => m_RotationOffset.z = value;
        public bool DoFrameUpdate { get; set; } = true;
        public bool CutRotationInXAndZ;

        private void OnEnable()
        {
            if (TrackedImageManager != null)
                TrackedImageManager.trackablesChanged.AddListener(OnChanged);
        }

        private void OnDisable()
        {
            if (TrackedImageManager != null)
                TrackedImageManager.trackablesChanged.RemoveListener(OnChanged);
        }

        private void Update()
        {
            if (TrackedImageManager == null && FindObjectOfType<ARTrackedImageManager>() is ARTrackedImageManager tim)
            {
                TrackedImageManager = tim;
                OnEnable();
            }
        }

        private void SetActive(bool value)
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(value);
        }

        private void Start()
        {
            //m_RotationOffset += transform.localEulerAngles;

            if (ToggleActive)
            {
                SetActive(false);
            }

            
        }

        void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
        {
            foreach (var newImage in eventArgs.added)
            {
                Debug.Log($"tracked image ADDED: {newImage.referenceImage.name}");

                if (newImage.referenceImage.name != TrackedImageID)
                    continue;

                // Handle added event
                if (ToggleActive)
                    SetActive(true);

                transform.position = newImage.transform.position;
                //transform.rotation = newImage.transform.rotation * Quaternion.Euler(m_RotationOffset);
                transform.eulerAngles = newImage.transform.eulerAngles;
                transform.Rotate(m_RotationOffset, Space.Self);

                OnAdded?.Invoke();
            }
            //evaluator's
            foreach (var updatedImage in eventArgs.updated)
            {
                if (!(FrameUpdate && DoFrameUpdate) || !updatedImage.referenceImage.name.StartsWith(TrackedImageID))
                    continue;

                if (updatedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
                {
                        transform.position = Vector3.Lerp(transform.position, updatedImage.transform.position, Smoothing);
                        transform.eulerAngles = updatedImage.transform.eulerAngles;
                        transform.Rotate(m_RotationOffset, Space.Self);

                    if(CutRotationInXAndZ)
                    {
                        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                    }

                    OnTracked?.Invoke(TrackedImageID);
                }
                //lastTrackingState = updatedImage.trackingState;
            }

            foreach (var removedImage in eventArgs.removed)
            {
                if (removedImage.Value.referenceImage.name != TrackedImageID)
                    continue;

                Debug.Log("tracked image REMOVED");

                // Handle removed event
                if (ToggleActive)
                    gameObject.SetActive(false);
            }

        }


        struct PosAndRot
        {
            public float PosX;
            public float PosY;
            public float PosZ;
            public float RotX;
            public float RotY;
            public float RotZ;
            public float RotW;
        }


        public void SubscribeTrackedImages(bool value)
        {
            TrackedImageManager.trackablesChanged.RemoveListener(OnChanged);

            if (value)
                TrackedImageManager.trackablesChanged.AddListener(OnChanged);
        }

        private void OnDrawGizmos()
        {
            if (TrackedImageManager == null)
                return;

            for (int i = 0; i < TrackedImageManager.referenceLibrary.count; i++)
            {
                var refImg = TrackedImageManager.referenceLibrary[i];
                if (refImg.name == TrackedImageID)
                {
                    var w = refImg.width * .5f;
                    var h = refImg.height * .5f;
                    var topLeft = transform.TransformPoint(-w, 0, h);
                    var topRight = transform.TransformPoint(w, 0, h);
                    var bottomRight = transform.TransformPoint(w, 0, -h);
                    var bottomLeft = transform.TransformPoint(-w, 0, -h);
                    Gizmos.DrawLine(topLeft, topRight);
                    Gizmos.DrawLine(topRight, bottomRight);
                    Gizmos.DrawLine(bottomRight, bottomLeft);
                    Gizmos.DrawLine(bottomLeft, topLeft);
                    break;
                }
            }
        }

    }
}//namespace TMRI.Client
