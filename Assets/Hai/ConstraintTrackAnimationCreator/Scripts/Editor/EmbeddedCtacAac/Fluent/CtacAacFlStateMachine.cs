using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

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

        public AacFlState NormalizedTime(AacFlFloatParameter floatParam)
        {
            State.timeParameterActive = true;
            State.timeParameter = floatParam.Name;

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
