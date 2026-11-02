// AI-Generated
using System;
using System.Collections.Generic;
using Godot;

public class FrameRateMonitor(Action<string> print)
{
    private const float WindowSeconds = 3.0f;
    private const int ReportEveryFrames = 5;

    private readonly Queue<float> _frameTimes = new();

    private float _windowTime;
    private float _worstFrameTime;
    private int _over20Ms;
    private int _over33Ms;
    private int _framesSinceReport;

    public void Update(float delta)
    {
        _frameTimes.Enqueue(delta);
        _windowTime += delta;
        _framesSinceReport++;

        if (delta > _worstFrameTime)
            _worstFrameTime = delta;

        if (delta > 0.020f)
            _over20Ms++;

        if (delta > 1f / 30f)
            _over33Ms++;

        // Keep samples spanning at most the most recent 3 seconds.
        while (_windowTime > WindowSeconds && _frameTimes.Count > 1)
        {
            var oldest = _frameTimes.Dequeue();
            _windowTime -= oldest;

            if (oldest >= _worstFrameTime)
                RecalculateWindowStats();
            else
            {
                if (oldest > 0.020f)
                    _over20Ms--;

                if (oldest > 1f / 30f)
                    _over33Ms--;
            }
        }

        if (_framesSinceReport < ReportEveryFrames)
            return;

        _framesSinceReport = 0;

        var averageFps = _frameTimes.Count / _windowTime;

        print(
            $"FPS avg: {averageFps:0.0} | "
                + $"worst: {_worstFrameTime * 1000:0.0} ms | "
                + $">20 ms: {_over20Ms} | >33 ms: {_over33Ms}"
        );
    }

    private void RecalculateWindowStats()
    {
        _worstFrameTime = 0;
        _over20Ms = 0;
        _over33Ms = 0;

        foreach (var frameTime in _frameTimes)
        {
            _worstFrameTime = Mathf.Max(_worstFrameTime, frameTime);

            if (frameTime > 0.020f)
                _over20Ms++;

            if (frameTime > 1f / 30f)
                _over33Ms++;
        }
    }
}
