using System;
using System.Collections.Generic;
using System.Text;

namespace SharpBroadlink.Devices
{
    public enum DeviceType
    {
        /// <summary>
        /// Unknown device
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Smart Plug 1
        /// </summary>
        /// <remarks>
        /// ?
        /// </remarks>
        Sp1 = 1,

        /// <summary>
        /// Smart Plug 2
        /// </summary>
        /// <remarks>
        /// SP2, Honeywell SP2, SPMini, SP3, OEM branded SP3, SP3S, SPMini2,
        /// OEM branded SPMini, OEM branded SPMini2, SPMiniPlus
        /// </remarks>

        Sp2 = 2,

        /// <summary>
        /// RM IR Controller
        /// </summary>
        /// <remarks>
        /// RM2, RM Mini, RM Pro Phicomm, RM2 Home Plus, RM2 Home Plus GDT,
        /// RM2 Pro Plus, RM2 Pro Plus2, RM2 Pro Plus3, RM2 Pro Plus_300,
        /// RM2 Pro Plus BL, RM2 Pro Plus HYC, RM2 Pro Plus R1, RM2 Pro PP,
        /// RM Mini Shate
        /// </remarks>
        Rm = 3,

        /// <summary>
        /// A1 Temperature, Humidity, Light, Noise, VOC Sensor
        /// </summary>
        /// <remarks>
        /// A1
        /// </remarks>
        A1 = 4,

        /// <summary>
        /// Smart Power Strip
        /// </summary>
        /// <remarks>
        /// MP1, Honyar oem mp1
        /// </remarks>
        Mp1 = 5,

        /// <summary>
        /// Hysen heating controller
        /// </summary>
        /// <remarks>
        /// Hysen controller
        /// </remarks>
        Hysen = 6,

        /// <summary>
        /// S1 Motion, Door Sensors
        /// </summary>
        /// <remarks>
        /// Smart One Alarm Kit
        /// </remarks>
        S1c = 7,

        /// <summary>
        /// Dooya Curtain
        /// </summary>
        /// <remarks>
        /// Dooya DT360E (DOOYA_CURTAIN_V2)
        /// </remarks>
        Dooya = 8
    }
}
