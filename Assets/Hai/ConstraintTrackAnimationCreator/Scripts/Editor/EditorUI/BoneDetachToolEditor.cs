using Hai.ConstraintTrackAnimationCreator.Scripts.Components;
using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI.Localization;
using UnityEditor;
using UnityEngine;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(BoneDetachTool))]
    public class BoneDetachToolEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(BoneDetachTool.skinnedMesh)));

            var that = (BoneDetachTool) target;
            that.enableDetachEditor = EditorGUILayout.Foldout(that.enableDetachEditor, CtacLocalization.Localize(CtacLocalization.Phrase.ShowBonesDetachEditor));
            if (that.enableDetachEditor && that.skinnedMesh != null)
            {
                GUILayout.BeginVertical("GroupBox");
                for (var index = 0; index < that.skinnedMesh.bones.Length; index++)
                {
                    Transform smrBone = that.skinnedMesh.bones[index];
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(smrBone, typeof(Transform));
                    if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.DetachBone)))
                    {
                        CreateDelegate(index, smrBone);
                    }

                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void CreateDelegate(int index, Transform originalBone)
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.DetachBone));
            var that = (BoneDetachTool) target;
            var smrBones = that.skinnedMesh.bones;

            var delegateObj = new GameObject();
            delegateObj.name = $"Z_{originalBone.name}_{that.skinnedMesh.name}_Detach";
            delegateObj.transform.parent = originalBone;
            delegateObj.transform.position = originalBone.position;
            delegateObj.transform.rotation = originalBone.rotation;
            delegateObj.transform.localScale = originalBone.localScale;
            Undo.RegisterCreatedObjectUndo(delegateObj, "");

            Undo.RecordObject(that.skinnedMesh, "");
            smrBones[index] = delegateObj.transform;
            that.skinnedMesh.bones = smrBones;
        }
    }
}
