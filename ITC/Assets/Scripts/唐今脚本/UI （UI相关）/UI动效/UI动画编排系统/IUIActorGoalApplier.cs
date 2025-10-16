namespace ITC.UI.Choreography
{
    /// <summary>
    /// Interface that allows custom components to respond when a UIActor receives a schedule.
    /// Implementations can update transforms, invoke tween players, etc.
    /// </summary>
    public interface IUIActorGoalApplier
    {
        void Apply(UIActor actor, UIActorSchedule schedule);
    }
}
