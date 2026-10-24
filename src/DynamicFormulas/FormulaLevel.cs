public sealed class LevelFormula(string expr) : AbstractDynamicFormula(expr, "lvl")
{
    public double Calculate(int level) => Evaluate(level);
}
