public class UIRouteChangeContext
{
    public UIRoute Previous { get; }
    public UIRoute Next { get; }
    public UIRouteCommand Command => _request.Command;
    public string RequestedPath => _request.Path;
    public object Payload => _request.Payload;
    public UIHierarchyLevel StartLevel => _request.StartLevel;
    public UIHierarchyLevel ModalLevel => _request.ModalLevel;

    private readonly UIRouteRequest _request;

    public UIRouteChangeContext(UIRoute previous, UIRoute next, UIRouteRequest request)
    {
        Previous = previous;
        Next = next;
        _request = request;
    }

    public bool WasActive(UIHierarchyLevel level) => Previous != null && Previous.IsActive(level);
    public bool WillBeActive(UIHierarchyLevel level) => Next != null && Next.IsActive(level);
    public UIRouteNode PreviousNode(UIHierarchyLevel level) => Previous?.GetNode(level);
    public UIRouteNode NextNode(UIHierarchyLevel level) => Next?.GetNode(level);
    public UIHierarchyLevel PreviousHighest => Previous != null ? Previous.GetHighestLevel() : UIHierarchyLevel.None;
    public UIHierarchyLevel NextHighest => Next != null ? Next.GetHighestLevel() : UIHierarchyLevel.None;
}
