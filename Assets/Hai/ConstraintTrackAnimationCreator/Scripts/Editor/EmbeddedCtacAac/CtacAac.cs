﻿using System;
using System.Linq;
using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac.Fluent;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Random = UnityEngine.Random;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac
{
    public static class AacV0
    {
        private static AnimatorController AnimatorOf(AnimatorAsCode component, VRCAvatarDescriptor.AnimLayerType animLayerType)
        {
            return (AnimatorController)component.avatar.baseAnimationLayers.First(it => it.type == animLayerType).animatorController;
        }

        /// <summary>
        /// Initializes the Aac framework.
        ///
        /// By default, the Aac framework has the following properties:
        /// - States have Write Defaults OFF.
        /// - Transition interruptions are set to None.
        /// - Transitions have no exit time.
        /// </summary>
        /// <param name="component"></param>
        /// <typeparam name="TAacType"></typeparam>
        /// <returns></returns>
        internal static AacFlBase<TAacType> Using<TAacType>(TAacType component) where TAacType : AnimatorAsCode
        {
            return new AacFlBase<TAacType>(component);
        }

        internal class AacFlBase<TAacType> where TAacType : AnimatorAsCode
        {
            private readonly TAacType _component;

            public AacFlBase(TAacType component)
            {
                _component = component;
            }

            public AacFlClip NewClip()
            {
                var clip = AacV0.NewClip(_component, Guid.NewGuid().ToString());
                return new AacFlClip(_component, clip);
            }

            public AacFlClip CopyClip(AnimationClip originalClip)
            {
                var newClip = UnityEngine.Object.Instantiate(originalClip);
                var clip = AacV0.NewClipRaw(_component, Guid.NewGuid().ToString(), newClip);
                return new AacFlClip(_component, clip);
            }

            public BlendTree NewBlendTreeAsRaw()
            {
                return AacV0.NewBlendTreeAsRaw(_component, Guid.NewGuid().ToString());
            }

            public AacFlClip NewClip(string name)
            {
                var clip = AacV0.NewClip(_component, name);
                return new AacFlClip(_component, clip);
            }

            public AacFlClip DummyClipLasting(float numberOf, AacFlUnit unit)
            {
                var dummyClip = AacV0.NewClip(_component, $"D({numberOf} {Enum.GetName(typeof(AacFlUnit), unit)})");

                var duration = unit == AacFlUnit.Frames ? numberOf / 60f : numberOf;
                return new AacFlClip(_component, dummyClip)
                    .That(clip => clip.Animates("_ignored", typeof(GameObject), "m_IsActive")
                        .WithUnit(unit, keyframes => keyframes.Constant(0, 0f).Constant(duration, 0f)));
            }

            public TAacType Get()
            {
                return _component;
            }

            public AacFlLayer CreateSupportingFxLayer(string suffix)
            {
                var layerName = (_component.layerNameSuffix + "__" + suffix);
                return CreateLayer(layerName, VRCAvatarDescriptor.AnimLayerType.FX);
            }

            private AacFlLayer CreateLayer(string layerName, VRCAvatarDescriptor.AnimLayerType animLayerType)
            {
                var animator = AnimatorOf(_component, animLayerType);
                var ag = new AacFlAnimatorGenerator(animator, CreateEmptyClip().Clip);
                var machine = ag.CreateOrRemakeLayerAtSameIndex("AAC_" + layerName, 1f, null);

                return new AacFlLayer(animator, _component, machine, "AAC_" + layerName);
            }

            private AacFlClip CreateEmptyClip()
            {
                var emptyClip = DummyClipLasting(1, AacFlUnit.Frames);
                return emptyClip;
            }

            public VrcAssetLibrary VrcAssets()
            {
                return new VrcAssetLibrary();
            }

            public void ResetAssetHolder()
            {
                RemoveAssetsStartingWith("zAutogenerated__" + _component._internal.assetKey + "_");
            }

            private void RemoveAssetsStartingWith(string prefix)
            {
                var allSubAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(_component.assetHolder));
                foreach (var subAsset in allSubAssets)
                {
                    if (subAsset.name.StartsWith(prefix))
                    {
                        AssetDatabase.RemoveObjectFromAsset(subAsset);
                    }
                }
            }
        }

        internal readonly struct AacFlLayer
        {
            private readonly AnimatorController _animatorController;
            private readonly AnimatorAsCode _component;
            private readonly string _fullLayerName;
            public AacFlStateMachine StateMachine { get; }

            public AacFlLayer(AnimatorController animatorController, AnimatorAsCode component, AacFlStateMachine stateMachine, string fullLayerName)
            {
                _animatorController = animatorController;
                _component = component;
                _fullLayerName = fullLayerName;
                StateMachine = stateMachine;
            }

            internal AacFlState NewState(string name, int x, int y)
            {
                return StateMachine.NewState(name, x, y);
            }

            public AacFlBoolParameter BoolParameter(string parameterName) => StateMachine.BackingAnimator().BoolParameter(parameterName);
            public AacFlFloatParameter FloatParameter(string parameterName) => StateMachine.BackingAnimator().FloatParameter(parameterName);

            public AacFlLayer WithAvatarMask(AvatarMask avatarMask)
            {
                var finalFullLayerName = _fullLayerName;
                _animatorController.layers = _animatorController.layers
                    .Select(layer =>
                    {
                        if (layer.name == finalFullLayerName)
                        {
                            layer.avatarMask = avatarMask;
                        }

                        return layer;
                    })
                    .ToArray();

                return this;
            }

            public void WithAvatarMaskNoTransforms()
            {
                ResolveAvatarMask(new Transform[0]);
            }

            private void ResolveAvatarMask(Transform[] paths)
            {
                // FIXME: Fragile
                var avatarMask = new AvatarMask();
                avatarMask.name = "zAutogenerated__" + _component._internal.assetKey + "_" + _fullLayerName + "__AvatarMask";
                avatarMask.hideFlags = HideFlags.None;

                if (paths.Length == 0)
                {
                    avatarMask.transformCount = 1;
                    avatarMask.SetTransformActive(0, false);
                    avatarMask.SetTransformPath(0, "_ignored");
                }
                else
                {
                    avatarMask.transformCount = paths.Length;
                    for (var index = 0; index < paths.Length; index++)
                    {
                        var transform = paths[index];
                        avatarMask.SetTransformActive(index, true);
                        avatarMask.SetTransformPath(index, ResolveRelativePath(_component.avatar.transform, transform));
                    }
                }

                for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                {
                    avatarMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
                }

                AssetDatabase.AddObjectToAsset(avatarMask, _component.assetHolder);

                WithAvatarMask(avatarMask);
            }

            public void ForceParameterInAnimator(AacFlBoolParameter toBeForced, bool value)
            {
                var parameters = _animatorController.parameters;
                foreach (var param in parameters)
                {
                    if (param.name == toBeForced.Name)
                    {
                        param.defaultBool = value;
                    }
                }

                _animatorController.parameters = parameters;
            }

            public void ForceParameterInAnimator(AacFlFloatParameter toBeForced, float value)
            {
                var parameters = _animatorController.parameters;
                foreach (var param in parameters)
                {
                    if (param.name == toBeForced.Name)
                    {
                        param.defaultFloat = value;
                    }
                }

                _animatorController.parameters = parameters;
            }
        }

        public static AnimationClip NewClipToggling(AnimatorAsCode component, GameObject[] togglables, bool value, string suffix)
        {
            return NewClipThat(component, suffix, clip =>
            {
                foreach (var togglable in togglables)
                {
                    AnimationUtility.SetEditorCurve(clip, Binding(component, typeof(GameObject), togglable.transform, "m_IsActive"), OneFrame(value ? 1f : 0f));
                }
            });
        }

        public static AnimationClip NewClipThat(AnimatorAsCode component, string suffix, Action<AnimationClip> action)
        {
            var clip = NewClip(component, suffix);

            action.Invoke(clip);
            return clip;
        }

        private static AnimationClip NewClip(AnimatorAsCode component, string suffix)
        {
            return NewClipRaw(component, suffix, new AnimationClip());
        }

        private static AnimationClip NewClipRaw(AnimatorAsCode component, string suffix, AnimationClip newClipAsset)
        {
            var clip = newClipAsset;
            // clip.name = component.layerNameSuffix + " " + suffix;
            clip.name = "zAutogenerated__" + component._internal.assetKey + "__" + suffix + "_" + Random.Range(0, Int32.MaxValue); // FIXME animation name conflict
            clip.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(clip, component.assetHolder);
            return clip;
        }

        private static BlendTree NewBlendTreeAsRaw(AnimatorAsCode component, string suffix)
        {
            var clip = new BlendTree();
            // clip.name = component.layerNameSuffix + " " + suffix;
            clip.name = "zAutogenerated__" + component._internal.assetKey + "__" + suffix + "_" + Random.Range(0, Int32.MaxValue); // FIXME animation name conflict
            clip.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(clip, component.assetHolder);
            return clip;
        }

        public static EditorCurveBinding Binding(AnimatorAsCode component, Type type, Transform transform, string propertyName)
        {
            return new EditorCurveBinding
            {
                path = ResolveRelativePath(component.avatar.transform, transform),
                type = type,
                propertyName = propertyName
            };
        }

        public static AnimationCurve OneFrame(float desiredValue)
        {
            return AnimationCurve.Constant(0f, 1 / 60f, desiredValue);
        }

        public static AnimationCurve ConstantSeconds(float seconds, float desiredValue)
        {
            return AnimationCurve.Constant(0f, seconds, desiredValue);
        }

        private static string ResolveRelativePath(Transform avatar, Transform item)
        {
            if (item.parent != avatar && item.parent != null)
            {
                return ResolveRelativePath(avatar, item.parent) + "/" + item.name;
            }

            return item.name;
        }

        internal static EditorCurveBinding ToSubBinding(EditorCurveBinding binding, string suffix)
        {
            return new EditorCurveBinding { path = binding.path, type = binding.type, propertyName = binding.propertyName + "." + suffix };
        }
    }

    internal class VrcAssetLibrary
    {
        public AvatarMask LeftHandAvatarMask()
        {
            return AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/VRCSDK/Examples3/Animation/Masks/vrc_Hand Left.mask");
        }

        public AvatarMask RightHandAvatarMask()
        {
            return AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/VRCSDK/Examples3/Animation/Masks/vrc_Hand Right.mask");
        }

        public AnimationClip ProxyForGesture(Gesture gesture, bool masculine)
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/VRCSDK/Examples3/Animation/ProxyAnim/" + ResolveProxyFilename(gesture, masculine));
        }

        private static string ResolveProxyFilename(Gesture gesture, bool masculine)
        {
            switch (gesture)
            {
                case Gesture.Neutral: return masculine ? "proxy_hands_idle.anim" : "proxy_hands_idle2.anim";
                case Gesture.Fist: return "proxy_hands_fist.anim";
                case Gesture.HandOpen: return "proxy_hands_open.anim";
                case Gesture.Fingerpoint: return "proxy_hands_point.anim";
                case Gesture.Victory: return "proxy_hands_peace.anim";
                case Gesture.RockNRoll: return "proxy_hands_rock.anim";
                case Gesture.HandGun: return "proxy_hands_gun.anim";
                case Gesture.ThumbsUp: return "proxy_hands_thumbs_up.anim";
                default:
                    throw new ArgumentOutOfRangeException(nameof(gesture), gesture, null);
            }
        }

        public enum Gesture
        {
            // Specify all the values explicitly because they should be dictated by VRChat, not enumeration order.
            Neutral = 0,
            Fist = 1,
            HandOpen = 2,
            Fingerpoint = 3,
            Victory = 4,
            RockNRoll = 5,
            HandGun = 6,
            ThumbsUp = 7
        }
    }
}
