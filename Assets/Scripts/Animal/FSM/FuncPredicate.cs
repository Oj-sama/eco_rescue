public class FuncPredicate : IPredicate
{
    private System.Func<bool> condition;

    public FuncPredicate(System.Func<bool> condition)
    {
        this.condition = condition;
    }

    public bool Evaluate()
    {
        return condition();
    }
}
