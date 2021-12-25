using Hai.ConstraintTrackAnimationCreator.Scripts.Components;
using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI.Localization;
using UnityEditor;
using UnityEngine;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(ConstraintTrackAnimation))]
    public class ConstraintTrackAnimationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Separator();
            if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.OpenDocumentation)))
            {
                Application.OpenURL(CtacLocalization.ManualUrl);
            }
        }
    }
}
