using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Components
{
    public class SingleConstraintTrack : MonoBehaviour
    {
        public bool autoUpdatePathNames = true;
        public ParentConstraint[] bones;
        public ParentConstraint proxy;
        public Transform neutral;
        public Transform path;
        public bool gizmoAlwaysVisible = true;
        public CtacGizmoDirection gizmoDirection;
        public float gizmoScale = 1f;
        public bool gizmoIncludeTransformScale = true;

        public float timingPaddingDistance = 0.01f;
        public float timingDelayStartSeconds;
        public float timingScale = 1f;
        private static readonly Color ColorGood = Color.cyan;
        private static readonly Color ColorBad = Color.red;

        [Serializable]
        public enum CtacGizmoDirection
        {
            Up, Right, Forward
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (gizmoAlwaysVisible) return;

            DoDrawGizmo();
        }

        private void OnDrawGizmos()
        {
            if (!gizmoAlwaysVisible) return;

            DoDrawGizmo();
        }

        private void DoDrawGizmo()
        {
            var guiStyle = new GUIStyle();
            guiStyle.fontSize = 36;
            guiStyle.alignment = TextAnchor.MiddleCenter;
            guiStyle.font = GUIStyle.none.font;

            if (path != null && neutral != null)
            {
                var pathTransforms = path.Cast<Transform>().Where(t => t != null).ToArray();
                if (pathTransforms.Length > 0)
                {
                    DrawPath(guiStyle, pathTransforms, ColorBad);
                }
            }

            if (proxy != null)
            {
                Handles.color = ColorGood;
                var direction = GizmoDirectionAsVector();
                Handles.DrawWireDisc(proxy.transform.position, direction, 0.03f * gizmoScale);
                Handles.DrawWireDisc(proxy.transform.position, direction, 0.025f * gizmoScale);
                DrawVertex(proxy.transform, "", guiStyle);
            }
        }

        private Vector3 GizmoDirectionAsVector()
        {
            switch (gizmoDirection)
            {
                case CtacGizmoDirection.Up:
                    return proxy.transform.up;
                case CtacGizmoDirection.Right:
                    return proxy.transform.right;
                case CtacGizmoDirection.Forward:
                    return proxy.transform.forward;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawPath(GUIStyle guiStyle, Transform[] transforms, Color colorIdle)
        {
            Gizmos.color = Color.yellow;
            Handles.color = Color.yellow;
            guiStyle.normal.textColor = Color.yellow;

            Handles.DrawLine(neutral.position, transforms[0].position);
            DrawVertex(neutral, "N", guiStyle);

            for (var index = 0; index < transforms.Length - 1; index++)
            {
                Handles.color = IsConstraintActive(transforms[index]) ? ColorGood : colorIdle;
                Handles.DrawLine(transforms[index].position, transforms[index + 1].position);
                DrawVertex(transforms[index], index + "", guiStyle);
            }

            DrawVertex(transforms[transforms.Length - 1], transforms.Length - 1 + "", guiStyle);
        }

        private void DrawVertex(Transform that, string text, GUIStyle guiStyle)
        {
            var originalColor = Handles.color;

            var color = IsConstraintActive(that) ? ColorGood : ColorBad;
            Handles.color = color;
            guiStyle.normal.textColor = color;

            var forwardSize = 0.01f * gizmoScale;
            var pos = that.position;
            var right = that.right;
            var up = that.up;
            var forward = that.forward;
            var xx = gizmoIncludeTransformScale ? that.localScale.x : 1f;
            var yy = gizmoIncludeTransformScale ? that.localScale.y : 1f;
            var zz = gizmoIncludeTransformScale ? that.localScale.z : 1f;
            for (var i = 0; i < 2; i++)
            {
                var rightSize = 0.0065f * gizmoScale * zz;
                if (i == 1) rightSize = -rightSize;
                Handles.DrawAAPolyLine(
                    pos + right * forwardSize * xx + up * forwardSize * yy + forward * rightSize,
                    pos + right * forwardSize * xx - up * forwardSize * yy + forward * rightSize,
                    pos - right * forwardSize * xx - up * forwardSize * yy + forward * rightSize,
                    pos - right * forwardSize * xx + up * forwardSize * yy + forward * rightSize,
                    pos + right * forwardSize * xx + up * forwardSize * yy + forward * rightSize
                );
            }

            var lineSize = 0.02f * gizmoScale;
            Handles.color = Color.red; Handles.DrawLine(pos, pos + right * lineSize * that.localScale.x);
            Handles.color = Color.green; Handles.DrawLine(pos, pos + up * lineSize * that.localScale.y);
            Handles.color = Color.blue; Handles.DrawLine(pos, pos + forward * lineSize * zz);
            Handles.color = originalColor;
        }

        private static bool IsConstraintActive(Transform that)
        {
            var pc = that.GetComponent<ParentConstraint>();
            var isConstraintActive = pc == null || pc.constraintActive && pc.enabled && pc.gameObject.activeInHierarchy;
            return isConstraintActive;
        }

        public List<float> Timings(float scale, float addDelay)
        {
            return CalculateTimings(scale, path, addDelay);
        }

        public List<float> DistanceBasedTimings(float scale, float addDelay)
        {
            return CalculateTimings(scale, path, addDelay);
        }

        private List<float> CalculateTimings(float scale, Transform whichHierarchy, float addDelay)
        {
            var previous = neutral.position;

            var transforms = whichHierarchy.Cast<Transform>().ToArray();
            var timings = new List<float> {0f};
            var total = 0f;
            var safePaddingDistance = timingPaddingDistance < 0 ? 0f : timingPaddingDistance;
            foreach (var currentTransform in transforms)
            {
                var current = currentTransform.position;
                var distance = Vector3.Distance(previous, current);
                total += Math.Max(distance, safePaddingDistance);
                timings.Add(total);
                previous = current;
            }

            return timings.Select(f => f / total * scale + addDelay).ToList();
        }

        public void UpdateConstraintTrack()
        {
            DoSetupConstraint();

            if (autoUpdatePathNames)
            {
                DoRenamePathObjects();
            }
        }

        private void DoSetupConstraint()
        {
            Undo.RecordObject(proxy, "");
            proxy.constraintActive = false;
            // TODO: Question: Isn't this dangerous to do since the bone is already copying the proxy???
            // TODO: Answer: Probably not as this ensures that the position is correct when activating the constraint again?
            proxy.transform.position = neutral.position;
            proxy.transform.rotation = neutral.rotation;

            var sources = MakeSources();
            proxy.SetSources(sources);

            proxy.constraintActive = true;
            proxy.locked = true;

            var maybeScaleConstraint = proxy.GetComponent<ScaleConstraint>();
            if (maybeScaleConstraint)
            {
                Undo.RecordObject(maybeScaleConstraint, "");
                maybeScaleConstraint.constraintActive = false;

                var scaleConstraintSources = MakeSources();
                maybeScaleConstraint.SetSources(scaleConstraintSources);

                maybeScaleConstraint.constraintActive = true;
                maybeScaleConstraint.locked = true;
            }
        }

        private List<ConstraintSource> MakeSources()
        {
            var neutralSource = new [] { new ConstraintSource { weight = 1, sourceTransform = neutral } };

            return neutralSource
                .Concat(path.Cast<Transform>()
                    .Select(t => new ConstraintSource {weight = 0, sourceTransform = t}))
                .ToList();
        }

        private void DoRenamePathObjects()
        {
            var pathObjects = path.Cast<Transform>().Select(transform => transform.gameObject).ToArray();
            for (var index = 0; index < pathObjects.Length; index++)
            {
                Undo.RecordObject(pathObjects[index], "");
                pathObjects[index].name = "P" + index;
            }
        }
#endif
    }
}
