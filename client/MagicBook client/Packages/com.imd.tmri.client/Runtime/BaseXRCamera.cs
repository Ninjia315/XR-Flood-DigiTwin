using System.Collections;
using System.Linq;
using TMRI.Core;
using UnityEngine;

namespace TMRI.Client
{
    abstract public class BaseXRCamera : MonoBehaviour
    {
        virtual public void OnXRTransitionStart()
        {
            this.ExecuteOnListeners<IBaseXRCameraListener>(listener => listener.OnXRTransitionStart());
        }
        virtual public void EnableAR(float fieldOfView)
        {
            this.ExecuteOnListeners<IBaseXRCameraListener>(listener => listener.OnEnableAR());
        }
        virtual public void EnableVR(float fieldOfView)
        {
            this.ExecuteOnListeners<IBaseXRCameraListener>(listener => listener.OnEnableVR());
        }
        virtual public void SetFoV(float fieldOfView) { }
        virtual public void ToggleStereoMono(float fieldOfView, CameraClearFlags monoCameraClearFlags) { }
        virtual public void SetStereoDisparity(float value) { }
        virtual public Transform GetAnimateableTransform() { return transform; }

        public virtual void Start()
        {
            Debug.Log($"BaseXRCamera {gameObject.name} just Started");

            this.ExecuteOnListeners<IBaseXRCameraListener>(listener => listener.OnBaseXRCamera(this));
        }
    }

    public interface IBaseXRCameraListener
    {
        abstract void OnBaseXRCamera(BaseXRCamera cam);
        abstract void OnEnableAR();
        abstract void OnEnableVR();
        abstract void OnXRTransitionStart();
    }
}