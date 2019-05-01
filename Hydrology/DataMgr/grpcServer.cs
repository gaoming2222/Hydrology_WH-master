using Grpc.Core;
using Grpcservices;
using Hydrology.Entity;
using Hydrology.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace Hydrology.DataMgr
{
    public class GrpcServer : gRPCServices.gRPCServicesBase
    {
        private string gprsNum;
        private string stationId;
        private string cmds;
        private string grpcIp;
        private int grpcPort;
        bool downdataGet = false;
        bool timeDown = false;
        bool timeDown_1 = false;

        private BatchList BatchList;
        private List<DownConf> downConfList;
        private List<string> truReturnList;
        private CDictionary<string, string> readDataDic;

        private System.Timers.Timer m_timer = new System.Timers.Timer()
        {
            Enabled = true,
            Interval = 15 * 1000
        };

        private System.Timers.Timer m_timer_1 = new System.Timers.Timer()
        {
            Enabled = true,
            Interval = 30 * 1000
        };

        private static GrpcServer instance;
        public static GrpcServer Instance
        {
            get
            {
                if (instance == null)
                    instance = new GrpcServer();
                return instance;
            }
        }

        #region 帮助方法
        public static List<T> Clone<T>(object List)
        {
            using (Stream objectStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(objectStream, List);
                objectStream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(objectStream) as List<T>;
            }
        }
        #endregion

        #region 初始化grpc服务器

        public void InitGrpcServer()
        {
            // 这个地方非常有疑问
            //var features = downConfList;
            XmlDocument doc = new XmlDocument();
            doc.Load("Config/grpc.xml");
            XmlNode xn = doc.SelectSingleNode("grpc");
            XmlNodeList xnl = xn.ChildNodes;
            grpcIp = xnl.Item(0).InnerText;
            grpcPort = int.Parse(xnl.Item(1).InnerText);

            try
            {
                Server server = new Server(new List<ChannelOption> { new ChannelOption(ChannelOptions.MaxSendMessageLength, int.MaxValue) })

                {
                    Services = { gRPCServices.BindService(Instance) },
                    Ports = { new ServerPort(grpcIp, grpcPort, ServerCredentials.Insecure) }
                };

                server.Start();

                this.downConfList = new List<DownConf>();

                m_timer.Elapsed += new ElapsedEventHandler(TimeElapsed);
                m_timer.Stop();
                m_timer_1.Elapsed += new ElapsedEventHandler(TimeElapsed_1);
                m_timer_1.Stop();

                //server.ShutdownAsync().Wait();

                CSystemInfoMgr.Instance.AddInfo(string.Format("配置...grpcServer...完成!"));
            }
            catch (Exception)
            {
                CSystemInfoMgr.Instance.AddInfo(string.Format("配置...grpcServer...失败!"));
            }
        }

        public void StopGrpcServer()
        {
            this.downConfList = null;
            instance = null;
        }

        void TimeElapsed(object sender, EventArgs e)
        {
            if (!timeDown)
            {
                timeDown = true;
            }
        }

        void TimeElapsed_1(object sender, EventArgs e)
        {
            if (!timeDown_1)
            {
                timeDown_1 = true;
            }
        }

        #endregion

        #region 远程参数设置

        /// <summary>
        /// get方法  据说是客户端可以调用的
        /// </summary>
        /// <param name="DownRequest"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<DownConf> GetDownConf(DownRequest DownRequest, ServerCallContext context)
        {
            return Task.FromResult(GetDownConf(DownRequest));
        }

        /// <summary>
        /// 处理请求方法，向终端发送请求并返回上行数据
        /// </summary>
        /// <param name="DownRequest"></param>
        /// <returns></returns>
        private DownConf GetDownConf(DownRequest DownRequest)
        {
            CProtocolEventManager.DownForData += this.DownForData_EventHandler;
            CProtocolEventManager.DownForTru += this.DownForTru_EventHandler;
            m_timer.Start();
            DownConf result = new DownConf();
            downConfList = null;
            downConfList = new List<DownConf>();
            downdataGet = false;
            string downQuery = string.Empty;
            gprsNum = DownRequest.GprsNum;
            stationId = DownRequest.StationId;
            cmds = DownRequest.Cmds;
            bool isSet = DownRequest.IsSet;

            if (isSet == false)
            {
                List<EDownParam> query = new List<EDownParam>();

                for (int i = cmds.Length; i > 0; i = i - 2)
                {

                    string downParamValue = cmds.Substring(0, 2);
                    EDownParam downParamKey = ProtocolMaps.DownParamMap.FindKey(downParamValue);
                    query.Add(downParamKey);
                    cmds = cmds.Substring(2);
                }

                downQuery = CPortDataMgr.Instance.SendGprsRead_BS(gprsNum, stationId, query);

                if (downQuery == "")
                {
                    result.IsOL = false;
                    return result;
                }
            }
            else if (isSet == true)
            {
                StringBuilder query = new StringBuilder();
                query.Append(ProtocolMaps.ChannelProtocolStartCharMap.FindValue(EChannelType.GPRS));
                query.Append(String.Format("{0:D4}", Int32.Parse(stationId.Trim())));
                query.Append("0S");

                query.Append(cmds);
                //while(cmds.Length > 0)
                //{
                //    try
                //    {
                //        query.Append(CSpecialChars.BALNK_CHAR);
                //        string downParamValue = cmds.Substring(0, 2);
                //        query.Append(downParamValue);
                //        EDownParam downParamKey = ProtocolMaps.DownParamMap.FindKey(downParamValue);
                //        int downParamLength = int.Parse(ProtocolMaps.DownParamLengthMap.FindValue(downParamKey));
                //        string downData = cmds.Substring(2, downParamLength);
                //        query.Append(downData);
                //        cmds = cmds.Substring(2 + downParamLength);
                //    }
                //    catch (Exception e)
                //    {
                //        Debug.WriteLine("下行指令解析失败" + e.Message);
                //    }
                //}

                query.Append(CSpecialChars.ENTER_CHAR);

                var gprs = CPortDataMgr.Instance.FindGprsByUserid(gprsNum);
                if (gprs != null)
                {
                    uint dtuID = 0;
                    if (gprs.FindByID(gprsNum, out dtuID))
                    {
                        gprs.SendDataTwice(dtuID, query.ToString());
                    }
                    else
                    {
                        result.IsOL = false;
                        return result;
                    }
                }
                else
                {
                    result.IsOL = false;
                    return result;
                }
            }

            result = CheckDownConf(DownRequest);

            while (!downdataGet)
            {
                Thread.Sleep(1000);
                result = CheckDownConf(DownRequest);
                if (timeDown)
                {
                    timeDown = false;
                    break;
                }
            }

            m_timer.Stop();

            CProtocolEventManager.DownForData -= this.DownForData_EventHandler;
            CProtocolEventManager.DownForTru -= this.DownForTru_EventHandler;
            return result;
        }

        /// <summary>
        /// 上行数据处理程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownForData_EventHandler(object sender, DownEventArgs e)
        {
            try
            {
                CDownConf info = e.Value;
                DownConf downConf = new DownConf();
                if (info == null)
                    return;
                if (info.Clock.HasValue)
                {
                    downConf.Clock = info.Clock.ToString();
                }
                if (info.NormalState.HasValue)
                {
                    if (info.NormalState == ENormalState.GPRS) downConf.NormalState = DownConf.Types.NormalState.GprsState;
                    if (info.NormalState == ENormalState.GSM) downConf.NormalState = DownConf.Types.NormalState.GsmState;
                }
                if (info.Voltage.HasValue)
                {
                    downConf.Voltage = (double)info.Voltage;
                }
                if (!String.IsNullOrEmpty(info.StationCmdID))
                {
                    downConf.StationCmdID = info.StationCmdID;
                }
                if (info.TimeChoice.HasValue)
                {
                    if (info.TimeChoice == ETimeChoice.AdjustTime) downConf.TimeChoice = DownConf.Types.TimeChoice.AdjustTime;
                    if (info.TimeChoice == ETimeChoice.Two) downConf.TimeChoice = DownConf.Types.TimeChoice.NoAdjustTime;
                }
                if (info.TimePeriod.HasValue)
                {
                    if (info.TimePeriod == ETimePeriod.One) downConf.TimePeriod = DownConf.Types.TimePeriod.One;
                    if (info.TimePeriod == ETimePeriod.Two) downConf.TimePeriod = DownConf.Types.TimePeriod.Two;
                    if (info.TimePeriod == ETimePeriod.Four) downConf.TimePeriod = DownConf.Types.TimePeriod.Four;
                    if (info.TimePeriod == ETimePeriod.Six) downConf.TimePeriod = DownConf.Types.TimePeriod.Six;
                    if (info.TimePeriod == ETimePeriod.Eight) downConf.TimePeriod = DownConf.Types.TimePeriod.Eight;
                    if (info.TimePeriod == ETimePeriod.Twelve) downConf.TimePeriod = DownConf.Types.TimePeriod.Twelve;
                    if (info.TimePeriod == ETimePeriod.TwentyFour) downConf.TimePeriod = DownConf.Types.TimePeriod.TwentyFour;
                    if (info.TimePeriod == ETimePeriod.FourtyEight) downConf.TimePeriod = DownConf.Types.TimePeriod.FourtyEight;
                }
                if (info.WorkStatus.HasValue)
                {
                    if (info.WorkStatus == EWorkStatus.Debug)
                        downConf.WorkStatus = DownConf.Types.WorkStatus.Debug;
                    if (info.WorkStatus == EWorkStatus.DoubleAddress)
                        downConf.WorkStatus = DownConf.Types.WorkStatus.DoubleAddress;
                    if (info.WorkStatus == EWorkStatus.Normal)
                        downConf.WorkStatus = DownConf.Types.WorkStatus.Normal;
                }
                if (!String.IsNullOrEmpty(info.VersionNum))
                {
                    downConf.VersionNum = info.VersionNum;
                }
                if (info.MainChannel.HasValue && info.ViceChannel.HasValue)
                {
                    try
                    {
                        if (info.MainChannel == EChannelType.BeiDou) downConf.MainChannel = DownConf.Types.ChannelType.BeiDou;
                        if (info.MainChannel == EChannelType.Beidou500) downConf.MainChannel = DownConf.Types.ChannelType.Beidou500;
                        if (info.MainChannel == EChannelType.BeidouNormal) downConf.MainChannel = DownConf.Types.ChannelType.BeidouNormal;
                        if (info.MainChannel == EChannelType.GPRS) downConf.MainChannel = DownConf.Types.ChannelType.Gprs;
                        if (info.MainChannel == EChannelType.GSM) downConf.MainChannel = DownConf.Types.ChannelType.Gsm;
                        if (info.MainChannel == EChannelType.None) downConf.MainChannel = DownConf.Types.ChannelType.None;
                        if (info.MainChannel == EChannelType.PSTN) downConf.MainChannel = DownConf.Types.ChannelType.Pstn;
                        if (info.MainChannel == EChannelType.VHF) downConf.MainChannel = DownConf.Types.ChannelType.Vhf;

                    }
                    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    try
                    {
                        if (info.ViceChannel == EChannelType.BeiDou) downConf.ViceChannel = DownConf.Types.ChannelType.BeiDou;
                        if (info.ViceChannel == EChannelType.Beidou500) downConf.ViceChannel = DownConf.Types.ChannelType.Beidou500;
                        if (info.ViceChannel == EChannelType.BeidouNormal) downConf.ViceChannel = DownConf.Types.ChannelType.BeidouNormal;
                        if (info.ViceChannel == EChannelType.GPRS) downConf.ViceChannel = DownConf.Types.ChannelType.Gprs;
                        if (info.ViceChannel == EChannelType.GSM) downConf.ViceChannel = DownConf.Types.ChannelType.Gsm;
                        if (info.ViceChannel == EChannelType.None) downConf.ViceChannel = DownConf.Types.ChannelType.None;
                        if (info.ViceChannel == EChannelType.PSTN) downConf.ViceChannel = DownConf.Types.ChannelType.Pstn;
                        if (info.ViceChannel == EChannelType.VHF) downConf.ViceChannel = DownConf.Types.ChannelType.Vhf;

                    }
                    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                }
                if (!String.IsNullOrEmpty(info.TeleNum))
                {
                    downConf.TeleNum = info.TeleNum;
                }
                if (info.RingsNum.HasValue)
                {
                    downConf.RingsNum = (double)info.RingsNum;
                }
                if (!String.IsNullOrEmpty(info.DestPhoneNum))
                {
                    downConf.DestPhoneNum = info.DestPhoneNum;
                }
                if (!String.IsNullOrEmpty(info.TerminalNum))
                {
                    downConf.TerminalNum = info.TerminalNum;
                }
                if (!String.IsNullOrEmpty(info.RespBeam))
                {
                    downConf.RespBeam = info.RespBeam;
                }
                if (info.AvegTime.HasValue)
                {
                    downConf.AvegTime = (double)info.AvegTime;
                }
                if (info.RainPlusReportedValue.HasValue)
                {
                    downConf.RainPlusReportedValue = (double)info.RainPlusReportedValue;
                }
                if (!String.IsNullOrEmpty(info.KC))
                {
                    downConf.KC = info.KC;
                    // KC值长度为20时，前十位为k，后十位为C
                }
                if (info.Rain.HasValue)
                {
                    downConf.Rain = (double)info.Rain;
                }
                if (info.StorageWater.HasValue)
                {
                    downConf.Water = (double)info.StorageWater;
                }
                if (info.WaterPlusReportedValue.HasValue)
                {
                    downConf.WaterPlusReportedValue = (double)info.WaterPlusReportedValue;
                }
                if (info.SelectCollectionParagraphs.HasValue)
                {
                    if (info.SelectCollectionParagraphs == ESelectCollectionParagraphs.FiveOrSix) downConf.Select = DownConf.Types.SelectCollectionParagraphs.FiveOrSix;
                    if (info.SelectCollectionParagraphs == ESelectCollectionParagraphs.TenOrTwelve) downConf.Select = DownConf.Types.SelectCollectionParagraphs.TenOrTwelve;
                }
                if (!String.IsNullOrEmpty(info.UserName))
                {
                    downConf.UserName = info.UserName;
                }
                if (!String.IsNullOrEmpty(info.StationName))
                {
                    downConf.StationName = info.StationName;
                }

                if (downConf != null)
                {
                    this.downConfList.Add(downConf);
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine("" + exp.Message);
            }
        }

        private void DownForTru_EventHandler(object sender, ReceiveErrorEventArgs e)
        {
            try
            {
                DownConf downConf = new DownConf();

                string msg = e.Msg;
                if (msg.Contains("TRU") && msg.Contains(stationId))
                {
                    downConf.TRU = true;
                    downConfList.Add(downConf);
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine("" + exp.Message);
            }
        }

        /// <summary>
        /// 检查上行数据列表中是否存在对应请求的数据
        /// </summary>
        /// <param name="DownRequest"></param>
        /// <returns></returns>
        private DownConf CheckDownConf(DownRequest DownRequest)
        {
            DownConf checkedDownConf = new DownConf();
            List<DownConf> downDataList = this.downConfList;
            foreach (var downdata in downDataList)
            {
                if (downdata.TRU == true)
                {
                    checkedDownConf = downdata;
                    checkedDownConf.IsOL = true;
                    downdataGet = true;
                    break;
                }
                if (downdata.StationCmdID == DownRequest.StationId)
                {
                    checkedDownConf = downdata;
                    checkedDownConf.IsOL = true;
                    downdataGet = true;
                    break;
                }
            }
            return checkedDownConf;
        }

        #endregion

        #region 批量传输
        public override Task<BatchList> ListBatchData(BatchRequest request, ServerCallContext context)
        {
            return Task.FromResult(GetBatchList(request));
        }

        private BatchList GetBatchList(BatchRequest request)
        {
            CProtocolEventManager.BatchForData += this.BatchForData_EventHandler;
            m_timer_1.Start();
            SendBatchReport(request);
            if (BatchList.IsOL == false)
            {
                m_timer_1.Stop();
                return BatchList;
            }
            while (true)
            {
                if (BatchList.Bdata.Count != 0)
                {
                    break;
                }
                Thread.Sleep(1000);
                if (timeDown_1)
                {
                    BatchList.NotTimeOut = false;
                    timeDown_1 = false;
                    break;
                }
            }
            m_timer_1.Stop();
            CProtocolEventManager.BatchForData -= this.BatchForData_EventHandler;
            return BatchList;
        }

        private void SendBatchReport(BatchRequest request)
        {
            BatchList = null;
            BatchList = new BatchList() { StationId = request.StationId, TType = request.TransType == true ? BatchList.Types.transType.Byday : BatchList.Types.transType.Byhour, IsOL = true };
            EStationType stype = request.ReportType == true ? EStationType.ERainFall : EStationType.EHydrology;
            ETrans trans = request.TransType == true ? ETrans.ByDay : ETrans.ByHour;
            DateTime st = Convert.ToDateTime(request.StartTime);
            DateTime et = Convert.ToDateTime(request.EndTime);
            DateTime beginTime;
            DateTime endTime;
            if (trans == ETrans.ByHour)
            {
                beginTime = new DateTime(
                year: st.Year,
                month: st.Month,
                day: st.Day,
                hour: st.Hour,
                minute: 0,
                second: 0

            );
                endTime = new DateTime(
                    year: et.Year,
                    month: et.Month,
                    day: et.Day,
                    hour: et.Hour,
                    minute: 0,
                    second: 0

                );
            }
            else
            {
                beginTime = new DateTime(
                year: st.Year,
                month: st.Month,
                day: st.Day,
                hour: 8,
                minute: 0,
                second: 0

            );
                endTime = new DateTime(
                     year: st.Year,
                     month: et.Month,
                     day: et.Day,
                     hour: 8,
                     minute: 0,
                     second: 0

                 );
            }

            string query = string.Empty;
            var gprs = CPortDataMgr.Instance.FindGprsByUserid(request.Gprsid);
            if (gprs != null)
            {
                uint dtuID = 0;
                if (gprs.FindByID(request.Gprsid, out dtuID))
                {
                    query = gprs.FlashBatch.BuildQuery(request.StationId, stype, trans, beginTime, endTime, EChannelType.GPRS);
                    gprs.SendDataTwiceForBatchTrans(dtuID, query);
                }
            }
            else
            {
                BatchList.IsOL = false;
            }
        }

        private void BatchForData_EventHandler(object sender, BatchEventArgs e)
        {
            try
            {
                CBatchStruct info = e.Value;
                if (info == null)
                    return;
                if (BatchList.Bdata.Count != 0 && info.StationID == BatchList.StationId)
                    return;
                BatchList.RawInfo = e.RawData;
                if (info.StationType == EStationType.ERainFall)
                {
                    BatchList.SType = BatchList.Types.stationType.RainStation;
                }
                else if (info.StationType == EStationType.ERiverWater)
                {
                    BatchList.SType = BatchList.Types.stationType.WaterStation;
                }

                foreach (var v in info.Datas)
                {
                    BatchData dataInfo = new BatchData();
                    dataInfo.BatchTime = v.Time.ToString();
                    dataInfo.BatchValue = v.Data;
                    BatchList.Bdata.Add(dataInfo);
                }

            }
            catch (Exception exp)
            {
                Debug.WriteLine("" + exp.Message);
            }
        }

        #region 刷新站点信息
        /// <summary>
        /// 刷新站点信息
        /// </summary>
        /// <param name="station"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<boolResult> RefreshStation(RpcStation station, ServerCallContext context)
        {
            return Task.FromResult(FreshStation(station));
        }
        private boolResult FreshStation(RpcStation station)
        {
            boolResult result = new boolResult();
            CDBDataMgr.Instance.UpdateAllStation();
            result.Flag = true;
            return result;
        }
        #endregion

        #endregion

        #region 获取在线列表


        public override Task<DtuList> GetDtuList(Subcenter subcenter, ServerCallContext context)
        {
            return Task.FromResult(FindDtuList(subcenter));
        }

        private DtuList FindDtuList(Subcenter subcenter)
        {
            DtuList dtuList = new DtuList();

            List<ModemInfoStruct> stateList = new List<ModemInfoStruct>();
            List<CEntityStation> stations = new List<CEntityStation>();
            //List<CEntitySoilStation> soilStations = new List<CEntitySoilStation>();
            List<CEntitySubCenter> subCenters = new List<CEntitySubCenter>();
            Dictionary<string, ModemInfoStruct> gprsDic = new Dictionary<string, ModemInfoStruct>();

            stateList = Clone<ModemInfoStruct>(CProtocolEventManager.Instance.GetOnlineStatusList());
            stations = CDBDataMgr.Instance.GetAllStation();
            //soilStations = CDBSoilDataMgr.Instance.GetAllSoilStation();
            subCenters = CDBDataMgr.Instance.GetAllSubCenter();

            if (stateList.Count() != 0)
            {
                for (int i = 0; i < stateList.Count(); i++)
                {
                    //string uid = ((uint)stateList[i].m_modemId).ToString("X").PadLeft(8, '0');
                    string uid = stateList[i].m_modemId.ToString();
                    if(uid != null && uid != "" && uid != "0")
                    {
                        for (int j = uid.Length; j < 10; j++)
                        {
                            uid = "0" + uid;
                        }
                        if (!gprsDic.ContainsKey(uid))
                        {
                            gprsDic.Add(uid, stateList[i]);
                        }
                    }
                }
            }

            foreach (var s in stations)
            {
                if (subcenter.SubcenterdId == s.SubCenterID.ToString() || subcenter.SubcenterdId == "0")
                {
                    if (gprsDic.Count() != 0)
                    {
                        if (gprsDic.ContainsKey(s.StationID))
                        {
                            Dtu dtu = new Dtu();
                            ModemInfoStruct state = gprsDic[s.StationID];
                            //string phoneno = CGprsUtil.Byte11ToPhoneNO(state.m_phoneno, 0);
                            string phoneno = "";
                            if (state.m_phoneno != null)
                            {
                                phoneno = state.m_phoneno.ToString();
                            }
                            
                            //string dynIP = CGprsUtil.Byte4ToIP(state.m_dynip, 0);
                            string dynIP = state.m_dynip.ToString();
                            string connectTime = CGprsUtil.ULongToDatetime(state.m_conn_time).ToString();
                            //string connectTime = state.m_conn_time.ToString();
                            string refreshTime = CGprsUtil.ULongToDatetime(state.m_refresh_time).ToString();
                            //string refreshTime = state.m_refresh_time.ToString();

                            dtu.SubcenterId = s.SubCenterID.ToString();
                            string subName = CDBDataMgr.Instance.GetSubCenterName(s.SubCenterID.ToString());
                            dtu.SubcenterName = subName;
                            dtu.StationId = s.StationID;
                            dtu.StationName = s.StationName;
                            dtu.GprsId = s.GPRS;
                            dtu.GsmNum = phoneno;
                            dtu.IpAddr = dynIP;
                            dtu.ConnTime = connectTime;
                            dtu.RefreshTime = refreshTime;
                            dtu.State = "1";
                            dtu.StationType = CEnumHelper.StationTypeToDBStr(s.StationType);
                            dtuList.Dtu.Add(dtu);
                        }
                        else
                        {
                            Dtu dtu = new Dtu();
                            dtu.SubcenterId = s.SubCenterID.ToString();
                            string subName = CDBDataMgr.Instance.GetSubCenterName(s.SubCenterID.ToString());
                            dtu.SubcenterName = subName;
                            dtu.StationId = s.StationID;
                            dtu.StationName = s.StationName;
                            dtu.GprsId = s.GPRS;
                            dtu.State = "2";
                            dtu.StationType = CEnumHelper.StationTypeToDBStr(s.StationType);
                            dtuList.Dtu.Add(dtu);
                        }
                    }
                }
            }

            //foreach (var s in stations)
            //{
            //    if (subcenter.SubcenterdId == s.SubCenterID.ToString() || subcenter.SubcenterdId == "0")
            //    {
            //        if (gprsDic.Count() != 0)
            //        {
            //            if (!gprsDic.ContainsKey(s.GPRS))
            //            {
            //                Dtu dtu = new Dtu();
            //                dtu.SubcenterId = s.SubCenterID.ToString();
            //                string subName = CDBDataMgr.Instance.GetSubCenterName(s.SubCenterID.ToString());
            //                dtu.SubcenterName = subName;
            //                dtu.StationId = s.StationID;
            //                dtu.StationName = s.StationName;
            //                dtu.GprsId = s.GPRS;
            //                dtu.State = "2";
            //                dtu.StationType = CEnumHelper.StationTypeToDBStr(s.StationType);
            //                dtuList.Dtu.Add(dtu);
            //            }
            //        }
            //    }
            //}

            //foreach (var s in soilStations)
            //{
            //    if (subcenter.SubcenterdId == s.SubCenterID.ToString() || subcenter.SubcenterdId == "0")
            //    {
            //        if (gprsDic.Count() != 0)
            //        {
            //            if (gprsDic.ContainsKey(s.GPRS))
            //            {
            //                Dtu dtu = new Dtu();

            //                ModemInfoStruct state = gprsDic[s.GPRS];
            //                string phoneno = CGprsUtil.Byte11ToPhoneNO(state.m_phoneno, 0);
            //                string dynIP = CGprsUtil.Byte4ToIP(state.m_dynip, 0);
            //                string connectTime = CGprsUtil.ULongToDatetime(state.m_conn_time).ToString();
            //                string refreshTime = CGprsUtil.ULongToDatetime(state.m_refresh_time).ToString();

            //                if (subcenter.SubcenterdId == s.SubCenterID.ToString() || subcenter.SubcenterdId == "0")
            //                {
            //                    dtu.SubcenterId = s.SubCenterID.ToString();
            //                    string subName = CDBDataMgr.Instance.GetSubCenterName(s.SubCenterID.ToString());
            //                    dtu.SubcenterName = subName;
            //                    dtu.StationId = s.StationID;
            //                    dtu.StationName = s.StationName;
            //                    dtu.GprsId = s.GPRS;
            //                    dtu.GsmNum = phoneno;
            //                    dtu.IpAddr = dynIP;
            //                    dtu.ConnTime = connectTime;
            //                    dtu.RefreshTime = refreshTime;
            //                    dtu.State = "1";
            //                    dtu.StationType = CEnumHelper.StationTypeToDBStr(s.StationType);
            //                    dtuList.Dtu.Add(dtu);
            //                }
            //            }
            //        }
            //        else
            //        {
            //            Dtu dtu = new Dtu();
            //            dtu.SubcenterId = s.SubCenterID.ToString();
            //            string subName = CDBDataMgr.Instance.GetSubCenterName(s.SubCenterID.ToString());
            //            dtu.SubcenterName = subName;
            //            dtu.StationId = s.StationID;
            //            dtu.StationName = s.StationName;
            //            dtu.GprsId = s.GPRS;
            //            dtu.State = "2";
            //            dtu.StationType = CEnumHelper.StationTypeToDBStr(s.StationType);
            //            dtuList.Dtu.Add(dtu);
            //        }
            //    }
            //}

            //foreach (var s in soilStations)
            //{
            //    if (subcenter.SubcenterdId == s.SubCenterID.ToString() || subcenter.SubcenterdId == "0")
            //    {
            //        if (gprsDic.Count() != 0)
            //        {
            //            if (!gprsDic.ContainsKey(s.GPRS))
            //            {
            //                Dtu dtu = new Dtu();
            //                dtu.SubcenterId = s.SubCenterID.ToString();
            //                string subName = CDBDataMgr.Instance.GetSubCenterName(s.SubCenterID.ToString());
            //                dtu.SubcenterName = subName;
            //                dtu.StationId = s.StationID;
            //                dtu.StationName = s.StationName;
            //                dtu.GprsId = s.GPRS;
            //                dtu.State = "2";
            //                dtu.StationType = CEnumHelper.StationTypeToDBStr(s.StationType);
            //                dtuList.Dtu.Add(dtu);
            //            }
            //        }
            //    }
            //}

            return dtuList;
        }

        #endregion

        #region 批量对时
        public override Task<TruList> BatchTime(StationList list, ServerCallContext context)
        {
            return Task.FromResult(BatchTime(list));
        }

        public TruList BatchTime(StationList list)
        {
            CProtocolEventManager.DownForTru += BatchForTru_EventHandler;
            truReturnList = new List<string>();
            TruList truList = new TruList();

            m_timer_1.Start();

            foreach (var id in list.Ids)
            {
                CEntityStation station = CDBDataMgr.Instance.GetStationById(id);
                CPortDataMgr.Instance.SendAdjustClock(station);
            }


            while (true)
            {
                // 等待
                if (timeDown_1)
                {
                    foreach (var id in list.Ids)
                    {
                        if (this.truReturnList.Contains(id))
                        {
                            truList.TruData.Add(new TruData() { StationId = id, Tru = true });
                        }
                        else
                        {
                            truList.TruData.Add(new TruData() { StationId = id, Tru = false });
                        }
                    }
                    break;
                }
            }

            m_timer_1.Stop();
            CProtocolEventManager.DownForTru -= DownForTru_EventHandler;
            return truList;
        }

        private void BatchForTru_EventHandler(object sender, ReceiveErrorEventArgs e)
        {
            string msg = e.Msg;
            try
            {
                if (msg.Contains("TRU"))
                {
                    string[] str = msg.Split(' ');
                    string str1 = str[1];
                    string sid = CDBDataMgr.Instance.GetStationIDByGprs(str1);
                    if (sid != null)
                    {
                        truReturnList.Add(sid);
                    }
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }

        }

        #endregion

        #region 批量读取
        public override Task<ReadDatas> BatchRead(ReadRequest request, ServerCallContext context)
        {
            return Task.FromResult(GetReadDatas(request));
        }

        public ReadDatas GetReadDatas(ReadRequest request)
        {
            readDataDic = new CDictionary<string, string>();
            ReadDatas readDatas = new ReadDatas();
            m_timer_1.Start();

            if (request.RType == ReadRequest.Types.readType.ReadStoredWater)
            {
                CProtocolEventManager.DownForData += ReadForData_EventHandler_1;
                foreach (var id in request.SList.Ids)
                {
                    CEntityStation station = CDBDataMgr.Instance.GetStationById(id);
                    CPortDataMgr.Instance.GroupStorageWaterFirst(station);
                }
            }
            else if (request.RType == ReadRequest.Types.readType.ReadRealWater)
            {
                CProtocolEventManager.DownForData += ReadForData_EventHandler_2;
                foreach (var id in request.SList.Ids)
                {
                    CEntityStation station = CDBDataMgr.Instance.GetStationById(id);
                    CPortDataMgr.Instance.GroupRealityWater(station);
                }
            }
            else if (request.RType == ReadRequest.Types.readType.ReadRain)
            {
                CProtocolEventManager.DownForData += ReadForData_EventHandler_3;
                foreach (var id in request.SList.Ids)
                {
                    CEntityStation station = CDBDataMgr.Instance.GetStationById(id);
                    CPortDataMgr.Instance.GroupRainWater(station);
                }
            }
            //else if (request.RType == ReadRequest.Types.readType.ReadSoil)
            //{
            //    foreach (var id in request.SList.Ids)
            //    {
            //        CEntityStation station = CDBDataMgr.Instance.GetStationById(id);
            //        CPortDataMgr.Instance.GroupSoilWater(station);
            //    }
            //}

            //等待读取完成
            while (true)
            {
                // 等待
                if (timeDown_1)
                {
                    foreach (var id in request.SList.Ids)
                    {
                        if (readDataDic.ContainsKey(id))
                        {
                            readDatas.RData.Add(new ReadData() { StationId = id, Data = readDataDic[id] });
                        }
                        else
                        {
                            readDatas.RData.Add(new ReadData() { StationId = id, Data = "" });
                        }
                    }
                    break;
                }
            }

            m_timer_1.Stop();

            CProtocolEventManager.DownForData -= ReadForData_EventHandler_1;
            CProtocolEventManager.DownForData -= ReadForData_EventHandler_2;
            CProtocolEventManager.DownForData -= ReadForData_EventHandler_3;
            return readDatas;
        }

        private void ReadForData_EventHandler_1(object sender, DownEventArgs e)
        {
            try
            {
                CDownConf info = e.Value;
                //$60031G12000828
                string rawData = e.RawData;
                if (info == null)
                    return;
                string stationid = rawData.Substring(1, 4);
                string type = rawData.Substring(5, 4);
                string data = rawData.Substring(9, 6);
                if (type == "1G12")
                {
                    if (info.StorageWater.HasValue)
                    {
                        if (readDataDic.ContainsKey(stationid))
                        {
                            readDataDic[stationid] = data;
                        }
                        readDataDic.Add(stationid, data);
                    }
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        private void ReadForData_EventHandler_2(object sender, DownEventArgs e)
        {
            try
            {
                CDownConf info = e.Value;
                //$60031G12000828
                //$60121G020845
                string rawData = e.RawData;
                if (info == null)
                    return;
                string stationid = rawData.Substring(1, 4);
                string type = rawData.Substring(5, 4);
                string data = rawData.Substring(9, 4);
                if (type == "1G13")
                {
                    if (info.RealWater.HasValue)
                    {
                        if (readDataDic.ContainsKey(stationid))
                        {
                            readDataDic[stationid] = data;
                        }
                        readDataDic.Add(stationid, data);
                    }
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        private void ReadForData_EventHandler_3(object sender, DownEventArgs e)
        {
            try
            {
                CDownConf info = e.Value;
                //$60031G12000828
                //$60121G020845
                string rawData = e.RawData;
                if (info == null)
                    return;
                string stationid = rawData.Substring(1, 4);
                string type = rawData.Substring(5, 4);
                string data = rawData.Substring(9, 4);
                if (type == "1G02")
                {
                    if (info.Rain.HasValue)
                    {
                        if (readDataDic.ContainsKey(stationid))
                        {
                            readDataDic[stationid] = data;
                        }
                        readDataDic.Add(stationid, data);
                    }
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        //private void ReadForData_EventHandler_4(object sender, DownEventArgs e)
        //{
        //    try
        //    {
        //        CDownConf info = e.Value;
        //        //$60031G12000828
        //        //$60121G020845
        //        string rawData = e.RawData;
        //        if (info == null)
        //            return;
        //        string stationid = rawData.Substring(1, 4);
        //        string type = rawData.Substring(5, 4);
        //        string data = rawData.Substring(9, 4);
        //        if (type == "1G25")
        //        {
        //            if (info.Rain.HasValue)
        //            {
        //                readDataDic.Add(stationid, data);
        //            }
        //        }
        //    }
        //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
        //}
        #endregion
    }
}
