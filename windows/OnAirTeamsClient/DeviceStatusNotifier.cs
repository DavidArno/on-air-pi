using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using static OnAirTeamsClient.IStatusNotifier.Statuses;

namespace OnAirTeamsClient
{
    internal sealed class DeviceStatusNotifier : IDisposable
    {
        private readonly string _device;
        private readonly MessageSender _messageSender;
        private readonly List<RegistryKey> _registryKeys;
        private readonly Action<IStatusNotifier.Statuses> _localStatusNotifier;

        internal DeviceStatusNotifier(
            string device,
            MessageSender messageSender,
            IEnumerable<string> keys,
            Action<IStatusNotifier.Statuses> localStatusNotifier)
        {
            _device = device;
            _messageSender = messageSender;
            _registryKeys = keys.Select(key => Registry.CurrentUser.OpenSubKey(key)).ToList();
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

        public void Dispose()
        {
            foreach (var key in _registryKeys)
            {
                key.Dispose();
            }
        }

        internal void OnDeviceChanged()
        {
            var (messageSuffix, status) = IsDeviceInUse() ? ("on", On) : ("off", Off);

            SendMessage(messageSuffix);
            _localStatusNotifier(status);
        }

        private bool IsDeviceInUse()
            => _registryKeys.Any(key => key.GetValue("LastUsedTimeStop") is long value && value == 0);

        private void SendMessage(string messageSuffix) => _messageSender.SendMessage($"{_device}:{messageSuffix}");
    }
}
