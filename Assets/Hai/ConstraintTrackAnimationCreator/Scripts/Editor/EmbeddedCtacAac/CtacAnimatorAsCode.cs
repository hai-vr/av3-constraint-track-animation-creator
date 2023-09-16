using System;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac
{
    public class CtacAacComp
    {
        public VRCAvatarDescriptor avatar;
        public AnimatorController assetHolder;
        public string layerNameSuffix;
        public string parameterName;

        public AacInternal _internal;
    }

    [Serializable]
    public struct AacInternal
    {
        public bool created;
        public string assetKey;
        public int createdWithMajorVersion;
    }
}