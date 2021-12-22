using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac.Fluent
{
    internal class AacFlBackingAnimator
    {
        private readonly AacFlAnimatorGenerator _animatorAnimatorGenerator;

        internal AacFlBackingAnimator(AacFlAnimatorGenerator animatorGenerator)
        {
            _animatorAnimatorGenerator = animatorGenerator;
        }

        public AacFlBoolParameter BoolParameter(string parameterName)
        {
            var result = AacFlBoolParameter.Internally(parameterName);
            _animatorAnimatorGenerator.CreateParamsAsNeeded(result);
            return result;
        }

        public AacFlFloatParameter FloatParameter(string parameterName)
        {
            var result = AacFlFloatParameter.Internally(parameterName);
            _animatorAnimatorGenerator.CreateParamsAsNeeded(result);
            return result;
        }

        public AacFlIntParameter IntParameter(string parameterName)
        {
            var result = AacFlIntParameter.Internally(parameterName);
            _animatorAnimatorGenerator.CreateParamsAsNeeded(result);
            return result;
        }

        public AacFlBoolParameterGroup BoolParameters(params string[] parameterNames)
        {
            var result = AacFlBoolParameterGroup.Internally(parameterNames);
            _animatorAnimatorGenerator.CreateParamsAsNeeded(result.ToList().ToArray());
            return result;
        }

        public AacFlFloatParameterGroup FloatParameters(params string[] parameterNames)
        {
            var result = AacFlFloatParameterGroup.Internally(parameterNames);
            _animatorAnimatorGenerator.CreateParamsAsNeeded(result.ToList().ToArray());
            return result;
        }

        public AacFlIntParameterGroup IntParameters(params string[] parameterNames)
        {
            var result = AacFlIntParameterGroup.Internally(parameterNames);
            _animatorAnimatorGenerator.CreateParamsAsNeeded(result.ToList().ToArray());
            return result;
        }

        public AacFlBoolParameterGroup BoolParameters(params AacFlBoolParameter[] parameters)
        {
            var result = AacFlBoolParameterGroup.Internally(parameters.Select(parameter => parameter.Name).ToArray());
            _animatorAnimatorGenerator.CreateParamsAsNeeded(parameters);
            return result;
        }

        public AacFlFloatParameterGroup FloatParameters(params AacFlFloatParameter[] parameters)
        {
            var result = AacFlFloatParameterGroup.Internally(parameters.Select(parameter => parameter.Name).ToArray());
            _animatorAnimatorGenerator.CreateParamsAsNeeded(parameters);
            return result;
        }

        public AacFlIntParameterGroup IntParameters(params AacFlIntParameter[] parameters)
        {
            var result = AacFlIntParameterGroup.Internally(parameters.Select(parameter => parameter.Name).ToArray());
            _animatorAnimatorGenerator.CreateParamsAsNeeded(parameters);
            return result;
        }
    }

    internal class AacFlStateMachine
    {
        private readonly AnimatorStateMachine _machine;
        private readonly AnimationClip _emptyClip;
        private readonly AacFlBackingAnimator _backingAnimator;

        internal AacFlStateMachine(AnimatorStateMachine machine, AnimationClip emptyClip, AacFlBackingAnimator backingAnimator)
        {
            _machine = machine;
            _emptyClip = emptyClip;
            _backingAnimator = backingAnimator;
        }

        internal AacFlBackingAnimator BackingAnimator()
        {
            return _backingAnimator;
        }

        internal AacFlStateMachine WithEntryPosition(int x, int y)
        {
            _machine.entryPosition = AacFlAnimatorGenerator.GridPosition(x, y);
            return this;
        }

        internal AacFlStateMachine WithExitPosition(int x, int y)
        {
            _machine.exitPosition = AacFlAnimatorGenerator.GridPosition(x, y);
            return this;
        }

        internal AacFlStateMachine WithAnyStatePosition(int x, int y)
        {
            _machine.anyStatePosition = AacFlAnimatorGenerator.GridPosition(x, y);
            return this;
        }

        internal AacFlState NewState(string name, int x, int y)
        {
            var state = _machine.AddState(name, AacFlAnimatorGenerator.GridPosition(x, y));
            state.motion = _emptyClip;
            state.writeDefaultValues = false;

            return new AacFlState(state, _machine);
        }

        public AacFlTransition AnyTransitionsTo(AacFlState destination)
        {
            return AnyTransition(destination, _machine);
        }

        internal static AacFlTransition AnyTransition(AacFlState destination, AnimatorStateMachine animatorStateMachine)
        {
            return new AacFlTransition(AacFlState.NewDefaultTransition(animatorStateMachine.AddAnyStateTransition(destination.State)), animatorStateMachine, null, destination.State);
        }

        public AnimatorStateMachine ExposeMachine()
        {
            return _machine;
        }
    }

    internal class AacFlState
    {
        internal readonly AnimatorState State;
        private readonly AnimatorStateMachine _machine;
        private readonly AacFlBackingAnimator _backingAnimator;
        private VRCAvatarParameterDriver _driver;
        private VRCAnimatorTrackingControl _tracking;
        private VRCAnimatorLocomotionControl _locomotionControl;

        internal AacFlState(AnimatorState state, AnimatorStateMachine machine)
        {
            State = state;
            _machine = machine;
        }

        internal AacFlState WithAnimation(Motion clip)
        {
            State.motion = clip;
            return this;
        }

        internal AacFlState WithAnimation(AacFlClip clip)
        {
            State.motion = clip.Clip;
            return this;
        }

        internal AacFlTransition TransitionsTo(AacFlState destination)
        {
            return new AacFlTransition(NewDefaultTransition(State.AddTransition(destination.State)), _machine, State, destination.State);
        }

        internal AacFlTransition TransitionsFromAny()
        {
            return AacFlStateMachine.AnyTransition(this, _machine);
        }

        internal AacFlState AutomaticallyMovesTo(AacFlState destination)
        {
            var transition = NewDefaultTransition(State.AddTransition(destination.State));
            transition.hasExitTime = true;
            return this;
        }

        internal AacFlState __ForceToMoveInstantlyTo(AacFlState destination)
        {
            var transition = NewDefaultTransition(State.AddTransition(destination.State));
            transition.hasExitTime = true;
            transition.exitTime = 0;
            return this;
        }

        internal AacFlTransition Exits()
        {
            return new AacFlTransition(NewDefaultTransition(State.AddExitTransition()), _machine, State, null);
        }

        internal static AnimatorStateTransition NewDefaultTransition(AnimatorStateTransition transition)
        {
            transition.duration = 0;
            transition.hasExitTime = false;
            transition.exitTime = 0;
            transition.hasFixedDuration = true;
            transition.offset = 0;
            transition.interruptionSource = TransitionInterruptionSource.None;
            transition.orderedInterruption = true;
            transition.canTransitionToSelf = true;
            return transition;
        }

        internal AacFlState Drives(AacFlIntParameter parameter, int value)
        {
            CreateDriverBehaviorIfNotExists();
            _driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Set,
                name = parameter.Name, value = value
            });
            return this;
        }

        internal AacFlState Drives(AacFlFloatParameter parameter, float value)
        {
            CreateDriverBehaviorIfNotExists();
            _driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Set,
                name = parameter.Name, value = value
            });
            return this;
        }

        internal AacFlState DrivingIncreases(AacFlFloatParameter parameter, float additiveValue)
        {
            CreateDriverBehaviorIfNotExists();
            _driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Add,
                name = parameter.Name, value = additiveValue
            });
            return this;
        }

        internal AacFlState DrivingDecreases(AacFlFloatParameter parameter, float positiveValueToDecreaseBy)
        {
            CreateDriverBehaviorIfNotExists();
            _driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Add,
                name = parameter.Name, value = -positiveValueToDecreaseBy
            });
            return this;
        }

        internal AacFlState DrivingRandomizesLocally(AacFlFloatParameter parameter, float min, float max)
        {
            CreateDriverBehaviorIfNotExists();
            _driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Random,
                name = parameter.Name, valueMin = min, valueMax = max
            });
            return this;
        }

        internal AacFlState DrivingRandomizesLocally(AacFlIntParameter parameter, int min, int max)
        {
            CreateDriverBehaviorIfNotExists();
            _driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Random,
                name = parameter.Name, valueMin = min, valueMax = max
            });
            _driver.localOnly = true;
            return this;
        }

        internal AacFlState Drives(AacFlBoolParameter parameter, bool value)
        {
            CreateDriverBehaviorIfNotExists();
            _driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                name = parameter.Name, value = value ? 1 : 0
            });
            return this;
        }

        internal AacFlState Drives(AacFlBoolParameterGroup parameters, bool value)
        {
            CreateDriverBehaviorIfNotExists();
            foreach (var parameter in parameters.ToList())
            {
                _driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    name = parameter.Name, value = value ? 1 : 0
                });
            }
            return this;
        }

        private void CreateDriverBehaviorIfNotExists()
        {
            if (_driver != null) return;
            _driver = State.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            _driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>();
        }

        public AacFlState WithWriteDefaultsSetTo(bool shouldWriteDefaults)
        {
            State.writeDefaultValues = shouldWriteDefaults;
            return this;
        }

        public AacFlState TrackingTracks(TrackingElement element)
        {
            CreateTrackingBehaviorIfNotExists();
            SettingElementTo(element, VRC_AnimatorTrackingControl.TrackingType.Tracking);

            return this;
        }

        public AacFlState TrackingAnimates(TrackingElement element)
        {
            CreateTrackingBehaviorIfNotExists();
            SettingElementTo(element, VRC_AnimatorTrackingControl.TrackingType.Animation);

            return this;
        }

        public AacFlState TrackingSets(TrackingElement element, VRC_AnimatorTrackingControl.TrackingType trackingType)
        {
            CreateTrackingBehaviorIfNotExists();
            SettingElementTo(element, trackingType);

            return this;
        }

        public AacFlState LocomotionEnabled()
        {
            CreateLocomotionBehaviorIfNotExists();
            _locomotionControl.disableLocomotion = false;

            return this;
        }

        public AacFlState LocomotionDisabled()
        {
            CreateLocomotionBehaviorIfNotExists();
            _locomotionControl.disableLocomotion = true;

            return this;
        }

        public AacFlState NormalizedTime(AacFlFloatParameter floatParam)
        {
            State.timeParameterActive = true;
            State.timeParameter = floatParam.Name;

            return this;
        }

        private void SettingElementTo(TrackingElement element, VRC_AnimatorTrackingControl.TrackingType target)
        {
            switch (element)
            {
                case TrackingElement.Head:
                    _tracking.trackingHead = target;
                    break;
                case TrackingElement.LeftHand:
                    _tracking.trackingLeftHand = target;
                    break;
                case TrackingElement.RightHand:
                    _tracking.trackingRightHand = target;
                    break;
                case TrackingElement.Hip:
                    _tracking.trackingHip = target;
                    break;
                case TrackingElement.LeftFoot:
                    _tracking.trackingLeftFoot = target;
                    break;
                case TrackingElement.RightFoot:
                    _tracking.trackingRightFoot = target;
                    break;
                case TrackingElement.LeftFingers:
                    _tracking.trackingLeftFingers = target;
                    break;
                case TrackingElement.RightFingers:
                    _tracking.trackingRightFingers = target;
                    break;
                case TrackingElement.Eyes:
                    _tracking.trackingEyes = target;
                    break;
                case TrackingElement.Mouth:
                    _tracking.trackingMouth = target;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(element), element, null);
            }
        }

        private void CreateTrackingBehaviorIfNotExists()
        {
            if (_tracking != null) return;
            _tracking = State.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        }


        private void CreateLocomotionBehaviorIfNotExists()
        {
            if (_locomotionControl != null) return;
            _locomotionControl = State.AddStateMachineBehaviour<VRCAnimatorLocomotionControl>();
        }

        internal enum TrackingElement
        {
            Head,
            LeftHand,
            RightHand,
            Hip,
            LeftFoot,
            RightFoot,
            LeftFingers,
            RightFingers,
            Eyes,
            Mouth
        }

        public AacFlState WithSpeed(AacFlFloatParameter parameter)
        {
            State.speedParameter = parameter.Name;
            State.speedParameterActive = true;

            return this;
        }
    }

    public class AacFlTransition : AacFlNewTransitionContinuation
    {
        private readonly AnimatorStateTransition _transition;

        public AacFlTransition(AnimatorStateTransition transition, AnimatorStateMachine machine, AnimatorState sourceNullableIfAny, AnimatorState destinationNullableIfExits) : base(transition, machine, sourceNullableIfAny, destinationNullableIfExits)
        {
            _transition = transition;
        }

        public AacFlTransition WithSourceInterruption()
        {
            _transition.interruptionSource = TransitionInterruptionSource.Source;
            return this;
        }

        public AacFlTransition WithTransitionDurationSeconds(float transitionDuration)
        {
            _transition.duration = transitionDuration;
            return this;
        }

        public AacFlTransition WithNoOrderedInterruption()
        {
            _transition.orderedInterruption = false;
            return this;
        }

        public AacFlTransition WithNoTransitionToSelf()
        {
            _transition.canTransitionToSelf = false;
            return this;
        }

        public AacFlTransition AfterAnimationFinishes()
        {
            _transition.hasExitTime = true;
            _transition.exitTime = 1;

            return this;
        }

        public AacFlTransition AfterAnimationIsAtLeastAtPercent(float exitTimeNormalized)
        {
            _transition.hasExitTime = true;
            _transition.exitTime = exitTimeNormalized;

            return this;
        }

        public AacFlTransition WithTransitionDurationPercent(float transitionDurationNormalized)
        {
            _transition.hasFixedDuration = false;
            _transition.duration = transitionDurationNormalized;

            return this;
        }
    }

    public interface IAacFlCondition
    {
        void ApplyTo(AacFlCondition appender);
    }

    public interface IAacFlOrCondition
    {
        List<AacFlTransitionContinuation> ApplyTo(AacFlNewTransitionContinuation firstContinuation);
    }

    public class AacFlCondition
    {
        private readonly AnimatorStateTransition _transition;

        internal AacFlCondition(AnimatorStateTransition transition)
        {
            _transition = transition;
        }

        public AacFlCondition Add(string parameter, AnimatorConditionMode mode, float threshold)
        {
            _transition.AddCondition(mode, threshold, parameter);
            return this;
        }
    }

    public class AacFlNewTransitionContinuation
    {
        private readonly AnimatorStateTransition _transition;
        private readonly AnimatorStateMachine _machine;
        private readonly AnimatorState _sourceNullableIfAny;
        private readonly AnimatorState _destinationNullableIfExits;

        internal AacFlNewTransitionContinuation(AnimatorStateTransition transition, AnimatorStateMachine machine, AnimatorState sourceNullableIfAny, AnimatorState destinationNullableIfExits)
        {
            _transition = transition;
            _machine = machine;
            _sourceNullableIfAny = sourceNullableIfAny;
            _destinationNullableIfExits = destinationNullableIfExits;
        }

        /// Adds a condition to the transition.
        ///
        /// The settings of the transition can no longer be modified after this point.
        /// <example>
        /// <code>
        /// .When(_aac.BoolParameter(_aac.Get().myBoolParameterName).IsTrue())
        /// .And(_aac.BoolParameter(_aac.Get().myIntParameterName).IsGreaterThan(2))
        /// .And(AacAv3.ItIsLocal())
        /// .Or()
        /// .When(_aac.BoolParameters(
        ///     _aac.Get().myBoolParameterName,
        ///     _aac.Get().myOtherBoolParameterName
        /// ).AreTrue())
        /// .And(AacAv3.ItIsRemote());
        /// </code>
        /// </example>
        public AacFlTransitionContinuation When(IAacFlCondition action)
        {
            action.ApplyTo(new AacFlCondition(_transition));
            return AsContinuationWithOr();
        }

        /// <summary>
        /// Applies a series of conditions to this transition, but this series of conditions cannot include an Or operator.
        /// </summary>
        /// <param name="actionsWithoutOr"></param>
        /// <returns></returns>
        internal AacFlTransitionContinuation When(Action<AacFlTransitionContinuationWithoutOr> actionsWithoutOr)
        {
            actionsWithoutOr(new AacFlTransitionContinuationWithoutOr(_transition));
            return AsContinuationWithOr();
        }

        /// <summary>
        /// Applies a series of conditions, and this series may contain Or operators. However, the result can not be followed by an And operator. It can only be an Or operator.
        /// </summary>
        /// <param name="actionsWithOr"></param>
        /// <returns></returns>
        internal AacFlTransitionContinuationOnlyOr When(Action<AacFlNewTransitionContinuation> actionsWithOr)
        {
            actionsWithOr(this);
            return AsContinuationOnlyOr();
        }

        /// <summary>
        /// Applies a series of conditions, and this series may contain Or operators. All And operators that follow will apply to all the conditions generated by this series, until the next Or operator.
        /// </summary>
        /// <param name="actionsWithOr"></param>
        /// <returns></returns>
        internal AacFlMultiTransitionContinuation When(IAacFlOrCondition actionsWithOr)
        {
            var pendingContinuations = actionsWithOr.ApplyTo(this);
            return new AacFlMultiTransitionContinuation(_transition, _machine, _sourceNullableIfAny, _destinationNullableIfExits, pendingContinuations);
        }

        internal AacFlTransitionContinuation StackingConditions()
        {
            return AsContinuationWithOr();
        }

        private AacFlTransitionContinuation AsContinuationWithOr()
        {
            return new AacFlTransitionContinuation(_transition, _machine, _sourceNullableIfAny, _destinationNullableIfExits);
        }

        private AacFlTransitionContinuationOnlyOr AsContinuationOnlyOr()
        {
            return new AacFlTransitionContinuationOnlyOr(_transition, _machine, _sourceNullableIfAny, _destinationNullableIfExits);
        }
    }

    public class AacFlTransitionContinuation
    {
        private readonly AnimatorStateTransition _transition;
        private readonly AnimatorStateMachine _machine;
        private readonly AnimatorState _sourceNullableIfAny;
        private readonly AnimatorState _destinationNullableIfExits;

        internal AacFlTransitionContinuation(AnimatorStateTransition transition, AnimatorStateMachine machine, AnimatorState sourceNullableIfAny, AnimatorState destinationNullableIfExits)
        {
            _transition = transition;
            _machine = machine;
            _sourceNullableIfAny = sourceNullableIfAny;
            _destinationNullableIfExits = destinationNullableIfExits;
        }

        /// Adds an additional condition to the transition that requires all preceding conditions to be true.
        /// <example>
        /// <code>
        /// .When(_aac.BoolParameter(_aac.Get().myBoolParameterName).IsTrue())
        /// .And(_aac.BoolParameter(_aac.Get().myIntParameterName).IsGreaterThan(2))
        /// .And(AacAv3.ItIsLocal())
        /// .Or()
        /// .When(_aac.BoolParameters(
        ///     _aac.Get().myBoolParameterName,
        ///     _aac.Get().myOtherBoolParameterName
        /// ).AreTrue())
        /// .And(AacAv3.ItIsRemote());
        /// </code>
        /// </example>
        internal AacFlTransitionContinuation And(IAacFlCondition action)
        {
            action.ApplyTo(new AacFlCondition(_transition));
            return this;
        }

        /// <summary>
        /// Applies a series of conditions to this transition. The conditions cannot include an Or operator.
        /// </summary>
        /// <param name="actionsWithoutOr"></param>
        /// <returns></returns>
        internal AacFlTransitionContinuation And(Action<AacFlTransitionContinuationWithoutOr> actionsWithoutOr)
        {
            actionsWithoutOr(new AacFlTransitionContinuationWithoutOr(_transition));
            return this;
        }

        /// <summary>
        /// Creates a new transition with identical settings but having no conditions defined yet.
        /// </summary>
        /// <example>
        /// <code>
        /// .When(_aac.BoolParameter(_aac.Get().myBoolParameterName).IsTrue())
        /// .And(_aac.BoolParameter(_aac.Get().myIntParameterName).IsGreaterThan(2))
        /// .And(AacAv3.ItIsLocal())
        /// .Or()
        /// .When(_aac.BoolParameters(
        ///     _aac.Get().myBoolParameterName,
        ///     _aac.Get().myOtherBoolParameterName
        /// ).AreTrue())
        /// .And(AacAv3.ItIsRemote());
        /// </code>
        /// </example>
        public AacFlNewTransitionContinuation Or()
        {
            var newTransition = NewTransition();
            newTransition.duration = _transition.duration;
            newTransition.offset = _transition.offset;
            newTransition.interruptionSource = _transition.interruptionSource;
            newTransition.orderedInterruption = _transition.orderedInterruption;
            newTransition.exitTime = _transition.exitTime;
            newTransition.hasExitTime = _transition.hasExitTime;
            newTransition.hasFixedDuration = _transition.hasFixedDuration;
            newTransition.canTransitionToSelf = _transition.canTransitionToSelf;
            return new AacFlNewTransitionContinuation(newTransition, _machine, _sourceNullableIfAny, _destinationNullableIfExits);
        }

        private AnimatorStateTransition NewTransition()
        {
            if (_sourceNullableIfAny == null)
            {
                return _machine.AddAnyStateTransition(_destinationNullableIfExits);
            }

            if (_destinationNullableIfExits == null)
            {
                return _sourceNullableIfAny.AddExitTransition();
            }

            return _sourceNullableIfAny.AddTransition(_destinationNullableIfExits);
        }
    }

    public class AacFlMultiTransitionContinuation
    {
        private readonly AnimatorStateTransition _templateTransition;
        private readonly AnimatorStateMachine _machine;
        private readonly AnimatorState _sourceNullableIfAny;
        private readonly AnimatorState _destinationNullableIfExits;
        private readonly List<AacFlTransitionContinuation> _pendingContinuations;

        internal AacFlMultiTransitionContinuation(AnimatorStateTransition templateTransition, AnimatorStateMachine machine, AnimatorState sourceNullableIfAny, AnimatorState destinationNullableIfExits, List<AacFlTransitionContinuation> pendingContinuations)
        {
            _templateTransition = templateTransition;
            _machine = machine;
            _sourceNullableIfAny = sourceNullableIfAny;
            _destinationNullableIfExits = destinationNullableIfExits;
            _pendingContinuations = pendingContinuations;
        }

        /// Adds an additional condition to these transitions that requires all preceding conditions to be true.
        /// <example>
        /// <code>
        /// .When(_aac.BoolParameter(_aac.Get().myBoolParameterName).IsTrue())
        /// .And(_aac.BoolParameter(_aac.Get().myIntParameterName).IsGreaterThan(2))
        /// .And(AacAv3.ItIsLocal())
        /// .Or()
        /// .When(_aac.BoolParameters(
        ///     _aac.Get().myBoolParameterName,
        ///     _aac.Get().myOtherBoolParameterName
        /// ).AreTrue())
        /// .And(AacAv3.ItIsRemote());
        /// </code>
        /// </example>
        internal AacFlMultiTransitionContinuation And(IAacFlCondition action)
        {
            foreach (var pendingContinuation in _pendingContinuations)
            {
                pendingContinuation.And(action);
            }

            return this;
        }

        /// <summary>
        /// Applies a series of conditions to these transitions. The conditions cannot include an Or operator.
        /// </summary>
        /// <param name="actionsWithoutOr"></param>
        /// <returns></returns>
        internal AacFlMultiTransitionContinuation And(Action<AacFlTransitionContinuationWithoutOr> actionsWithoutOr)
        {
            foreach (var pendingContinuation in _pendingContinuations)
            {
                pendingContinuation.And(actionsWithoutOr);
            }

            return this;
        }

        /// <summary>
        /// Creates a new transition with identical settings but having no conditions defined yet.
        /// </summary>
        /// <example>
        /// <code>
        /// .When(_aac.BoolParameter(_aac.Get().myBoolParameterName).IsTrue())
        /// .And(_aac.BoolParameter(_aac.Get().myIntParameterName).IsGreaterThan(2))
        /// .And(AacAv3.ItIsLocal())
        /// .Or()
        /// .When(_aac.BoolParameters(
        ///     _aac.Get().myBoolParameterName,
        ///     _aac.Get().myOtherBoolParameterName
        /// ).AreTrue())
        /// .And(AacAv3.ItIsRemote());
        /// </code>
        /// </example>
        public AacFlNewTransitionContinuation Or()
        {
            var newTransition = NewTransition();
            newTransition.duration = _templateTransition.duration;
            newTransition.offset = _templateTransition.offset;
            newTransition.interruptionSource = _templateTransition.interruptionSource;
            newTransition.orderedInterruption = _templateTransition.orderedInterruption;
            newTransition.exitTime = _templateTransition.exitTime;
            newTransition.hasExitTime = _templateTransition.hasExitTime;
            newTransition.hasFixedDuration = _templateTransition.hasFixedDuration;
            newTransition.canTransitionToSelf = _templateTransition.canTransitionToSelf;
            return new AacFlNewTransitionContinuation(newTransition, _machine, _sourceNullableIfAny, _destinationNullableIfExits);
        }

        private AnimatorStateTransition NewTransition()
        {
            if (_sourceNullableIfAny == null)
            {
                return _machine.AddAnyStateTransition(_destinationNullableIfExits);
            }

            if (_destinationNullableIfExits == null)
            {
                return _sourceNullableIfAny.AddExitTransition();
            }

            return _sourceNullableIfAny.AddTransition(_destinationNullableIfExits);
        }
    }

    public class AacFlTransitionContinuationOnlyOr
    {
        private readonly AnimatorStateTransition _transition;
        private readonly AnimatorStateMachine _machine;
        private readonly AnimatorState _sourceNullableIfAny;
        private readonly AnimatorState _destinationNullableIfExits;

        internal AacFlTransitionContinuationOnlyOr(AnimatorStateTransition transition, AnimatorStateMachine machine, AnimatorState sourceNullableIfAny, AnimatorState destinationNullableIfExits)
        {
            _transition = transition;
            _machine = machine;
            _sourceNullableIfAny = sourceNullableIfAny;
            _destinationNullableIfExits = destinationNullableIfExits;
        }

        /// <summary>
        /// Creates a new transition with identical settings but having no conditions defined yet.
        /// </summary>
        /// <example>
        /// <code>
        /// .When(_aac.BoolParameter(_aac.Get().myBoolParameterName).IsTrue())
        /// .And(_aac.BoolParameter(_aac.Get().myIntParameterName).IsGreaterThan(2))
        /// .And(AacAv3.ItIsLocal())
        /// .Or()
        /// .When(_aac.BoolParameters(
        ///     _aac.Get().myBoolParameterName,
        ///     _aac.Get().myOtherBoolParameterName
        /// ).AreTrue())
        /// .And(AacAv3.ItIsRemote());
        /// </code>
        /// </example>
        public AacFlNewTransitionContinuation Or()
        {
            var newTransition = NewTransition();
            newTransition.duration = _transition.duration;
            newTransition.offset = _transition.offset;
            newTransition.interruptionSource = _transition.interruptionSource;
            newTransition.orderedInterruption = _transition.orderedInterruption;
            newTransition.exitTime = _transition.exitTime;
            newTransition.hasExitTime = _transition.hasExitTime;
            newTransition.hasFixedDuration = _transition.hasFixedDuration;
            newTransition.canTransitionToSelf = _transition.canTransitionToSelf;
            return new AacFlNewTransitionContinuation(newTransition, _machine, _sourceNullableIfAny, _destinationNullableIfExits);
        }

        private AnimatorStateTransition NewTransition()
        {
            if (_sourceNullableIfAny == null)
            {
                return _machine.AddAnyStateTransition(_destinationNullableIfExits);
            }

            if (_destinationNullableIfExits == null)
            {
                return _sourceNullableIfAny.AddExitTransition();
            }

            return _sourceNullableIfAny.AddTransition(_destinationNullableIfExits);
        }
    }

    public class AacFlTransitionContinuationWithoutOr
    {
        private readonly AnimatorStateTransition _transition;

        internal AacFlTransitionContinuationWithoutOr(AnimatorStateTransition transition)
        {
            _transition = transition;
        }

        internal AacFlTransitionContinuationWithoutOr And(IAacFlCondition action)
        {
            action.ApplyTo(new AacFlCondition(_transition));
            return this;
        }

        /// <summary>
        /// Applies a series of conditions to this transition. The conditions cannot include an Or operator.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        internal AacFlTransitionContinuationWithoutOr AndWhenever(Action<AacFlTransitionContinuationWithoutOr> action)
        {
            action(this);
            return this;
        }
    }
}
