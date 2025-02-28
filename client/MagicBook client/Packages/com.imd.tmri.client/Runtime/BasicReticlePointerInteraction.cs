using System.Collections;
using System.Collections.Generic;
using TMRI.Client;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BasicReticlePointerInteraction : MonoBehaviour, ReticlePointerInteractable, IPointerClickHandler
{
    public UnityEvent OnInteraction;
    public Button InteractionButton;

    public float DragSecondsTillInteraction = 1f;

    public Material GazeMaterial { get; set; }

    float lastDragStart;
    float lastDragTime;

    public void OnDown(RaycastHit hitInfo)
    {
        
    }

    public void OnDrag(RaycastHit hitInfo)
    {
        if (lastDragStart == 0f)
            lastDragStart = Time.time;

        lastDragTime = Time.time;

        var pointer = new PointerEventData(EventSystem.current);

        if (InteractionButton != null)
            ExecuteEvents.Execute(InteractionButton.gameObject, pointer, ExecuteEvents.pointerDownHandler);

        if (Time.time - lastDragStart > DragSecondsTillInteraction)
        {
            lastDragStart = 0f;
            var executedClick = false;

            if (InteractionButton != null)
            {
                ExecuteEvents.Execute(InteractionButton.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                if(InteractionButton.onClick.GetPersistentEventCount() > 0)
                {
                    InteractionButton.onClick.Invoke();
                    executedClick = true;
                }
            }

            if(!executedClick)
                OnInteraction?.Invoke();
        }
    }

    public void OnTapped(RaycastHit hitInfo)
    {
        var pointer = new PointerEventData(EventSystem.current);

        if (InteractionButton != null)
            ExecuteEvents.Execute(InteractionButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);

        OnInteraction?.Invoke();
    }

    public void OnUp(RaycastHit hitInfo)
    {
        
    }

    private void Update()
    {
        if(Time.time - lastDragTime > DragSecondsTillInteraction)
            lastDragStart = 0f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (InteractionButton == null)
            OnInteraction?.Invoke();
    }
}
