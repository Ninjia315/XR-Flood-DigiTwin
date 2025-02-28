using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMRI.Core;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace TMRI.Client
{
    public class RuntimeImageTarget : MonoBehaviour
    {

        ARTrackedImageManager m_TrackedImageManager;

        public Vector3 m_RotationOffset;
        public bool ToggleActive;
        public bool FrameUpdate;
        public string TrackedImageID;
        public Texture2D trackedImageTexture;
        public float trackedImageSize;
        [Range(0, 1)]
        public float Smoothing = 1f;

        public UnityEvent OnAdded;
        public UnityEvent OnTracking;
        public UnityEvent OnRemoved;

        bool runtimeImageAdded;
        AddReferenceImageJobState addJob;

        private void OnEnable()
        {
            if (m_TrackedImageManager != null)
            {
                /// Creating a runtime library is only supported when there's a valid tracking subsystem in ARFoundation,
                /// which is not the case when running in Editor mode.
#if !UNITY_EDITOR
                if(m_TrackedImageManager.referenceLibrary is not MutableRuntimeReferenceImageLibrary)
                    m_TrackedImageManager.referenceLibrary = m_TrackedImageManager.CreateRuntimeLibrary();
#endif

                m_TrackedImageManager.trackedImagesChanged += OnChanged;
            }
        }

        private void OnDisable()
        {
            if (m_TrackedImageManager != null)
                m_TrackedImageManager.trackedImagesChanged -= OnChanged;
        }

        private void Update()
        {
            if (m_TrackedImageManager == null && FindFirstObjectByType<ARTrackedImageManager>() is ARTrackedImageManager tim)
            {
                m_TrackedImageManager = tim;
                OnEnable();
            }
            else if(m_TrackedImageManager != null && !runtimeImageAdded)
            {
                var success = AddImageToTrack(trackedImageTexture, TrackedImageID, trackedImageSize);
                if (success)
                {
                    runtimeImageAdded = true;
                    Debug.Log($"RuntimeImageTarget: successfully added '{TrackedImageID}' with size {trackedImageSize}");
                }
            }

            if (addJob != null && !(addJob.status == AddReferenceImageJobStatus.Success || addJob.status == AddReferenceImageJobStatus.None))
                Debug.Log($"AddJob status {addJob.status}");
        }

        bool AddImageToTrack(Texture2D imageToAdd, string imageID, float realSize)
        {
            if (!(ARSession.state == ARSessionState.SessionInitializing || ARSession.state == ARSessionState.SessionTracking))
            {
                //Debug.Log($"Session state is {ARSession.state}");
                return false; // Session state is invalid
            }

            if (m_TrackedImageManager.referenceLibrary is MutableRuntimeReferenceImageLibrary mutableLibrary)
            {
                addJob = mutableLibrary.ScheduleAddImageWithValidationJob(imageToAdd, imageID, realSize);
            }
            else
            {
                Debug.Log("TrackedImageManager reference library is not mutable!");
                return false;
            }
            return true;
        }

        private void SetActive(bool value)
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(value);
        }

        private void Start()
        {
            if (ToggleActive)
            {
                SetActive(false);
            }
        }

        void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
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
                if (!FrameUpdate || !updatedImage.referenceImage.name.StartsWith(TrackedImageID))
                    continue;

                if (updatedImage.trackingState == TrackingState.Tracking)
                {
                    transform.position = Vector3.Lerp(transform.position, updatedImage.transform.position, Smoothing);
                    transform.eulerAngles = updatedImage.transform.eulerAngles;
                    transform.Rotate(m_RotationOffset, Space.Self);

                    OnTracking?.Invoke();
                }
            }

            foreach (var removedImage in eventArgs.removed)
            {
                if (removedImage.referenceImage.name != TrackedImageID)
                    continue;

                Debug.Log("tracked image REMOVED");

                // Handle removed event
                if (ToggleActive)
                    gameObject.SetActive(false);

                OnRemoved?.Invoke();
            }
        }

        private void OnDrawGizmos()
        {
            if (m_TrackedImageManager == null)
                return;

            for (int i = 0; i < m_TrackedImageManager.referenceLibrary.count; i++)
            {
                var refImg = m_TrackedImageManager.referenceLibrary[i];
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
