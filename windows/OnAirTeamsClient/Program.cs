using OnAirTeamsClient;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

SingleGlobalInstance.Enforce(Run, ShowAlreadyRunningMessage);

        private static void Run()
        {
            const string webCam = "webcam";
            const string microphone = "microphone";
            const string teamsApplication = "MSTeams_8wekyb3d8bbwe";
            const string edgeApplication = "C:#Program Files (x86)#Microsoft#Edge#Application#msedge.exe";
            const string serverHostName = "on-air-pi";
            const int serverPort = 65432;

            var username = Environment.UserName;
            var zoomApplication = $"C:#Users#{username}#AppData#Roaming#Zoom#bin#Zoom.exe";

            var webcamKeys = new List<string>
            {
                BuildPackagedRegistryKeyString(webCam, teamsApplication),
                BuildNonPackagedRegistryKeyString(webCam, edgeApplication),
                BuildNonPackagedRegistryKeyString(webCam, zoomApplication)
            };

            var microphoneKeys = new List<string>
            {
                BuildPackagedRegistryKeyString(microphone, teamsApplication),
                BuildNonPackagedRegistryKeyString(microphone, edgeApplication),
                BuildNonPackagedRegistryKeyString(microphone, zoomApplication)
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

    var iconManager = new IconTrayIconManager();

    var messageSender = new MessageSender(serverHostName, serverPort, iconManager.SetServerStatus);

            using var webcamClientControl =
                new DeviceStatusNotifier(webCam, messageSender, webcamKeys, iconManager.SetWebcamStatus);

            using var microphoneClientControl =
                new DeviceStatusNotifier(microphone, messageSender, microphoneKeys, iconManager.SetMicrophoneStatus);

            using var webcamMonitor0 = new RegistryMonitor(webcamKeys[0], webcamClientControl);
            using var webcamMonitor1 = new RegistryMonitor(webcamKeys[1], webcamClientControl);
            using var webcamMonitor2 = new RegistryMonitor(webcamKeys[2], webcamClientControl);

            using var microphoneMonitor0 = new RegistryMonitor(microphoneKeys[0], microphoneClientControl);
            using var microphoneMonitor1 = new RegistryMonitor(microphoneKeys[1], microphoneClientControl);
            using var microphoneMonitor2 = new RegistryMonitor(microphoneKeys[2], microphoneClientControl);

            iconManager.SetWebcamOff = webcamClientControl.OverrideDeviceStateToOff;
            iconManager.SetWebcamOn = webcamClientControl.OverrideDeviceStateToOn;

    iconManager.SetMicrophoneOff = microphoneClientControl.OverrideDeviceStateToOff;
    iconManager.SetMicrophoneOn = microphoneClientControl.OverrideDeviceStateToOn;

    microphoneClientControl.OnDeviceChanged();
    webcamClientControl.OnDeviceChanged();


            Application.Run(iconManager);

        }

static void ShowAlreadyRunningMessage() => MessageBox.Show(
    "Only one instance of the client can be run at a time.",
    "OnAirTeamsClient is already running",
    MessageBoxButtons.OK);

        private static string BuildNonPackagedRegistryKeyString(string device, string application)
            => $@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\{device}" +
               $@"\NonPackaged\{application}";

        private static string BuildPackagedRegistryKeyString(string device, string application)
            => $@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\{device}" +
               $@"\{application}";
    }
}
