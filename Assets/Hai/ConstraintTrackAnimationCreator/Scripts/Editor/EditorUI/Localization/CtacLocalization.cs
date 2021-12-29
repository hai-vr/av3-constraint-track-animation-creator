using System;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI.Localization
{
    public class CtacLocalization
    {
        public const string ManualUrl = "https://hai-vr.notion.site/Constraint-Track-Animation-Creator-b8e60b42401140e28b2ba05ff21b71bd";

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
            RevertPrefabBonesArray,
            Optimize,
            OpenForEditing,
            WhenIsNotOptimized,
            WhenIsOptimized,
            OpenDocumentation,
            ChangeName,
            NotInAvatar,
            Timings
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
                case Phrase.Optimize:
                    return "Optimize";
                case Phrase.OpenForEditing:
                    return "Open For Editing";
                case Phrase.WhenIsNotOptimized:
                    return "If you think you're finished, click Optimize.\n" +
                           "This will disable the constraints on the bones, and disable the system GameObject.";
                case Phrase.WhenIsOptimized:
                    return "The system is optimized. Click Open For Editing to un-optimize and edit the paths again.";
                case Phrase.OpenDocumentation:
                    return "Documentation / 説明書 / 설명서 ";
                case Phrase.ChangeName:
                    return "Change the name of this object.";
                case Phrase.NotInAvatar:
                    return "Move this object to be inside your avatar.";
                case Phrase.Timings:
                    return "Timings";
                default:
                    throw new ArgumentOutOfRangeException(nameof(phrase), phrase, null);
            }
        }
    }
}