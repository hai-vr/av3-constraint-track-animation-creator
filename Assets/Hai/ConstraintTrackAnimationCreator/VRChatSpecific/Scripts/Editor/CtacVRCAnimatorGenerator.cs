using System;
using System.Linq;
using Hai.ConstraintTrackAnimationCreator.Scripts.Components;
using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac;
using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac.Fluent;
using Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Components;
using UnityEngine;
using UnityEngine.Animations;

namespace Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Editor
{
    public class CtacVRCAnimatorGenerator
    {
        private readonly AacV0.AacFlBase<CtacController> _aac;

        public CtacVRCAnimatorGenerator(AnimatorAsCode aac)
        {
            _aac = AacV0.Using((CtacController) aac);
        }

        public void Create()
        {
            var data = _aac.Get();

            var cta = data.generator.constraintTrackAnimation;

            GenerateAnimations(cta, out var neverEnabled, out var offButEnabledOnce, out var whenMoving);
            CreateControlLayer();
            CreateMotionLayer(data.generator, neverEnabled, whenMoving, offButEnabledOnce);
        }

        private void GenerateAnimations(ConstraintTrackAnimation cta, out AacFlClip neverEnabled, out AacFlClip offButEnabledOnce, out AacFlClip whenMoving)
        {
            ParentConstraint[] boneConstraints = cta.tracks.SelectMany(track => track.bones).Distinct().ToArray();
            ParentConstraint[] proxyConstraints = cta.tracks.Select(track => track.proxy).ToArray();

            var inactiveClip = cta.optionalAnimationInactive != null ? cta.optionalAnimationInactive : new AnimationClip();
            var activeClip = cta.optionalAnimationActive != null ? cta.optionalAnimationActive : new AnimationClip();

            neverEnabled = _aac.CopyClip(inactiveClip)
                .TogglingComponent(boneConstraints, false)
                .TogglingComponent(proxyConstraints, false)
                .Toggling(cta.parentOfAllTracks, false);

            offButEnabledOnce = _aac.CopyClip(inactiveClip)
                .TogglingComponent(boneConstraints, true)
                .TogglingComponent(proxyConstraints, false)
                .Toggling(cta.parentOfAllTracks, false)
                .That(clip =>
                {
                    clip.Animates(boneConstraints, "m_Weight").WithOneFrame(0f);
                });

            whenMoving = _aac.CopyClip(activeClip)
                .TogglingComponent(boneConstraints, true)
                .TogglingComponent(proxyConstraints, true)
                .Toggling(cta.parentOfAllTracks, true)
                .That(clip =>
                {
                    for (var trackIndex = 0; trackIndex < cta.tracks.Length; trackIndex++)
                    {
                        var singleConstraintTrack = cta.tracks[trackIndex];
                        var timingConfig = trackIndex < cta.optionalTimings.Length ? cta.optionalTimings[trackIndex] : new ConstraintTrackAnimation.TrackTiming
                        {
                            scale = 1f,
                            delayStartSeconds = 0
                        };
                        var scaleCorrected = timingConfig.scale == 0f ? 1f : timingConfig.scale;
                        var timings = singleConstraintTrack.Timings(scaleCorrected * cta.globalTimingScale, timingConfig.delayStartSeconds);

                        clip.Animates(boneConstraints, "m_Weight").WithOneFrame(1f);

                        {
                            // Index 0
                            var index = 0;
                            clip.Animates(proxyConstraints, $"m_Sources.Array.data[{index}].weight")
                                .WithSecondsUnit(keyframes => keyframes.Linear(0f, 1f).Linear(timings[1], 0f));
                            var anyComponents = proxyConstraints
                                .Select(constraint => constraint.GetSource(index).sourceTransform)
                                .Select(transform => transform.GetComponent<ParentConstraint>())
                                .Where(subConstraint => subConstraint != null)
                                .ToArray();
                            clip.Animates(anyComponents, "m_Enabled")
                                .WithSecondsUnit(keyframes => keyframes.Constant(0f, 1f).Constant(timings[1], 0f));
                        }
                        {
                            // Index 1+
                            for (var index = 1; index <= timings.Count + 1; index++)
                            {
                                var zeroTime = timings.Count > index ? timings[index - 1] : timings[timings.Count - 1];
                                var oneTime = timings.Count > index ? timings[index] : timings[timings.Count - 1];
                                var twoTime = timings.Count > index + 1 ? timings[index + 1] : timings[timings.Count - 1];
                                clip.Animates(proxyConstraints, $"m_Sources.Array.data[{index}].weight")
                                    .WithSecondsUnit(keyframes => keyframes.Linear(zeroTime, 0f).Linear(oneTime, 1f).Linear(twoTime, 0f));
                                var anyComponents = proxyConstraints
                                    .Where(constraint => constraint.sourceCount > index)
                                    .Select(constraint => constraint.GetSource(index).sourceTransform)
                                    .Where(transform => transform != null)
                                    .Select(transform => transform.GetComponent<ParentConstraint>())
                                    .Where(subConstraint => subConstraint != null)
                                    .ToArray();
                                clip.Animates(anyComponents, "m_Enabled")
                                    .WithSecondsUnit(keyframes => keyframes.Constant(zeroTime, 0f).Constant(zeroTime + 1 / 60f, 1f).Constant(oneTime, 1f).Constant(twoTime, 0f));
                            }
                        }
                    }
                });
        }

