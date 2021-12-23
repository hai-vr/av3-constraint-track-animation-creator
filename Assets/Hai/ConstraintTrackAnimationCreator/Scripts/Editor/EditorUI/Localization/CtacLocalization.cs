using System;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI.Localization
{
    public class CtacLocalization
    {
        public enum Phrase
        {
            UpdateConstraintTrack,
            UpdateAllConstraintTracks,
            RegenerateAnimator,
            CreateNeutralObjects,
            ConfirmSetup,
            DetachBone,
            ShowBonesDetachEditor,
            DetachedBones,
            ApplyAgain,
            RevertWithoutRemoving,
            FixModelUpdate,
            Advanced,
            RevertPrefabBonesArray
        }

        public static string Localize(Phrase phrase)
        {
            switch (phrase)
            {
                case Phrase.UpdateConstraintTrack:
                    return "Update Constraint Track";
                case Phrase.UpdateAllConstraintTracks:
                    return "Update All Constraint Tracks";
                case Phrase.RegenerateAnimator:
                    return "Regenerate Animator";
                case Phrase.CreateNeutralObjects:
                    return "Create Neutral Objects";
                case Phrase.ConfirmSetup:
                    return "Confirm Setup";
                case Phrase.DetachBone:
                    return "Detach Bone";
                case Phrase.ShowBonesDetachEditor:
                    return "Show Bones Detach Editor";
                case Phrase.DetachedBones:
                    return "Detached Bones";
                case Phrase.ApplyAgain:
                    return "Apply Again";
                case Phrase.RevertWithoutRemoving:
                    return "Revert Without Removing";
                case Phrase.FixModelUpdate:
                    return "Fix Model Update";
                case Phrase.Advanced:
                    return "Advanced";
                case Phrase.RevertPrefabBonesArray:
                    return "Revert Prefab Bones Array";
                default:
                    throw new ArgumentOutOfRangeException(nameof(phrase), phrase, null);
            }
        }
    }
}