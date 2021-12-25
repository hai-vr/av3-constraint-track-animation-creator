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

            EditorGUILayout.Separator();
            if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.OpenDocumentation)))
            {
                Application.OpenURL(CtacLocalization.ManualUrl);
            }
        }

        private void UpdateConstraintTrack()
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.UpdateConstraintTrack));
            ((SingleConstraintTrack)target).UpdateConstraintTrack();
        }
    }
}
