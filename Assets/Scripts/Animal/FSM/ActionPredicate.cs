public class ActionPredicate : IPredicate
{
    private System.Action action;

    public ActionPredicate(System.Action action)
    {
        this.action = action;
    }

    public bool Evaluate()
    {
        action.Invoke();
        return true;
    }
}
