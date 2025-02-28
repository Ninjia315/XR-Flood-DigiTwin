using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShaderChangeEventTrigger : MonoBehaviour, IPointerClickHandler
{
    public Shader ShaderToChangeTo;
    public string TargetEventKey;

    public void BroadcastShaderChange()
    {
        foreach (var listener in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OfType<IShaderChangeEventListener>().Where(mb => mb.EventKey == TargetEventKey))
        {
            listener.OnChangeToShader(ShaderToChangeTo);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        foreach (var listener in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OfType<IShaderChangeEventListener>().Where(mb => mb.EventKey == TargetEventKey))
        {
            listener.OnChangeToShader(ShaderToChangeTo);
        }
    }
}

public interface IShaderChangeEventListener
{
    abstract string EventKey { get; set; }
    abstract void OnChangeToShader(Shader shader);
}