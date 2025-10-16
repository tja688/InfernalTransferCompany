namespace ITC.UI.Choreography
{
    /// <summary>
    /// Optional interface for components that change visual styles when a UIActor switches role/state.
    /// </summary>
    public interface IUIActorStyleHandler
    {
        void ApplyStyle(UIActor actor, string styleVariant, string phaseName);
    }
}
