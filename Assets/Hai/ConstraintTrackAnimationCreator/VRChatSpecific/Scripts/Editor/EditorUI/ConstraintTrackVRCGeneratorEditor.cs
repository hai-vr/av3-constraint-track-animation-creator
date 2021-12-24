using System.Collections.Generic;
using System.Linq;
using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI.Localization;
using Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

namespace Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(ConstraintTrackVRCGenerator))]
    public class ConstraintTrackVRCGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.UpdateAllConstraintTracks)))
            {
                UpdateAllConstraintTracks();
            }

            if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.RegenerateAnimator)))
            {
                RegenerateAnimator();
            }


            var isOptimized = IsOptimized();
            if (!isOptimized)
            {
                EditorGUILayout.HelpBox(CtacLocalization.Localize(CtacLocalization.Phrase.WhenIsNotOptimized), MessageType.Warning);
                if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.Optimize)))
                {
                    Optimize();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(CtacLocalization.Localize(CtacLocalization.Phrase.WhenIsOptimized), MessageType.Info);
                if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.OpenForEditing)))
                {
                    OpenForEditing();
                }
            }
        }

        private bool IsOptimized()
        {
            var cta = That().constraintTrackAnimation;
            if (cta == null) return true;
            if (cta.parentOfAllTracks == null) return true;
            if (cta.tracks == null || cta.tracks.Length == 0) return true;

            return AllBoneConstraints().All(constraint => constraint.enabled == false)
                   && AllScaleConstraints().All(constraint => constraint.enabled == false)
                   && cta.parentOfAllTracks.gameObject.activeSelf == false;
        }

        private void Optimize()
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.Optimize));
            ChangeEditMode(false);
        }

        private void OpenForEditing()
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.OpenForEditing));
            ChangeEditMode(true);
        }

        private void ChangeEditMode(bool enableSystemAndConstraint)
        {
            var parentOfAllTracksGo = That().constraintTrackAnimation.parentOfAllTracks.gameObject;
            Undo.RecordObject(parentOfAllTracksGo, "");
            parentOfAllTracksGo.SetActive(enableSystemAndConstraint);

            var boneConstraints = AllBoneConstraints();
            var boneScaleConstraints = AllScaleConstraints();

            foreach (var parentConstraint in boneConstraints)
            {
                Undo.RecordObject(parentConstraint, "");
                parentConstraint.enabled = enableSystemAndConstraint;
            }

            foreach (var boneScaleConstraint in boneScaleConstraints)
            {
                Undo.RecordObject(boneScaleConstraint, "");
                boneScaleConstraint.enabled = enableSystemAndConstraint;
            }
        }

        private List<ParentConstraint> AllBoneConstraints()
        {
            return That().constraintTrackAnimation.tracks.SelectMany(track => track.bones).ToList();
        }

        private List<ScaleConstraint> AllScaleConstraints()
        {
            return AllBoneConstraints()
                .Select(constraint => constraint.GetComponent<ScaleConstraint>())
                .Where(constraint => constraint != null)
                .ToList();
        }

        private void UpdateAllConstraintTracks()
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.UpdateConstraintTrack));

            foreach (var track in That().constraintTrackAnimation.tracks)
            {
                track.UpdateConstraintTrack();
            }
        }

        private void RegenerateAnimator()
        {
            var that = That();
            var generator = new CtacVRCAnimatorGenerator(new CtacController
            {
                avatar = that.avatar,
                generator = that,
                layerNameSuffix = that.trackName,
                parameterName = that.trackName
            });
            generator.Create();
        }

        private ConstraintTrackVRCGenerator That()
        {
            return (ConstraintTrackVRCGenerator)target;
        }
    }
}
