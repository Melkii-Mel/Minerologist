// AI-generated
using Godot;

public class FrameRateMonitor
{
    private const float ReportInterval = 1.0f;
    private float _elapsed;
    private int _frames;
    private int _over20Ms;
    private int _over33Ms;
    private float _totalFrameTime;
    private float _worstFrameTime;

    public void Update(float delta)
    {
        _elapsed += delta;
        _frames++;
        _totalFrameTime += delta;
        _worstFrameTime = Mathf.Max(_worstFrameTime, delta);

        if (delta > 0.020f)
            _over20Ms++;

        if (delta > 1f / 30f)
            _over33Ms++;

        if (_elapsed < ReportInterval)
            return;

        var averageFps = _frames / _totalFrameTime;

        GD.Print(
            $"FPS avg: {averageFps:0.0} | "
                + $"worst: {_worstFrameTime * 1000:0.0} ms | "
                + $">20 ms: {_over20Ms} | >33 ms: {_over33Ms}"
        );

        _elapsed = 0;
        _frames = 0;
        _totalFrameTime = 0;
        _worstFrameTime = 0;
        _over20Ms = 0;
        _over33Ms = 0;
    }
}
