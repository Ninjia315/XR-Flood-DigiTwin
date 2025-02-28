using System.Collections;
using TMRI.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.InputSystem.XR;

namespace TMRI.Client
{
    [RequireComponent(typeof(Camera))]
    public class ToggleXRMode : MonoBehaviour
    {
        public bool UseGravityInVR;
        public GameObject SavedVRPositionInARPrefab;
        public float TransitionTime = 2f;

        private GameObject arContainer;
        private GameObject vrContainer;
        private int currentlyLoadedSceneIndex;
        private GameObject currentlyLoadedCaller;
        public GameObject savedVRPosition;
        private Vector3 savedARPosition;
        public float fieldOfView = 75;
        private int mySceneIndex;
        private bool isLoadingScene;

        const string SCREEN_DISPARITY_KEY = "ScreenDisparity";
        const string FIELD_OF_VIEW_KEY = "FieldOFView";

        public Transform GetActiveXRModeTransform()
        {
            if (mode == XRMode.AR)
            {
                return arContainer.transform.GetChild(0);
            }
            else if (vrContainer != null && vrContainer.transform.childCount > 0)
            {
                return vrContainer.transform.GetChild(0);
            }

            return null;
        }

        public bool SaveAndApplyCameraSettings;
        public bool IsXRReady => arContainer != null && vrContainer != null;
        public XRMode mode { get; private set; } = XRMode.AR;
        public bool IsLookingUp =>
#if UNITY_EDITOR
            true;
#else
            Vector3.Angle(transform.forward, Vector3.up) < 30;
#endif

        private void OnEnable()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
        }

        // Start is called before the first frame update
        void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            mySceneIndex = currentlyLoadedSceneIndex;

            SceneManager_sceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

            if(SaveAndApplyCameraSettings)
                SetSavedDisparityAndFov();
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            Debug.Log($"Scene {scene.buildIndex} ({scene.name}) was loaded.");
            currentlyLoadedSceneIndex = scene.buildIndex;

            arContainer = GameObject.FindWithTag("AR container");
            vrContainer = GameObject.FindWithTag("VR container");

