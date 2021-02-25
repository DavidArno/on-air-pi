namespace OnAirTeamsClient
{
    internal interface IStatusNotifier
    {
        enum Statuses { Off, On }

        void SetServerStatus(Statuses status);
        void SetMicrophoneStatus(Statuses status);
        void SetWebcamStatus(Statuses status);
    }
}
