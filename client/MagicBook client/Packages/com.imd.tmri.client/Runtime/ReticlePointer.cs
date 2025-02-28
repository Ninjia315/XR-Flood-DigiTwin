using System.Collections;
using System.Collections.Generic;
using TMRI.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace TMRI.Client
{
    public class ReticlePointer : MonoBehaviour
    {
        public float MaxInteractionDistance;
        public LayerMask InteractionLayers;
        public UnityEvent<bool> GazeDidHit;
        public ToggleXRMode toggleXR;
        public TMRIPlayer networkClient;
        public bool GazeHoldIsDrag;
        [SerializeField]
        XRModeMask ActiveInMode;
        [SerializeField]
        float TapThresholdSeconds = 0.15f;

        float touchDownTime;
        GameObject hitPointGO;

        [System.Flags]
        enum XRModeMask
        {
            None = 0,
            AR = 1,
            VR = 2
        }

        // Update is called once per frame
        void Update()
        {
            if (!ActiveInMode.HasFlag((XRModeMask)(toggleXR.mode+1)))
            {
                GazeDidHit?.Invoke(false);
                return;
            }

            if (MixedInput.ActionDown)
            {
                touchDownTime = Time.time;
            }

            var deltaTime = Time.time - touchDownTime;
            var holdingDown = (MixedInput.ActionHeld || GazeHoldIsDrag) && (deltaTime > TapThresholdSeconds);
            var tapped = (deltaTime < TapThresholdSeconds) && MixedInput.ActionUp;
            var outsideUI = !EventSystem.current.IsPointerOverGameObject();
            var raycastHit = Physics.Raycast(transform.position, transform.forward, out RaycastHit hitInfo, MaxInteractionDistance, InteractionLayers.value);

            if (hitPointGO == null)
            {
                hitPointGO = new GameObject("hit point");
                hitPointGO.transform.parent = transform;
            }

            if (raycastHit)
                hitPointGO.transform.position = hitInfo.point;

            //Debug.Log($"ReticlePointer holdingDown: {holdingDown} tapped: {tapped} outsideUI: {outsideUI} raycastHit: {raycastHit}");
            if (tapped && outsideUI && raycastHit)
            {
                hitInfo.collider.gameObject.SendMessageUpwards(nameof(ReticlePointerInteractable.OnTapped), hitInfo, SendMessageOptions.DontRequireReceiver);
            }

            if (holdingDown && raycastHit)
            {
                hitInfo.collider.gameObject.SendMessageUpwards(nameof(ReticlePointerInteractable.OnDrag), hitInfo, SendMessageOptions.DontRequireReceiver);
            }

            if (!raycastHit && ActiveInMode.HasFlag(XRModeMask.VR) && holdingDown)
                networkClient.UserState = TMRIPlayer.AnimationState.Waving;
            else if (ActiveInMode.HasFlag(XRModeMask.VR) && (MixedInput.ActionUp || raycastHit)
                && networkClient.UserState == TMRIPlayer.AnimationState.Waving)
                networkClient.UserState = TMRIPlayer.AnimationState.Idle;

            GazeDidHit?.Invoke(raycastHit);
        }
    }

    public interface ReticlePointerInteractable
    {
        void OnTapped(RaycastHit hitInfo);
        void OnDrag(RaycastHit hitInfo);
        void OnDown(RaycastHit hitInfo);
        void OnUp(RaycastHit hitInfo);
        Material GazeMaterial { get; set; }
    }

}//namespace TMRI.Client