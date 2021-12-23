using UnityEngine;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Components
{
    public class ConstraintTrackSetup : MonoBehaviour
    {
        public Transform[] bones;
        public Transform[] neutrals;
        public bool ignoreBoneRotation = true;
        public bool ignoreBoneScale = true;

        public SingleConstraintTrack.CtacGizmoDirection gizmoDirection;
        public float gizmoScale = 1f;
    }
}