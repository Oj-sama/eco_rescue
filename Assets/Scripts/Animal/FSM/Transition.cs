public class Transition : ITransition
{
    private IState fromState;
    private IState toState;

    public Transition(IState from, IState to)
    {
        fromState = from;
        toState = to;
    }

    public bool ShouldTransition()
    {
        return true;
    }
}
