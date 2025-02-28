using System.Collections;
using System.Collections.Generic;
using TMRI.Core;
using UnityEngine;

namespace TMRI.Client
{
    public class PlayerIdentifier : MonoBehaviour
    {
        public string playerID;
        public int team;
        public string imageTarget;
        //public string visibleImageTarget;
        public XRMode mode;

        private void Update()
        {
            //gameObject.SetActive(visibleImageTarget == imageTarget);
        }

    }
}