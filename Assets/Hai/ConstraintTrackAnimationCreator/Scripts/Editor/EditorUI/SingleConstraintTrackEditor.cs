using System.Collections.Generic;
using System.Linq;
using Hai.ConstraintTrackAnimationCreator.Scripts.Components;
using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EditorUI
{
    [CustomEditor(typeof(SingleConstraintTrack))]
    public class SingleConstraintTrackEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button(CtacLocalization.Localize(CtacLocalization.Phrase.UpdateConstraintTrack)))
            {
                UpdateConstraintTrack();
            }
        }

        private SingleConstraintTrack That()
        {
            return (SingleConstraintTrack)target;
        }

        private void UpdateConstraintTrack()
        {
            Undo.SetCurrentGroupName(CtacLocalization.Localize(CtacLocalization.Phrase.UpdateConstraintTrack));

            DoSetupConstraint();

            if (That().autoUpdatePathNames)
            {
                DoRenamePathObjects();
            }
        }

        private void DoSetupConstraint()
        {
            var that = That();

            Undo.RecordObject(that.proxy, "");
            var proxy = that.proxy;
            proxy.constraintActive = false;
            proxy.transform.position = that.neutral.position;
            proxy.transform.rotation = that.neutral.rotation;

            var sources = MakeSources(that);
            proxy.SetSources(sources);

            proxy.constraintActive = true;
            proxy.locked = true;
        }

        private static List<ConstraintSource> MakeSources(SingleConstraintTrack that)
        {
            var neutral = new [] { new ConstraintSource { weight = 1, sourceTransform = that.neutral } };

            return neutral
                .Concat(that.path.Cast<Transform>()
                    .Select(t => new ConstraintSource {weight = 0, sourceTransform = t}))
                .ToList();
        }

        private void DoRenamePathObjects()
        {
            var that = (SingleConstraintTrack)target;
            var pathObjects = that.path.Cast<Transform>().Select(transform => transform.gameObject).ToArray();
            for (var index = 0; index < pathObjects.Length; index++)
            {
                Undo.RecordObject(pathObjects[index], "");
                pathObjects[index].name = "P" + index;
            }
        }
    }
}
