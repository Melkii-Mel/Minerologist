using System;
using System.Linq;
using DynamicExpresso;

public abstract class AbstractDynamicFormula
{
    private readonly Lambda _lambda;

    public AbstractDynamicFormula(string expression, params string[] names)
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

        var parameters = names.Select(name => new Parameter(name, typeof(double))).ToArray();

        _lambda = interpreter.Parse(expression, typeof(double), parameters);
    }

    protected double Evaluate(params double[] arguments) =>
        (double)_lambda.Invoke(arguments.Cast<object>().ToArray());

    private static double Clamp(double x, double min, double max) =>
        Math.Min(Math.Max(x, min), max);
}
