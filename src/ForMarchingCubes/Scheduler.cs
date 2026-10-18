using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class Scheduler
{
    public int WorkersCount
    {
        get => _workersCount;
        set
        {
            lock (_lock)
            {
                if (value < 0)
                {
                    throw new ArgumentException(
                        "Worker count must be greate than or equal to zero."
                    );
                }
                while (_workersCount < value)
                {
                    _workersCount++;
                    AddWorker();
                }
                while (_workersCount > value)
                {
                    _workersCount--;
                    FreeWorker();
                }
            }
        }
    }
    public int CurrentHighestPriority { get; private set; } = int.MinValue;
    private readonly SemaphoreSlim _semaphore = new(0);
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, Action>> _jobs = [];
    private readonly ConcurrentDictionary<int, int> _jobPriorities = [];
    private int _id;
    private int NextId => Interlocked.Increment(ref _id);
    private int _workersCount;
    private readonly Stack<Action> _workerStoppers = [];
    private readonly object _lock = new();

    public Scheduler(int workersCount)
    {
        WorkersCount = workersCount;
    }

    public Scheduler(float workload)
    {
        if (workload > 1 || workload <= 0)
        {
            throw new ArgumentException("`worload` parameter must be in range (0, 1]");
        }
        WorkersCount = Math.Max(1, (int)Math.Round(TotalCpuCores() * workload));
        Godot.GD.Print("Total CPU cores: ", TotalCpuCores());
        Godot.GD.Print("Workers: ", WorkersCount);
    }

    private void AddWorker()
    {
        var stop = false;
        void WorkerStopper()
        {
            stop = true;
        }
        _workerStoppers.Push(WorkerStopper);
        Task.Run(async () =>
        {
            while (true)
            {
                await _semaphore.WaitAsync();
                if (stop)
                {
                    return;
                }
                try
                {
                    StartJob();
                }
                catch (Exception e)
                {
                    Godot.GD.PrintErr(e);
                }
            }
        });
    }

    private void FreeWorker()
    {
        var workerStopper = _workerStoppers.Pop();
        workerStopper.Invoke();
    }

    public static int TotalCpuCores()
    {
        return Environment.ProcessorCount;
    }

    public int QueueJob(Action a, int priority)
    {
        var id = NextId;
        QueueJob(a, priority, id);
        return id;
    }

    private void QueueJob(Action a, int priority, int id)
    {
        lock (_lock)
        {
            if (!_jobs.TryGetValue(priority, out var jobs))
            {
                jobs = [];
                _jobs[priority] = jobs;
            }
            jobs[id] = a;
            _jobPriorities[id] = priority;
            if (CurrentHighestPriority < priority)
            {
                CurrentHighestPriority = priority;
            }
            _semaphore.Release();
        }
    }

    public bool TryUpdatePriority(int id, int newPriority)
    {
        lock (_lock)
        {
            if (_jobPriorities.ContainsKey(id) && _jobPriorities[id] == newPriority)
            {
                return true;
            }
            if (!TryCancelJob(id, out var job))
            {
                return false;
            }
            QueueJob(job, newPriority, id);
            return true;
        }
    }

    public bool TryCancelJob(int id, out Action job)
    {
        lock (_lock)
        {
            if (_jobPriorities.ContainsKey(id))
            {
                var priority = _jobPriorities[id];
                return TryPopJob(priority, id, out job);
            }
            job = null!;
            return false;
        }
    }

    private void StartJob()
    {
        var job = PopMostRelevantJob();
        if (!job.HasValue)
        {
            return;
        }
        job.Value.Value();
    }

    private bool TryPopJob(int priority, int id, out Action removedJob)
    {
        lock (_lock)
        {
            if (!_jobs.TryGetValue(priority, out var jobs) || !jobs.TryGetValue(id, out removedJob))
            {
                removedJob = null!;
                return false;
            }
            jobs.TryRemove(id, out _);
            _jobPriorities.TryRemove(id, out _);
            if (jobs.Count == 0)
            {
                _jobs.TryRemove(priority, out _);
                if (priority == CurrentHighestPriority)
                {
                    CurrentHighestPriority = _jobPriorities.IsEmpty
                        ? int.MinValue
                        : _jobPriorities.Values.Max();
                }
            }
            return true;
        }
    }

    private KeyValuePair<int, Action>? PopMostRelevantJob()
    {
        lock (_lock)
        {
            if (!_jobs.TryGetValue(CurrentHighestPriority, out var jobs))
            {
                return null;
            }
            if (jobs.Count == 0)
            {
                return null;
            }
            var job = jobs.First();
            TryPopJob(CurrentHighestPriority, job.Key, out _);
            return job;
        }
    }
}