            if (arContainer != null && arContainer.transform.childCount > 0)
                ToggleARVR(XRMode.AR);
        }

        private void SetSavedDisparityAndFov()
        {
            if (PlayerPrefs.HasKey(SCREEN_DISPARITY_KEY))
                ScaleScreenDisparity(PlayerPrefs.GetFloat(SCREEN_DISPARITY_KEY));
            if (PlayerPrefs.HasKey(FIELD_OF_VIEW_KEY))
                ScaleFieldOfView(PlayerPrefs.GetFloat(FIELD_OF_VIEW_KEY));
        }


        public void ToggleStereoMono()
        {
            var flags = mode == XRMode.AR ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
            FindFirstObjectByType<BaseXRCamera>().ToggleStereoMono(fieldOfView, flags);
        }

        public void ToggleARVR()
        {
            var newMode = mode == XRMode.AR ? XRMode.VR : XRMode.AR;

            if (GetActiveXRModeTransform() == null)
            {
                // No scene loaded yet or the scene does not have the requested mode. Revert
                return;
            }

            ToggleARVR(newMode);
        }

        public void Reset()
        {
            if (mode == XRMode.VR)
                ToggleARVR(XRMode.AR);

            savedARPosition = Vector3.zero;

            if (savedVRPosition != null && vrContainer != null)
                savedVRPosition.transform.localPosition = vrContainer.transform.GetChild(0).InverseTransformPoint(Vector3.zero);
        }

        public void TransitionToVR(Vector3 vrWorldPos = new Vector3(), Vector3 arWorldPos = new Vector3())
        {
            if (this.mode == XRMode.VR)
                return;

            var XRCam = FindFirstObjectByType<BaseXRCamera>();

            if (XRCam != null)
                XRCam.OnXRTransitionStart();
                
            savedARPosition = arContainer.transform.GetChild(0).InverseTransformPoint(transform.position);

            if (vrWorldPos == Vector3.zero && savedVRPosition != null)
            {
                vrWorldPos = vrContainer.transform.GetChild(0).TransformPoint(savedVRPosition.transform.localPosition);
                Debug.Log($"Going into VR SAVED position {vrWorldPos}");
                Destroy(savedVRPosition);
            }

            Action onComplete = () =>
            {
                if (XRCam != null)
                    XRCam.EnableVR(fieldOfView: 120);

                foreach (Transform child in vrContainer.transform)
                    child.gameObject.SetActive(true);

                arContainer.transform.GetChild(0).gameObject.SetActive(false);

                var rigidBody = GetComponent<Rigidbody>();

                if (UseGravityInVR)
                {
                    rigidBody.useGravity = true;
                }
            };

            StartCoroutine(EnterVR(vrWorldPos, arWorldPos, onComplete, XRCam));

            this.mode = XRMode.VR;
        }

        public void TransitionToAR()
        {
            var XRCam = FindFirstObjectByType<BaseXRCamera>();

            if (XRCam != null)
                XRCam.OnXRTransitionStart();

            if (savedVRPosition == null)
            {
                if (SavedVRPositionInARPrefab != null)
                    savedVRPosition = Instantiate(SavedVRPositionInARPrefab, arContainer.transform.GetChild(0));
                else
                {
                    savedVRPosition = new GameObject("Saved VR Position");
                    savedVRPosition.transform.parent = arContainer.transform.GetChild(0);
                }
            }

            if (this.mode == XRMode.VR)
                savedVRPosition.transform.localPosition = vrContainer.transform.GetChild(0).InverseTransformPoint(transform.position);
            else
                savedVRPosition.transform.localPosition = vrContainer.transform.GetChild(0).InverseTransformPoint(Vector3.zero);

            Action onComplete = () =>
            {
                if (XRCam != null)
                    XRCam.EnableAR(fieldOfView);

                foreach (Transform child in vrContainer.transform)
                    child.gameObject.SetActive(false);

                if (GetComponentInChildren<GazeMoveWithCollision>(includeInactive: true) is GazeMoveWithCollision gmc)
                {
                    gmc.enabled = false;
                    if (gmc.GetComponent<Collider>() is Collider c)
                        c.enabled = false;
                }

                arContainer.transform.GetChild(0).gameObject.SetActive(true);
            };

            var rigidBody = GetComponent<Rigidbody>();
            rigidBody.isKinematic = true;
            if (UseGravityInVR)
            {
                rigidBody.useGravity = false;
            }

            onComplete();
            XRCam.GetAnimateableTransform().position = savedARPosition;

            this.mode = XRMode.AR;
        }

        private void ToggleARVR(XRMode mode)
        {
            Debug.Log($"Going into {mode} mode");

            if (mode == XRMode.VR)
            {
                TransitionToVR();
            }
            else if (mode == XRMode.AR)
            {
                TransitionToAR();
            }

        }

        public void SetCaller(GameObject caller)
        {
            if (caller == null)
                return;

            if (currentlyLoadedCaller != null)
                currentlyLoadedCaller.SetActive(true);
            currentlyLoadedCaller = caller;
            currentlyLoadedCaller.SetActive(false);
        }


        public void SwitchScene(int sceneBuildIndex)
        {
            if (!isLoadingScene)
                StartCoroutine(SwitchSceneAsync(sceneBuildIndex));
        }

        private IEnumerator SwitchSceneAsync(int sceneBuildIndex)
        {
            if (currentlyLoadedSceneIndex == sceneBuildIndex)
                yield break;

            isLoadingScene = true;

            if (currentlyLoadedSceneIndex != mySceneIndex)
            {
                var unloadOperation = SceneManager.UnloadSceneAsync(currentlyLoadedSceneIndex);
                while (!unloadOperation.isDone)
                    yield return null;
            }

            if (sceneBuildIndex >= 0)
            {
                var loadOperation = SceneManager.LoadSceneAsync(sceneBuildIndex, LoadSceneMode.Additive);

                while (!loadOperation.isDone)
                    yield return null;
            }
            else
            {
                // Manual force switch back to AR mode
                if (TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.useGravity = false;
                    rb.isKinematic = true;
                }

                var XRCam = FindFirstObjectByType<BaseXRCamera>();
                XRCam.EnableAR(fieldOfView);

                if (GetComponentInChildren<GazeMoveWithCollision>(includeInactive: true) is GazeMoveWithCollision gmc)
                {
                    gmc.enabled = false;
                    if (gmc.GetComponent<Collider>() is Collider c)
                        c.enabled = false;
                }

                if (savedVRPosition != null)
                    Destroy(savedVRPosition);

                currentlyLoadedSceneIndex = mySceneIndex;
            }

            isLoadingScene = false;
        }

        public void ScaleScreenDisparity(float value)
        {
            if (FindFirstObjectByType<BaseXRCamera>() is BaseXRCamera sc)
                sc.SetStereoDisparity(value);

            PlayerPrefs.SetFloat(SCREEN_DISPARITY_KEY, value);
        }

        public void ScaleFieldOfView(float value)
        {
            fieldOfView = (value + 0.5f) * 100;

            FindFirstObjectByType<BaseXRCamera>().SetFoV(fieldOfView);

            PlayerPrefs.SetFloat(FIELD_OF_VIEW_KEY, value);
        }


        private IEnumerator EnterVR(Vector3 vrWorldPosition, Vector3 arWorldPosition, Action doOnComplete, BaseXRCamera XRCam)
        {
            var trans = XRCam.GetAnimateableTransform();
            
            var goalInsideTransform = arWorldPosition;
            var dirInsideTransform = arContainer.transform.GetChild(0).TransformDirection(transform.forward);
            var startPos = transform.position;
            var startRot = transform.rotation;
            var startTime = Time.time;
            var transitionTime = Vector3.Distance(transform.position, goalInsideTransform) * TransitionTime;
            Debug.Log("Start EnterVR");
            while ((transform.position - goalInsideTransform).sqrMagnitude > 0.0001f && (Time.time - startTime) <= transitionTime && (Time.time - startTime) < 5f)
            {
                var t = (Time.time - startTime) / transitionTime;
                var targetPosition = Vector3.Lerp(startPos, goalInsideTransform, t);

                if (trans != transform && transform.IsChildOf(trans))
                {
                    // Animating a parent of the XRcamera
                    var parent = trans;
                    var child = transform;
                    var offset = child.position - parent.position;
                    parent.position = targetPosition - offset;
                }
                else
                {
                    transform.position = targetPosition;
                }

                transform.rotation = Quaternion.Lerp(startRot, Quaternion.LookRotation(dirInsideTransform), t);

                yield return null;
            }

            if (trans != transform && transform.IsChildOf(trans))
            {
                // Animating a parent of the XRcamera
                var parent = trans;
                var child = transform;
                var offset = child.position - parent.position;
                parent.position = vrWorldPosition - offset;
            }
            else
            {
                trans.position = vrWorldPosition;
            }

            doOnComplete();

            Debug.Log("Finished EnterVR");
        }

        private IEnumerator ExitVR(Vector3 goalPosition, Action doOnComplete, BaseXRCamera XRCam)
        {
            var trans = XRCam.GetAnimateableTransform();
            var startPos = trans.position;
            var startTime = Time.time;
            var transitionTime = (Vector3.Distance(trans.position, goalPosition) / 150f) * TransitionTime;
            Debug.Log("Start ExitVR");
            while ((trans.position - goalPosition).sqrMagnitude > 0.0001f && (Time.time - startTime) <= transitionTime && (Time.time - startTime) < 5f)
            {
                var t = (Time.time - startTime) / transitionTime;
                trans.position = Vector3.Lerp(startPos, goalPosition, t);
                //vrContainer.transform.localScale = Vector3.Slerp(startScale, startScale * 0.01f, t);
                yield return null;
            }

            doOnComplete();
            yield return null;

            trans.position = goalPosition;

            Debug.Log("Finished ExitVR");
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (GUI.Button(new Rect(Screen.width * .5f, Screen.height * .5f, 100, 25), "Toggle XR"))
            {
                if(Camera.main != null && Camera.main.GetComponent<TrackedPoseDriver>() is TrackedPoseDriver tpd)
                    tpd.enabled = false;

                ToggleARVR();
            }
        }
#endif
    }

}//namespace TMRI.Client