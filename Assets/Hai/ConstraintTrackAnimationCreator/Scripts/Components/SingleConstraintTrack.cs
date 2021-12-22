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
        public CtacGizmoDirection gizmoDirection;
        public float gizmoScale = 1f;

        [Serializable]
        public enum CtacGizmoDirection
        {
            Up, Right, Forward
        }

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (path != null && neutral != null)
            {
                var pathTransforms = path.Cast<Transform>().Where(t => t != null).ToArray();
                if (pathTransforms.Length > 0)
                {
                    var guiStyle = new GUIStyle();
                    guiStyle.fontSize = 36;
                    guiStyle.alignment = TextAnchor.MiddleCenter;
                    guiStyle.font = GUIStyle.none.font;

                    DrawPath(guiStyle, pathTransforms, Color.red);
                }
            }

            if (proxy != null)
            {
                Handles.color = Color.green;
                var direction = GizmoDirectionAsVector();
                Handles.DrawWireDisc(proxy.transform.position, direction, 0.03f * gizmoScale);
                Handles.DrawWireDisc(proxy.transform.position, direction, 0.025f * gizmoScale);
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
            DrawVertex(neutral, "N", guiStyle, gizmoScale);

            for (var index = 0; index < transforms.Length - 1; index++)
            {
                Handles.color = IsConstraintActive(transforms[index]) ? Color.green : colorIdle;
                Handles.DrawLine(transforms[index].position, transforms[index + 1].position);
                DrawVertex(transforms[index], index + "", guiStyle, gizmoScale);
            }

            DrawVertex(transforms[transforms.Length - 1], transforms.Length - 1 + "", guiStyle, gizmoScale);
        }

        private static void DrawVertex(Transform that, string text, GUIStyle guiStyle, float gizmoScale)
        {
            var originalColor = Handles.color;

            var color = IsConstraintActive(that) ? Color.green : Color.red;
            Handles.color = color;
            guiStyle.normal.textColor = color;

            var forwardSize = 0.01f * gizmoScale;
            var pos = that.position;
            var right = that.right;
            var up = that.up;
            var forward = that.forward;
            for (var i = 0; i < 2; i++)
            {
                var rightSize = 0.0065f * gizmoScale;
                if (i == 1) rightSize = -rightSize;
                Handles.DrawAAPolyLine(
                    pos + right * forwardSize + up * forwardSize + forward * rightSize,
                    pos + right * forwardSize - up * forwardSize + forward * rightSize,
                    pos - right * forwardSize - up * forwardSize + forward * rightSize,
                    pos - right * forwardSize + up * forwardSize + forward * rightSize,
                    pos + right * forwardSize + up * forwardSize + forward * rightSize
                );
            }

            var lineSize = 0.02f * gizmoScale;
            Handles.color = Color.red; Handles.DrawLine(pos, pos + right * lineSize);
            Handles.color = Color.green; Handles.DrawLine(pos, pos + up * lineSize);
            Handles.color = Color.blue; Handles.DrawLine(pos, pos + forward * lineSize);
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

        private List<float> CalculateTimings(float scale, Transform whichHierarchy, float addDelay)
        {
            var previous = neutral.position;

            var transforms = whichHierarchy.Cast<Transform>().ToArray();
            var timings = new List<float> {0f};
            var total = 0f;
            foreach (var currentTransform in transforms)
            {
                var current = currentTransform.position;
                total += Vector3.Distance(previous, current);
                timings.Add(total);
                previous = current;
            }

            return timings.Select(f => f / total * scale + addDelay).ToList();
        }
#endif
    }
}
