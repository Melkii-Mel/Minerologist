using System;
using DynamicExpresso;

public sealed class DynamicFormula
{
    private readonly Func<int, double> _calculate;

    public DynamicFormula(string expression)
    {
        var interpreter = new Interpreter()
            .SetFunction("pow", Math.Pow)
            .SetFunction("sqrt", Math.Sqrt)
            .SetFunction("log", (Func<double, double, double>)Math.Log)
            .SetFunction("lg", Math.Log10)
            .SetFunction("ln", (Func<double, double>)Math.Log)
            .SetFunction("ceil", (Func<double, double>)Math.Ceiling)
            .SetFunction("floor", (Func<double, double>)Math.Floor)
            .SetFunction("round", (Func<double, double>)Math.Round)
            .SetFunction("abs", (Func<double, double>)Math.Abs)
            .SetFunction("min", (Func<double, double, double>)Math.Min)
            .SetFunction("max", (Func<double, double, double>)Math.Max)
            .SetFunction("clamp", Clamp);

        _calculate = interpreter.ParseAsDelegate<Func<int, double>>(expression, "lvl");
    }

    public double Calculate(int level) => _calculate(level);

    private static double Clamp(double x, double min, double max) =>
        Math.Min(Math.Max(x, min), max);
}
