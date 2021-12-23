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
            ShowBonesDetachEditor
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(phrase), phrase, null);
            }
        }
    }
}