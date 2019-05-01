using System;
using System.Collections.Generic;
using System.Diagnostics;
using Hydrology.Entity;
using Protocol.Channel.Interface;
using Protocol.Manager;
using System.Windows.Forms;
using Hydrology.Forms;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Timers;
using Hydrology.DBManager;

namespace Hydrology.DataMgr
{
    public class CPortDataMgr
    {
        public static int gsmNum = 0;

        public int dataNum = 0;
        public int gprsNum = 0;
        public ITransparen transparen = null;

        private System.Timers.Timer m_timer = new System.Timers.Timer()
        {
            Enabled = true,
            Interval = 60 * 60 * 1000
        };
        private System.Timers.Timer m_timer_1 = new System.Timers.Timer()
        {
            Enabled = true,
            Interval = 60 * 60 * 1000
        };


        #region CPortDataMgr单例模式

        private static CPortDataMgr instance;
        public static CPortDataMgr Instance
        {
            get
            {
                if (instance == null)
                    instance = new CPortDataMgr();
                return instance;
            }
        }

        private CPortDataMgr()
        {
            CDBDataMgr.Instance.StationUpdated += new EventHandler(Instance_StationUpdated);
            //m_timer.Elapsed += new ElapsedEventHandler(m_timer_Elapsed);
            //m_timer_1.Elapsed += new ElapsedEventHandler(m_timer_Elapsed_1);

        }

        void Instance_StationUpdated(object sender, EventArgs e)
        {
            var stations = CDBDataMgr.Instance.GetAllStation();
            //  更新GPRS列表中站点信息
            if (null != this.m_gprsLists)
            {
                foreach (var gprs in m_gprsLists)
                {
                    gprs.InitStations(stations);
                }
            }
            //  更新GSM列表中站点信息
            if (null != this.m_gsmLists)
            {
                foreach (var gsm in m_gsmLists)
                {
                    gsm.InitStations(stations);
                }
            }
            //  更新北斗卫星500型指挥机列表中站点信息
            if (null != this.m_beidou500Lists)
            {
                foreach (var item in this.m_beidou500Lists)
                {
                    item.InitStations(stations);
                }
            }
            //  更新北斗卫星普通终端列表中站点信息
            if (null != this.m_beidouNormalLists)
            {
                foreach (var item in this.m_beidouNormalLists)
                {
                    item.InitStations(stations);
                }
            }
        }

        #endregion

        private String _lastError;
        public String LastError
        {
            get
            {
                return this._lastError;
            }
        }


