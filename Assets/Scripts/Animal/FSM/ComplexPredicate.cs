public class ComplexPredicate : IPredicate
{
    private IPredicate[] predicates;

    public ComplexPredicate(IPredicate[] predicates)
    {
        this.predicates = predicates;
    }

    public bool Evaluate()
    {
        foreach (var predicate in predicates)
        {
            if (!predicate.Evaluate())
            {
                return false;
            }
        }
        return true;
    }
}
