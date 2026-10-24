public sealed class GenerationFormula(string expr) : AbstractDynamicFormula(expr, "depth")
{
    public float Calculate(int depth) => (float)Evaluate(depth);
}
