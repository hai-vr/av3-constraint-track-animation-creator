﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac.Fluent
{
    internal readonly struct AacFlClip
    {
        private readonly AnimatorAsCode _component;
        public AnimationClip Clip { get; }

        public AacFlClip(AnimatorAsCode component, AnimationClip clip)
        {
            _component = component;
            Clip = clip;
        }

        public AacFlClip Looping()
        {
            var settings = AnimationUtility.GetAnimationClipSettings(Clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(Clip, settings);

            return this;
        }

        public AacFlClip Which(Action<AacFlClip> action)
        {
            action.Invoke(this);

            return this;
        }

        public AacFlClip That(Action<AacFlEditClip> action)
        {
            action.Invoke(new AacFlEditClip(_component, Clip));
            return this;
        }

        public AacFlClip Toggling(GameObject[] gameObjectsWithNulls, bool value)
        {
            var defensiveObjects = gameObjectsWithNulls.Where(o => o != null); // Allow users to remove an item in the middle of the array
            foreach (var component in defensiveObjects)
            {
                var binding = AacV0.Binding(_component, typeof(GameObject), component.transform, "m_IsActive");

                AnimationUtility.SetEditorCurve(Clip, binding, AacV0.OneFrame(value ? 1f : 0f));
            }

            return this;
        }

        public AacFlClip Scaling(GameObject[] gameObjectsWithNulls, Vector3 scale)
        {
            var defensiveObjects = gameObjectsWithNulls.Where(o => o != null); // Allow users to remove an item in the middle of the array
            foreach (var component in defensiveObjects)
            {
                AnimationUtility.SetEditorCurve(Clip, AacV0.Binding(_component, typeof(Transform), component.transform, "m_LocalScale.x"), AacV0.OneFrame(scale.x));
                AnimationUtility.SetEditorCurve(Clip, AacV0.Binding(_component, typeof(Transform), component.transform, "m_LocalScale.y"), AacV0.OneFrame(scale.y));
                AnimationUtility.SetEditorCurve(Clip, AacV0.Binding(_component, typeof(Transform), component.transform, "m_LocalScale.z"), AacV0.OneFrame(scale.z));
            }

            return this;
        }

        public AacFlClip Toggling(GameObject gameObject, bool value)
        {
            var binding = AacV0.Binding(_component, typeof(GameObject), gameObject.transform, "m_IsActive");

            AnimationUtility.SetEditorCurve(Clip, binding, AacV0.OneFrame(value ? 1f : 0f));

            return this;
        }

        public AacFlClip TogglingComponent(Component[] componentsWithNulls, bool value)
        {
            var defensiveComponents = componentsWithNulls.Where(o => o != null); // Allow users to remove an item in the middle of the array
            foreach (var component in defensiveComponents)
            {
                var binding = AacV0.Binding(_component, component.GetType(), component.transform, "m_Enabled");

                AnimationUtility.SetEditorCurve(Clip, binding, AacV0.OneFrame(value ? 1f : 0f));
            }

            return this;
        }

        public AacFlClip TogglingComponent(Component component, bool value)
        {
            var binding = AacV0.Binding(_component, component.GetType(), component.transform, "m_Enabled");

            AnimationUtility.SetEditorCurve(Clip, binding, AacV0.OneFrame(value ? 1f : 0f));

            return this;
        }
    }

    internal readonly struct AacFlEditClip
    {
        private readonly AnimatorAsCode _component;
        public AnimationClip Clip { get; }

        public AacFlEditClip(AnimatorAsCode component, AnimationClip clip)
        {
            _component = component;
            Clip = clip;
        }

        public AacFlSettingCurve Animates(string path, Type type, string propertyName)
        {
            var binding = new EditorCurveBinding
            {
                path = path,
                type = type,
                propertyName = propertyName
            };
            return new AacFlSettingCurve(Clip, new[] {binding});
        }

        public AacFlSettingCurve Animates(Transform transform, Type type, string propertyName)
        {
            var binding = AacV0.Binding(_component, type, transform, propertyName);

            return new AacFlSettingCurve(Clip, new[] {binding});
        }

        public AacFlSettingCurve Animates(GameObject gameObject)
        {
            var binding = AacV0.Binding(_component, typeof(GameObject), gameObject.transform, "m_IsActive");

            return new AacFlSettingCurve(Clip, new[] {binding});
        }

        public AacFlSettingCurve Animates(Component anyComponent, string property)
        {
            var binding = Internal_BindingFromComponent(anyComponent, property);

            return new AacFlSettingCurve(Clip, new[] {binding});
        }

        public AacFlSettingCurve Animates(Component[] anyComponents, string property)
        {
            var that = this;
            var bindings = anyComponents
                .Select(anyComponent => that.Internal_BindingFromComponent(anyComponent, property))
                .ToArray();

            return new AacFlSettingCurve(Clip, bindings);
        }

        private EditorCurveBinding Internal_BindingFromComponent(Component anyComponent, string propertyName)
        {
            return AacV0.Binding(_component, anyComponent.GetType(), anyComponent.transform, propertyName);
        }
    }

    internal class AacFlSettingCurve
    {
        private readonly AnimationClip _clip;
        private readonly EditorCurveBinding[] _bindings;

        public AacFlSettingCurve(AnimationClip clip, EditorCurveBinding[] bindings)
        {
            _clip = clip;
            _bindings = bindings;
        }

        public void WithOneFrame(float desiredValue)
        {
            foreach (var binding in _bindings)
            {
                AnimationUtility.SetEditorCurve(_clip, binding, AacV0.OneFrame(desiredValue));
            }
        }

        public void WithFixedSeconds(float seconds, float desiredValue)
        {
            foreach (var binding in _bindings)
            {
                AnimationUtility.SetEditorCurve(_clip, binding, AacV0.ConstantSeconds(seconds, desiredValue));
            }
        }

        public void WithSecondsUnit(Action<AacFlSettingKeyframes> action)
        {
            InternalWithUnit(AacFlUnit.Seconds, action);
        }

        public void WithFrameCountUnit(Action<AacFlSettingKeyframes> action)
        {
            InternalWithUnit(AacFlUnit.Frames, action);
        }

        public void WithUnit(AacFlUnit unit, Action<AacFlSettingKeyframes> action)
        {
            InternalWithUnit(unit, action);
        }

        private void InternalWithUnit(AacFlUnit unit, Action<AacFlSettingKeyframes> action)
        {
            var mutatedKeyframes = new List<Keyframe>();
            var builder = new AacFlSettingKeyframes(unit, mutatedKeyframes);
            action.Invoke(builder);

            foreach (var binding in _bindings)
            {
                AnimationUtility.SetEditorCurve(_clip, binding, new AnimationCurve(mutatedKeyframes.ToArray()));
            }
        }
    }

    public class AacFlSettingKeyframes
    {
        private readonly AacFlUnit _unit;
        private readonly List<Keyframe> _mutatedKeyframes;

        public AacFlSettingKeyframes(AacFlUnit unit, List<Keyframe> mutatedKeyframes)
        {
            _unit = unit;
            _mutatedKeyframes = mutatedKeyframes;
        }

        public AacFlSettingKeyframes Easing(float timeInUnit, float value)
        {
            _mutatedKeyframes.Add(new Keyframe(AsSeconds(timeInUnit), value, 0, 0));

            return this;
        }

        public AacFlSettingKeyframes Constant(float timeInUnit, float value)
        {
            _mutatedKeyframes.Add(new Keyframe(AsSeconds(timeInUnit), value, 0, float.PositiveInfinity));

            return this;
        }

        public AacFlSettingKeyframes Linear(float timeInUnit, float value)
        {
            float valueEnd = value;
            float valueStart = _mutatedKeyframes.Count == 0 ? value : _mutatedKeyframes.Last().value;
            float timeEnd = AsSeconds(timeInUnit);
            float timeStart = _mutatedKeyframes.Count == 0 ? value : _mutatedKeyframes.Last().time;
            float num = (float) (((double) valueEnd - (double) valueStart) / ((double) timeEnd - (double) timeStart));

            if (_mutatedKeyframes.Count > 0)
            {
                var lastKeyframe = _mutatedKeyframes.Last();
                lastKeyframe.outTangent = num;
                _mutatedKeyframes[_mutatedKeyframes.Count - 1] = lastKeyframe;
            }
            _mutatedKeyframes.Add(new Keyframe(AsSeconds(timeInUnit), value, num, 0.0f));

            return this;
        }

        private float AsSeconds(float timeInUnit)
        {
            switch (_unit)
            {
                case AacFlUnit.Frames:
                    return timeInUnit / 60f;
                case AacFlUnit.Seconds:
                    return timeInUnit;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum AacFlUnit
    {
        Frames, Seconds
    }
}
