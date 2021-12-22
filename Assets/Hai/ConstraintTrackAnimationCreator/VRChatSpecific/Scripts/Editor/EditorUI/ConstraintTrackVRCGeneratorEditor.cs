using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI.Localization;
using Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Components;
using UnityEditor;
using UnityEngine;

namespace Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(ConstraintTrackVRCGenerator))]
    public class ConstraintTrackVRCGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.RegenerateAnimator)))
            {
                RegenerateAnimator();
            }
        }

        private void RegenerateAnimator()
        {
            var that = (ConstraintTrackVRCGenerator)target;
            var generator = new CtacVRCAnimatorGenerator(new CtacController
            {
                avatar = that.avatar,
                generator = that,
                layerNameSuffix = that.trackName,
                parameterName = that.trackName
            });
            generator.Create();
        }
    }
}
