using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hai.ConstraintTrackAnimationCreator.Scripts.Components;
using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI.Localization;
using Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.Components;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(ConstraintTrackSetup))]
    public class ConstraintTrackSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var bones = serializedObject.FindProperty(nameof(ConstraintTrackSetup.bones));
            var neutrals = serializedObject.FindProperty(nameof(ConstraintTrackSetup.neutrals));
            var ignoreBoneRotation = serializedObject.FindProperty(nameof(ConstraintTrackSetup.ignoreBoneRotation));
            var ignoreBoneScale = serializedObject.FindProperty(nameof(ConstraintTrackSetup.ignoreBoneScale));
            var generateScaleConstraint = serializedObject.FindProperty(nameof(ConstraintTrackSetup.generateScaleConstraint));

            var neutralsAreCreated = neutrals.arraySize > 0;

            var goSerializedObject = new SerializedObject(That().gameObject);
            EditorGUILayout.PropertyField(goSerializedObject.FindProperty("m_Name"), new GUIContent("System Name"));
            goSerializedObject.ApplyModifiedProperties();

            EditorGUI.BeginDisabledGroup(neutralsAreCreated);
            EditorGUILayout.PropertyField(bones);
            EditorGUILayout.PropertyField(ignoreBoneRotation);
            EditorGUILayout.PropertyField(ignoreBoneScale);
            EditorGUILayout.PropertyField(generateScaleConstraint);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ConstraintTrackSetup.gizmoDirection)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ConstraintTrackSetup.gizmoScale)));
            serializedObject.ApplyModifiedProperties();

            if (!neutralsAreCreated)
            {
                EditorGUI.BeginDisabledGroup(bones.arraySize == 0);
                if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.CreateNeutralObjects)))
                {
                    CreateNeutralObjects();
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(neutrals);
                EditorGUI.EndDisabledGroup();

                var isNotInAvatar = FindAvatar(That()) == null;
                EditorGUI.BeginDisabledGroup(isNotInAvatar);
                if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.ConfirmSetup)))
                {
                    CreateSystem();
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void CreateNeutralObjects()
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.CreateNeutralObjects));

            var that = That();

            var neutrals = new List<Transform>();
            foreach (var thatBone in that.bones)
            {
                var neutral = new GameObject();
                neutral.transform.parent = thatBone.parent;
                neutral.transform.position = thatBone.transform.position;
                if (!that.ignoreBoneRotation)
                {
                    neutral.transform.rotation = thatBone.transform.rotation;
                }
                if (!that.ignoreBoneScale)
                {
                    neutral.transform.localScale = thatBone.transform.localScale;
                }
                // FIXME: The rotation is not copied????!!!! This may be a good thing? Provide an option?
                neutral.name = thatBone.name + "_NEUTRAL";
                Undo.RegisterCreatedObjectUndo(neutral, "");

                neutrals.Add(neutral.transform);
            }

            Undo.RecordObject(that, "");
            that.neutrals = neutrals.ToArray();
        }

        private ConstraintTrackSetup That()
        {
            return (ConstraintTrackSetup)target;
        }

        private void CreateSystem()
        {
            var that = That();

            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.ConfirmSetup));

            var proxies = new GameObject("Proxies");
            proxies.transform.parent = that.transform;
            proxies.transform.position = that.transform.position;
            Undo.RegisterCreatedObjectUndo(proxies, "");

            var paths = new GameObject("Paths");
            paths.transform.parent = that.transform;
            paths.transform.position = that.transform.position;
            Undo.RegisterCreatedObjectUndo(paths, "");

            var systemInSceneRoot = new GameObject($"{that.name}_CTAC");
            Undo.RegisterCreatedObjectUndo(systemInSceneRoot, "");

            var tracks = new List<SingleConstraintTrack>();
            for (var index = 0; index < that.bones.Length; index++)
            {
                var track = SetupTrackForBone(that, index, systemInSceneRoot, proxies, paths);
                tracks.Add(track);
            }

            var constraintTrackAnimation = CreateConstraintTrackAnimation(systemInSceneRoot, tracks, that);
            var vrcGenerator = CreateVrcGenerator(systemInSceneRoot, constraintTrackAnimation, FindAvatar(that), that.name);

            Undo.DestroyObjectImmediate(that);

            SetExpandedRecursive(paths, true);
            EditorGUIUtility.PingObject(vrcGenerator);
        }

        private static VRCAvatarDescriptor FindAvatar(ConstraintTrackSetup that)
        {
            return that.GetComponentInParent<VRCAvatarDescriptor>();
        }

        private static SingleConstraintTrack SetupTrackForBone(ConstraintTrackSetup that, int index, GameObject systemInSceneRoot, GameObject proxies, GameObject paths)
        {
            var thatBone = that.bones[index];
            var neutral = that.neutrals[index];

            var trackGo = new GameObject();
            trackGo.name = "Track_" + thatBone.name;
            trackGo.transform.parent = systemInSceneRoot.transform;
            trackGo.transform.position = systemInSceneRoot.transform.position;
            Undo.RegisterCreatedObjectUndo(trackGo, "");

            Undo.RecordObject(thatBone.transform, ""); // I don't know if it will actually fix the position when we undo (due to the constraint)
            var boneParentConstraint = Undo.AddComponent<ParentConstraint>(thatBone.gameObject);

            var track = trackGo.AddComponent<SingleConstraintTrack>();
            track.bones = new[] {boneParentConstraint};
            track.neutral = neutral;
            track.gizmoDirection = that.gizmoDirection;
            track.gizmoScale = that.gizmoScale;

            var proxy = new GameObject($"{thatBone.name}_Proxy");
            proxy.transform.parent = proxies.transform;
            proxy.transform.position = neutral.position;
            proxy.transform.rotation = neutral.rotation;
            Undo.RegisterCreatedObjectUndo(proxy, "");

            var proxyConstraint = proxy.AddComponent<ParentConstraint>();
            track.proxy = proxyConstraint;

            boneParentConstraint.AddSource(new ConstraintSource
            {
                sourceTransform = proxy.transform,
                weight = 1f
            });
            ConstraintActivate(boneParentConstraint);
            boneParentConstraint.enabled = true; // FIXME: Figure out why constraint break on init if it's false instead of true

            if (that.generateScaleConstraint)
            {
                proxy.AddComponent<ScaleConstraint>();

                var boneScaleConstraint = Undo.AddComponent<ScaleConstraint>(thatBone.gameObject);
                boneScaleConstraint.AddSource(new ConstraintSource
                {
                    sourceTransform = proxy.transform,
                    weight = 1f
                });
                ConstraintActivate(boneScaleConstraint);
                boneScaleConstraint.enabled = true; // FIXME: Figure out why constraint break on init if it's false instead of true
            }

            var path = new GameObject($"{thatBone.name}_Path");
            path.transform.parent = paths.transform;
            path.transform.position = paths.transform.position;
            Undo.RegisterCreatedObjectUndo(path, "");

            track.path = path.transform;

            var p0 = new GameObject("P0");
            p0.transform.parent = path.transform;
            p0.transform.position = neutral.transform.position;
            p0.transform.rotation = neutral.transform.rotation;
            p0.transform.localScale = neutral.transform.localScale;
            p0.AddComponent<ParentConstraint>().AddSource(new ConstraintSource
            {
                sourceTransform = null,
                weight = 1f
            });
            Undo.RegisterCreatedObjectUndo(p0, "");

            track.UpdateConstraintTrack();

            return track;
        }

        private static ConstraintTrackAnimation CreateConstraintTrackAnimation(GameObject systemInSceneRoot, List<SingleConstraintTrack> tracks, ConstraintTrackSetup that)
        {
            var ctaGo = new GameObject();
            ctaGo.name = "ConstraintTrackAnimation";
            ctaGo.transform.parent = systemInSceneRoot.transform;
            ctaGo.transform.position = systemInSceneRoot.transform.position;
            Undo.RegisterCreatedObjectUndo(ctaGo, "");

            var constraintTrackAnimation = ctaGo.AddComponent<ConstraintTrackAnimation>();
            constraintTrackAnimation.tracks = tracks.ToArray();
            constraintTrackAnimation.parentOfAllTracks = that.gameObject;

            return constraintTrackAnimation;
        }

        private static ConstraintTrackVRCGenerator CreateVrcGenerator(GameObject systemInSceneRoot, ConstraintTrackAnimation constraintTrackAnimation, VRCAvatarDescriptor avatar, string systemName)
        {
            var generatorGo = new GameObject();
            generatorGo.name = "ConstraintTrackVRCGenerator";
            generatorGo.transform.parent = systemInSceneRoot.transform;
            generatorGo.transform.position = systemInSceneRoot.transform.position;
            Undo.RegisterCreatedObjectUndo(generatorGo, "");

            var vrcGenerator = generatorGo.AddComponent<ConstraintTrackVRCGenerator>();
            vrcGenerator.constraintTrackAnimation = constraintTrackAnimation;
            vrcGenerator.avatar = avatar;
            vrcGenerator.layerName = systemName;
            vrcGenerator.parameterPrefixName = systemName;

            return vrcGenerator;
        }

        private static void ConstraintActivate(IConstraint constraint)
        {
            constraint.GetType().GetMethod("ActivateAndPreserveOffset", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(constraint, null);
        }

        // https://answers.unity.com/questions/656869/foldunfold-gameobject-from-code.html?childToView=858132#comment-858132
        public static void SetExpandedRecursive(GameObject go, bool expand)
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            var methodInfo = type.GetMethod("SetExpandedRecursive");

            EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
            var window = EditorWindow.focusedWindow;

            methodInfo.Invoke(window, new object[] { go.GetInstanceID(), expand });
        }
    }
}
