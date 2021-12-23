using System;
using Hai.ConstraintTrackAnimationCreator.Scripts.Components;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Components
{
    public class ConstraintTrackVRCGenerator : MonoBehaviour
    {
        public ConstraintTrackAnimation constraintTrackAnimation;
        public VRCAvatarDescriptor avatar;
        public string trackName;
        public CtacVRCFloatType floatType = CtacVRCFloatType.Manual;
        public bool manualIncludeSmoothing;

        public bool systemIsAllowedByDefault = true;
        public string optionalAllowSystemParamName = "";

        [Serializable]
        public enum CtacVRCFloatType
        {
            Manual
        }
    }
}