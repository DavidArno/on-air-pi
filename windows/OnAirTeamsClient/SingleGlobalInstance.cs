using System;
using System.Threading;

namespace OnAirTeamsClient
{
    internal static class SingleGlobalInstance
    {
        internal static void Enforce(Action okToRunAction, Action alreadyRunningAction)
        {
            var mutexId = $@"Global\{typeof(SingleGlobalInstance).GUID}";
            var hasHandle = false;

            using var mutex = new Mutex(false, mutexId, out _);

            try
            {
                try
                {
                    hasHandle = mutex.WaitOne(2000, false);
                }
                catch (AbandonedMutexException)
                {
                    hasHandle = true;
                }

                if (hasHandle)
                {
                    okToRunAction();
                }
                else
                {
                    alreadyRunningAction();
                }
            }
            finally
            {
                if (hasHandle) mutex.ReleaseMutex();
            }
        }
    }
}