        void m_timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            m_timer.Stop();
            if (dataNum == 0)
            {
                DateTime dt = DateTime.Now;
                int minute = dt.Minute;
                if (minute <= 60 || minute >= 0)
                {
                    try
                    {
                        CPortDataMgr.Instance.StopGprs();
                        CPortDataMgr.Instance.StartGprs();
                        CPortDataMgr.Instance.RealodGsm();
                        CPortDataMgr.Instance.InitBeidouNormal();
                        CPortDataMgr.Instance.InitBeidou500();
                        m_timer.Start();
                    }
                    catch (Exception eee)
                    {
                        m_timer.Start();
                        Debug.WriteLine("重启端口和串口失败");
                    }
                }
                else
                {

                }

            }
            else
            {
                dataNum = 0;
                m_timer.Start();
            }
        }
        void m_timer_Elapsed_1(object sender, ElapsedEventArgs e)
        {
            m_timer_1.Stop();
            if (gprsNum == 0)
            {
                DateTime dt = DateTime.Now;
                int minute = dt.Minute;
                if (minute <= 60 || minute >= 0)
                {
                    try
                    {
                        CSystemInfoMgr.Instance.AddInfo(string.Format("接收数据超时，重启GPRS......"));
                        CPortDataMgr.Instance.StopGprs();
                        CPortDataMgr.Instance.StartGprs();
                        CSystemInfoMgr.Instance.AddInfo(string.Format("接收数据超时，重启GPRS......   成功！"));
                        m_timer_1.Start();
                    }
                    catch (Exception eee)
                    {
                        m_timer_1.Start();
                        CSystemInfoMgr.Instance.AddInfo(string.Format("接收数据超时，重启GPRS......   失败！"));
                        Debug.WriteLine("重启端口失败");
                    }
                }
                else
                {
                }

            }
            else
            {
                gprsNum = 0;
                m_timer_1.Start();
            }
        }
            #region GSM和GPRS发送数据
            /// <summary>
            /// 调整时钟时间
            /// </summary>
            /// <param name="gprsNum">GPRS号码</param>
            /// <param name="stationID">站点ID</param>
            /// <param name="dt">时间</param>
            /// <returns>查询字符串</returns>
            public String SendAdjustClock(CEntityStation station)
        {
            string userID = station.GPRS;
            string query = string.Empty;// SendGprsSet(station.GPRS, station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now });
            var gprs = FindGprsByUserid(userID);
            if (gprs == null)
            {
                CProtocolEventManager.GPRS_OffLine(null, null);
            }
            else
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    query = gprs.Down.BuildSet(station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now }, EChannelType.GPRS);
                    gprs.SendDataTwice(dtuID, query);
                }
                else
                {
                    CProtocolEventManager.GPRS_OffLine(null, null);
                }
            }
            return query;
        }

        public String SendAdjustClockFirst(CEntityStation station)
        {
            string userID = station.GPRS;
            string query = string.Empty;// SendGprsSet(station.GPRS, station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now });
            var gprs = FindGprsByUserid(userID);
            if (gprs == null)
            {
                //     CProtocolEventManager.GPRS_OffLine(null, null);
            }
            else
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    query = gprs.Down.BuildSet(station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now }, EChannelType.GPRS);
                    gprs.SendData(dtuID, query);
                }
                else
                {
                    CProtocolEventManager.GPRS_OffLine(null, null);
                }
            }
            return query;
        }

        public String SendTcpBatchAdjustClock(CEntityStation station)
        {
            string stationID = station.StationID;
            string query = string.Empty;
            var tcp = FindTcpByUserid(stationID);
            if (tcp != null)
            {
                //uint dtuID = 0;
                if (tcp.getSessionIdbyStationid(stationID) != null && tcp.getSessionIdbyStationid(stationID) != "0")
                {
                    //TODO
                    query = tcp.Down.BuildSet(station.StationID, new List<EDownParamGY>() { EDownParamGY.clockset }, null, EChannelType.TCP);
                    tcp.SendData(stationID, query);
                }
                //1109
                else
                {
                    //MessageBox.Show("站点" + stationID + "当前不在线！");
                    CDBLog.Instance.AddInfo(string.Format("站点" + stationID + "当前不在线！"));
                }
            }
            else
            {
                //MessageBox.Show("站点" + stationID + "当前不在线！");
                CDBLog.Instance.AddInfo(string.Format("站点" + stationID + "当前不在线！"));
            }
            return query;
        }

        public String GroupStorageWater(CEntityStation station)
        {
            StringBuilder sb = new StringBuilder();
            string userID = station.GPRS;
            string query = string.Empty;// SendGprsSet(station.GPRS, station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now });
            var gprs = FindGprsByUserid(userID);
            if (gprs == null)
            {
                //     CProtocolEventManager.GPRS_OffLine(null, null);
            }
            else
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    // query = gprs.Down.BuildSet(station.StationID, new List<EDownParam>() { EDownParam.Rain }, new CDownConf() { }, EChannelType.GPRS);
                    sb.Append("$");
                    sb.Append(station.StationID);
                    sb.Append("0G");
                    sb.Append(" 12\r\n");
                    query = sb.ToString();
                    //60030G 02\r\n";
                    gprs.SendDataTwice(dtuID, query);
                }
                else
                {
                    // CProtocolEventManager.GPRS_OffLine(null, null);
                }
            }
            return query;
        }

        public String GroupStorageWaterFirst(CEntityStation station)
        {
            StringBuilder sb = new StringBuilder();
            string userID = station.GPRS;
            string query = string.Empty;// SendGprsSet(station.GPRS, station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now });
            var gprs = FindGprsByUserid(userID);
            if (gprs == null)
            {
                //   CProtocolEventManager.GPRS_OffLine(null, null);
            }
            else
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    // query = gprs.Down.BuildSet(station.StationID, new List<EDownParam>() { EDownParam.Rain }, new CDownConf() { }, EChannelType.GPRS);
                    sb.Append("$");
                    sb.Append(station.StationID);
                    sb.Append("0G");
                    sb.Append(" 12\r\n");
                    query = sb.ToString();
                    //60030G 02\r\n";
                    // gprs.SendDataTwice(dtuID, query);
                    gprs.SendData(dtuID, query);
                }
                else
                {
                    //   CProtocolEventManager.GPRS_OffLine(null, null);
                }
            }
            return query;
        }

        public String GroupRealityWater(CEntityStation station)
        {
            StringBuilder sb = new StringBuilder();
            string userID = station.GPRS;
            string query = string.Empty;// SendGprsSet(station.GPRS, station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now });
            var gprs = FindGprsByUserid(userID);
            if (gprs == null)
            {
                //    CProtocolEventManager.GPRS_OffLine(null, null);
            }
            else
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    // query = gprs.Down.BuildSet(station.StationID, new List<EDownParam>() { EDownParam.Rain }, new CDownConf() { }, EChannelType.GPRS);
                    sb.Append("$");
                    sb.Append(station.StationID);
                    sb.Append("0G");
                    sb.Append(" 13\r\n");
                    query = sb.ToString();
                    //60030G 02\r\n";
                    gprs.SendDataTwice(dtuID, query);
                }
                else
                {
                    //     CProtocolEventManager.GPRS_OffLine(null, null);
                }
            }
            return query;
        }

        public String GroupRealityWaterFirst(CEntityStation station)
        {
            StringBuilder sb = new StringBuilder();
            string userID = station.GPRS;
            string query = string.Empty;// SendGprsSet(station.GPRS, station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now });
            var gprs = FindGprsByUserid(userID);
            if (gprs == null)
            {
                //   CProtocolEventManager.GPRS_OffLine(null, null);
            }
            else
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    // query = gprs.Down.BuildSet(station.StationID, new List<EDownParam>() { EDownParam.Rain }, new CDownConf() { }, EChannelType.GPRS);
                    sb.Append("$");
                    sb.Append(station.StationID);
                    sb.Append("0G");
                    sb.Append(" 13\r\n");
                    query = sb.ToString();
                    //60030G 02\r\n";
                    gprs.SendData(dtuID, query);
                }
                else
                {
                    //    CProtocolEventManager.GPRS_OffLine(null, null);
                }
            }
            return query;
        }
        public String GroupRainWater(CEntityStation station)
        {
            StringBuilder sb = new StringBuilder();
            string userID = station.GPRS;
            string query = string.Empty;// SendGprsSet(station.GPRS, station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now });
            var gprs = FindGprsByUserid(userID);
            if (gprs == null)
            {
                //   CProtocolEventManager.GPRS_OffLine(null, null);
            }
            else
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    // query = gprs.Down.BuildSet(station.StationID, new List<EDownParam>() { EDownParam.Rain }, new CDownConf() { }, EChannelType.GPRS);
                    sb.Append("$");
                    sb.Append(station.StationID);
                    sb.Append("0G");
                    sb.Append(" 02\r\n");
                    query = sb.ToString();
                    //60030G 02\r\n";
                    gprs.SendDataTwice(dtuID, query);
                }
                else
                {
                    //   CProtocolEventManager.GPRS_OffLine(null, null);
                }
            }
            return query;
        }

        public String GroupRainWaterFirst(CEntityStation station)
        {
            StringBuilder sb = new StringBuilder();
            string userID = station.GPRS;
            string query = string.Empty;// SendGprsSet(station.GPRS, station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now });
            var gprs = FindGprsByUserid(userID);
            if (gprs == null)
            {
                //      CProtocolEventManager.GPRS_OffLine(null, null);
            }
            else
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    // query = gprs.Down.BuildSet(station.StationID, new List<EDownParam>() { EDownParam.Rain }, new CDownConf() { }, EChannelType.GPRS);
                    sb.Append("$");
                    sb.Append(station.StationID);
                    sb.Append("0G");
                    sb.Append(" 02\r\n");
                    query = sb.ToString();
                    //60030G 02\r\n";
                    gprs.SendData(dtuID, query);
                }
                else
                {
                    //       CProtocolEventManager.GPRS_OffLine(null, null);
                }
            }
            return query;
        }

        public String GroupSoilWater(CEntityStation station)
        {
            StringBuilder sb = new StringBuilder();
            string userID = station.GPRS;
            string query = string.Empty;// SendGprsSet(station.GPRS, station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now });
            var gprs = FindGprsByUserid(userID);
            if (gprs == null)
            {
                //    CProtocolEventManager.GPRS_OffLine(null, null);
            }
            else
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    // query = gprs.Down.BuildSet(station.StationID, new List<EDownParam>() { EDownParam.Rain }, new CDownConf() { }, EChannelType.GPRS);
                    sb.Append("$");
                    sb.Append(station.StationID);
                    sb.Append("0G");
                    sb.Append(" 25\r\n");
                    query = sb.ToString();
                    //60030G 02\r\n";
                    gprs.SendDataTwice(dtuID, query);
                }
                else
                {
                    //      CProtocolEventManager.GPRS_OffLine(null, null);
                }
            }
            return query;
        }

        public String GroupSoilWaterFirst(CEntityStation station)
        {
            StringBuilder sb = new StringBuilder();
            string userID = station.GPRS;
            string query = string.Empty;// SendGprsSet(station.GPRS, station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now });
            var gprs = FindGprsByUserid(userID);
            if (gprs == null)
            {
                //    CProtocolEventManager.GPRS_OffLine(null, null);
            }
            else
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    // query = gprs.Down.BuildSet(station.StationID, new List<EDownParam>() { EDownParam.Rain }, new CDownConf() { }, EChannelType.GPRS);
                    sb.Append("$");
                    sb.Append(station.StationID);
                    sb.Append("0G");
                    sb.Append(" 25\r\n");
                    query = sb.ToString();
                    //60030G 02\r\n";
                    gprs.SendData(dtuID, query);
                }
                else
                {
                    //    CProtocolEventManager.GPRS_OffLine(null, null);
                }
            }
            return query;
        }

        //1116gm
        public String GroupSoilWaterFirst_1(CEntitySoilStation station)
        {
            StringBuilder sb = new StringBuilder();
            string userID = station.GPRS;
            string query = string.Empty;// SendGprsSet(station.GPRS, station.StationID, new List<EDownParam>() { EDownParam.Clock }, new CDownConf() { Clock = DateTime.Now });
            var gprs = FindGprsByUserid(userID);
            if (gprs == null)
            {
                //    CProtocolEventManager.GPRS_OffLine(null, null);
            }
            else
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    // query = gprs.Down.BuildSet(station.StationID, new List<EDownParam>() { EDownParam.Rain }, new CDownConf() { }, EChannelType.GPRS);
                    sb.Append("$");
                    sb.Append(station.StationID);
                    sb.Append("0G");
                    sb.Append(" 25\r\n");
                    query = sb.ToString();
                    //60030G 02\r\n";
                    gprs.SendData(dtuID, query);
                }
                else
                {
                    //    CProtocolEventManager.GPRS_OffLine(null, null);
                }
            }
            return query;
        }


        public String SendReadMsg(string id, string stationID, IList<EDownParamGY> cmds, EChannelType ctype)
        {
            string query = string.Empty;
            //if (EChannelType.GPRS == ctype)
            //{
            //    query = SendGprsRead(id, stationID, cmds);
            //}
            if (EChannelType.TCP == ctype)
            {
                query = SendTcpRead(id, stationID, cmds);
            }
            //else if (EChannelType.GSM == ctype)
            //{
            //    query = SendGsmRead(id, stationID, cmds);
            //}
            return query;
        }

        public String SendSetMsg(string id, string stationID, IList<EDownParam> cmds, CDownConf down, EChannelType ctype)
        {
            string query = string.Empty;
            if (EChannelType.GPRS == ctype)
            {
                query = SendGprsSet(id, stationID, cmds, down);
            }
            else if (EChannelType.GSM == ctype)
            {
                query = SendGsmSet(id, stationID, cmds, down);
            }
            else if (EChannelType.TCP == ctype)
            {
                query = SendTcpSet(id, stationID, cmds, down);
            }
            return query;
        }

        public String SendSetMsgGY(string id, string stationID, IList<EDownParamGY> cmds, CDownConfGY down, EChannelType ctype)
        {
            string query = string.Empty;
            if (EChannelType.GPRS == ctype)
            {
                query = SendGprsSetGY(id, stationID, cmds, down);
            }
            else if (EChannelType.GSM == ctype)
            {
                query = SendGsmSetGY(id, stationID, cmds, down);
            }
            else if (EChannelType.TCP == ctype)
            {
                query = SendTcpSetGY(id, stationID, cmds, down);
            }
            return query;
        }

        private String SendUDiskMsg(string id, string stationID, EStationType stype, ETrans trans, DateTime beginTime, EChannelType ctype)
        {
            string query = string.Empty;
            if (ctype == EChannelType.GPRS)
                query = SendGprsUDisk(id, stationID, stype, trans, beginTime);
            else if (ctype == EChannelType.GSM)
                query = SendGsmUDisk(id, stationID, stype, trans, beginTime);

            return query;
        }
        public String SendUDiskMsg(CEntityStation station, ETrans trans, DateTime beginTime, EChannelType ctype)
        {
            string id = string.Empty;
            switch (ctype)
            {
                case EChannelType.GPRS: id = station.GPRS; break;
                case EChannelType.GSM: id = station.GSM; break;
                //   case EChannelType.PSTN: id = station.PSTV; break;
                case EChannelType.BeiDou: id = station.BDSatellite; break;
                default: throw new Exception("通讯方式参数错误！");
            }
            return SendUDiskMsg(id, station.StationID, station.StationType, trans, beginTime, ctype);
        }

        private String SendFlashMsg(string id, string stationID, EStationType stype, ETrans trans, DateTime beginTime, DateTime endTime, EChannelType ctype)
        {
            string query = string.Empty;
            if (ctype == EChannelType.GPRS)
                return SendGprsFlash(id, stationID, stype, trans, beginTime, endTime);
            else if (ctype == EChannelType.GSM)
                return SendGsmFlash(id, stationID, stype, trans, beginTime, endTime);
            else if (ctype == EChannelType.TCP)
                return SendTcpFlash(id, stationID, stype, trans, beginTime, endTime);
            return query;
        }
        public String SendFlashMsg(CEntityStation station, ETrans trans, DateTime beginTime, DateTime endTime, EChannelType ctype)
        {
            string id = string.Empty;
            switch (ctype)
            {
                case EChannelType.GPRS: id = station.GPRS; break;
                case EChannelType.GSM: id = station.GSM; break;
                //   case EChannelType.PSTN: id = station.PSTV; break;
                case EChannelType.BeiDou: id = station.BDSatellite; break;
                case EChannelType.TCP: id = ""; break;
                default: throw new Exception("通讯方式参数错误！");
            }
            return SendFlashMsg(id, station.StationID, station.StationType, trans, beginTime, endTime, ctype);
        }

        public void SendMsg(string gsmNum, string stationID, string query, EChannelType ctype)
        {
            if (EChannelType.GPRS == ctype)
            {
                SendGprs(gsmNum, stationID, query);
            }
            else if (EChannelType.GSM == ctype)
            {
                SendGsm(gsmNum, stationID, query);
            }
        }
        #endregion

        #region GPRS模块数据维护

        private List<IGprs> m_gprsLists;
        public List<IGprs> GprsLists { get { return this.m_gprsLists; } }
        public void StartGprs(bool bIsSysLoad = true)
        {
            if (m_gprsLists != null)
            {
                // 停止之前的所有监听端口，然后重新打开
                StopGprs();
            }
            bool isFirst = false;
            m_gprsLists = new List<IGprs>();
            //  读取配置文件，加载所有gprs信道协议
            var gprsNameLists = XmlDocManager.Instance.GPRSProtocolNames;
            foreach (var gname in gprsNameLists)
            {
                CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......", gname));
                var dll = XmlDocManager.Instance.GetInfoByName(gname);
                if (dll == null)
                    continue;
                //  配置文件中的每个端口监听一个GPRS
                foreach (var port in dll.Ports)
                {
                    try
                    {
                        //  默认不启动手动加载的GPRS协议
                        if (!bIsSysLoad)
                        {
                            // 不是系统启动，也就是中途更改，只要是启动状态的都启动，否则都不启动
                            if (!port.BStartOrNot)
                            {
                                // 并且不启动，手动配置不启动
                                CSystemInfoMgr.Instance.AddInfo(string.Format("默认不启用协议\"{0}\"监听端口{1}", gname, port.PortNumber));
                                continue;
                            }
                            else
                            {
                                //手动配置，启动，并且是界面通知
                            }
                        }
                        else
                        {
                            // 是系统启动，跳过启动手动启动
                            if (!port.BAutoStart)
                            {
                                continue; //不是系统启动的，就不用启动了
                            }
                        }
                        
                        IGprs gprs = ProtocolManager.Gprs(dll);
                        if (gprs == null)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----通讯方式配置出错!", gname));
                            continue;
                        }
                        //gprs.addPort((ushort)port.PortNumber);
                        gprs.UpDataReceived += CProtocolEventManager.UpDataReceived;
                        gprs.DownDataReceived += CProtocolEventManager.DownDataReceived;
                        gprs.BatchDataReceived += CProtocolEventManager.BatchDataReceived;
                        gprs.ErrorReceived += CProtocolEventManager.ErrorForUIReceived;
                        gprs.MessageSendCompleted += CProtocolEventManager.MessageSendCompleted;
                        gprs.SerialPortStateChanged += CProtocolEventManager.SerialPortStateChanged;
                        gprs.ModemDataReceived += CProtocolEventManager.GPRS_ModemDataReceived;
                        gprs.ModemInfoDataReceived += CProtocolEventManager.GPRS_ModemInfoDataReceived;
                        gprs.GPRSTimeOut += CProtocolEventManager.GPRS_TimeOut;
                        gprs.SoilDataReceived += CProtocolEventManager.SoilDataReceived;
                        var stations = CDBDataMgr.Instance.GetAllStation();
                        gprs.Init();
                        gprs.InitStations(stations);
                        //  加载数据协议信息
                        var dataInfoDll = XmlDocManager.Instance.GetDataDllByComOrPort(port.PortNumber, false);
                        if (dataInfoDll == null)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----数据协议配置出错!", gname));
                            continue;
                        }
                        var iup = ProtocolManager.Up(dataInfoDll);
                        var idown = ProtocolManager.Down(dataInfoDll);
                        var iudisk = ProtocolManager.UDisk(dataInfoDll);
                        var iflash = ProtocolManager.Flash(dataInfoDll);
                        var iSoil = ProtocolManager.Soil(dataInfoDll);
                        gprs.InitInterface(iup, idown, iudisk, iflash, iSoil);
                        if (!isFirst)
                        {
                            if (gprs.DSStartService((ushort)port.PortNumber))
                            {
                                m_gprsLists.Add(gprs);
                                CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......完成!", gname));
                                isFirst = true;
                            }
                            else
                            {
                                CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----启动GPRS失败!", gname));
                            }
                        }
                        else
                        {
                            if (gprs.addPort((ushort)port.PortNumber))
                            {
                                m_gprsLists.Add(gprs);
                                CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......完成!", gname));
                            }
                        }
                    }

                    catch (Exception exp)
                    {
                        Debug.WriteLine(exp.Message);
                    }
                }
            }
        }
        public IGprs FindGprsByListeningPort(ushort ListenPort)
        {
            if (this.m_gprsLists == null)
                return null;
            foreach (var item in this.m_gprsLists)
            {
                if (item.GetListenPort() == ListenPort)
                {
                    return item;
                }
            }
            return null;
        }
        /// <summary>
        /// 根据yo'g
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public IGprs FindGprsByUserid(string uid)
        {
            IGprs gprs = null;
            uint dtuID = 0;
            foreach (var item in this.m_gprsLists)
            {
                if (item.FindByID(uid, out dtuID))
                {
                    gprs = item;
                    break;
                }
            }
            return gprs;
        }

        



        public void StopGprs()
        {
            if (this.m_gprsLists == null)
                return;
            foreach (var gprs in this.m_gprsLists)
            {
                try
                {
                    gprs.UpDataReceived -= CProtocolEventManager.UpDataReceived;
                    gprs.DownDataReceived -= CProtocolEventManager.DownDataReceived;
                    gprs.BatchDataReceived -= CProtocolEventManager.BatchDataReceived;
                    gprs.ErrorReceived -= CProtocolEventManager.ErrorForUIReceived;
                    gprs.MessageSendCompleted -= CProtocolEventManager.MessageSendCompleted;
                    gprs.SerialPortStateChanged -= CProtocolEventManager.SerialPortStateChanged;
                    gprs.GPRSTimeOut -= CProtocolEventManager.GPRS_TimeOut;
                    gprs.SoilDataReceived -= CProtocolEventManager.SoilDataReceived;

                    gprs.DSStopService();
                }
                catch (Exception exp) { Debug.WriteLine(exp.Message); }
            }
        }

        public int GPRSCurrentOnlineUserCount
        {
            get
            {
                int count = 0;
                if (m_gprsLists != null)
                {
                    foreach (var gprs in this.m_gprsLists)
                    {
                        if (gprs.GetStarted() && gprs.DTUList != null)
                        {
                            count += gprs.DTUList.Count;
                        }
                    }
                }
                return count;
            }
        }

        public IGprs StartGprs(ushort port)
        {
            throw new NotImplementedException("GPRS方法未实现！");
        }
        public IGprs FindGprs()
        {
            throw new NotImplementedException("GPRS方法未实现！");
        }

        private void SendGprs(string userid, string stationID, string query)
        {
            var gprs = FindGprsByUserid(userid);
            if (gprs != null)
            {
                uint dtuID = 0;
                if (gprs.FindByID(userid, out dtuID))
                {
                    //query = gprs.Down.BuildQuery(stationID, cmds, EChannelType.GPRS);
                    gprs.SendDataTwice(dtuID, query);
                }
            }
        }

        public bool SendHex(CEntityStation station)
        {
            byte[] bts = new byte[] { 84, 82, 85, 13, 10 };
            // var gprs = FindGprsByUserid(userid);
            string userID = station.GPRS;
            string query = string.Empty;
            var gprs = FindGprsByUserid(userID);
            if (gprs != null)
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    //query = gprs.Down.BuildQuery(stationID, cmds, EChannelType.GPRS);
                    gprs.SendHex(dtuID, bts);
                    // gprs.SendHexTwice(dtuID, bts);
                }
                return true;
            }
            return false;
        }

        public bool SendSoilHex(CEntitySoilStation station)
        {
            byte[] bts = new byte[] { 84, 82, 85, 13, 10 };
            // var gprs = FindGprsByUserid(userid);
            string userID = station.GPRS;
            string query = string.Empty;
            var gprs = FindGprsByUserid(userID);
            if (gprs != null)
            {
                uint dtuID = 0;
                if (gprs.FindByID(userID, out dtuID))
                {
                    //query = gprs.Down.BuildQuery(stationID, cmds, EChannelType.GPRS);
                    gprs.SendHex(dtuID, bts);
                    // gprs.SendHexTwice(dtuID, bts);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// GPRS 发送读取数据命令
        /// userid对应于CEntityStation中的gprs号码
        /// </summary>
        /// <param name="dtuID">GPRS ID</param>
        /// <param name="stationID">站点ID</param>
        /// <param name="cmds">读取指令参数集合</param>
        public String SendGprsRead(string userid, string stationID, IList<EDownParam> cmds)
        {
            string query = string.Empty;
            var gprs = FindGprsByUserid(userid);
            if (gprs != null)
            {
                uint dtuID = 0;
                if (gprs.FindByID(userid, out dtuID))
                {
                    query = gprs.Down.BuildQuery(stationID, cmds, EChannelType.GPRS);
                    gprs.SendDataTwice(dtuID, query);
                }
                //1109
                else
                {
                    MessageBox.Show("站点" + stationID + "当前不在线！");
                }
            }
            else
            {
                MessageBox.Show("站点" + stationID + "当前不在线！");
            }
            return query;
        }

        public String SendGprsRead_BS(string userid, string stationID, IList<EDownParam> cmds)
        {
            string query = string.Empty;
            var gprs = FindGprsByUserid(userid);
            if (gprs != null)
            {
                uint dtuID = 0;
                if (gprs.FindByID(userid, out dtuID))
                {
                    query = gprs.Down.BuildQuery(stationID, cmds, EChannelType.GPRS);
                    gprs.SendDataTwice(dtuID, query);
                }

            }

            return query;
        }

        
        /// <summary>
        /// GPRS 发送设置命令
        /// </summary>
        /// <param name="dtuID">GPRS ID</param>
        /// <param name="stationID">站点ID</param>
        /// <param name="down">查询参数</param>
        public String SendGprsSet(string userid, string stationID, IList<EDownParam> cmds, CDownConf down)
        {
            string query = string.Empty; ;
            var gprs = FindGprsByUserid(userid);
            if (gprs != null)
            {
                uint dtuID = 0;
                if (gprs.FindByID(userid, out dtuID))
                {
                    query = gprs.Down.BuildSet(stationID, cmds, down, EChannelType.GPRS);
                    gprs.SendDataTwice(dtuID, query);
                }
                else
                {
                    MessageBox.Show("站点" + stationID + "当前不在线！");
                }
            }
            else
            {
                MessageBox.Show("站点" + stationID + "当前不在线！");
            }
            return query;
        }

        public String SendGprsSetGY(string userid, string stationID, IList<EDownParamGY> cmds, CDownConfGY down)
        {
            string query = string.Empty; ;
            var gprs = FindGprsByUserid(userid);
            if (gprs != null)
            {
                uint dtuID = 0;
                if (gprs.FindByID(userid, out dtuID))
                {
                    query = gprs.Down.BuildSet(stationID, cmds, down, EChannelType.GPRS);
                    gprs.SendDataTwice(dtuID, query);
                }
                else
                {
                    MessageBox.Show("站点" + stationID + "当前不在线！");
                }
            }
            else
            {
                MessageBox.Show("站点" + stationID + "当前不在线！");
            }
            return query;
        }

        /// <summary>
        /// 透明传输发送指令
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="stationID"></param>
        /// <param name="cmds"></param>
        /// <param name="down"></param>
        /// <returns></returns>
        public String SendTcpSet(string userid, string stationID, IList<EDownParam> cmds, CDownConf down)
        {
            //TODO
            string query = string.Empty; ;
            var gprs = FindGprsByUserid(userid);
            if (gprs != null)
            {
                uint dtuID = 0;
                if (gprs.FindByID(userid, out dtuID))
                {
                    query = gprs.Down.BuildSet(stationID, cmds, down, EChannelType.GPRS);
                    gprs.SendDataTwice(dtuID, query);
                }
                else
                {
                    MessageBox.Show("站点" + stationID + "当前不在线！");
                }
            }
            else
            {
                MessageBox.Show("站点" + stationID + "当前不在线！");
            }
            return query;
        }


        public String SendTcpSetGY(string userid, string stationID, IList<EDownParamGY> cmds, CDownConfGY down)
        {
            //TODO
            string query = string.Empty; ;
            var gprs = FindGprsByUserid(userid);
            if (gprs != null)
            {
                uint dtuID = 0;
                if (gprs.FindByID(userid, out dtuID))
                {
                    query = gprs.Down.BuildSet(stationID, cmds, down, EChannelType.GPRS);
                    gprs.SendDataTwice(dtuID, query);
                }
                else
                {
                    MessageBox.Show("站点" + stationID + "当前不在线！");
                }
            }
            else
            {
                MessageBox.Show("站点" + stationID + "当前不在线！");
            }
            return query;
        }

        /// <summary>
        /// GPRS 发送优盘批量传输数据
        /// </summary>
        /// <param name="dtuID">GPRS ID</param>
        /// <param name="stationID">站点ID</param>
        /// <param name="stype">站点类型</param>
        /// <param name="trans">传输类型：按天传，按小时传</param>
        /// <param name="beginTime">数据开始时间</param>
        /// <returns>
        /// True ：传输成功
        /// Flase：传输失败
        /// </returns>
        public String SendGprsUDisk(string userid, string stationID, EStationType stype, ETrans trans, DateTime beginTime)
        {
            string query = string.Empty;
            var gprs = FindGprsByUserid(userid);
            if (gprs != null)
            {
                uint dtuID = 0;
                if (gprs.FindByID(userid, out dtuID))
                {
                    query = gprs.UBatch.BuildQuery(stationID, stype, trans, beginTime, EChannelType.GPRS);
                    gprs.SendDataTwiceForBatchTrans(dtuID, query);
                }
            }
            return query;
        }

        /// <summary>
        /// GPRS 发送Flash批量传输数据
        /// </summary>
        /// <param name="dtuID">GPRS ID</param>
        /// <param name="stationID">站点ID</param>
        /// <param name="stype">站点类型</param>
        /// <param name="trans">传输类型：按天传，按小时传</param>
        /// <param name="beginTime">数据开始时间</param>
        /// <param name="endTime">数据结束时间</param>
        /// <returns>
        /// True ：传输成功
        /// Flase：传输失败
        /// </returns>
        public String SendGprsFlash(string userid, string stationID, EStationType stype, ETrans trans, DateTime beginTime, DateTime endTime)
        {
            string query = string.Empty;
            var gprs = FindGprsByUserid(userid);
            if (gprs != null)
            {
                uint dtuID = 0;
                if (gprs.FindByID(userid, out dtuID))
                {
                    query = gprs.FlashBatch.BuildQuery(stationID, stype, trans, beginTime, endTime, EChannelType.GPRS);
                    gprs.SendDataTwiceForBatchTrans(dtuID, query);
                }
            }
            else
            {
                MessageBox.Show("站点" + stationID + "当前不在线！");
            }
            return query;
        }
        

        #endregion

        #region GSM模块数据维护

        private List<IGsm> m_gsmLists;
        public void InitGsms()
        {
            //  初始化系统中GSM列表
            if (this.m_gsmLists != null)
            {
                StopGsmLists();
            }
            this.m_gsmLists = new List<IGsm>();

            var gsmNameLists = XmlDocManager.Instance.WebGSMProtocolNames;

            var sysComs = new List<string>(System.IO.Ports.SerialPort.GetPortNames());

            foreach (var gsmName in gsmNameLists)
            {
                CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......", gsmName));
                var dll = XmlDocManager.Instance.GetInfoByName(gsmName);
                if (dll == null)
                    continue;

                foreach (var portNumber in dll.Coms)
                {
                    //  如果系统中不包含串口
                    //      获取下一条串口配置信息
                    string comPortName = string.Format("COM{0}", portNumber);
                    if (!sysComs.Contains(comPortName))
                    {
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----计算机系统不包含串口{1}!", gsmName, comPortName));
                        continue;
                    }

                    var portInfo = CDBDataMgr.Instance.GetSerialPortByPortNumber(portNumber);
                    if (portInfo == null)
                    {
                        Debug.WriteLine(string.Format("COM {0} is not the record in database!", portNumber));
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----数据库中不包含串口{1}信息!", gsmName, comPortName));
                        continue;
                    }
                    // 判断配置是否启动
                    if (portInfo.SwitchSatus.HasValue && portInfo.SwitchSatus.Value == false)
                    {
                        // 配置不打开串口
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置不启用串口{0}", comPortName));
                        continue;
                    }

                    try
                    {
                        //  初始化gsm
                        IGsm gsm = ProtocolManager.GSM(dll);
                        if (gsm == null)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----通讯方式配置出错!", gsmName));
                            continue;
                        }
                        //  事件注册
                        gsm.UpDataReceived += CProtocolEventManager.UpDataReceived;
                        gsm.DownDataReceived += CProtocolEventManager.DownDataReceived;
                        gsm.BatchDataReceived += CProtocolEventManager.BatchDataReceived;
                        gsm.ErrorReceived += CProtocolEventManager.ErrorForUIReceived;
                        gsm.MessageSendCompleted += CProtocolEventManager.MessageSendCompleted;
                        gsm.SerialPortStateChanged += CProtocolEventManager.SerialPortStateChanged;
                        gsm.SoilDataReceived += CProtocolEventManager.SoilDataReceived;
                        gsm.GSMTimeOut += CProtocolEventManager.GSM_TimeOut;
                        gsm.InitPort(comPortName, portInfo.Baudrate);
                        if (!gsm.OpenPort())
                        {
                            MessageBox.Show("串口" + comPortName + "打开失败！");
                            continue;
                        }
                        if (!gsm.InitGsm())
                        {
                            MessageBox.Show("串口" + comPortName + "参数设置失败！");
                            gsm.ClosePort();
                            continue;
                        }

                        //  加载数据协议信息
                        var dataDllInfo = XmlDocManager.Instance.GetDataDllByComOrPort(portNumber,true);
                        if (dataDllInfo == null)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----数据协议配置出错!", gsmName));
                            continue;
                        }
                        var iup = ProtocolManager.Up(dataDllInfo);
                        var idown = ProtocolManager.Down(dataDllInfo);
                        var iudisk = ProtocolManager.UDisk(dataDllInfo);
                        var iflash = ProtocolManager.Flash(dataDllInfo);
                        var iSoil = ProtocolManager.Soil(dataDllInfo);
                        gsm.InitInterface(iup, idown, iudisk, iflash, iSoil);

                        var stations = CDBDataMgr.Instance.GetAllStation();
                        gsm.InitStations(stations);

                        m_gsmLists.Add(gsm);
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......完成!", gsmName));
                    }
                    catch (Exception ex)
                    {
                        // 打开串口失败
                        Debug.WriteLine(ex.ToString());
                    }

                }
            }
        }
        public static void ClearGSM(int portNum)
        {
            //bool isSendSuccess = true;
            var dll = XmlDocManager.Instance.GetInfoByName("GSM");
            var portInfo = CDBDataMgr.Instance.GetSerialPortByPortNumber(portNum);
            string comPortName = string.Format("COM{0}", portNum);
            if (dll != null)
            {
                IGsm gsm = ProtocolManager.GSM(dll);
                if (gsm != null)
                {
                    
                }
            }

        }
        public void RealodGsm()
        {
            try
            {
                if (m_gsmLists != null)
                {
                    foreach (IGsm gsm in m_gsmLists)
                    {
                        gsm.Close();
                    }
                    m_gsmLists.Clear();
                }
                InitGsms();
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        public void StopGsmLists()
        {
            if (m_gsmLists != null)
            {
                foreach (IGsm gsm in m_gsmLists)
                {
                    try
                    {
                        gsm.Close();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
        }

        public void EHSerialPortUpdated(object sender, EventArgs args)
        {
            try
            {
                if (m_gsmLists != null && m_gsmLists.Count > 0)
                {
                    foreach (var item in m_gsmLists)
                    {
                        item.ClosePort();
                    }
                    m_gsmLists.Clear();
                    m_CurrentGsmIndex = 0;
                }
                InitGsms();
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }

            try
            {
                //  更新北斗卫星信息
                InitBeidouNormal();
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }

            try
            {
                //  更新北斗卫星指挥机信息
                InitBeidou500();
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }
        }

        private int m_CurrentGsmIndex = 0;
        /// <summary>
        /// 查找可用的GSM，可以发送数据
        /// </summary>
        private IGsm FindGsm(string stationID)
        {
            IGsm mygsm = null;
            if (m_gsmLists != null && m_gsmLists.Count > 0)
            {
                mygsm = m_gsmLists[m_CurrentGsmIndex];
                m_CurrentGsmIndex = (m_CurrentGsmIndex + 1) % m_gsmLists.Count;
            }
            return mygsm;
        }
        /// <summary>
        /// GSM 发送读取数据命令
        /// </summary>
        /// <param name="gsmNum">GSM号码</param>
        /// <param name="stationID">站点ID</param>
        /// <param name="cmds">读取指令参数集合</param>
        /// <returns></returns>
        private void SendGsm(string gsmNum, string stationID, string query)
        {
            var gsm = FindGsm(stationID);
            if (gsm != null)
            {
                Debug.Write(query);
                if (!string.IsNullOrEmpty(query))
                {
                    // 写入系统日志
                    string returnMsg = string.Empty;
                    gsm.SendMsg(gsmNum, query);
                }
            }
        }

        private string SendGsmRead(string gsmNum, string stationID, IList<EDownParam> cmds)
        {
            string qry = string.Empty;
            var gsm = FindGsm(stationID);
            if (gsm != null)
            {
                qry = gsm.Down.BuildQuery(stationID, cmds, EChannelType.GSM);
                Debug.Write(qry);
                if (!string.IsNullOrEmpty(qry))
                {
                    // 写入系统日志
                    string returnMsg = string.Empty;
                    gsm.SendMsg(gsmNum, qry);
                }
                // 写入系统日志
                //CSystemInfoMgr.Instance.AddInfo(string.Format("GSM 读取参数：目标站点（{0}） 参数过多，读取失败", stationID));
                //_lastError = string.Format("如果发送{0}接收数据的长度将大于140个字符,会导致发送数据失败！请重新选择读取项！", qry);
            }
            return qry;
        }

        /// <summary>
        /// GSM 发送设置命令
        /// </summary>
        /// <param name="gsmNum">GSM号码</param>
        /// <param name="stationID">站点ID</param>
        /// <param name="down">查询参数</param>
        /// <returns></returns>
        private string SendGsmSet(string gsmNum, string stationID, IList<EDownParam> cmds, CDownConf down)
        {
            string qry = string.Empty;
            var gsm = FindGsm(stationID);
            if (gsm != null)
            {
                qry = gsm.Down.BuildSet(stationID, cmds, down, EChannelType.GSM);
                Debug.Write(qry);
                // 写入系统日志
                String returnMsg = string.Empty;
                gsm.SendMsg(gsmNum, qry);
            }
            return qry;
        }

        private string SendGsmSetGY(string gsmNum, string stationID, IList<EDownParamGY> cmds, CDownConfGY down)
        {
            string qry = string.Empty;
            var gsm = FindGsm(stationID);
            if (gsm != null)
            {
                qry = gsm.Down.BuildSet(stationID, cmds, down, EChannelType.GSM);
                Debug.Write(qry);
                // 写入系统日志
                String returnMsg = string.Empty;
                gsm.SendMsg(gsmNum, qry);
            }
            return qry;
        }

        /// <summary>
        /// GSM 发送优盘批量传输数据
        /// </summary>
        /// <param name="gsmNum">GSM号码</param>
        /// <param name="stationID">站点ID</param>
        /// <param name="stype">站点类型</param>
        /// <param name="trans">传输类型：按天传，按小时传</param>
        /// <param name="beginTime">数据开始时间</param>
        /// <returns>
        /// True ：传输成功
        /// Flase：传输失败
        /// </returns>
        private string SendGsmUDisk(string gsmNum, string stationID, EStationType stype, ETrans trans, DateTime beginTime)
        {
            string qry = string.Empty;
            var gsm = FindGsm(stationID);
            if (gsm != null)
            {
                qry = gsm.UBatch.BuildQuery(stationID, stype, trans, beginTime, EChannelType.GSM);
                Debug.WriteLine(qry);
                // 写入系统日志
                string returnMsg = string.Empty;
                gsm.SendMsg(gsmNum, qry);
            }
            return qry;
        }

        /// <summary>
        /// GSM 发送Flash批量传输数据
        /// </summary>
        /// <param name="gsmNum">GSM号码</param>
        /// <param name="stationID">站点ID</param>
        /// <param name="stype">站点类型</param>
        /// <param name="trans">传输类型：按天传，按小时传</param>
        /// <param name="beginTime">数据开始时间</param>
        /// <param name="endTime">数据结束时间</param>
        /// <returns>
        /// True ：传输成功
        /// Flase：传输失败
        /// </returns>
        private String SendGsmFlash(string gsmNum, string stationID, EStationType stype, ETrans trans, DateTime beginTime, DateTime endTime)
        {
            string query = string.Empty;
            var gsm = FindGsm(stationID);
            if (gsm != null)
            {
                query = gsm.FlashBatch.BuildQuery(stationID, stype, trans, beginTime, endTime, EChannelType.GSM);
                Debug.WriteLine(query);
                // 写入系统日志
                string returnMsg = string.Empty;
                gsm.SendMsg(gsmNum, query);
            }
            return query;
        }

        #endregion

        #region WebGSM模块数据维护
        private List<IWebGsm> m_webGsmLists;
        public void InitWebGsm()
        {
            //  初始化系统中GSM列表
            if (this.m_webGsmLists != null)
            {
                StopWebGsmLists();
            }
            this.m_webGsmLists = new List<IWebGsm>();

            var webGsmNameLists = XmlDocManager.Instance.WebGSMProtocolNames;

            foreach (var gsmName in webGsmNameLists)
            {
                CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......", gsmName));
                var dll = XmlDocManager.Instance.GetInfoByName(gsmName);
                if (dll == null)
                    continue;

                foreach (var web in dll.Webs)
                {
                    // 判断配置是否启动
                    if (web.BStartOrNot == false)
                    {
                        // 配置不打开串口
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置不启用WebService{0}", web.IP+ ":" + web.PortNumber.ToString()));
                        continue;
                    }

                    try
                    {
                        //  初始化gsm
                        IWebGsm webGsm = ProtocolManager.WEBGSM(dll);
                        if (webGsm == null)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----通讯方式配置出错!", gsmName));
                            continue;
                        }
                        //  事件注册
                        webGsm.UpDataReceived += CProtocolEventManager.UpDataReceived;
                        //gsm.DownDataReceived += CProtocolEventManager.DownDataReceived;
                        //gsm.BatchDataReceived += CProtocolEventManager.BatchDataReceived;
                        //gsm.ErrorReceived += CProtocolEventManager.ErrorForUIReceived;
                        webGsm.MessageSendCompleted += CProtocolEventManager.MessageSendCompleted;
                        //gsm.SerialPortStateChanged += CProtocolEventManager.SerialPortStateChanged;
                        webGsm.SoilDataReceived += CProtocolEventManager.SoilDataReceived;

                        bool isStarted = webGsm.Init(web.IP,web.PortNumber,web.Account,web.Password);
                        if (!isStarted)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----通讯方式配置出错!检查Web服务设置！", gsmName));
                            continue;
                        }

                        //  加载数据协议信息
                        var dataDllInfo = XmlDocManager.Instance.GetDataDllByWeb(web.ProtocolData, true);
                        if (dataDllInfo == null)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----数据协议配置出错!", gsmName));
                            continue;
                        }
                        var iup = ProtocolManager.Up(dataDllInfo);
                        var idown = ProtocolManager.Down(dataDllInfo);
                        var iudisk = ProtocolManager.UDisk(dataDllInfo);
                        var iflash = ProtocolManager.Flash(dataDllInfo);
                        var iSoil = ProtocolManager.Soil(dataDllInfo);
                        webGsm.InitInterface(iup, idown, iudisk, iflash, iSoil);

                        var stations = CDBDataMgr.Instance.GetAllStation();
                        webGsm.InitStations(stations);

                        m_webGsmLists.Add(webGsm);
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......完成!", gsmName));
                    }
                    catch (Exception ex)
                    {
                        // 打开服务失败
                        Debug.WriteLine(ex.ToString());
                    }

                }
            }
        }

        public void CloseWebGsm()
        {
            try
            {
                if (m_webGsmLists != null)
                {
                    foreach (IWebGsm gsm in m_webGsmLists)
                    {
                        gsm.Close();
                    }
                    m_webGsmLists.Clear();
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }

        public void RealodWebGsm()
        {
            try
            {
                if (m_webGsmLists != null)
                {
                    foreach (IWebGsm gsm in m_webGsmLists)
                    {
                        gsm.Close();
                    }
                    m_webGsmLists.Clear();
                }
                InitWebGsm();
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        public void StopWebGsmLists()
        {
            if (m_webGsmLists != null)
            {
                foreach (IWebGsm gsm in m_webGsmLists)
                {
                    try
                    {
                        gsm.Close();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
        }
        #endregion

        #region Beidou普通终端模块数据维护
        private List<IBeidouNormal> m_beidouNormalLists;
        public IBeidouNormal FindBeidouNormal(string comPort)
        {
            if (m_beidouNormalLists == null)
                return null;
            foreach (var item in this.m_beidouNormalLists)
            {
                if (item.Port.PortName == comPort)
                    return item;
            }
            return null;
        }
        public void InitBeidouNormal()
        {
            //  初始化系统中Beidou列表
            if (this.m_beidouNormalLists != null)
            {
                StopBeidouNormal();
            }
            this.m_beidouNormalLists = new List<IBeidouNormal>();

            //  配置文件中的北斗卫星普通终端名称信息
            var beidouNormalNameLists = XmlDocManager.Instance.BeidouNormalProtocolNames;

            //  系统中存在的串口
            var sysComs = new List<string>(System.IO.Ports.SerialPort.GetPortNames());

            foreach (var beidouName in beidouNormalNameLists)
            {
                CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......", beidouName));
                var dll = XmlDocManager.Instance.GetInfoByName(beidouName);
                if (dll == null)
                    continue;

                foreach (var comNumber in dll.Coms)
                {

                    //  如果系统中不包含串口
                    //      获取下一条串口配置信息
                    string comPortName = string.Format("COM{0}", comNumber);
                    if (!sysComs.Contains(comPortName))
                    {
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!----计算机系统不包含串口{1}!", beidouName, comPortName));
                        continue;
                    }
                    //  如果数据库配置中不包含串口配置信息
                    //      获取下一条串口配置信息
                    var portInfo = CDBDataMgr.Instance.GetSerialPortByPortNumber(comNumber);
                    if (portInfo == null)
                    {
                        Debug.WriteLine(string.Format("COM {0} is not the record in database!", comNumber));
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!----数据库中不包含串口{1}信息!", beidouName, comPortName));
                        continue;
                    }
                    // 判断配置是否启动
                    if (portInfo.SwitchSatus.HasValue && portInfo.SwitchSatus.Value == false)
                    {
                        // 配置不打开串口
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置不启用串口{0}", comPortName));
                        continue;
                    }

                    try
                    {
                        //  实例化北斗卫星普通终端实例
                        var beidou = ProtocolManager.BeidouNormal(dll);
                        if (beidou == null)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!----通讯方式配置出错!", beidouName));
                            continue;
                        }
                        //  事件注册
                        beidou.TSTACompleted += CProtocolEventManager.BeidouTSTACompleted;
                        beidou.COUTCompleted += CProtocolEventManager.BeidouCOUTCompleted;
                        beidou.BeidouErrorReceived += CProtocolEventManager.BeidouErrorReceived;
                        beidou.MessageSendCompleted += CProtocolEventManager.MessageSendCompleted;
                        beidou.MessageSendCompleted += CBeidouNewMgrForm.MessageSendCompleted1;
                        ///   beidou.MessageSendCompleted1 += CProtocolEventManager.MessageSendCompleted1;
                        beidou.ErrorReceived += CProtocolEventManager.ErrorForUIReceived;
                        beidou.DownDataReceived += CProtocolEventManager.DownDataReceived;
                        beidou.UpDataReceived += CProtocolEventManager.UpDataReceived;
                        beidou.BatchDataReceived += CProtocolEventManager.BatchDataReceived;
                        beidou.SerialPortStateChanged += CProtocolEventManager.SerialPortStateChanged;
                        beidou.SoilDataReceived += CProtocolEventManager.SoilDataReceived;
                        //  初始化
                        beidou.Init(comPortName, portInfo.Baudrate);

                        var dataDllInfo = XmlDocManager.Instance.GetDataDllByComOrPort(comNumber, true);
                        if (dataDllInfo == null)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!----数据协议配置出错!", beidouName));
                            continue;
                        }
                        var iup = ProtocolManager.Up(dataDllInfo);
                        var idown = ProtocolManager.Down(dataDllInfo);
                        var iudisk = ProtocolManager.UDisk(dataDllInfo);
                        var iflash = ProtocolManager.Flash(dataDllInfo);
                        var isoil = ProtocolManager.Soil(dataDllInfo);
                        beidou.InitInterface(iup, idown, iudisk, iflash, isoil);

                        var stations = CDBDataMgr.Instance.GetAllStation();
                        beidou.InitStations(stations);

                        if (!beidou.Open())
                        {
                            MessageBox.Show("串口" + comPortName + "打开失败！");
                            continue;
                        }

                        //  查询信息
                        beidou.SendQSTA();

                        this.m_beidouNormalLists.Add(beidou);
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......完成!", beidouName));
                    }
                    catch (Exception ex)
                    {
                        // 打开串口失败
                        Debug.WriteLine(ex.ToString());
                    }

                }
            }
        }
        public void StartBeidouNormal(string portName, int baudrate)
        {
            try
            {
                var beidou = FindBeidouNormal(portName);
                if (beidou != null && !beidou.Port.IsOpen)
                {
                    beidou.Open();
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        public void StopBeidouNormal(string comPort)
        {
            try
            {
                var beidou = FindBeidouNormal(comPort);
                if (beidou != null)
                {
                    beidou.Close();
                    CProtocolEventManager.SerialPortStateChanged(this, new CEventSingleArgs<CSerialPortState>(new CSerialPortState()
                    {
                        BNormal = false,
                        PortType = EListeningProtType.SerialPort,
                        PortNumber = int.Parse(comPort.Replace("COM", ""))
                    }));
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        public void StopBeidouNormal()
        {
            if (this.m_beidouNormalLists == null)
                return;
            foreach (var beidou in this.m_beidouNormalLists)
            {
                try
                {
                    beidou.TSTACompleted -= CProtocolEventManager.BeidouTSTACompleted;
                    beidou.COUTCompleted -= CProtocolEventManager.BeidouCOUTCompleted;
                    beidou.BeidouErrorReceived -= CProtocolEventManager.BeidouErrorReceived;
                    beidou.MessageSendCompleted -= CProtocolEventManager.MessageSendCompleted;
                    beidou.SoilDataReceived -= CProtocolEventManager.SoilDataReceived;
                    beidou.ErrorReceived -= CProtocolEventManager.ErrorForUIReceived;
                    beidou.DownDataReceived -= CProtocolEventManager.DownDataReceived;
                    beidou.UpDataReceived -= CProtocolEventManager.UpDataReceived;
                    beidou.BatchDataReceived -= CProtocolEventManager.BatchDataReceived;
                    beidou.SerialPortStateChanged -= CProtocolEventManager.SerialPortStateChanged;

                    beidou.Close();
                }
                catch (Exception exp) { Debug.WriteLine(exp.Message); }
            }
        }

        public void SendBeidouQSTA(string comPort)
        {
            try
            {
                var beidou = FindBeidouNormal(comPort);
                if (beidou != null && beidou.Port.IsOpen)
                    beidou.SendQSTA();
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        public void SendBeidouSTST(string comPort, CSTSTStruct param)
        {
            try
            {
                var beidou = FindBeidouNormal(comPort);
                if (beidou != null && beidou.Port.IsOpen)
                    beidou.SendSTST(param);
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }

        //  public void SendBeidouTTCA(string comPort, CTTCAStruct param)
        public String SendBeidouTTCA(string comPort, CTTCAStruct param)
        {
            String str = "";
            try
            {
                var beidou = FindBeidouNormal(comPort);
                if (beidou != null && beidou.Port.IsOpen)
                    str = beidou.SendTTCA(param);
                return str;
                // beidou.SendBackTTCA();
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); return ""; }

        }

        public String SendBackTTCA(string comPort)
        {
            try
            {
                var beidou = FindBeidouNormal(comPort);
                if (beidou != null && beidou.Port.IsOpen)
                {
                    return beidou.SendBackTTCA();
                }
                else
                {
                    return "";
                }
                //  gSendBackTTCAString;
                //  return "";
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); return ""; }
        }

        public void SendBeidouTAPP(string comPort)
        {
            try
            {
                var beidou = FindBeidouNormal(comPort);
                if (beidou != null && beidou.Port.IsOpen)
                    beidou.SendTAPP();
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        #endregion

        #region 北斗卫星（指挥机）模块数据维护
        private List<IBeidou500> m_beidou500Lists;
        public IBeidou500 FindBeidou500(string comPort)
        {
            if (null == m_beidou500Lists)
                return null;

            foreach (var item in m_beidou500Lists)
            {
                if (item.Port.PortName == comPort)
                    return item;
            }
            return null;
        }
        public IBeidou500 FindBeidou500()
        {
            if (null == m_beidou500Lists || 0 == m_beidou500Lists.Count)
                return null;
            return m_beidou500Lists[0];
        }

        public void InitBeidou500()
        {
            //  初始化系统中Beidou列表
            if (this.m_beidou500Lists != null)
            {
                StopBeidou500();
            }
            this.m_beidou500Lists = new List<IBeidou500>();

            //  配置文件中的北斗卫星（指挥机）名称信息
            var beidou500NameLists = XmlDocManager.Instance.Beidou500ProtocolNames;

            //  系统中存在的串口
            var sysComs = new List<string>(System.IO.Ports.SerialPort.GetPortNames());

            foreach (var beidouName in beidou500NameLists)
            {
                CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......", beidouName));
                var dll = XmlDocManager.Instance.GetInfoByName(beidouName);
                if (dll == null)
                    continue;

                foreach (var comNumber in dll.Coms)
                {

                    //  如果系统中不包含串口
                    //      获取下一条串口配置信息
                    string comPortName = string.Format("COM{0}", comNumber);
                    if (!sysComs.Contains(comPortName))
                    {
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!----计算机系统不包含串口{1}!", beidouName, comPortName));
                        continue;
                    }
                    //  如果数据库配置中不包含串口配置信息
                    //      获取下一条串口配置信息
                    var portInfo = CDBDataMgr.Instance.GetSerialPortByPortNumber(comNumber);
                    if (portInfo == null)
                    {
                        Debug.WriteLine(string.Format("COM {0} is not the record in database!", comNumber));
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!----数据库中不包含串口{1}信息!", beidouName, comPortName));
                        continue;
                    }
                    // 判断配置是否启动
                    if (portInfo.SwitchSatus.HasValue && portInfo.SwitchSatus.Value == false)
                    {
                        // 配置不打开串口
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置不启用串口{0}", comPortName));
                        continue;
                    }

                    try
                    {
                        //  实例化北斗卫星普通终端实例
                        var beidou = ProtocolManager.Beidou500(dll);
                        if (beidou == null)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!----通讯方式配置出错!", beidouName));
                            continue;
                        }
                        //  事件注册
                        beidou.MessageSendCompleted += CProtocolEventManager.MessageSendCompleted;
                        beidou.MessageSendCompleted += CBeidou500MgrForm.MessageSendCompleted1;
                        beidou.UpDataReceived += CProtocolEventManager.UpDataReceived;
                        beidou.SerialPortStateChanged += CProtocolEventManager.SerialPortStateChanged;
                        beidou.SoilDataReceived += CProtocolEventManager.SoilDataReceived;

                        beidou.Beidou500BJXXReceived += CProtocolEventManager.Beidou500BJXXReceived;
                        beidou.Beidou500SJXXReceived += CProtocolEventManager.Beidou500SJXXReceived;
                        beidou.Beidou500ZTXXReceived += CProtocolEventManager.Beidou500ZTXXReceived;

                        //  初始化
                        beidou.Init(comPortName, portInfo.Baudrate);

                        var dataDllInfo = XmlDocManager.Instance.GetDataDllByComOrPort(comNumber,true);
                        if (dataDllInfo == null)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!----数据协议配置出错!", beidouName));
                            continue;
                        }
                        var iup = ProtocolManager.Up(dataDllInfo);
                        var idown = ProtocolManager.Down(dataDllInfo);
                        var iudisk = ProtocolManager.UDisk(dataDllInfo);
                        var iflash = ProtocolManager.Flash(dataDllInfo);
                        var iSoil = ProtocolManager.Soil(dataDllInfo);
                        beidou.InitInterface(iup, idown, iudisk, iflash, iSoil);

                        var stations = CDBDataMgr.Instance.GetAllStation();
                        beidou.InitStations(stations);
                        if (!beidou.Open())
                        {
                            MessageBox.Show("串口" + comPortName + "打开失败！");
                            continue;
                        }
                        //beidou.Open();
                        //  查询信息
                        beidou.Query();

                        this.m_beidou500Lists.Add(beidou);
                        CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......完成!", beidouName));
                    }
                    catch (Exception ex)
                    {
                        // 打开串口失败
                        Debug.WriteLine(ex.ToString());
                    }

                }
            }
        }
        public void StartBeidou500(string portName, int baudrate)
        {
            try
            {
                var beidou = FindBeidou500(portName);
                if (beidou != null && !beidou.Port.IsOpen)
                {
                    beidou.Open();
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        public void StopBeidou500(string comPort)
        {
            try
            {
                var beidou = FindBeidou500(comPort);
                if (beidou != null)
                {
                    beidou.Close();
                    CProtocolEventManager.SerialPortStateChanged(this, new CEventSingleArgs<CSerialPortState>(new CSerialPortState()
                    {
                        BNormal = false,
                        PortType = EListeningProtType.SerialPort,
                        PortNumber = int.Parse(comPort.Replace("COM", ""))
                    }));
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        public void StopBeidou500()
        {
            if (null == this.m_beidou500Lists)
                return;
            foreach (var beidou in m_beidou500Lists)
            {
                try
                {
                    //  事件解除绑定
                    beidou.MessageSendCompleted -= CProtocolEventManager.MessageSendCompleted;
                    beidou.UpDataReceived -= CProtocolEventManager.UpDataReceived;
                    beidou.SerialPortStateChanged -= CProtocolEventManager.SerialPortStateChanged;
                    beidou.SoilDataReceived -= CProtocolEventManager.SoilDataReceived;
                    beidou.Beidou500BJXXReceived -= CProtocolEventManager.Beidou500BJXXReceived;
                    beidou.Beidou500SJXXReceived -= CProtocolEventManager.Beidou500SJXXReceived;
                    beidou.Beidou500ZTXXReceived -= CProtocolEventManager.Beidou500ZTXXReceived;
                    //  关闭北斗卫星
                    beidou.Close();
                }
                catch (Exception exp) { Debug.WriteLine(exp.Message); }
            }
        }

        /// <summary>
        /// 向指挥机发送查询信息
        /// </summary>
        /// <param name="comPort"></param>
        public void SendBeidou500Query(string comPort)
        {
            try
            {
                var beidou = FindBeidou500(comPort);
                if (null != beidou && beidou.Port.IsOpen)
                {
                    beidou.Query();
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }

        public String SendBeidou500TTCA(string comPort, CBeiDouTTCA param)
        {
            String str = "";
            try
            {
                var beidou = FindBeidou500(comPort);
                if (beidou != null && beidou.Port.IsOpen)
                    str = beidou.Send500TTCA(param);
                return str;
                // beidou.SendBackTTCA();
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); return ""; }

        }


        public void SendBeidou500CXSJ(string comPort)
        {
            try
            {
                var beidou = FindBeidou500(comPort);
                if (beidou != null && beidou.Port.IsOpen)
                    beidou.Send500CXSJ();
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        #endregion

        #region 透明传输模块数据维护
        private List<ITransparen> m_TransparenLists;
        public List<ITransparen> TransparenLists { get { return this.m_TransparenLists; } }
        /// <summary>
        /// 开启透明传输模式
        /// </summary>
        /// <param name="bIsSysLoad"></param>
        public void StartTransparen(bool bIsSysLoad = true)
        {
            //  初始化系统中GSM列表
            if (this.m_TransparenLists != null)
            {
                StopTransparenLists();
            }
            this.m_TransparenLists = new List<ITransparen>();

            var transparenNamesLists = XmlDocManager.Instance.NoneProtocolNames;

            foreach (var gname in transparenNamesLists)
            {
                CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......", gname));
                var dll = XmlDocManager.Instance.GetInfoByName(gname);
                if (dll == null)
                    continue;
                //  配置文件中的每个端口监听一个GPRS
                foreach (var port in dll.Ports)
                {
                    try
                    {
                        //  默认不启动手动加载的GPRS协议
                        if (!bIsSysLoad)
                        {
                            // 不是系统启动，也就是中途更改，只要是启动状态的都启动，否则都不启动
                            if (!port.BStartOrNot)
                            {
                                // 并且不启动，手动配置不启动
                                CSystemInfoMgr.Instance.AddInfo(string.Format("默认不启用协议\"{0}\"监听端口{1}", gname, port.PortNumber));
                                continue;
                            }
                            else
                            {
                                //手动配置，启动，并且是界面通知
                            }
                        }
                        else
                        {
                            // 是系统启动，跳过启动手动启动
                            if (!port.BAutoStart)
                            {
                                continue; //不是系统启动的，就不用启动了
                            }
                        }

                        ITransparen itransoaren = ProtocolManager.Transparen(dll);
                        transparen = itransoaren;
                        if (itransoaren == null)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----通讯方式配置出错!", gname));
                            continue;
                        }
                        var dataInfoDll = XmlDocManager.Instance.GetDataDllByComOrPort(port.PortNumber, false);
                        if (dataInfoDll == null)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----数据协议配置出错!", gname));
                            continue;
                        }
                        //  事件注册
                        itransoaren.UpDataReceived += CProtocolEventManager.UpDataReceived;
                        itransoaren.DownDataReceived += CProtocolEventManager.DownDataReceived;
                        itransoaren.BatchDataReceived += CProtocolEventManager.BatchDataReceived;
                        itransoaren.ErrorReceived += CProtocolEventManager.ErrorForUIReceived;
                        itransoaren.MessageSendCompleted += CProtocolEventManager.MessageSendCompleted;
                        itransoaren.ModemInfoDataReceived += CProtocolEventManager.TRANS_ModemInfoDataReceived;
                        //itransoaren.SerialPortStateChanged += CProtocolEventManager.SerialPortStateChanged;
                        //itransoaren.SoilDataReceived += CProtocolEventManager.SoilDataReceived;
                        itransoaren.Init();
                        var iup = ProtocolManager.Up(dataInfoDll);
                        var idown = ProtocolManager.Down(dataInfoDll);
                        var iudisk = ProtocolManager.UDisk(dataInfoDll);
                        var iflash = ProtocolManager.Flash(dataInfoDll);
                        var iSoil = ProtocolManager.Soil(dataInfoDll);
                        itransoaren.InitInterface(iup, idown, iudisk, iflash, iSoil);
                        bool isStarted = itransoaren.DSStartService((ushort)port.PortNumber);
                        if (isStarted)
                        {
                            m_TransparenLists.Add(itransoaren);
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......完成!", gname));
                        }
                        if (!isStarted)
                        {
                            CSystemInfoMgr.Instance.AddInfo(string.Format("配置\"{0}\"......失败!-----通讯方式配置出错!检查Web服务设置！", gname));
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        // 打开服务失败
                        Debug.WriteLine(ex.ToString());
                    }

                }
            }
        }
        /// <summary>
        /// 关闭透明传输模式
        /// </summary>
        public void StopTransparenLists()
        {
            if (m_TransparenLists != null)
            {
                foreach (ITransparen transparen in m_TransparenLists)
                {
                    try
                    {
                        transparen.DSStopService();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
        }
        public bool SendData(uint stationId,string message)
        {
            bool flag = true;
            var transparen = FindTransparenByUserid(stationId);
            //TODO
            if (transparen != null)
            {
                transparen.SendData(stationId, message);
            }
            return flag;
        }
        /// <summary>
        /// 发送TCP读取命令
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="stationID"></param>
        /// <param name="cmds"></param>
        /// <returns></returns>
        public String SendTcpRead(string userid, string stationID, IList<EDownParamGY> cmds)
        {
            string query = string.Empty;
            var tcp = FindTcpByUserid(stationID);
            if (tcp != null)
            {
                //uint dtuID = 0;
                if (tcp.getSessionIdbyStationid(stationID) != "0")
                {
                    //TODO
                    query = tcp.Down.BuildQuery(stationID, cmds, EChannelType.TCP);
                    tcp.SendData(stationID, query);


                }
                //1109
                else
                {
                    MessageBox.Show("站点" + stationID + "当前不在线！");
                }
            }
            else
            {
                MessageBox.Show("站点" + stationID + "当前不在线！");
            }
            return query;
        }

        /// <summary>
        /// TCP发送flash批量传输
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="stationID"></param>
        /// <param name="stype"></param>
        /// <param name="trans"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public String SendTcpFlash(string userid, string stationID, EStationType stype, ETrans trans, DateTime beginTime, DateTime endTime)
        {
            string query = string.Empty;
            var tcp = FindTcpByUserid(stationID);
            if (tcp != null)
            {
                //uint dtuID = 0;
                if (tcp.getSessionIdbyStationid(stationID) != "0")
                {
                    //TODO
                    query = tcp.Down.BuildQuery_Flash(stationID, stype, trans, beginTime, endTime, EChannelType.TCP);
                    query = "\u0001001122334400123438802A\u000200001904241608081904231519042415 DRN05 DRP\u0005A9B1";
                    tcp.SendData(stationID, query);
                }
                //1109
                else
                {
                    MessageBox.Show("站点" + stationID + "当前不在线！");
                }
            }
            else
            {
                MessageBox.Show("站点" + stationID + "当前不在线！");
            }
            return query;
        }
        /// <summary>
        /// 根据stationid获取TCP session
        /// </summary>
        /// <param name="stationId"></param>
        /// <returns></returns>
        public ITransparen FindTcpByUserid(string stationId)
        {
            ITransparen tcp = null;
            foreach (var item in this.m_TransparenLists)
            {

                if (item.getSessionIdbyStationid(stationId) != "0")
                {
                    tcp = item;
                    break;
                }
            }
            return tcp;
        }
        private ITransparen FindTransparenByUserid(uint uid)
        {
            ITransparen transparen = null;
            //TODO
            return transparen;
        }

        /// <summary>
        /// TCP发送批量传输
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="stationID"></param>
        /// <param name="stype"></param>
        /// <param name="trans"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        
        #endregion

        /// <summary>
        /// 关闭方法
        /// </summary>
        public void CloseAll()
        {
            try
            {
                //  关闭GPRS
                if (this.m_gprsLists != null)
                {
                    foreach (var gprs in this.m_gprsLists)
                    {
                        gprs.Close();
                    }
                }
                //  关闭GSM
                if (this.m_gsmLists != null)
                {
                    foreach (IGsm gsm in m_gsmLists)
                    {
                        gsm.Close();
                    }
                }
                //  关闭北斗卫星（普通终端）
                if (this.m_beidouNormalLists != null)
                {
                    foreach (var beidou in m_beidouNormalLists)
                    {
                        beidou.Close();
                    }
                }
                //  关闭北斗卫星（指挥机）
                if (null != this.m_beidou500Lists)
                {
                    foreach (var beidou in m_beidou500Lists)
                    {
                        beidou.Close();
                    }
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
    }

    // private delegate void ThreadCallBackDelegate(string msg);
    internal class CProtocolEventManager
    {
        public static int num = 0;
        public static int num1 = 0;
        // //定义一个需要string类型参数的委托
        //public delegate void MyDelegate2(String str);
        //定义该委托的事件
        public event Hydrology.Forms.CBeidouNewMgrForm.MyDelegate2 AddStationEvent2;
        private static CProtocolEventManager m_sInstance;   //实例指针
        public static List<ModemInfoStruct> m_dtuList;
        static Thread thread_1;  //静态变量，用来获取主线程
        //private AutoResetEvent fd_thr_supend = new AutoResetEvent(false);
        //  public ThreadCallBackDelegate callBack;

        //
        //定义一个需要string类型参数的委托
        public delegate void MyDelegate3(string sid);
        //定义该委托的事件
        public static event MyDelegate3 AddTRUEvent;

        private CProtocolEventManager()
        {
            m_dtuList = new List<ModemInfoStruct>();
        }


        public static CProtocolEventManager Instance
        {
            get { return GetInstance(); }
        }
        public static CProtocolEventManager GetInstance()
        {
            if (m_sInstance == null)
            {
                m_sInstance = new CProtocolEventManager();
            }
            return m_sInstance;
        }

        public List<ModemInfoStruct> GetOnlineStatusList()
        {
            return m_dtuList;
        }

        public static String str;

        public static void MessageSendCompleted(object sender, SendOrRecvMsgEventArgs e)
        {
            // 写入日志文件
            CSystemInfoMgr.Instance.AddInfo(String.Format("[{0}] {1}    : {2}", e.ChannelType, e.Description.PadRight(10), e.Msg));
            if (e.Msg.Contains("COUT"))
            {
                //CBeidouNewMgrForm fm = new CBeidouNewMgrForm();
                //fm.textBox_Receive.Text = "";
                //fm.textBox_Receive.Text=e.Msg.ToString();
                // var CPro = new CProtocolEventManager();
                // CPro.AddStationEvent2(e.Msg.ToString());
                // CProtocolEventManager.Instance.AddStationEvent2(e.Msg.ToString());
                str = e.Msg.ToString();
                CBeidouNewMgrForm.returnStr(str);
                //  CBeidouNewMgrForm.MyDelegate2.textBox_Receive
            }
            if (e.Msg.Contains("加报") || e.Msg.Contains("定时报"))
            {
                //int id = Thread.CurrentThread.ManagedThreadId;
                //Thread nonParameterThread = new Thread(new ThreadStart(NonParameterRun));
                //nonParameterThread.Start();
                //CEntityStation entity = new CEntityStation();
                //entity.StationID = "6018";
                //entity.GPRS = "60006018";
                //CPortDataMgr.Instance.SendHex(entity);

                // int threadId = Thread.CurrentThread.ManagedThreadId;
                // f2.Text = threadId.ToString();

                // thread_1 = Thread.CurrentThread;//获取主线程
                // //Thread.Sleep(3000);
                // //this.fd_thr_supend.Reset();
                //// MainForm.Mainthread.Resume();
                // Console.WriteLine("主线程：" + MainForm.Mainthread.ThreadState + "主线程id: " + MainForm.Mainthread.ManagedThreadId);
                // Console.WriteLine("主线程优先级：" + MainForm.Mainthread.Priority);
                // Console.WriteLine("当前线程：" + thread_1.ThreadState + "当前线程id: " + thread_1.ManagedThreadId);
                // Console.WriteLine("当前线程优先级：" + thread_1.Priority);
                // MainForm.Mainthread.Priority = ThreadPriority.Highest;
                // thread_1.Abort();
                //Invoke(new Form1());

                string str = e.Msg;
                //string[] arrstr = str.Split(' ');
                int strIndex = str.IndexOf("$");

                string stationid = str.Substring(strIndex + 1, 4);


                // string type = msg.Substring(4, 2);
                // int a1 = 0;
                //CProtocolEventManager.AddTRUEvent(stationid);
                //Form1 f1 = new Form1();
                //f1.ShowDialog();
                // thread2.Start();  
                //bool flag1 = false;
                //System.Threading.Mutex mutex = new System.Threading.Mutex(true, "Test", out flag1);
                //if (!flag1)
                //{
                //    MessageBox.Show("只能运行一个客户端程序！", "请确定", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //    Environment.Exit(1);//退出程序  
                //}  
                //Form1 f2 = new Form1();

                //f2.Close();
                // f1.Show();
                //f2.Hide();//隐藏当前窗口
                // f1.SendTru(sender, e);
                // f1.For
                //Form1.SendTru();
                //Form1.SendTru();
                //   Form1.button1_Click(null, null);
            }
        }
        //public static void NonParameterRun()
        //{
        //    int id = Thread.CurrentThread.ManagedThreadId;

        //    CEntityStation entity = new CEntityStation();
        //    entity.StationID = "6018";
        //    entity.GPRS = "60006018";
        //    CPortDataMgr.Instance.SendHex(entity);
        //}


        //public static void MessageSendCompleted(object sender, SendOrRecvMsgEventArgs1 e)
        //{
        //    // 写入日志文件
        //    CSystemInfoMgr.Instance.AddInfo(String.Format("[{0}] {1}    : {2}", e.ChannelType, e.Description.PadRight(10), e.Msg));
        //}

        public static void BeidouCOUTCompleted(object sender, COUTEventArgs e)
        {
            var cout = e.BeidouStatus;
            if (cout != null && TSTA4UI != null)
            {
                COUT4UI(sender, e);
            }
        }
        public static void BeidouTSTACompleted(object sender, TSTAEventArgs e)
        {
            CTSTAStruct tsta = e.TSTAInfo;
            if (tsta != null && TSTA4UI != null)
            {
                TSTA4UI.Invoke(sender, e);
            }
        }
        public static void BeidouErrorReceived(object sender, ReceiveErrorEventArgs e)
        {
            if (BeidouErrorForUI != null)
            {
                BeidouErrorForUI(sender, e);
            }
        }
        /// <summary>
        /// 批量传输事件处理程序
        /// 包含UDisk和Flash数据
        /// </summary>
        public static void BatchDataReceived(object sender, BatchEventArgs e)
        {
            CBatchStruct batch = e.Value;
            if (batch != null && BatchForUI != null)
            {
                BatchForUI.Invoke(sender, e);
            }
            if (batch != null && BatchForData != null)
            {
                BatchForData.Invoke(sender, e);
            }
        }
        /// <summary>
        /// 远地下行指令事件处理程序
        /// </summary>
        public static void DownDataReceived(object sender, DownEventArgs e)
        {
            #region 原来的处理
            //CDownConf down = e.Value;
            //if (down != null && DownForUI != null)
            //{
            //    DownForUI.Invoke(sender, e);
            //}
            //if (down != null && DownForData != null)
            //{
            //    DownForData.Invoke(sender, e);
            //}
            #endregion
            CReportStruct rpValue = e.rpValue;
            if(rpValue != null && DownForUI != null)
            {
                DownForUI.Invoke(sender, e);
            }
            if (rpValue != null && DownForData != null)
            {
                DownForData.Invoke(sender, e);
            }
        }
        /// <summary>
        /// 上行指令事件处理程序
        /// 将解析后的数据写入实时数据表，并保存到数据库中
        /// </summary>
        public static void UpDataReceived(object sender, UpEventArgs e)
        {
            CReportStruct report = e.Value;
            string dbId = string.Empty;
            if (report.Stationid.Length == 8)
            {
                dbId = CDBDataMgr.Instance.QueryStationNameByUserID(report.Stationid);
                report.Stationid = dbId;
            }
            else
            {
                dbId = CDBDataMgr.Instance.GetComFlagById(report.Stationid, report.ChannelType);
                if (dbId != report.flagId)
                {
                    if (report.ChannelType == EChannelType.GPRS || report.ChannelType == EChannelType.GSM || report.ChannelType == EChannelType.Beidou500)
                    {
                        CSystemInfoMgr.Instance.AddInfo("接收Beidou卫星终端号" + report.flagId.ToString() + "数据库存储卫星终端号" + dbId + "不对应！");
                        return;
                    }
                }
            }
            //gm 0331
            string str = e.RawData;
            #region 1G21
            if (str.Contains("1G21") || str.Contains("1G22"))
            {
                CPortDataMgr.Instance.dataNum = CPortDataMgr.Instance.dataNum + 1;
            }
            #endregion

            #region 1G2111
            if (str.Contains("1G2111"))
            {
                var stationDatas = new CEventRecvStationDatasArgs()
                {
                    StrStationID = report.Stationid,
                    EStationType = EStationType.ERiverWater,
                    EMessageType = EMessageType.Manual,
                    //EMessageType = EMessageType.Batch,
                    RecvDataTime = report.RecvTime,
                    EChannelType = report.ChannelType,
                    StrSerialPort = report.ListenPort
                };
                string[] lists = str.Substring(15).Split(CSpecialChars.BALNK_CHAR);
                //stationDatas.Datas
                try
                {
                    int hour = int.Parse(str.Substring(11, 2));
                    int minute = int.Parse(str.Substring(13, 2));
                    DateTime collect = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, 0);

                    foreach (var item in lists)
                    {
                        stationDatas.Datas.Add(new CSingleStationData()
                        {

                            WaterStage = decimal.Parse(item.Substring(0, 6)) / 100,
                            DataTime = collect
                        });
                    }
                }
                catch (Exception e8)
                {

                }
                CDBDataMgr.Instance.EHRecvStationTSDatas(null, stationDatas);
                return;
            }
            #endregion

            #region 1G23
            if (str.Contains("1G23"))
            {
                var stationDatas = new CEventRecvStationDatasArgs()
                {
                    StrStationID = report.Stationid,
                    EStationType = EStationType.ERiverWater,
                    EMessageType = EMessageType.Manual,
                    //EMessageType = EMessageType.Batch,
                    RecvDataTime = report.RecvTime,
                    EChannelType = report.ChannelType,
                    StrSerialPort = report.ListenPort
                };
                //string hour = str.Substring(11, 2);
                //string minute = str.Substring(13, 2);
                //DateTime collect = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, 0);
                string[] lists = str.Substring(15).Split(CSpecialChars.BALNK_CHAR);
                //stationDatas.Datas
                try
                {
                    int day = int.Parse(str.Substring(9, 2));
                    int hour = int.Parse(str.Substring(11, 2));
                    int minute = int.Parse(str.Substring(13, 2));

                    DateTime collect = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, 0);

                    foreach (var item in lists)
                    {
                        int shuifen = int.Parse(str.Substring(21, 1));
                        int acc = int.Parse(str.Substring(22, 1));
                        int num = int.Parse(str.Substring(23, 3));
                        int method = int.Parse(str.Substring(26, 1));

                        stationDatas.Datas.Add(new CSingleStationData()
                        {

                            WaterStage = decimal.Parse(item.Substring(0, 6)) / 100,

                            DataTime = collect
                        });
                    }
                }
                catch (Exception e8)
                {

                }
                CDBDataMgr.Instance.EHRecvStationTSDatas(null, stationDatas);
                return;
            }
            #endregion

            #region +32 短信
            // 有待修改
            if (str.Contains("+32"))
            {
                if (str.Contains("+32"))
                {
                    num = num + 1;
                }
                FileStream fs = new FileStream("numgsm.txt", FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                //开始写入
                sw.Write(num);
                //清空缓冲区
                sw.Flush();
                //关闭流
                sw.Close();
                fs.Close();
            }
            #endregion

            #region COUT 北斗
            if (str.Contains("COUT"))
            {
                if (str.Contains("COUT"))
                {
                    num1 = num1 + 1;
                }
                FileStream fs = new FileStream("numbd.txt", FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                //开始写入
                sw.Write(num1);
                //清空缓冲区
                sw.Flush();
                //关闭流
                sw.Close();
                fs.Close();
            }
            #endregion

            #region TS 调试信息
            if (str.Contains("TS"))
            {
                var stationDatas = new CEventRecvStationDatasArgs()
                {
                    StrStationID = report.Stationid,
                    EStationType = report.StationType,
                    EMessageType = report.ReportType,
                    RecvDataTime = report.RecvTime,
                    EChannelType = report.ChannelType,
                    StrSerialPort = report.ListenPort
                };
                if (report.Datas.Count == 0)
                {
                    string rawStr = e.RawData;
                    report.Datas.Add(WrongParser(rawStr));
                }

                foreach (var item in report.Datas)
                {
                    stationDatas.Datas.Add(new CSingleStationData()
                    {
                        WaterStage = item.Water,
                        TotalRain = item.Rain,
                        Voltage = item.Voltge,
                        DataTime = item.Time
                    });
                }

                //  写入内存，写库
                CDBDataMgr.Instance.EHRecvStationTSDatas(null, stationDatas);

            }

            #endregion

            #region 其他报文
            else
            {
                if (report != null)
                {
                    var stationDatas = new CEventRecvStationDatasArgs()
                    {
                        StrStationID = report.Stationid,
                        EStationType = report.StationType,
                        EMessageType = report.ReportType,
                        RecvDataTime = report.RecvTime,
                        EChannelType = report.ChannelType,
                        StrSerialPort = report.ListenPort
                    };
                    if(stationDatas.EChannelType == EChannelType.GPRS)
                    {
                        CPortDataMgr.Instance.gprsNum = CPortDataMgr.Instance.gprsNum + 1;
                    }
                    if (report.Datas.Count == 0)
                    {
                        string rawStr = e.RawData;
                        report.Datas.Add(WrongParser(rawStr));
                    }

                    foreach (var item in report.Datas)
                    {
                        stationDatas.Datas.Add(new CSingleStationData()
                        {
                            WaterStage = item.Water,
                            TotalRain = item.Rain,
                            DiffRain = item.DiffRain,
                            PeriodRain = item.PeriodRain,
                            CurrentRain = item.CurrentRain,
                            Voltage = item.Voltge,
                            DataTime = item.Time
                        });
                    }

                    //  写入内存，写库
                    CDBDataMgr.Instance.EHRecvStationDatas(null, stationDatas);
                }
            }
            #endregion
        }

        public static CReportData WrongParser(string data)
        {
            CReportData report = new CReportData();
            //$6003 1G21 01 16 10 10 11 49 6;5535 0143 1378
            try
            {
                //  解析站点类别
                EStationType stationType = ProtocolHelpers.ProtoStr2StationType(data.Substring(9, 2));

                //  解析时间
                report.Time = new DateTime
                         (
                             year: Int32.Parse("20" + data.Substring(11, 2)), //  年
                             month: Int32.Parse(data.Substring(13, 2)),       //  月
                             day: Int32.Parse(data.Substring(15, 2)),         //  日
                             hour: Int32.Parse(data.Substring(17, 2)),        //  时
                             minute: Int32.Parse(data.Substring(19, 2)),      //  分
                             second: 0                                       //  秒
                         );
                if (Regex.IsMatch(data.Substring(31, 4), @"^[0-9]*[0-9][0-9][0-9]*$"))
                {
                    // 解析电压  2(整数位) + 2(小数位)  单位V
                    Decimal Voltge = Decimal.Parse(data.Substring(31, 4)) * (Decimal)0.01;
                    report.Voltge = Voltge;
                }
                else
                {
                    //report.Voltge = Decimal.Parse("1111") * (Decimal)0.01;
                    Debug.WriteLine("drop");
                    return null;

                }

                // cln:20141110修改，此处根据站点类型类判断是否需要解析相应的数据，避免因为非必要字段导致的异常信息
                //   解析水位  4(整数位) + 2(小数位)  单位m
                //Decimal water = Decimal.Parse(data.Substring(10, 6)) * (Decimal)0.01;
                ////  解析雨量                         单位mm，未乘以精度
                //Decimal rain = Decimal.Parse(data.Substring(16, 4));
                //  初始化雨量，水位，电压值
                //  雨量  包含雨量Rain
                //  水文  包含雨量Rain，水位Water
                //  水位  包含水位Water
                switch (stationType)
                {
                    case EStationType.ERainFall:
                        {
                            Console.WriteLine("..." + data.Substring(27, 4));
                            //  雨量
                            //  解析雨量                         单位mm，未乘以精度
                            if (Regex.IsMatch(data.Substring(27, 4), @"^[0-9]*[0-9][0-9][0-9]*$"))
                            {
                                Decimal rain = Decimal.Parse(data.Substring(27, 4));
                                report.Rain = rain;
                            }
                            else
                            {
                                //report.Rain = Decimal.Parse("1111");
                                Debug.WriteLine("drop");
                                return null;
                            }
                        }
                        break;
                    case EStationType.EHydrology:
                        {
                            //  水文
                            //  解析雨量                         单位mm，未乘以精度
                            if (Regex.IsMatch(data.Substring(27, 4), @"^[0-9]*[0-9][0-9][0-9]*$"))
                            {
                                Decimal rain = Decimal.Parse(data.Substring(27, 4));
                                report.Rain = rain;
                            }
                            else
                            {
                                //report.Rain = Decimal.Parse("1111");
                                Debug.WriteLine("drop");
                                return null;
                            }
                            //  解析水位  4(整数位) + 2(小数位)  单位m
                            if (Regex.IsMatch(data.Substring(21, 6), @"^[0-9]*[0-9][0-9][0-9][0-9][0-9]*$"))
                            {
                                Decimal water = Decimal.Parse(data.Substring(21, 6)) * (Decimal)0.01;
                                report.Water = water;
                            }
                            else
                            {
                                // report.Water = Decimal.Parse("111111") * (Decimal)0.01;
                                Debug.WriteLine("drop");
                                return null;
                            }
                        }
                        break;
                    case EStationType.ERiverWater:
                        {
                            //  水位
                            //  解析水位  4(整数位) + 2(小数位)  单位m
                            if (Regex.IsMatch(data.Substring(21, 6), @"^[0-9]*[0-9][0-9][0-9][0-9][0-9]*$"))
                            {
                                Decimal water = Decimal.Parse(data.Substring(21, 6)) * (Decimal)0.01;
                                report.Water = water;
                            }
                            else
                            {
                                //report.Water = Decimal.Parse("111111") * (Decimal)0.01;
                                Debug.WriteLine("drop");
                                return null;
                            }
                            break;
                        }
                    default: break;
                }
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.Message);
            }

            return report;

        }
        public static void ErrorForUIReceived(object sender, ReceiveErrorEventArgs e)
        {
            if (ErrorForUI != null)
            {
                ErrorForUI(null, e);
            }
            if (ErrorForData != null)
            {
                ErrorForData(null, e);
            }
            if (ErrorForData != null)
            {
                DownForTru(null, e);
            }
        }
        public static void SerialPortStateChanged(object sender, CEventSingleArgs<CSerialPortState> e)
        {
            if (SerialPortStateChanged4UI != null)
                SerialPortStateChanged4UI(sender, e);
        }

        public static void GPRS_ModemDataReceived(object sender, ModemDataEventArgs e)
        {
            if (ModemDataReceived != null)
            {
                ModemDataReceived(sender, e);
            }
        }
        
        public static void GPRS_ModemInfoDataReceived(object sender, EventArgs e)
        {
            //TODO
            var gprs = sender as IGprs;
            if (gprs != null)
            {
                m_dtuList = gprs.DTUList;
            }
            if (ModemInfoDataReceived != null)
            {
                    ModemInfoDataReceived(sender, e);
            }
        }
        public static void TRANS_ModemInfoDataReceived(object sender, EventArgs e)
        {
            var transparen = sender as ITransparen;
            if(transparen != null)
            {
                m_dtuList = transparen.getDTUList();
            }
            if (ModemInfoDataReceived1 != null)
            {
                ModemInfoDataReceived1(sender, e);
            }
        }

            
        public static event EventHandler<DownEventArgs> DownForUI;
        public static event EventHandler<DownEventArgs> DownForData;
        public static event EventHandler<BatchEventArgs> BatchForUI;
        public static event EventHandler<BatchEventArgs> BatchForData;
        public static event EventHandler<UpEventArgs> UpForUI;
        public static event EventHandler<ReceiveErrorEventArgs> ErrorForUI;
        public static event EventHandler<ReceiveErrorEventArgs> DownForTru;
        public static event EventHandler<ReceiveErrorEventArgs> ErrorForData;
        public static event EventHandler<COUTEventArgs> COUT4UI;
        public static event EventHandler<TSTAEventArgs> TSTA4UI;
        public static event EventHandler<ReceiveErrorEventArgs> BeidouErrorForUI;
        public static event EventHandler<CEventSingleArgs<CSerialPortState>> SerialPortStateChanged4UI;

        public static event EventHandler<ModemDataEventArgs> ModemDataReceived;
        public static event EventHandler ModemInfoDataReceived;
        public static event EventHandler ModemInfoDataReceived1;

        public static void Beidou500BJXXReceived(object sender, Beidou500BJXXEventArgs e)
        {
            if (null != Beidou500BJXXForUI)
            {
                Beidou500BJXXForUI(sender, e);
            }
        }
        public static void Beidou500SJXXReceived(object sender, Beidou500SJXXEventArgs e)
        {
            if (null != Beidou500SJXXForUI)
            {
                Beidou500SJXXForUI(sender, e);
            }
        }
        public static void Beidou500ZTXXReceived(object sender, Beidou500ZTXXEventArgs e)
        {
            if (null != Beidou500ZTXXForUI)
            {
                Beidou500ZTXXForUI(sender, e);
            }
        }

        public static event EventHandler<Beidou500BJXXEventArgs> Beidou500BJXXForUI;
        public static event EventHandler<Beidou500SJXXEventArgs> Beidou500SJXXForUI;
        public static event EventHandler<Beidou500ZTXXEventArgs> Beidou500ZTXXForUI;

        public static void GPRS_TimeOut(object sender, ReceivedTimeOutEventArgs e)
        {
            if (null != GPRS_TimeOut4UI)
            {
                GPRS_TimeOut4UI(sender, e);
            }
        }
        public static event EventHandler<ReceivedTimeOutEventArgs> GPRS_TimeOut4UI;
        public static void GSM_TimeOut(object sender, ReceivedTimeOutEventArgs e)
        {
            if (null != GSM_TimeOut4UI)
            {
                GSM_TimeOut4UI(sender, e);
            }
        }

        public static event EventHandler<ReceivedTimeOutEventArgs> GSM_TimeOut4UI;

        /// <summary>
        /// 墒情系统数据接收完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void SoilDataReceived(object sender, CEventSingleArgs<CEntitySoilData> e)
        {
            var soilData = e.Value;
            var station = CDBSoilDataMgr.Instance.GetStationInfoBySoilDataInfo(e.Value);
            if (null != soilData && null != station)
            {
                soilData.StationID = station.StationID;
            }

            CDBSoilDataMgr.Instance.EHRecvSoildData(null, new CSingleSoilDataArgs()
            {
                StrStationId = soilData.StationID,
                EMessageType = soilData.MessageType,
                EChannelType = soilData.ChannelType,
                Voltage = soilData.DVoltage,
                DataTime = soilData.DataTime,
                D10Value = soilData.Voltage10,
                D20Value = soilData.Voltage20,
                D30Value = soilData.Voltage30,
                D40Value = soilData.Voltage40,
                D60Value = soilData.Voltage60
            });

            // 发消息通知界面
            // 20180315辽宁修改，可能会导致重复
            CDBSoilDataMgr.Instance.RecvData(sender, new CEventSingleArgs<CEntitySoilData>(soilData));
        }

        public static void GPRS_OffLine(object sender, EventArgs e)
        {
            if (null != GPRS_OffLine4UI)
            {
                GPRS_OffLine4UI(sender, e);
            }
        }

        public static event EventHandler GPRS_OffLine4UI;

    }
}
