using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpBroadlink.Devices
{
    public class Sp2 : Device
    {
        public Sp2(IPEndPoint host, byte[] mac, int devType) : base(host, mac, devType)
        {
            this.DeviceType = DeviceType.Sp2;

            //throw new NotImplementedException(@"if you want: https://github.com/mjg59/python-broadlink/blob/56b2ac36e5a2359272f4af8a49cfaf3e1891733a/broadlink/__init__.py#L368-L443");
        }

        public async Task<bool> CheckPower()
        {
            var packet = new byte[16];
            packet[0] = 1;
            var response = await this.SendPacket(0x6a, packet);
            var err = response[0x22] | (response[0x23] << 8);

            if (err == 0)
            {
                var payload = this.Decrypt(response.Skip(0x38).Take(int.MaxValue).ToArray());
                var charInt = ((char)packet[0x4]).ToString();
                var state = false;

                var tmpInt = 0;
                if (int.TryParse(charInt, out tmpInt))
                {
                    // character returned
                    state = (tmpInt == 1 || tmpInt == 3);
                }
                else
                {
                    // int returned
                    var sp2State = (int)packet[0x4];
                    state = (sp2State == 1 || sp2State == 3);
                }

                return state;
            }

            return false;
        }

        public async Task<bool> CheckNightLight()
        {
            var packet = new byte[16];
            packet[0] = 1;
            var response = await this.SendPacket(0x6a, packet);
            var err = response[0x22] | (response[0x23] << 8);

            if (err == 0)
            {

            }

            return false;
        }
    }
}
