﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MobfishCardboard
{
    public class CardboardHeadTransform: MonoBehaviour
    {
        [SerializeField]
        private Transform targetTransform;

        private void Awake()
        {
            if (targetTransform == null)
                targetTransform = GetComponent<Transform>();

            if (Application.isEditor)
                enabled = false;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (!CardboardManager.enableVRView || Application.isEditor)
                return;

            CardboardHeadTracker.UpdatePose();
            //targetTransform.localPosition = CardboardHeadTracker.trackerUnityPosition;
            targetTransform.localRotation = CardboardHeadTracker.trackerUnityRotation;
        }
    }
}