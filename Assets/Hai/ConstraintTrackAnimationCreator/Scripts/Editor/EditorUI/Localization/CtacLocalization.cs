using System;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI.Localization
{
    public class CtacLocalization
    {
        public enum Phrase
        {
            UpdateConstraintTrack,
            RegenerateAnimator
        }

        public static string Localize(Phrase phrase)
        {
            switch (phrase)
            {
                case Phrase.UpdateConstraintTrack:
                    return "Update Constraint Track";
                case Phrase.RegenerateAnimator:
                    return "Regenerate Animator";
                default:
                    throw new ArgumentOutOfRangeException(nameof(phrase), phrase, null);
            }
        }
    }
}