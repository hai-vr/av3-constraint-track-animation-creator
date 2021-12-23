using System.Linq;
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

            var that = That();
            that.enableDetachEditor = EditorGUILayout.Foldout(that.enableDetachEditor, CtacLocalization.Localize(CtacLocalization.Phrase.ShowBonesDetachEditor));
            if (that.enableDetachEditor && that.skinnedMesh != null)
            {
                GUILayout.BeginVertical("GroupBox");
                for (var index = 0; index < that.skinnedMesh.bones.Length; index++)
                {
                    Transform smrBone = that.skinnedMesh.bones[index];
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(smrBone, typeof(Transform));

                    EditorGUI.BeginDisabledGroup(IsSmrBoneInAnyMemberOfDetachment(smrBone));
                    if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.DetachBone)))
                    {
                        DetachBone(index, smrBone);
                    }
                    EditorGUI.EndDisabledGroup();

                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();

            if (that.detachments.Length > 0)
            {
                GUILayout.BeginVertical("GroupBox");
                if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.ApplyAgain)))
                {
                    ApplyAgain();
                }
                if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.RevertWithoutRemoving)))
                {
                    RevertWithoutRemoving();
                }
                EditorGUILayout.LabelField(CtacLocalization.Localize(CtacLocalization.Phrase.DetachedBones));
                foreach (var detachment in that.detachments)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(detachment.original, typeof(Transform));
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.ObjectField(detachment.detached, typeof(Transform));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
        }

        private bool IsSmrBoneInAnyMemberOfDetachment(Transform smrBone)
        {
            return That().detachments.Any(detachment => detachment.original == smrBone || detachment.detached == smrBone);
        }

        private void DetachBone(int index, Transform originalBone)
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.DetachBone));
            var that = That();
            var smrBones = that.skinnedMesh.bones;

            var detachedBone = CreateDetachedBone(that.skinnedMesh, originalBone);

            Undo.RecordObject(that.skinnedMesh, "");
            smrBones[index] = detachedBone.transform;
            that.skinnedMesh.bones = smrBones;

            Undo.RecordObject(that, "");
            var newDetachments = that.detachments.ToList();
            newDetachments.Add(new BoneDetachTool.Detachment
            {
                original = originalBone,
                detached = detachedBone.transform
            });
            that.detachments = newDetachments.ToArray();
        }

        private void ApplyAgain()
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.ApplyAgain));

            var that = That();

            Undo.RecordObject(that.skinnedMesh, "");
            var smrBones = that.skinnedMesh.bones;
            foreach (var detachment in that.detachments)
            {
                if (detachment.original != null && detachment.detached != null)
                {
                    for (var index = 0; index < smrBones.Length; index++)
                    {
                        var smrBone = smrBones[index];
                        if (smrBone == detachment.original)
                        {
                            smrBones[index] = detachment.detached;
                        }
                    }
                }
            }
            that.skinnedMesh.bones = smrBones;
        }

        private void RevertWithoutRemoving()
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.RevertWithoutRemoving));

            var that = That();

            Undo.RecordObject(that.skinnedMesh, "");
            var smrBones = that.skinnedMesh.bones;
            foreach (var detachment in that.detachments)
            {
                if (detachment.original != null && detachment.detached != null)
                {
                    for (var index = 0; index < smrBones.Length; index++)
                    {
                        var smrBone = smrBones[index];
                        if (smrBone == detachment.detached)
                        {
                            smrBones[index] = detachment.original;
                        }
                    }
                }
            }
            that.skinnedMesh.bones = smrBones;
        }

        private static GameObject CreateDetachedBone(SkinnedMeshRenderer skinnedMeshRenderer, Transform originalBone)
        {
            var delegateObj = new GameObject();
            delegateObj.name = $"Z_{originalBone.name}_{skinnedMeshRenderer.name}_Detach";
            delegateObj.transform.parent = originalBone;
            delegateObj.transform.position = originalBone.position;
            delegateObj.transform.rotation = originalBone.rotation;
            delegateObj.transform.localScale = originalBone.localScale;
            Undo.RegisterCreatedObjectUndo(delegateObj, "");
            return delegateObj;
        }

        private BoneDetachTool That()
        {
            return (BoneDetachTool) target;
        }
    }
}
