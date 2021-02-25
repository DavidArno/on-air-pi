using System;
using Microsoft.Win32;
using static OnAirTeamsClient.IStatusNotifier.Statuses;

namespace OnAirTeamsClient
{
    internal sealed class DeviceStatusNotifier : IDisposable
    {
        private readonly string _device;
        private readonly MessageSender _messageSender;
        private readonly RegistryKey _registryKey;
        private readonly Action<IStatusNotifier.Statuses> _localStatusNotifier;

        internal DeviceStatusNotifier(
            string device, 
            MessageSender messageSender, 
            string key,
            Action<IStatusNotifier.Statuses> localStatusNotifier)
        {
            _device = device;
            _messageSender = messageSender;
            _registryKey = Registry.CurrentUser.OpenSubKey(key);
            _localStatusNotifier = localStatusNotifier;
        }

        public void OverrideDeviceStateToOn()
        {
            SendMessage("on");
            _localStatusNotifier(On);
        }

        public void OverrideDeviceStateToOff()
        {
            SendMessage("off");
            _localStatusNotifier(Off);
        }

        public void Dispose() => _registryKey.Dispose();

        internal void OnDeviceChanged()
        {
            var (messageSuffix, status) = IsDeviceInUse() ? ("on", On) : ("off", Off);

            SendMessage(messageSuffix);
            _localStatusNotifier(status); 
        }

        private bool IsDeviceInUse() => _registryKey?.GetValue("LastUsedTimeStop") is long value && value == 0;

        private void SendMessage(string messageSuffix) => _messageSender.SendMessage($"{_device}:{messageSuffix}");
    }
}
