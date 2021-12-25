using System;
using Hai.ConstraintTrackAnimationCreator.Scripts.Components;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Components
{
    public class ConstraintTrackVRCGenerator : MonoBehaviour
    {
        public ConstraintTrackAnimation constraintTrackAnimation;
        public VRCAvatarDescriptor avatar;
        public string layerName;
        public string parameterPrefixName;

        [Header("Avatar Dynamics Mode")]
        public string customParameter;

        [Header("Automatic Mode")]
        public float autoDurationSeconds = 5f;

        [Header("Manual Mode")]
        public bool manualIncludeSmoothing = true;
        public float smoothingFactor = 0.8f;

        [Header("System Lock")]
        public bool systemIsAllowedByDefault = true;
        public string optionalAllowSystemParamName = "";

        [HideInInspector] public AnimatorController assetHolder;
        [HideInInspector] public string assetKey = Guid.NewGuid().ToString();
    }
}