public sealed class TimeFormula(string expr) : AbstractDynamicFormula(expr, "time")
{
    public double Calculate(int time) => Evaluate(time);
}
