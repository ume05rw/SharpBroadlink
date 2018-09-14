using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpBroadlink.Devices
{
    public interface IDevice
    {
        IPEndPoint Host { get; }

        byte[] Mac { get; }

        int DevType { get; }

        int Timeout { get; }

        byte[] Id { get; }

        string DeviceType { get; }

        Task<bool> Auth();

        string GetDeviceType();

        Task<byte[]> SendPacket(int command, byte[] payload);
    }
}
