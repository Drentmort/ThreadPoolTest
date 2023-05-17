using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TimerConsoleTest
{
    #region Poller
    public enum PollType
    {
        Timer = 1,
        Task = 2,
        Thread = 3,
        ThreadRecreate = 4
    }

    internal interface IPoller
    {
        void Start(int pollPeriod);
        event Action OnPoll;
    }

    internal class TimerPoll : IPoller
    {
        private Timer _timer;

        public TimerPoll()
        {
            _timer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        public event Action OnPoll = delegate { };

        public void Start(int pollPeriod)
        {
            _timer.Change(0, pollPeriod);
        }

        private void OnTimerTick(object o)
        {
            OnPoll();
        }
    }

    internal class TaskPoll : IPoller
    {
        public event Action OnPoll = delegate{ };

        public void Start(int pollPeriod)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    OnPoll();
                    Thread.Sleep(pollPeriod);
                }
            });
        }
    }

    internal class ThreadPoll : IPoller
    {
        public event Action OnPoll;

        public void Start(int pollPeriod)
        {
            new Thread(() =>
            {
                while (true)
                {
                    OnPoll();
                    Thread.Sleep(pollPeriod);
                }
            }).Start();
        }
    }

    internal class ThreadRecreatePoll : IPoller
    {
        private int _pollPeriod;
        public event Action OnPoll;

        public void Start(int pollPeriod)
        {
            ThreadWorker worker = new ThreadWorker();
            worker.ThreadDone += HandleThreadDone;

            Thread thread1 = new Thread(() => worker.Run(_pollPeriod, OnPoll));
            thread1.Start();
        }

        private void HandleThreadDone(object sender, EventArgs e)
        {
            ThreadWorker worker = new ThreadWorker();
            worker.ThreadDone += HandleThreadDone;

            Thread thread2 = new Thread(() => worker.Run(_pollPeriod, OnPoll));
            thread2.Start();
        }

        class ThreadWorker
        {
            public event EventHandler ThreadDone;
            public void Run(int pollPeriod, Action onPoll)
            {
                onPoll();
                Thread.Sleep(pollPeriod);

                if (ThreadDone != null)
                    ThreadDone(this, EventArgs.Empty);
            }
        }
    }
    #endregion

    #region Sync
    public enum SyncObjLocalization
    {
        Local = 1,
        Static = 2
    }
    public enum SyncObjType
    {
        Monitor = 1,
        Mutex = 2,
        Semaphore = 3,
        ResetEvent = 4
    }
    internal interface ISycnObj
    {
        void Enter();
        void Exit();
    }

    internal class MonitorSync : ISycnObj
    {
        private object _lock = new object(); 
        public void Enter()
        {
            Monitor.Enter(_lock);
        }

        public void Exit()
        {
            Monitor.Exit(_lock);
        }
    }

    internal class MutexSync : ISycnObj
    {
        private readonly Mutex _mutex = new Mutex();

        public void Enter()
        {
            _mutex.WaitOne();
        }

        public void Exit()
        {
            _mutex.ReleaseMutex();
        }
    }

    internal class SemaphoreSync:ISycnObj
    {
        private readonly Semaphore _semaphore = new Semaphore(1,1);

        public void Enter()
        {
            _semaphore.WaitOne();
        }

        public void Exit()
        {
            _semaphore.Release();
        }
    }

    internal class ResetEventSync : ISycnObj
    {
        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(true);
        public void Enter()
        {
            _autoResetEvent.WaitOne();
        }

        public void Exit()
        {
            _autoResetEvent.Set();
        }
    }
    #endregion

    internal class PollingClass
    {
        public event Action<TimeSpan> PollLockHandler;
        private IPoller _poller;
        private ISycnObj _sycnObjLocal;
        private static ISycnObj _sycnObjStatic;
        private bool useLocal = true;
        private int _immitationTime = 0;

        public void Start(PollType timerType, SyncObjType syncType, SyncObjLocalization syncObjType, int pollPeriod, int immitationTime)
        {
            _immitationTime = immitationTime;
            _poller = CreatePoller(timerType);
            useLocal = syncObjType == SyncObjLocalization.Local;
            if (useLocal)
            {
                _sycnObjLocal = CreateSyncObj(syncType);
            }
            else
            {
                _sycnObjStatic = CreateSyncObj(syncType);
            }
            _poller.OnPoll += DoSyncWithTimeMeasure;
            _poller.Start(pollPeriod);
        }

        private static IPoller CreatePoller(PollType timerType)
        {
            switch(timerType) 
            {
                case PollType.Timer: 
                    return new TimerPoll();
                case PollType.Thread:
                    return new ThreadPoll();
                case PollType.Task:
                    return new TaskPoll();
                case PollType.ThreadRecreate:
                    return new ThreadRecreatePoll();
                default: 
                    throw new NotImplementedException();
            }
        }

        private static ISycnObj CreateSyncObj(SyncObjType syncType)
        {
            switch (syncType) 
            {
                case SyncObjType.Monitor:
                    return new MonitorSync();
                case SyncObjType.Mutex:
                    return new MutexSync();
                case SyncObjType.ResetEvent:
                    return new ResetEventSync();
                case SyncObjType.Semaphore:
                    return new SemaphoreSync();
                default:
                    throw new NotImplementedException();

            }
        }

        private void DoSyncWithTimeMeasure()
        {
            var timer = new Stopwatch();
            timer.Start();
            var sycnObj = useLocal 
                ? _sycnObjLocal
                : _sycnObjStatic;
            sycnObj.Enter();
            Thread.Sleep(_immitationTime);
            sycnObj.Exit();
            timer.Stop();

            PollLockHandler?.Invoke(timer.Elapsed);
        }
    }
}
