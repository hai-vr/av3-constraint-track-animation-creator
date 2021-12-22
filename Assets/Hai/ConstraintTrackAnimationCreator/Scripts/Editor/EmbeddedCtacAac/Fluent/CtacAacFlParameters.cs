using System;
using static Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac.Fluent.AacFlConditionSimple;
using static UnityEditor.Animations.AnimatorConditionMode;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac.Fluent
{
    class AacFlConditionSimple : IAacFlCondition
    {
        private readonly Action<AacFlCondition> _action;

        public AacFlConditionSimple(Action<AacFlCondition> action)
        {
            _action = action;
        }

        public static AacFlConditionSimple Just(Action<AacFlCondition> action)
        {
            return new AacFlConditionSimple(action);
        }

        public void ApplyTo(AacFlCondition appender)
        {
            _action.Invoke(appender);
        }
    }

    public abstract class AacFlParameter
    {
        public string Name { get; }

        protected AacFlParameter(string name)
        {
            Name = name;
        }
    }

    public class AacFlFloatParameter : AacFlParameter
    {
        internal static AacFlFloatParameter Internally(string name) => new AacFlFloatParameter(name);
        private AacFlFloatParameter(string name) : base(name) { }
        public IAacFlCondition IsGreaterThan(float other) => Just(condition => condition.Add(Name, Greater, other));
        public IAacFlCondition IsLessThan(float other) => Just(condition => condition.Add(Name, Less, other));
    }

    public class AacFlBoolParameter : AacFlParameter
    {
        internal static AacFlBoolParameter Internally(string name) => new AacFlBoolParameter(name);
        private AacFlBoolParameter(string name) : base(name) { }
        public IAacFlCondition IsTrue() => Just(condition => condition.Add(Name, If, 0));
        public IAacFlCondition IsFalse() => Just(condition => condition.Add(Name, IfNot, 0));
        public IAacFlCondition IsEqualTo(bool other) => Just(condition => condition.Add(Name, other ? If : IfNot, 0));
    }
}
