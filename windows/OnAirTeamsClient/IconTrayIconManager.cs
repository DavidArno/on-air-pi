using Microsoft.Win32;
using OnAirTeamsClient.Properties;
using System;
using System.Drawing;
using System.Windows.Forms;
using static OnAirTeamsClient.IStatusNotifier.Statuses;

namespace OnAirTeamsClient
{

    internal sealed class IconTrayIconManager : ApplicationContext, IStatusNotifier
    {
        private const string AutoRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string HkcuAutoRunKey = @"HKEY_CURRENT_USER\" + AutoRunKey;
        private const string HkcuPersonalizeKey = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        private const string AppName = "OnAirTeamsClient";

        private enum IconState { Disconnected, Connected, MicrophoneOn, WebcamOn }

        private static readonly Icon[] DarkModeIcons = {
            Resources.DisconnectedDark,
            Resources.ConnectedDark,
            Resources.MicrophoneOnDark,
            Resources.CameraOnDark
        };

        private static readonly Icon[] LightModeIcons = {
            Resources.DisconnectedLight,
            Resources.ConnectedLight,
            Resources.MicrophoneOnLight,
            Resources.CameraOnLight
        };

        private readonly NotifyIcon _trayIcon;
        private readonly Icon[] _windowsModeIconSet;

        private bool _serverConnected;
        private bool _microphoneOn;
        private bool _webcamOn;

        private IconState _iconState;
        private IconState _lastIconState;


        public IconTrayIconManager()
        {
            var lightThemeValue = (int)Registry.GetValue(HkcuPersonalizeKey, "SystemUsesLightTheme", 0);
            _windowsModeIconSet = lightThemeValue == 0 ? DarkModeIcons : LightModeIcons;
            
            _trayIcon = new NotifyIcon() {
                Icon = Resources.DisconnectedDark,
                ContextMenu = GenerateContextMenu(),
                Visible = true
            };

            new Timer {
                Interval = 500,
                Enabled = true
            }.Tick += AnimationTimerTick;

        }

        private ContextMenu GenerateContextMenu()
            => new ContextMenu(
                new[] {
                    new MenuItem("Exit", Exit),
                    new MenuItem("-"),
                    new MenuItem("Microphone Off", MicrophoneOff) { Enabled = _iconState != IconState.Disconnected },
                    new MenuItem("Microphone On", MicrophoneOn) { Enabled = _iconState != IconState.Disconnected },
                    new MenuItem("-"),
                    new MenuItem("Webcam Off", WebcamOff) { Enabled = _iconState != IconState.Disconnected },
                    new MenuItem("Webcam On", WebcamOn) { Enabled = _iconState != IconState.Disconnected },
                    new MenuItem("-"),
                    AppIsSetToAutoRun()
                        ? new MenuItem("Stop auto-running on Windows start", StopAppAutoRunning)
                        : new MenuItem("Automatically run when Windows starts", SetAppToAutoRun)
                });

        public Action SetMicrophoneOn { get; set; }
        public Action SetMicrophoneOff { get; set; }

        public Action SetWebcamOn { get; set; }
        public Action SetWebcamOff { get; set; }

        public void SetServerStatus(IStatusNotifier.Statuses status)
        {
            _serverConnected = status == On;
            UpdateIconState();
        }

        public void SetMicrophoneStatus(IStatusNotifier.Statuses status)
        {
            _microphoneOn = status == On;
            UpdateIconState();
        }

        public void SetWebcamStatus(IStatusNotifier.Statuses status)
        {
            _webcamOn = status == On;
            UpdateIconState();
        }

        private static bool AppIsSetToAutoRun() => Registry.GetValue(HkcuAutoRunKey, AppName, null) != null;

        private void SetAppToAutoRun(object sender, EventArgs e)
        {
            using var key = Registry.CurrentUser.OpenSubKey(AutoRunKey, true);
            key?.SetValue(AppName, Application.ExecutablePath);
            _trayIcon.ContextMenu = GenerateContextMenu();
        }

        private void StopAppAutoRunning(object sender, EventArgs e)
        {
            using var key = Registry.CurrentUser.OpenSubKey(AutoRunKey, true);
            key?.DeleteValue(AppName);
            _trayIcon.ContextMenu = GenerateContextMenu();
        }

        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            Application.Exit();
        }

        private void MicrophoneOff(object sender, EventArgs e) => SetMicrophoneOff?.Invoke();

        private void MicrophoneOn(object sender, EventArgs e) => SetMicrophoneOn?.Invoke();

        private void WebcamOff(object sender, EventArgs e) => SetWebcamOff?.Invoke();

        private void WebcamOn(object sender, EventArgs e) => SetWebcamOn?.Invoke();

        private void AnimationTimerTick(object sender, EventArgs e)
        {
            if (_iconState == _lastIconState) return;

            _lastIconState = _iconState;

            _trayIcon.Icon = _iconState switch {
                IconState.Disconnected => _windowsModeIconSet[0],
                IconState.Connected => _windowsModeIconSet[1],
                IconState.MicrophoneOn => _windowsModeIconSet[2],
                _ => _windowsModeIconSet[3]
            };

            _trayIcon.ContextMenu = GenerateContextMenu();
        }

        private void UpdateIconState() 
            => _iconState = (_webcamOn, _microphoneOn, _serverConnected) switch {
                (_, _, false) => IconState.Disconnected,
                (true, _, _) => IconState.WebcamOn,
                (_, true, _) => IconState.MicrophoneOn,
                _ => IconState.Connected
            };
    }
}
