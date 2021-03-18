using System;
using System.Windows.Forms;

namespace OnAirTeamsClient
{
    internal static class Program
    {
        [STAThread]
        private static void Main() => SingleGlobalInstance.Enforce(Run, ShowAlreadyRunningMessage);

        private static void Run()
        {
            const string webCam = "webcam";
            const string microphone = "microphone";
            const string serverHostName = "on-air-pi";
            const int serverPort = 65432;

            var username = Environment.UserName;

            var webcamKey = BuildRegistryKeyString(webCam, username);
            var microphoneKey = BuildRegistryKeyString(microphone, username);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var iconManager = new IconTrayIconManager();

            var messageSender = new MessageSender(serverHostName, serverPort, iconManager.SetServerStatus);

            using var webcamClientControl = 
                new DeviceStatusNotifier(webCam, messageSender, webcamKey, iconManager.SetWebcamStatus); 

            using var microphoneClientControl =
                new DeviceStatusNotifier(microphone, messageSender, microphoneKey, iconManager.SetMicrophoneStatus);

            using var webcamMonitor = new RegistryMonitor(webcamKey, webcamClientControl);
            using var microphoneMonitor = new RegistryMonitor(microphoneKey, microphoneClientControl);

            iconManager.SetWebcamOff = webcamClientControl.OverrideDeviceStateToOff;
            iconManager.SetWebcamOn= webcamClientControl.OverrideDeviceStateToOn;

            iconManager.SetMicrophoneOff = microphoneClientControl.OverrideDeviceStateToOff;
            iconManager.SetMicrophoneOn = microphoneClientControl.OverrideDeviceStateToOn;

            microphoneClientControl.OnDeviceChanged();
            webcamClientControl.OnDeviceChanged();

            
            Application.Run(iconManager);
            
        }

        private static void ShowAlreadyRunningMessage()
            => MessageBox.Show(
                "Only one instance of the client can be run at a time.",
                "OnAirTeamsClient is already running",
                MessageBoxButtons.OK);


        private static string BuildRegistryKeyString(string device, string user)
            => $@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\{device}\NonPackaged" +
               $@"\C:#Users#{user}#AppData#Local#Microsoft#Teams#current#Teams.exe";
    }
}
