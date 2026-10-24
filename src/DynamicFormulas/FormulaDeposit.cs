public sealed class DepositFormula(string expr) : AbstractDynamicFormula(expr, "deposit_duration")
{
    public double Calculate(float duration) => Evaluate(duration);
}
