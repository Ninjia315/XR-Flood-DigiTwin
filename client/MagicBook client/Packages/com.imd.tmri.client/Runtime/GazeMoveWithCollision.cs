using System.Collections;
using System.Collections.Generic;
using TMRI.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TMRI.Client
{
    public class GazeMoveWithCollision : MonoBehaviour
    {
        public float MoveSpeed = 1f;
        public Transform TransformToMove;
        public MoveMode mode;
        public TMRIPlayer networkClient;

        //float touchDownFrame;
        //bool pointerClick;

        public enum MoveMode
        {
            GazeDirectionButton,
            Joystick
        }

        public void OnPointerClick(BaseEventData eventData)
        {
            //pointerClick = true;
        }

        private void Start()
        {
            if (TransformToMove == null)
                TransformToMove = transform;
#if UNITY_EDITOR
            mode = MoveMode.GazeDirectionButton;
#endif
        }

        // Update is called once per frame
        void Update()
        {
            var doMove = false;
            var dir = Vector3.zero;
            //networkClient.UserState = MagicBookClient.AnimationState.Idle;

            if (mode == MoveMode.GazeDirectionButton)
            {
                if (MixedInput.ActionDown)
                {
                    //touchDownFrame = Time.frameCount;
                    //pointerClick = false;
                }

                doMove = MixedInput.ActionHeld
#if UNITY_EDITOR
                    && UnityEngine.InputSystem.Keyboard.current.altKey.isPressed
#endif
                 && true;//(Time.frameCount - touchDownFrame > 10) && !pointerClick;

                dir = TransformToMove.forward;
            }
            else if (mode == MoveMode.Joystick)
            {
                doMove = MixedInput.AxisDir.sqrMagnitude > 0f;
                dir = TransformToMove.TransformDirection(new Vector3(MixedInput.AxisDir.x, 0f, MixedInput.AxisDir.y));
            }

            if (doMove)
            {
                dir.y = 0f;
                dir.Normalize();
                TransformToMove.Translate(dir * MoveSpeed * Time.deltaTime, Space.World);
                networkClient.UserState = TMRIPlayer.AnimationState.Walking;
            }
            else if (networkClient.UserState == TMRIPlayer.AnimationState.Walking)
                networkClient.UserState = TMRIPlayer.AnimationState.Idle;

            if (MixedInput.SecondaryActionUp)
            {
                var attackable = GetComponentInChildren<IAttackable>(includeInactive: false);
                if (attackable != null)
                    attackable.StopAttack();
            }
            else if (MixedInput.SecondaryActionDown)
            {
                var attackable = GetComponentInChildren<IAttackable>(includeInactive: false);
                if (attackable != null)
                    attackable.StartAttack();
            }
            else if (MixedInput.SecondaryActionHeld)
            {
                var attackable = GetComponentInChildren<IAttackable>(includeInactive: false);
                if (attackable != null)
                    attackable.UpdateAttack();
                networkClient.UserState = TMRIPlayer.AnimationState.IceBall;
            }
            else if (MixedInput.SecondaryActionUp && networkClient.UserState == TMRIPlayer.AnimationState.IceBall)
                networkClient.UserState = TMRIPlayer.AnimationState.Idle;
        }
    }
}//namespace TMRI.Client