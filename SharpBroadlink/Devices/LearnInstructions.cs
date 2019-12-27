using System.ComponentModel;

namespace SharpBroadlink.Devices
{
    public enum LearnInstructions
    {
        [Description("Press and hold the remote button")]
        PressAndHoldRemotedButton,
        [Description("Release the remote button")]
        ReleaseRemoteButton,
        [Description("Press shortly the remote button you want to learn")]
        PressRemoteButtonShortly,
    }
}