        private void CreateControlLayer()
        {
            var layer = _aac.CreateSupportingFxLayer("Control");
            var data = _aac.Get();
            var generator = data.generator;

            var aapParameter = AapParameter(layer, data.parameterName);
            var manualControlParameter = ManualControlParameter(layer, data.parameterName, generator);
            var autoParameter = AutoParameter(layer, data.parameterName);
            var allowSystemParameter = AllowSystemParameter(layer, data.parameterName, generator);
            if (generator.systemIsAllowedByDefault)
            {
                layer.ForceParameterInAnimator(allowSystemParameter, true);
            }
            else
            {
                if (HasNoCustomAllowSystemName(generator))
                {
                    layer.ForceParameterInAnimator(allowSystemParameter, false);
                }
            }

            var idle = layer.NewState("Idle", 0, 0);
            var animating = layer.NewState("Animating", 0, 1).WithAnimation(_aac.NewClip()
                .That(clip => { clip.Animates("", typeof(Animator), aapParameter.Name).WithSecondsUnit(keyframes => keyframes.Easing(0f, 0f).Easing(9f, 0.9999f)); }));
            var done = layer.NewState("Done", 0, 2).WithAnimation(_aac.NewClip().Looping().That(clip => clip.Animates("", typeof(Animator), aapParameter.Name).WithFixedSeconds(60f, 0.9999f)));
            var manual = layer.NewState("ManualControl", 1, 1)
                .NormalizedTime(manualControlParameter)
                .WithAnimation(_aac.NewClip().Looping().That(clip => clip.Animates("", typeof(Animator), aapParameter.Name).WithSecondsUnit(keyframes => keyframes.Easing(0f, 0f).Easing(1f, 0.9999f))));

            animating.TransitionsTo(done).AfterAnimationFinishes();

            idle.TransitionsTo(animating).When(autoParameter.IsTrue()).And(allowSystemParameter.IsTrue()).And(manualControlParameter.IsLessThan(0.01f));
            animating.TransitionsTo(idle).When(autoParameter.IsFalse()).Or().When(allowSystemParameter.IsFalse());
            done.TransitionsTo(idle).When(autoParameter.IsFalse()).And(manualControlParameter.IsLessThan(0.01f)).Or().When(allowSystemParameter.IsFalse());
            manual.TransitionsTo(idle).When(manualControlParameter.IsLessThan(0.01f)).Or().When(allowSystemParameter.IsFalse());

            idle.TransitionsTo(manual).When(manualControlParameter.IsGreaterThan(0.01f)).And(allowSystemParameter.IsTrue());
            animating.TransitionsTo(manual).When(manualControlParameter.IsGreaterThan(0.01f)).And(allowSystemParameter.IsTrue());

            manual.TransitionsTo(done).When(manualControlParameter.IsGreaterThan(0.99f)).And(allowSystemParameter.IsTrue());
            done.TransitionsTo(manual).When(manualControlParameter.IsLessThan(0.99f)).And(manualControlParameter.IsGreaterThan(0.01f)).And(allowSystemParameter.IsTrue());

            layer.WithAvatarMaskNoTransforms();
        }

        private void CreateMotionLayer(ConstraintTrackVRCGenerator generator, AacFlClip neverEnabled, AacFlClip whenMoving, AacFlClip offButEnabledOnce)
        {
            var layer = _aac.CreateSupportingFxLayer("Motion");

            var aapParam = AapParameter(layer, _aac.Get().parameterName);
            var allowSystemParam = AllowSystemParameter(layer, _aac.Get().parameterName, generator);

            var never = layer.NewState("Never enabled", 0, 0)
                .WithAnimation(neverEnabled);
            var moving = layer.NewState("Moving", 0, 1)
                .WithAnimation(whenMoving)
                .NormalizedTime(aapParam);
            var once = layer.NewState("Once", 0, 2)
                .WithAnimation(offButEnabledOnce);
            never.TransitionsTo(moving).When(aapParam.IsGreaterThan(0.001f)).And(allowSystemParam.IsTrue());
            moving.TransitionsTo(once).When(aapParam.IsLessThan(0.001f)).Or().When(allowSystemParam.IsFalse());
            once.TransitionsTo(moving).When(aapParam.IsGreaterThan(0.001f)).And(allowSystemParam.IsTrue());

            layer.WithAvatarMaskNoTransforms();
        }

        private static AacFlFloatParameter AapParameter(AacV0.AacFlLayer layer, string paramName)
        {
            return layer.FloatParameter(paramName + "__AAP");
        }

        private static AacFlBoolParameter AllowSystemParameter(AacV0.AacFlLayer layer, string paramName, ConstraintTrackVRCGenerator generator)
        {
            return layer.BoolParameter(generator.systemIsAllowedByDefault || HasNoCustomAllowSystemName(generator) ? paramName + "_Allow" : generator.optionalAllowSystemParamName);
        }

        private static bool HasNoCustomAllowSystemName(ConstraintTrackVRCGenerator generator)
        {
            return generator.optionalAllowSystemParamName == null || generator.optionalAllowSystemParamName.Trim() == "";
        }

        private static AacFlBoolParameter AutoParameter(AacV0.AacFlLayer layer, string paramName)
        {
            return layer.BoolParameter($"{paramName}_Auto");
        }

        private static AacFlFloatParameter ManualControlParameter(AacV0.AacFlLayer layer, string paramName, ConstraintTrackVRCGenerator generator)
        {
            var manualControlParameterName = ToManualControlParameterName(generator);
            return layer.FloatParameter(paramName + manualControlParameterName);
        }

        private static string ToManualControlParameterName(ConstraintTrackVRCGenerator generator)
        {
            switch (generator.floatType)
            {
                case ConstraintTrackVRCGenerator.CtacVRCFloatType.Simple: return "_Simple";
                case ConstraintTrackVRCGenerator.CtacVRCFloatType.Complex: return "_Complex";
                case ConstraintTrackVRCGenerator.CtacVRCFloatType.Manual: return "_Manual";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
