using System;
using UnityEngine;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Components
{
    public class BoneDetachTool : MonoBehaviour
    {
        public SkinnedMeshRenderer skinnedMesh;
        public bool enableDetachEditor = true;
        public Detachment[] detachments = new Detachment[0];

        [Serializable]
        public struct Detachment
        {
            public Transform original;
            public Transform detached;
        }
    }
}
