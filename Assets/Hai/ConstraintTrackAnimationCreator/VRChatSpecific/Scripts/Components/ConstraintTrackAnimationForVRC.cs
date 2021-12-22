using System;
using Hai.ConstraintTrackAnimationCreator.Scripts.Components;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Components
{
    public class ConstraintTrackAnimationForVRC : MonoBehaviour
    {
        public ConstraintTrackAnimation constraintTrackAnimation;
        public VRCAvatarDescriptor avatar;
        public string layerName;
        public CtacVRCFloatType floatType = CtacVRCFloatType.Simple;
        public bool includeSmoothing;

        [Serializable]
        public enum CtacVRCFloatType
        {
            Simple,
            Complex,
            Manual
        }
    }
}