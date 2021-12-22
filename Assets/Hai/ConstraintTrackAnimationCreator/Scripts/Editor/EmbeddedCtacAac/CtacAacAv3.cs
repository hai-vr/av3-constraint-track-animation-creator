using Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac.Fluent;

namespace Hai.ConstraintTrackAnimationCreator.Scripts.Editor.EmbeddedCtacAac
{
    public class AacAv3
    {
        private readonly AacFlBackingAnimator _backingAnimator;

        internal AacAv3(AacFlBackingAnimator backingAnimator)
        {
            _backingAnimator = backingAnimator;
        }

        // ReSharper disable IdentifierTypo
        public AacFlBoolParameter IsLocal => _backingAnimator.BoolParameter("IsLocal");
        public AacFlIntParameter Viseme => _backingAnimator.IntParameter("Viseme");
        public AacFlIntParameter GestureLeft => _backingAnimator.IntParameter("GestureLeft");
        public AacFlIntParameter GestureRight => _backingAnimator.IntParameter("GestureRight");
        public AacFlFloatParameter GestureLeftWeight => _backingAnimator.FloatParameter("GestureLeftWeight");
        public AacFlFloatParameter GestureRightWeight => _backingAnimator.FloatParameter("GestureRightWeight");
        public AacFlFloatParameter AngularY => _backingAnimator.FloatParameter("AngularY");
        public AacFlFloatParameter VelocityX => _backingAnimator.FloatParameter("VelocityX");
        public AacFlFloatParameter VelocityY => _backingAnimator.FloatParameter("VelocityY");
        public AacFlFloatParameter VelocityZ => _backingAnimator.FloatParameter("VelocityZ");
        public AacFlFloatParameter Upright => _backingAnimator.FloatParameter("Upright");
        public AacFlBoolParameter Grounded => _backingAnimator.BoolParameter("Grounded");
        public AacFlBoolParameter Seated => _backingAnimator.BoolParameter("Seated");
        public AacFlBoolParameter AFK => _backingAnimator.BoolParameter("AFK");
        public AacFlIntParameter TrackingType => _backingAnimator.IntParameter("TrackingType");
        public AacFlIntParameter VRMode => _backingAnimator.IntParameter("VRMode");
        public AacFlBoolParameter MuteSelf => _backingAnimator.BoolParameter("MuteSelf");
        public AacFlBoolParameter InStation => _backingAnimator.BoolParameter("InStation");
        // ReSharper restore IdentifierTypo

        public IAacFlCondition ItIsRemote() => IsLocal.IsFalse();
        public IAacFlCondition ItIsLocal() => IsLocal.IsTrue();
    }
}
