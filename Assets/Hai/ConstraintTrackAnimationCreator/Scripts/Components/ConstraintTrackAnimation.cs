using System;
using UnityEngine;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Components
{
    public class ConstraintTrackAnimation : MonoBehaviour
    {
        public SingleConstraintTrack[] tracks;
        public TrackTiming[] timings;
        public AnimationClip optionalAnimationActive;
        public AnimationClip optionalAnimationInactive;

        [Serializable]
        public struct TrackTiming
        {
            public float delayStartSeconds;
            public float scale;
        }
    }
}