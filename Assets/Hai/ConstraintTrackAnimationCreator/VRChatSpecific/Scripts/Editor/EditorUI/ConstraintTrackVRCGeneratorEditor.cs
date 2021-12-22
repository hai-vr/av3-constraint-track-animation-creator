using Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Components;
using UnityEditor;

namespace Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(ConstraintTrackVRCGenerator))]
    public class ConstraintTrackVRCGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}
