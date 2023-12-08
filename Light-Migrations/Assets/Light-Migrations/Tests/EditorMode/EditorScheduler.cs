using System;
using Responsible;
using Responsible.Utilities;
using UnityEditor;

namespace Light_Migrations.Tests.EditorMode
{
    public sealed class EditorScheduler : ITestScheduler, IDisposable
    {
        public int FrameNow { get; private set; }
        public DateTimeOffset TimeNow => DateTimeOffset.Now;
        
        private readonly RetryingPoller _poller = new();

        public EditorScheduler()
        {
            EditorApplication.update += OnUpdate;
        }

        public void Dispose()
        {
            EditorApplication.update -= OnUpdate;
        }

        public IDisposable RegisterPollCallback(Action action) => _poller.RegisterPollCallback(action);
        private void OnUpdate()
        {
            FrameNow++;
            _poller.Poll();
        }
    }
}