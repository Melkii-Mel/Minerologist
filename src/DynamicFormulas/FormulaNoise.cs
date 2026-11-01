public sealed class NoiseFormula(string expr) : AbstractDynamicFormula(expr, "depth")
{
    public double Calculate(int depth) => Evaluate(depth);
}
