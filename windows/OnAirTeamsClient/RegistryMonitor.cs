using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace OnAirTeamsClient
{
    internal sealed class RegistryMonitor : IDisposable
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegOpenKeyEx(
            IntPtr hKey,
            string subKey,
            uint options,
            int samDesired,
            out IntPtr phkResult);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegNotifyChangeKeyValue(
            IntPtr hKey,
            bool bWatchSubtree,
            int dwNotifyFilter,
            IntPtr hEvent,
            bool fAsynchronous);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegCloseKey(IntPtr hKey);

        private const int KeyQueryValue = 0x0001;
        private const int KeyNotify = 0x0010;
        private const int StandardRightsRead = 0x00020000;
        private const int RegChangeNotifyFilterOnValue = 4;

        private static readonly IntPtr HkCurrentUser = new IntPtr(unchecked((int) 0x80000001));


        private readonly string _registrySubName;
        private readonly object _threadLock = new object();
        private readonly ManualResetEvent _eventTerminate = new ManualResetEvent(false);
        private readonly Action _notifyOfValueChange;

        private Thread _thread;
        private bool _disposed;


        public RegistryMonitor(string subKey, DeviceStatusNotifier notifier)
        {
            _registrySubName = subKey;
            _notifyOfValueChange = notifier.OnDeviceChanged;

            _eventTerminate.Reset();
            _thread = new Thread(MonitorThread) {IsBackground = true};
            _thread.Start();
        }

        public void Dispose()
        {
            if (_disposed) throw new ObjectDisposedException(null, "This instance is already disposed");

            lock (_threadLock)
            {
                if (_thread == null) return;

                _eventTerminate.Set();
                _thread.Join();
            }

            _disposed = true;
        }

        private void MonitorThread()
        {
            try
            {
                ThreadLoop();
            }
            finally
            {
                _thread = null;
            }
        }

        private void ThreadLoop()
        {
            var result = RegOpenKeyEx(
                HkCurrentUser,
                _registrySubName,
                0,
                StandardRightsRead | KeyQueryValue | KeyNotify,
                out var registryKey);

            if (result != 0) throw new Win32Exception(result);

            try
            {
                var eventNotify = new AutoResetEvent(false);
                var waitHandles = new WaitHandle[] {eventNotify, _eventTerminate};

                while (!_eventTerminate.WaitOne(0, true))
                {
                    result = RegNotifyChangeKeyValue(
                        registryKey,
                        true,
                        RegChangeNotifyFilterOnValue,
                        eventNotify.SafeWaitHandle.DangerousGetHandle(),
                        true);

                    if (result != 0) throw new Win32Exception(result);
                    if (WaitHandle.WaitAny(waitHandles) == 0) _notifyOfValueChange();
                }
            }
            finally
            {
                if (registryKey != IntPtr.Zero) RegCloseKey(registryKey);
            }
        }
    }
}
