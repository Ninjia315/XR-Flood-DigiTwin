using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace TMRI.Client
{
    public class JoyConPointer : MonoBehaviour
    {
        public float MaxInteractionDistance;
        public LayerMask InteractionLayers;
        public UnityEvent<bool> GazeDidHit;
        public UnityEvent<Material> GazeHitWithMaterial;
        //public bool InvertXAxis;

        int touchDownFrame;
        bool startedDrag;


        // Update is called once per frame
        void Update()
        {
            if (!MixedInput.HasInstance)
                return;

            //MixedInput.Invert = InvertXAxis;

#if UNITY_EDITOR
            var wp = Camera.main.ScreenToWorldPoint(new Vector3(Mouse.current.position.x.value, Mouse.current.position.y.value, 0.5f));
            var wr = Quaternion.LookRotation(Camera.main.transform.up, -Camera.main.transform.forward);
            transform.SetPositionAndRotation(wp, wr);
#endif

            if (MixedInput.ActionDown)
            {
                touchDownFrame = Time.frameCount;
            }

            var holdingDown = MixedInput.ActionHeld && (Time.frameCount - touchDownFrame > 10);
            var tapped = (Time.frameCount - touchDownFrame < 10) && MixedInput.ActionUp;
            var raycastHit = Physics.Raycast(transform.position, -transform.up, out RaycastHit hitInfo, MaxInteractionDistance, InteractionLayers.value);

            GazeDidHit?.Invoke(raycastHit);

            if (!raycastHit)
                return;

            if (hitInfo.collider.gameObject.GetComponentInParent<ReticlePointerInteractable>() is ReticlePointerInteractable rpi && rpi != null)
            {
                if (rpi.GazeMaterial != null)
                    GazeHitWithMaterial?.Invoke(rpi.GazeMaterial);
            }

            if (holdingDown && startedDrag)
            {
                hitInfo.collider.gameObject.SendMessageUpwards(nameof(ReticlePointerInteractable.OnDrag), hitInfo);
            }
            if (holdingDown && !startedDrag)
            {
                startedDrag = true;
                hitInfo.collider.gameObject.SendMessageUpwards(nameof(ReticlePointerInteractable.OnDown), hitInfo);
            }
            if (MixedInput.SecondaryActionUp)
            {
                Debug.Log("JoyCon: Tapped");
                hitInfo.collider.gameObject.SendMessageUpwards(nameof(ReticlePointerInteractable.OnTapped), hitInfo);
            }
            if (MixedInput.ActionUp)
            {
                startedDrag = false;
                hitInfo.collider.gameObject.SendMessageUpwards(nameof(ReticlePointerInteractable.OnUp), hitInfo);
            }
        }
    }
}//namespace TMRI.Client