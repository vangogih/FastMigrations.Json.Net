using System.Text.RegularExpressions;
using NUnit.Framework;
using Responsible;
using Responsible.Unity;
using UnityEngine;

namespace Light_Migrations.Tests.EditorMode
{
    public sealed class InstructionExecutor : TestInstructionExecutor
    {
        private readonly EditorScheduler _scheduler;
        private readonly UnityErrorLogInterceptor _errorLogInterceptor;

        private InstructionExecutor(
            EditorScheduler scheduler,
            UnityErrorLogInterceptor errorLogInterceptor,
            bool logErrors,
            IGlobalContextProvider globalContextProvider)
            : base(
                scheduler,
                errorLogInterceptor,
                logErrors ? new UnityFailureListener() : null,
                globalContextProvider,
                new[]
                {
                    typeof(IgnoreException),
                    typeof(InconclusiveException),
                    typeof(SuccessException),
                })
        {
            _scheduler = scheduler;
            _errorLogInterceptor = errorLogInterceptor;
        }
        
        public InstructionExecutor(bool logErrors = true, IGlobalContextProvider globalContextProvider = null)
            : this(new EditorScheduler(), new UnityErrorLogInterceptor(), logErrors, globalContextProvider)
        {
        }
        
        public void ExpectLog(LogType logType, Regex regex) => _errorLogInterceptor.ExpectLog(logType, regex);
        
        public override void Dispose()
        {
            _scheduler.Dispose();
            base.Dispose();
        }
    }
}