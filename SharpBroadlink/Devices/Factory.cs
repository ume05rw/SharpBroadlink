using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace SharpBroadlink.Devices
{
    internal class Factory
    {
        /// <summary>
        /// Device type definition
        /// </summary>
        private static readonly Dictionary<string, int[]> Devices
            = new Dictionary<string, int[]>()
            {
                { "Sp1", new int[]{ 0 } },
                { "Sp2", new int[]
                    {
                        0x2711,                          // SP2
                        0x2719, 0x7919, 0x271a, 0x791a,  // Honeywell SP2
                        0x2720,                          // SPMini
                        0x753e,                          // SP3
                        0x7D00,                          // OEM branded SP3
                        0x947a, 0x9479,                  // SP3S
                        0x2728,                          // SPMini2
                        0x2733, 0x273e,                  // OEM branded SPMini
                        0x7530, 0x7918,                  // OEM branded SPMini2
                        0x2736,                          // SPMiniPlus
                    }
                },
                { "Rm", new int[]
                    {
                        0x2712,  // RM2
                        0x2737,  // RM Mini
                        0x273d,  // RM Pro Phicomm // <- have RF?
                        0x2783,  // RM2 Home Plus
                        0x277c,  // RM2 Home Plus GDT
                        0x278f,  // RM Mini Shate
                    }
                },
                { "Rm2Pro", new int[]
                    {
                        0x272a,  // RM2 Pro Plus
                        0x2787,  // RM2 Pro Plus2
                        0x279d,  // RM2 Pro Plus3
                        0x27a9,  // RM2 Pro Plus_300
                        0x278b,  // RM2 Pro Plus BL
                        0x2797,  // RM2 Pro Plus HYC
                        0x27a1,  // RM2 Pro Plus R1
                        0x27a6,  // RM2 Pro PP
                    }
                },
                { "A1", new int[] { 0x2714 } },  // A1
                { "Mp1", new int[] {
                        0x4EB5 ,  // MP1
                        0x4EF7    // Honyar oem mp1
                    }
                },
                { "Hysen", new int[] { 0x4EAD } },  // Hysen controller
                { "S1c", new int[] { 0x2722 } },    // S1 (SmartOne Alarm Kit)
                { "Dooya", new int[] { 0x4E4D } }   // Dooya DT360E (DOOYA_CURTAIN_V2)
            };

        /// <summary>
        /// Generate device of type matching.
        /// </summary>
        /// <param name="devType"></param>
        /// <param name="host"></param>
        /// <param name="mac"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://github.com/mjg59/python-broadlink/blob/56b2ac36e5a2359272f4af8a49cfaf3e1891733a/broadlink/__init__.py#L17-L59
        /// </remarks>
        internal static Devices.IDevice GenDevice(int devType, IPEndPoint host, byte[] mac)
        {
            var devicePair = Factory.Devices
                .FirstOrDefault(d => d.Value.Contains(devType));

            IDevice result = null;

            if (devicePair.Equals(default(KeyValuePair<string, int[]>)))
            {
                result = new Devices.Device(host, mac, devType);
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();

                result = (IDevice)assembly.CreateInstance(
                    $"SharpBroadlink.Devices.{devicePair.Key}", // Full class name
                    false,
                    BindingFlags.CreateInstance,
                    null,
                    new object[] { host, mac, devType }, // passing values for constructor
                    null,
                    null);
            }

            return result;
        }
    }
}
