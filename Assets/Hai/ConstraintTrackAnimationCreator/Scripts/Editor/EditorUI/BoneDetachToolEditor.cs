﻿using System;
using System.Globalization;
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
        private static bool _enableDetachEditor;

        public override void OnInspectorGUI()
        {
            var that = That();
            var anyDetachedBones = that.detachments.Length > 0;
            EditorGUI.BeginDisabledGroup(anyDetachedBones);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(BoneDetachTool.skinnedMesh)));
            EditorGUI.EndDisabledGroup();

            _enableDetachEditor = EditorGUILayout.Foldout(_enableDetachEditor, CtacLocalization.Localize(CtacLocalization.Phrase.ShowBonesDetachEditor));
            if (_enableDetachEditor && that.skinnedMesh != null)
            {
                SumVertexWeightsPerBone(that, out int[] boneToVertexCount, out float[] boneToTotalWeight);

                GUILayout.BeginVertical("GroupBox");
                for (var index = 0; index < that.skinnedMesh.bones.Length; index++)
                {
                    var vertexCount = boneToVertexCount[index];
                    var totalWeight = boneToTotalWeight[index];
                    // Hips bone often has a total weight of 0.0, so exclude it
                    if (vertexCount == 0 || totalWeight == 0f) continue;

                    Transform smrBone = that.skinnedMesh.bones[index];
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(smrBone, typeof(Transform));
                    EditorGUILayout.LabelField(string.Format(CultureInfo.InvariantCulture, "{0:0.0}", totalWeight), GUILayout.Width(60));

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

            if (anyDetachedBones)
            {
                GUILayout.BeginVertical("GroupBox");
                if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.FixModelUpdate)))
                {
                    FixModelUpdate();
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

                that.advancedFoldout = EditorGUILayout.Foldout(that.advancedFoldout, CtacLocalization.Localize(CtacLocalization.Phrase.Advanced));
                if (that.advancedFoldout)
                {
                    if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.ApplyAgain)))
                    {
                        ApplyAgain();
                    }
                    if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.RevertWithoutRemoving)))
                    {
                        RevertWithoutRemoving();
                    }
                    if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.RevertPrefabBonesArray)))
                    {
                        RevertPrefabBonesArray();
                    }
                }
                GUILayout.EndVertical();
            }

            EditorGUILayout.Separator();
            if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.OpenDocumentation)))
            {
                Application.OpenURL(CtacLocalization.ManualUrl);
            }
        }

        private static void SumVertexWeightsPerBone(BoneDetachTool that, out int[] boneToVertexCount, out float[] boneToTotalWeight)
        {
            boneToVertexCount = new int[that.skinnedMesh.bones.Length];
            boneToTotalWeight = new float[that.skinnedMesh.bones.Length];
            foreach (var sharedMeshBoneWeight in that.skinnedMesh.sharedMesh.boneWeights)
            {
                Increment(boneToVertexCount, boneToTotalWeight, sharedMeshBoneWeight.boneIndex0, sharedMeshBoneWeight.weight0);
                Increment(boneToVertexCount, boneToTotalWeight, sharedMeshBoneWeight.boneIndex1, sharedMeshBoneWeight.weight1);
                Increment(boneToVertexCount, boneToTotalWeight, sharedMeshBoneWeight.boneIndex2, sharedMeshBoneWeight.weight2);
                Increment(boneToVertexCount, boneToTotalWeight, sharedMeshBoneWeight.boneIndex3, sharedMeshBoneWeight.weight3);
            }
        }

        private static void Increment(int[] boneToVertexCount, float[] boneToTotalWeight, int index, float weight)
        {
            if (index < 0 || index >= boneToVertexCount.Length) return;
            boneToVertexCount[index]++;
            boneToTotalWeight[index] += weight;
        }

        private bool IsSmrBoneInAnyMemberOfDetachment(Transform smrBone)
        {
            return That().detachments.Any(detachment => detachment.original == smrBone || detachment.detached == smrBone);
        }

        private void FixModelUpdate()
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.FixModelUpdate));

            RevertPrefabBonesArrayInternal();
            ApplyAgainInternal();
        }

        private void RevertPrefabBonesArray()
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.RevertPrefabBonesArray));
            RevertPrefabBonesArrayInternal();
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

            ApplyAgainInternal();
        }

        private void ApplyAgainInternal()
        {
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

        private void RevertPrefabBonesArrayInternal()
        {
            Undo.RecordObject(That().skinnedMesh, "");
            PrefabUtility.RevertPropertyOverride(new SerializedObject(That().skinnedMesh).FindProperty("m_Bones"), InteractionMode.AutomatedAction);
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
