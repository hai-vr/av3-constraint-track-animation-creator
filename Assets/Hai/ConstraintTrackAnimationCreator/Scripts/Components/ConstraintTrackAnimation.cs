using UnityEngine;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Components
{
    public class ConstraintTrackAnimation : MonoBehaviour
    {
        public SingleConstraintTrack[] tracks;
        public GameObject parentOfAllTracks;
        public AnimationClip optionalAnimationActive;
        public AnimationClip optionalAnimationInactive;

        [HideInInspector] public float globalTimingScale = 10f;
    }
}