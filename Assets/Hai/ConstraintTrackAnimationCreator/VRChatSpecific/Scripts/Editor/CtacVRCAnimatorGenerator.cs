using System;
using System.Linq;
using Hai.ConstraintTrackAnimationCreator.Scripts.Components;
using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac;
using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac.Fluent;
using Hai.ConstraintTrackAnimationCreator.VRChatSpecific.Scripts.Components;
using UnityEditor.Animations;
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
            _aac.ResetAssetHolder();
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
            ScaleConstraint[] maybeBoneScaleConstraints = boneConstraints.Select(constraint => constraint.GetComponent<ScaleConstraint>()).Where(constraint => constraint != null).ToArray();
            ScaleConstraint[] maybeProxyScaleConstraints = proxyConstraints.Select(constraint => constraint.GetComponent<ScaleConstraint>()).Where(constraint => constraint != null).ToArray();

            var inactiveClip = cta.optionalAnimationInactive != null ? cta.optionalAnimationInactive : new AnimationClip();
            var activeClip = cta.optionalAnimationActive != null ? cta.optionalAnimationActive : new AnimationClip();

            neverEnabled = _aac.CopyClip(inactiveClip)
                .TogglingComponent(boneConstraints, false)
                .TogglingComponent(proxyConstraints, false)
                .TogglingComponent(maybeBoneScaleConstraints, false)
                .TogglingComponent(maybeProxyScaleConstraints, false)
                .Toggling(cta.parentOfAllTracks, false);

            offButEnabledOnce = _aac.CopyClip(inactiveClip)
                .TogglingComponent(boneConstraints, true)
                .TogglingComponent(proxyConstraints, false)
                .TogglingComponent(maybeBoneScaleConstraints, false)
                .TogglingComponent(maybeProxyScaleConstraints, false)
                .Toggling(cta.parentOfAllTracks, false)
                .That(clip =>
                {
                    clip.Animates(boneConstraints, "m_Weight").WithOneFrame(0f);
                });

            whenMoving = _aac.CopyClip(activeClip)
                .TogglingComponent(boneConstraints, true)
                .TogglingComponent(proxyConstraints, true)
                .TogglingComponent(maybeBoneScaleConstraints, true)
                .TogglingComponent(maybeProxyScaleConstraints, true)
                .Toggling(cta.parentOfAllTracks, true)
                .That(clip =>
                {
                    clip.Animates(boneConstraints, "m_Weight").WithOneFrame(1f);

                    for (var trackIndex = 0; trackIndex < cta.tracks.Length; trackIndex++)
                    {
                        var singleConstraintTrack = cta.tracks[trackIndex];
                        var scaleCorrected = singleConstraintTrack.timingScale == 0f ? 1f : singleConstraintTrack.timingScale;
                        var timings = singleConstraintTrack.Timings(scaleCorrected * cta.globalTimingScale, singleConstraintTrack.timingDelayStartSeconds);


                        var currentTrackProxyConstraints = new []{singleConstraintTrack.proxy};
                        var currentTrackMultiProxyConstraints = new Component[]{singleConstraintTrack.proxy, singleConstraintTrack.proxy.GetComponent<ScaleConstraint>()}
                            .Where(component => component != null)
                            .ToArray();
                        {
                            // Index 0
                            var index = 0;
                            clip.Animates(currentTrackMultiProxyConstraints, $"m_Sources.Array.data[{index}].weight")
                                .WithSecondsUnit(keyframes => keyframes.Linear(0f + 1 / 60f, 1f).Linear(timings[1], 0f));
                            var anyComponents = currentTrackProxyConstraints
                                .Select(constraint => constraint.GetSource(index).sourceTransform)
                                .Select(transform => transform.GetComponent<ParentConstraint>())
                                .Where(subConstraint => subConstraint != null)
                                .ToArray();
                            clip.Animates(anyComponents, "m_Enabled")
                                .WithSecondsUnit(keyframes => keyframes.Constant(0f, 1f).Constant(timings[1] + 1 / 60f, 0f));
                        }
                        {
                            // Index 1+
                            for (var index = 1; index < timings.Count; index++)
                            {
                                var zeroTime = timings.Count > index ? timings[index - 1] : timings[timings.Count - 1];
                                var oneTime = timings.Count > index ? timings[index] : timings[timings.Count - 1];
                                var twoTime = timings.Count > index + 1 ? timings[index + 1] : timings[timings.Count - 1];
                                var isLast = timings.Count - 1 == index;
                                clip.Animates(currentTrackMultiProxyConstraints, $"m_Sources.Array.data[{index}].weight")
                                    .WithSecondsUnit(keyframes =>
                                    {
                                        // Hack: if oneTime == twoTime, it creates a NaN tangent, so add 1 / 60f to it to prevent it
                                        // Can't find a way to fix this the right way
                                        var preventNaN = oneTime == twoTime ? 1 / 60f : 0f;
                                        keyframes.Linear(zeroTime + 1 / 60f, 0f).Linear(oneTime + 1 / 60f, 1f).Linear(twoTime + 1 / 60f + preventNaN, isLast ? 1f : 0f);
                                    });
                                var anyComponents = currentTrackProxyConstraints
                                    .Where(constraint => constraint.sourceCount > index)
                                    .Select(constraint => constraint.GetSource(index).sourceTransform)
                                    .Where(transform => transform != null)
                                    .Select(transform => transform.GetComponent<ParentConstraint>())
                                    .Where(subConstraint => subConstraint != null)
                                    .ToArray();
                                clip.Animates(anyComponents, "m_Enabled")
                                    .WithSecondsUnit(keyframes => keyframes.Constant(zeroTime, 0f).Constant(zeroTime + 1 / 60f, 1f).Constant(oneTime, 1f).Constant(twoTime + 2 / 60f, isLast ? 1f : 0f));
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
            var useSmoothing = data.generator.manualIncludeSmoothing;

            var aapParameter = AapParameter(layer, data.parameterName);
            var manualControlParameter = ManualControlParameter(layer, data.parameterName);
            var customControlParameter = CustomControlParameter(layer, data.parameterName, generator);
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

            var autoDurationSeconds = data.generator.autoDurationSeconds;

            var idle = layer.NewState("Idle", 0, 0);
            var auto = layer.NewState("Auto", 0, 1).WithAnimation(_aac.NewClip()
                .That(clip => { clip.AnimatingAnimator(aapParameter).WithSecondsUnit(keyframes => keyframes.Easing(0f, 0f).Easing(autoDurationSeconds, 0.9999f)); }));
            var reverse = layer.NewState("Reverse", 1, 1).WithAnimation(_aac.NewClip()
                .That(clip => { clip.AnimatingAnimator(aapParameter).WithSecondsUnit(keyframes => keyframes.Easing(0f, 0.9999f).Easing(autoDurationSeconds, 0f)); }));
            var done = layer.NewState("Done", 0, 2).WithAnimation(_aac.NewClip().Looping().That(clip => clip.AnimatingAnimator(aapParameter).WithFixedSeconds(60f, 0.9999f)));
            AacFlState manual;
            if (useSmoothing)
            {
                var smoothingFactorParameter = SmoothingFactorParameter(layer, data.parameterName);
                layer.ForceParameterInAnimator(smoothingFactorParameter, data.generator.smoothingFactor);

                // Manual Smoothed Trees
                var zeroClip = _aac.NewClip().That(clip => clip.AnimatingAnimator(aapParameter).WithOneFrame(0f));
                var oneClip = _aac.NewClip().That(clip => clip.AnimatingAnimator(aapParameter).WithOneFrame(1f));
                var proxyTree = CreateProxyTree(manualControlParameter, zeroClip, oneClip);
                var smoothingTree = CreateSmoothingTree(aapParameter, zeroClip, oneClip);
                var factorTree = CreateFactorTree(smoothingFactorParameter, proxyTree, smoothingTree);
                manual = layer.NewState("ManualControl", 3, 1) // Smoothed
                    .WithAnimation(factorTree);
            }
            else
            {
                manual = layer.NewState("ManualControl", 3, 1)
                    .NormalizedTime(manualControlParameter)
                    .WithAnimation(_aac.NewClip().Looping().That(clip => clip.AnimatingAnimator(aapParameter).WithSecondsUnit(keyframes => keyframes.Easing(0f, 0f).Easing(1f, 0.9999f))));
            }

            var custom = layer.NewState("CustomControl", 5, 1)
                .NormalizedTime(customControlParameter)
                .WithAnimation(_aac.NewClip().Looping().That(clip => clip.AnimatingAnimator(aapParameter).WithSecondsUnit(keyframes => keyframes.Easing(0f, 0f).Easing(1f, 0.9999f))));

            // Automatic Cycle
            idle.TransitionsTo(auto).When(autoParameter.IsTrue()).And(allowSystemParameter.IsTrue()).And(manualControlParameter.IsLessThan(0.01f));
            auto.TransitionsTo(done).AfterAnimationFinishes();
            done.TransitionsTo(reverse).When(autoParameter.IsFalse()).And(allowSystemParameter.IsTrue()).And(manualControlParameter.IsLessThan(0.01f));
            reverse.TransitionsTo(idle).AfterAnimationFinishes();

            // Automatic Cycle without Reverse
            // auto.TransitionsTo(idle).When(autoParameter.IsFalse());
            // done.TransitionsTo(idle).When(autoParameter.IsFalse()).And(manualControlParameter.IsLessThan(0.01f));

            // Custom Cycle
            idle.TransitionsTo(custom).When(customControlParameter.IsGreaterThan(0.01f))
                .And(manualControlParameter.IsLessThan(0.01f)).And(allowSystemParameter.IsTrue()).And(autoParameter.IsFalse());
            custom.TransitionsTo(done).When(customControlParameter.IsGreaterThan(0.99f)).And(allowSystemParameter.IsTrue());
            done.TransitionsTo(custom).When(customControlParameter.IsLessThan(0.99f)).And(customControlParameter.IsGreaterThan(0.01f))
                .And(manualControlParameter.IsLessThan(0.01f)).And(allowSystemParameter.IsTrue()).And(autoParameter.IsFalse());
            custom.TransitionsTo(idle).When(customControlParameter.IsLessThan(0.01f));
            custom.TransitionsTo(auto).When(autoParameter.IsTrue()).And(allowSystemParameter.IsTrue());

            // Manual Cycle
            foreach (var moveToManual in new[] {idle, auto, reverse, custom})
            {
                moveToManual.TransitionsTo(manual).When(manualControlParameter.IsGreaterThan(0.01f)).And(allowSystemParameter.IsTrue());
            }

            if (useSmoothing)
            {
                manual.TransitionsTo(done).When(aapParameter.IsGreaterThan(0.99f)).And(allowSystemParameter.IsTrue());
                done.TransitionsTo(manual).When(manualControlParameter.IsLessThan(0.99f)).And(manualControlParameter.IsGreaterThan(0.01f)).And(allowSystemParameter.IsTrue());
                manual.TransitionsTo(idle).When(manualControlParameter.IsLessThan(0.01f)).And(aapParameter.IsLessThan(0.01f));
            }
            else
            {
                manual.TransitionsTo(done).When(manualControlParameter.IsGreaterThan(0.99f)).And(allowSystemParameter.IsTrue());
                done.TransitionsTo(manual).When(manualControlParameter.IsLessThan(0.99f)).And(manualControlParameter.IsGreaterThan(0.01f)).And(allowSystemParameter.IsTrue());
                manual.TransitionsTo(idle).When(manualControlParameter.IsLessThan(0.01f));
            }

            // Allow System Immediate Shutoff
            foreach (var cancelWhenNotAllowed in new[] {auto, reverse, manual, custom, done})
            {
                cancelWhenNotAllowed.TransitionsTo(idle).When(allowSystemParameter.IsFalse());
            }

            layer.WithAvatarMaskNoTransforms();
        }

        private BlendTree CreateProxyTree(AacFlFloatParameter manualControlParameter, AacFlClip zeroClip, AacFlClip oneClip)
        {
            var proxyTree = _aac.NewBlendTreeAsRaw();
            proxyTree.blendParameter = manualControlParameter.Name;
            proxyTree.blendType = BlendTreeType.Simple1D;
            proxyTree.minThreshold = 0;
            proxyTree.maxThreshold = 1;
            proxyTree.useAutomaticThresholds = true;
            proxyTree.children = new[]
            {
                new ChildMotion {motion = zeroClip.Clip, timeScale = 1, threshold = 0},
                new ChildMotion {motion = oneClip.Clip, timeScale = 1, threshold = 1}
            };
            return proxyTree;
        }

        private BlendTree CreateSmoothingTree(AacFlFloatParameter aapParameter, AacFlClip zeroClip, AacFlClip oneClip)
        {
            var smoothingTree = _aac.NewBlendTreeAsRaw();
            smoothingTree.blendParameter = aapParameter.Name;
            smoothingTree.blendType = BlendTreeType.Simple1D;
            smoothingTree.minThreshold = 0;
            smoothingTree.maxThreshold = 1;
            smoothingTree.useAutomaticThresholds = true;
            smoothingTree.children = new[]
            {
                new ChildMotion {motion = zeroClip.Clip, timeScale = 1, threshold = 0},
                new ChildMotion {motion = oneClip.Clip, timeScale = 1, threshold = 1}
            };
            return smoothingTree;
        }

        private BlendTree CreateFactorTree(AacFlFloatParameter smoothingFactorParameter, BlendTree proxyTree, BlendTree smoothingTree)
        {
            var factorTree = _aac.NewBlendTreeAsRaw();
            {
                factorTree.blendParameter = smoothingFactorParameter.Name;
                factorTree.blendType = BlendTreeType.Simple1D;
                factorTree.minThreshold = 0;
                factorTree.maxThreshold = 1;
                factorTree.useAutomaticThresholds = true;
                factorTree.children = new[]
                {
                    new ChildMotion {motion = proxyTree, timeScale = 1, threshold = 0},
                    new ChildMotion {motion = smoothingTree, timeScale = 1, threshold = 1}
                };
            }
            return factorTree;
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

        private static AacFlFloatParameter SmoothingFactorParameter(AacV0.AacFlLayer layer, string paramName)
        {
            return layer.FloatParameter(paramName + "_SmoothingFactor");
        }

        private static bool HasNoCustomAllowSystemName(ConstraintTrackVRCGenerator generator)
        {
            return generator.optionalAllowSystemParamName == null || generator.optionalAllowSystemParamName.Trim() == "";
        }

        private static AacFlBoolParameter AutoParameter(AacV0.AacFlLayer layer, string paramName)
        {
            return layer.BoolParameter($"{paramName}_Auto");
        }

        private static AacFlFloatParameter ManualControlParameter(AacV0.AacFlLayer layer, string paramName)
        {
            return layer.FloatParameter($"{paramName}_Manual");
        }

        private static AacFlFloatParameter CustomControlParameter(AacV0.AacFlLayer layer, string paramName, ConstraintTrackVRCGenerator generator)
        {
            return layer.FloatParameter(string.IsNullOrEmpty(generator.customParameter) ? $"{paramName}_Custom" : $"{generator.customParameter}");
        }
    }
}
