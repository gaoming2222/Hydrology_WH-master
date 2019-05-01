using System;
using System.Runtime.InteropServices;

namespace Hydrology.Entity
{
    [Serializable]
    public struct ModemInfoStruct
    {
        /// <summary>
        /// 站点ID
        /// </summary>
        public string stationid;
        /// <summary>
        /// Modem模块的ID号
        /// </summary>
        public uint m_modemId;

        /// <summary>
        /// Modem的11位SIM卡号，必须以'\0'字符结尾 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] m_phoneno;

        /// <summary>
        /// Modem的4位动态ip地址   
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] m_dynip;

        /// <summary>
        /// Modem模块最后一次建立TCP连接的时间 
        /// </summary>
        public uint m_conn_time;

        /// <summary>
        /// Modem模块最后一次收发数据的时间  
        /// </summary>
        public uint m_refresh_time;

        public override bool Equals(object obj)
        {
            try
            {
                var item = (ModemInfoStruct)obj;

                return (this.m_modemId == item.m_modemId && this.m_phoneno == item.m_phoneno && this.m_dynip == item.m_dynip && this.m_conn_time == item.m_conn_time && this.m_refresh_time == item.m_refresh_time);
            }
            catch
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
