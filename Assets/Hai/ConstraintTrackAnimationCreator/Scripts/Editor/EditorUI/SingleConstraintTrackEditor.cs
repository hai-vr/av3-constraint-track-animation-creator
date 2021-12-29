using System.Globalization;
using System.Linq;
using Hai.ConstraintTrackAnimationCreator.Scripts.Components;
using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI.Localization;
using UnityEditor;
using UnityEngine;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(SingleConstraintTrack))]
    [CanEditMultipleObjects]
    public class SingleConstraintTrackEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!serializedObject.isEditingMultipleObjects)
            {
                if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.UpdateConstraintTrack)))
                {
                    UpdateConstraintTrack();
                }
            }

            var that = That();
            if (that.neutral != null && that.path != null)
            {
                EditorGUILayout.LabelField(CtacLocalization.Localize(CtacLocalization.Phrase.Timings), EditorStyles.boldLabel);
                var displayTimings = that.DistanceBasedTimings(10f, 0f)
                    .Select(f => string.Format(CultureInfo.InvariantCulture, "{0:0.00}", f))
                    .Aggregate((s, s1) => s + " : " + s1);
                EditorGUILayout.LabelField(displayTimings);
            }

            EditorGUILayout.Separator();
            if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.OpenDocumentation)))
            {
                Application.OpenURL(CtacLocalization.ManualUrl);
            }
        }

        private void UpdateConstraintTrack()
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.UpdateConstraintTrack));
            That().UpdateConstraintTrack();
        }

        private SingleConstraintTrack That()
        {
            return ((SingleConstraintTrack)target);
        }
    }
}
