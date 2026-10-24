using System;
using static System.DateTime;

public class DurationDebug
{
    private DateTime _startTime;

    public DurationDebug()
    {
        Reset();
    }

    public void Reset()
    {
        _startTime = Now;
    }

    public TimeSpan Time()
    {
        return Now - _startTime;
    }

    public float TotalMilliseconds()
    {
        return (float)Time().TotalMilliseconds;
    }
}
