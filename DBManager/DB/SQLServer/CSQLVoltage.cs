using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Hydrology.DBManager.Interface;
using Hydrology.Entity;

namespace Hydrology.DBManager.DB.SQLServer
{
    public class CSQLVoltage : CSQLBase, IVoltageProxy
    {
        #region 静态常量
        private string urlPrefix = "127.0.0.1:8088";
        private const string CT_EntityName = "CEntityVoltage";   //  数据库表Voltage实体类
                                                                 // public static readonly string CT_TableName = "voltage";      //数据库中电压表的名字
                                                                 //public static readonly string CT_TableName = "voltage" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + (DateTime.Now.Day < 15 ? "A" : "B"); //数据库中电压表的名字
        public static string CT_TableName
        {
            get { return "voltage" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + (DateTime.Now.Day < 15 ? "A" : "B"); }
        }

        //数据库查询的电压表名
        //查询起始表名
        public static string CTF_TableName;
        //查询结束表名
        public static string CTT_TableName;
        //查询联合表名
        public static string CF_TableName
        {
            get
            {
                if (CTF_TableName == CTT_TableName)
                {
                    return CTF_TableName;
                }
                else
                {
                    return "( select * from " + CTF_TableName + " union all select * from " + CTT_TableName + " ) ut ";
                }
            }
        }
        public static string CFF_TableName;
        public static string CM_TableName
        {
            get
            {
                if (CTF_TableName == CTT_TableName)
                {
                    return CTF_TableName;
                }
                else if (CFF_TableName == CTT_TableName)
                {
                    return "( select * from " + CTF_TableName + " union all select * from " + CTT_TableName + " ) ut ";
                }
                else
                {
                    return "( select * from " + CTF_TableName + " union all select * from " + CTT_TableName + " union all select * from " + CFF_TableName + " ) ut ";
                }
            }
        }
        // public static readonly string CN_VoltageID = "VID";            //电压表的唯一ID
        public static readonly string CN_StationId = "stationid";   //站点ID
        public static readonly string CN_DataTime = "datatime";    //数据的采集时间
        public static readonly string CN_Voltage = "data";  //电压值
        public static readonly string CN_TransType = "transtype";  //通讯方式
        public static readonly string CN_MsgType = "messagetype";  //报送类型
        public static readonly string CN_RecvDataTime = "recvdatatime";    //接收到数据的时间
        public static readonly string CN_State = "state";
        private const int CN_FIELD_COUNT = 6;
        #endregion

        #region 成员变量

        private List<long> m_listDelRows;            // 删除电压记录的链表
        private List<CEntityVoltage> m_listUpdateRows; // 更新电压记录的链表

        private string m_strStaionId;       //需要查询的测站
        private DateTime m_startTime;  //查询起始时间
        private DateTime m_endTime;    //查询结束时间
        private bool m_TimeSelect;
        private string TimeSelectString
        {
            get
            {
                if (m_TimeSelect == false)
                {
                    return "";
                }
                else
                {
                    return "convert(VARCHAR," + CN_DataTime + ",120) LIKE '%00:00%' and ";
                }
            }
        }

        public System.Timers.Timer m_addTimer_1;
        public System.Timers.Timer m_addTimer_2;
        List<CEntityVoltage> batchInsertVoltageList = new List<CEntityVoltage>();
        #endregion ///<PRIVATE_DATAMEMBER

        #region 公共方法

        public CSQLVoltage()
            : base()
        {
            m_listDelRows = new List<long>();
            m_listUpdateRows = new List<CEntityVoltage>();
            // 为数据表添加列
            m_tableDataAdded.Columns.Add(CN_StationId);
            m_tableDataAdded.Columns.Add(CN_DataTime);
            m_tableDataAdded.Columns.Add(CN_RecvDataTime);
            m_tableDataAdded.Columns.Add(CN_Voltage);
            m_tableDataAdded.Columns.Add(CN_TransType);
            m_tableDataAdded.Columns.Add(CN_MsgType);
            m_tableDataAdded.Columns.Add(CN_State);

            // 初始化互斥量
            m_mutexWriteToDB = CDBMutex.Mutex_TB_Voltage;

            m_addTimer_1 = new System.Timers.Timer();
            m_addTimer_1.Elapsed += new System.Timers.ElapsedEventHandler(EHTimer_1);
            m_addTimer_1.Enabled = false;
            m_addTimer_1.Interval = CDBParams.GetInstance().AddToDbDelay;


            m_addTimer_2 = new System.Timers.Timer();
            m_addTimer_2.Elapsed += new System.Timers.ElapsedEventHandler(EHTimer_2);
            m_addTimer_2.Enabled = false;
            m_addTimer_2.Interval = CDBParams.GetInstance().AddToDbDelay;
            //m_addTimer_2.Interval = 15 * 1000 * 60;
            m_addTimer_2.Start();
            if (XmlHelper.urlDic == null || XmlHelper.urlDic.Count == 0)
            {
                XmlHelper.getXMLInfo();
            }
            urlPrefix = XmlHelper.urlDic["ip"];
        }

        /// <summary>
        /// 定时器事件
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        protected virtual void EHTimer_1(object source, System.Timers.ElapsedEventArgs e)
        {
            //定时器事件，将所有的记录都写入数据库
            m_addTimer_1.Stop();  //停止定时器
            m_dateTimePreAddTime = DateTime.Now;
            //将数据写入数据库
            NewTask(() => { InsertSqlBulk(m_tableDataAdded); });
        }

        protected virtual void EHTimer_2(object source, System.Timers.ElapsedEventArgs e)
        {
            //定时器事件，将所有的记录都写入数据库
            m_addTimer_2.Stop();  //停止定时器
            m_dateTimePreAddTime = DateTime.Now;
            //将数据写入数据库
            NewTask(() => { BatchInsert(); });
            m_addTimer_2.Start();
        }

        // 添加新列
        public void AddNewRow(CEntityVoltage voltage)
        {
            // 记录超过1000条，或者时间超过1分钟，就将当前的数据写入数据库
            m_mutexDataTable.WaitOne(); //等待互斥量
            DataRow row = m_tableDataAdded.NewRow();
            row[CN_StationId] = voltage.StationID;
            row[CN_DataTime] = voltage.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
            row[CN_Voltage] = voltage.Voltage;
            row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(voltage.MessageType);
            row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(voltage.ChannelType);
            row[CN_RecvDataTime] = voltage.TimeRecieved.ToString(CDBParams.GetInstance().DBDateTimeFormat);
            row[CN_State] = voltage.state;
            m_tableDataAdded.Rows.Add(row);
            m_mutexDataTable.ReleaseMutex();

            // 判断是否需要创建新分区
            //CSQLPartitionMgr.Instance.MaintainVoltage(voltage.TimeCollect);
            if (m_tableDataAdded.Rows.Count >= CDBParams.GetInstance().AddBufferMax)
            {
                // 如果超过最大值，写入数据库
                NewTask(() => { AddDataToDB(); });
            }
            else
            {
                // 没有超过缓存最大值，开启定时器进行检测,多次调用Start()会导致重新计数
                m_addTimer.Start();
            }
        }


        public void AddNewRows(List<CEntityVoltage> voltages)
        {
            // 记录超过1000条，或者时间超过1分钟，就将当前的数据写入数据库
            m_mutexDataTable.WaitOne(); //等待互斥量
            foreach (CEntityVoltage voltage in voltages)
            {
                DataRow row = m_tableDataAdded.NewRow();
                row[CN_StationId] = voltage.StationID;
                row[CN_DataTime] = voltage.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(voltage.MessageType);
                row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(voltage.ChannelType);
                row[CN_RecvDataTime] = voltage.TimeRecieved.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                row[CN_Voltage] = voltage.Voltage;
                row[CN_State] = voltage.state;
                m_tableDataAdded.Rows.Add(row);
                // 判断是否需要创建新分区
                //CSQLPartitionMgr.Instance.MaintainVoltage(voltage.TimeCollect);
            }
            if (m_tableDataAdded.Rows.Count >= CDBParams.GetInstance().AddBufferMax)
            {
                // 如果超过最大值，写入数据库
                // NewTask(() => { AddDataToDB(); });
                NewTask(() => { InsertSqlBulk(m_tableDataAdded); });
            }
            else
            {
                // 没有超过缓存最大值，开启定时器进行检测,多次调用Start()会导致重新计数
                m_addTimer_1.Start();
            }
            m_mutexDataTable.ReleaseMutex();
        }

        public void AddNewRows_1(List<CEntityVoltage> voltages)
        {
            if (voltages.Count <= 0)
            {
                return;
            }
            batchInsertVoltageList.AddRange(voltages);

            //Dictionary<string, object> param = new Dictionary<string, object>();
            //string suffix = "/voltage/insertVoltage";
            //string url = "http://" + urlPrefix + suffix;
            ////string url = "http://127.0.0.1:8088/voltage/insertVoltage";
            //Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            ////这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
            //timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            //string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(voltages, Newtonsoft.Json.Formatting.None, timeConverter);
            ////string jsonStr = HttpHelper.ObjectToJson(voltages);
            ////param["voltage"] = "[{\"ChannelType\":6,\"MessageType\":1,\"StationID\":\"0229\",\"TimeCollect\":\"2019-3-13 18:00:00\",\"TimeRecieved\":\"2019-3-13 18:15:00\",\"Voltage\":4,\"VoltageID\":0,\"state\":1,\"type\":null}]";
            //param["voltage"] = jsonStr;
            //try
            //{
            //    string resultJson = HttpHelper.Post(url, param);
            //    CDBLog.Instance.AddInfo(string.Format("添加{0}行到电压表", voltages.Count));
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine("添加电压信息失败");
            //}
            //// 记录超过1000条，或者时间超过1分钟，就将当前的数据写入数据库
            //m_mutexDataTable.WaitOne(); //等待互斥量
            //foreach (CEntityVoltage voltage in voltages)
            //{
            //    DataRow row = m_tableDataAdded.NewRow();
            //    row[CN_StationId] = voltage.StationID;
            //    row[CN_DataTime] = voltage.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
            //    row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(voltage.MessageType);
            //    row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(voltage.ChannelType);
            //    row[CN_RecvDataTime] = voltage.TimeRecieved.ToString(CDBParams.GetInstance().DBDateTimeFormat);
            //    row[CN_Voltage] = voltage.Voltage;
            //    row[CN_Voltage] = voltage.state;
            //    m_tableDataAdded.Rows.Add(row);
            //    // 判断是否需要创建新分区
            //    //CSQLPartitionMgr.Instance.MaintainVoltage(voltage.TimeCollect);
            //}

            //// 如果超过最大值，写入数据库
            //NewTask(() => { AddDataToDB(); });

            //m_mutexDataTable.ReleaseMutex();
        }

        public bool DeleteRows(List<String> voltages_StationId, List<String> voltages_StationDate)
        {
            if (voltages_StationId.Count <= 0)
            {
                return true;
            }
            List<CEntityVoltage> voltageList = new List<CEntityVoltage>();
            for (int i = 0; i < voltages_StationId.Count; i++)
            {
                voltageList.Add(new CEntityVoltage()
                {
                    StationID = voltages_StationId[i],
                    TimeCollect = Convert.ToDateTime(voltages_StationDate[i]),
                    TimeRecieved = Convert.ToDateTime("2019/3/13 15:00:00")
                });
            }
            Dictionary<string, object> param = new Dictionary<string, object>();
            string suffix = "/voltage/deleteVoltage";
            string url = "http://" + urlPrefix + suffix;
            Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(voltageList, Newtonsoft.Json.Formatting.None, timeConverter);
            //string jsonStr = HttpHelper.ObjectToJson(voltageList);
            //param["voltage"] = "[{\"ChannelType\":0,\"MessageType\":0,\"StationID\":\"0229\",\"TimeCollect\":\"2019/3/13 18:00:00\",\"TimeRecieved\":\"2019/3/13 15:00:00\",\"Voltage\":null,\"VoltageID\":0,\"state\":0,\"type\":null}]";
            param["voltage"] = jsonStr;
            try
            {
                string resultJson = HttpHelper.Post(url, param);
            }
            catch (Exception e)
            {
                Debug.WriteLine("删除电压信息失败");
                return false;
            }
            return true;
            //// 删除某条雨量记录
            //StringBuilder sql = new StringBuilder();
            //int currentBatchCount = 0;
            //for (int i = 0; i < voltages_StationId.Count; i++)
            //{
            //    ++currentBatchCount;
            //    CTF_TableName = "voltage" + DateTime.Parse(voltages_StationDate[i]).Year.ToString() + DateTime.Parse(voltages_StationDate[i]).Month.ToString() + (DateTime.Parse(voltages_StationDate[i]).Day < 15 ? "A" : "B");
            //    sql.AppendFormat("delete from {0} where {1}={2} and {3}='{4}';",
            //        CTF_TableName,
            //        CN_StationId, voltages_StationId[i].ToString(),
            //        CN_DataTime, voltages_StationDate[i].ToString()
            //        // CN_VoltageID, voltages[i].ToString()
            //    );
            //    if (currentBatchCount >= CDBParams.GetInstance().UpdateBufferMax)
            //    {
            //        // 更新数据库
            //        if (!this.ExecuteSQLCommand(sql.ToString()))
            //        {
            //            // 保存失败
            //            return false;
            //        }
            //        sql.Clear(); //清除以前的所有命令
            //        currentBatchCount = 0;
            //    }
            //}
            //// 如何考虑线程同异步
            //if (!ExecuteSQLCommand(sql.ToString()))
            //{
            //    return false;
            //}
            //ResetAll();
            //return true;
        }

        public bool UpdateRows(List<CEntityVoltage> voltages)
        {
            if (voltages.Count <= 0)
            {
                return true;
            }
            Dictionary<string, object> param = new Dictionary<string, object>();
            string suffix = "/voltage/updateVoltage";
            string url = "http://" + urlPrefix + suffix;
            //string url = "http://127.0.0.1:8088/voltage/updateVoltage";
            Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(voltages, Newtonsoft.Json.Formatting.None, timeConverter);
            //param["voltage"] = "[{\"ChannelType\":16,\"MessageType\":2,\"StationID\":\"3004\",\"TimeCollect\":\"2019/3/13 18:00:00\",\"TimeRecieved\":\"2019/3/13 18:15:44\",\"Voltage\":44,\"VoltageID\":0,\"state\":1,\"type\":null}]";
            param["voltage"] = jsonStr;
            try
            {
                string resultJson = HttpHelper.Post(url, param);
            }
            catch (Exception e)
            {
                Debug.WriteLine("更新电压信息失败");
                return false;
            }
            return true;
            //// 除主键外和站点外，其余信息随意修改
            //StringBuilder sql = new StringBuilder();
            //int currentBatchCount = 0;
            //for (int i = 0; i < voltages.Count; i++)
            //{
            //    ++currentBatchCount;
            //    CTF_TableName = "voltage" + voltages[i].TimeCollect.Year.ToString() + voltages[i].TimeCollect.Month.ToString() + (voltages[i].TimeCollect.Day < 15 ? "A" : "B");
            //    sql.AppendFormat("update {0} set {1}={2},{3}={4},{5}={6},{7}={8},{9}={10} where {11}={12} and {13}='{14}';",
            //        CTF_TableName,
            //        CN_Voltage, voltages[i].Voltage,
            //        CN_TransType, CEnumHelper.ChannelTypeToDBStr(voltages[i].ChannelType),
            //        CN_MsgType, CEnumHelper.MessageTypeToDBStr(voltages[i].MessageType),
            //        CN_State, voltages[i].state,
            //        CN_RecvDataTime, DateTimeToDBStr(voltages[i].TimeRecieved),
            //        CN_StationId, voltages[i].StationID,
            //        CN_DataTime, voltages[i].TimeCollect.ToString()
            //        //    CN_VoltageID, voltages[i].VoltageID
            //    );
            //    //if (currentBatchCount >= CDBParams.GetInstance().UpdateBufferMax)
            //    //{
            //    //    // 更新数据库
            //    //    if (!this.ExecuteSQLCommand(sql.ToString()))
            //    //    {
            //    //        // 保存失败
            //    //        return false;
            //    //    }
            //    //    sql.Clear(); //清除以前的所有命令
            //    //    currentBatchCount = 0;
            //    //}
            //}
            //// 更新数据库
            //if (!this.ExecuteSQLCommand(sql.ToString()))
            //{
            //    return false;
            //}
            //sql.Clear(); //清除以前的所有命令
            //ResetAll();
            //return true;
        }
        public void SetFilter(string stationId, DateTime timeStart, DateTime timeEnd, bool TimeSelect)
        {

            //设置查询条件
            if (null == m_strStaionId)
            {
                // 第一次查询
                m_iRowCount = -1;
                m_iPageCount = -1;
                m_strStaionId = stationId;
                m_startTime = timeStart;
                m_endTime = timeEnd;
                m_TimeSelect = TimeSelect;
            }
            else
            {
                // 不是第一次查询
                if (stationId != m_strStaionId || timeStart != m_startTime || timeEnd != m_endTime || m_TimeSelect != TimeSelect)
                {
                    m_iRowCount = -1;
                    m_iPageCount = -1;
                    m_mapDataTable.Clear(); //清空上次查询缓存
                }
                m_strStaionId = stationId;
                m_startTime = timeStart;
                m_endTime = timeEnd;
                m_TimeSelect = TimeSelect;
            }
        }
        /// <summary>
        /// 设置条件并且查询数据
        /// </summary>
        /// <param name="stationId"></param>
        /// <param name="timeStart"></param>
        /// <param name="timeEnd"></param>
        /// <param name="TimeSelect"></param>
        /// <returns></returns>
        public List<CEntityVoltage> SetFilterData(string stationId, DateTime timeStart, DateTime timeEnd, bool TimeSelect)
        {
            //传递得参数
            Dictionary<string, object> param = new Dictionary<string, object>();

            //查询条件
            Dictionary<string, string> paramInner = new Dictionary<string, string>();
            paramInner["stationid"] = stationId;
            //paramInner["strttime"] = timeStart.ToString("yyyy-MM-dd HH:mm:ss");
            paramInner["strttime"] = timeStart.ToString();
            //paramInner["endtime"] = timeEnd.ToString("yyyy-MM-dd HH:mm:ss");
            paramInner["endtime"] = timeEnd.ToString();
            //返回结果
            List<CEntityVoltage> voltageList = new List<CEntityVoltage>();
            //string suffix = "/subcenter/getSubcenter";
            string url = "http://127.0.0.1:8088/voltage/getVoltage";
            string jsonStr = HttpHelper.SerializeDictionaryToJsonString(paramInner);
            param["voltage"] = jsonStr;
            try
            {
                string resultJson = HttpHelper.Post(url, param);
                resultJson = HttpHelper.JsonDeserialize(resultJson);
                voltageList = (List<CEntityVoltage>)HttpHelper.JsonToObject(resultJson, new List<CEntityVoltage>());
            }
            catch (Exception e)
            {
                Debug.WriteLine("查询电压信息失败");
                throw e;
                
            }
            return voltageList;
            // 设置查询条件
            //if (null == m_strStaionId)
            //{
            //    // 第一次查询
            //    m_iRowCount = -1;
            //    m_iPageCount = -1;
            //    m_strStaionId = stationId;
            //    m_startTime = timeStart;
            //    m_endTime = timeEnd;
            //    m_TimeSelect = TimeSelect;
            //}
            //else
            //{
            //    // 不是第一次查询
            //    if (stationId != m_strStaionId || timeStart != m_startTime || timeEnd != m_endTime || m_TimeSelect != TimeSelect)
            //    {
            //        m_iRowCount = -1;
            //        m_iPageCount = -1;
            //        m_mapDataTable.Clear(); //清空上次查询缓存
            //    }
            //    m_strStaionId = stationId;
            //    m_startTime = timeStart;
            //    m_endTime = timeEnd;
            //    m_TimeSelect = TimeSelect;
            //}
        }

        public int GetPageCount()
        {
            if (-1 == m_iPageCount)
            {
                DoCountQuery();
            }
            return m_iPageCount;
        }
        public int GetRowCount()
        {
            if (-1 == m_iPageCount)
            {
                DoCountQuery();
            }
            return m_iRowCount;
        }

        public List<CEntityVoltage> GetPageData(int pageIndex)
        {
            if (pageIndex <= 0 || m_startTime == null || m_endTime == null || m_strStaionId == null)
            {
                return new List<CEntityVoltage>();
            }
            // 获取某一页的数据，判断所需页面是否在内存中有值
            int startIndex = (pageIndex - 1) * CDBParams.GetInstance().UIPageRowCount + 1;
            int key = (int)(startIndex / CDBParams.GetInstance().DBPageRowCount) + 1; //对应于数据库中的索引
            int startRow = startIndex - (key - 1) * CDBParams.GetInstance().DBPageRowCount - 1;
            Debug.WriteLine("voltage startIndex;{0} key:{1} startRow:{2}", startIndex, key, startRow);
            // 判断MAP中是否有值
            if (m_mapDataTable.ContainsKey(key))
            {
                // 从内存中读取
                return CopyDataToList(key, startRow);
            }
            else
            {
                // 从数据库中查询
                int topcount = key * CDBParams.GetInstance().DBPageRowCount;
                int rowidmim = topcount - CDBParams.GetInstance().DBPageRowCount;
                CTF_TableName = "voltage" + m_startTime.Year.ToString() + m_startTime.Month.ToString() + (m_startTime.Day < 15 ? "A" : "B");
                CTT_TableName = "voltage" + m_endTime.Year.ToString() + m_endTime.Month.ToString() + (m_endTime.Day < 15 ? "A" : "B");
                CFF_TableName = "voltage" + m_startTime.Year.ToString() + (m_startTime.Day <= 15 ? (m_startTime.Month.ToString() + "B") : ((m_startTime.Month + 1).ToString() + "A"));
                string sql = " select * from ( " +
                    "select top " + topcount.ToString() + " row_number() over( order by " + CN_DataTime + " ) as " + CN_RowId + ",* " +
                    "from " + CM_TableName + " " +
                    "where " + CN_StationId + "=" + m_strStaionId.ToString() + " " +
                    "and " + TimeSelectString + CN_DataTime + " between " + DateTimeToDBStr(m_startTime) +
                    "and " + DateTimeToDBStr(m_endTime) +
                    ") as tmp1 " +
                    "where " + CN_RowId + ">" + rowidmim.ToString() +
                    " order by " + CN_DataTime + " DESC";
                SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                DataTable dataTableTmp = new DataTable();
                adapter.Fill(dataTableTmp);
                m_mapDataTable.Add(key, dataTableTmp);
                return CopyDataToList(key, startRow);
            }
        }

        public bool GetMinDataTime(ref DateTime time)
        {
            // 获取数据表中最早的记录时间
            CTF_TableName = "voltage" + time.Year.ToString() + time.Month.ToString() + (time.Day < 15 ? "A" : "B");
            string sql = string.Format("select top 1 {0} from {1} order by {2};",
                CN_DataTime,
                CTF_TableName, CN_DataTime);
            m_mutexWriteToDB.WaitOne();
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                DataTable dataTableTmp = new DataTable();
                adapter.Fill(dataTableTmp);
                if (dataTableTmp.Rows.Count > 0)
                {
                    time = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                m_mutexWriteToDB.ReleaseMutex();
            }
            return false;

        }

        public bool GetLastData(ref Nullable<Decimal> lastVoltage, ref Nullable<DateTime> lastDayTime, ref Nullable<EChannelType> lastChannelType, ref Nullable<EMessageType> lastMessageType, string stationId)
        {
            // 获取计算雨量值所需的数据
            try
            {
                // 获取最近一条的雨量值
                string sql = string.Format("select top 1 * from {0} where {1} = '{2}' order by {3} desc;",
                    CT_TableName,
                    CN_StationId, stationId,
                    CN_DataTime);
                SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                DataTable dataTableTmp = new DataTable();
                adapter.Fill(dataTableTmp);
                if (dataTableTmp.Rows.Count > 0)
                {
                    lastVoltage = Decimal.Parse(dataTableTmp.Rows[0][CN_Voltage].ToString());
                    lastDayTime = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
                    lastChannelType = CEnumHelper.DBStrToChannelType(dataTableTmp.Rows[0][CN_TransType].ToString());
                    lastMessageType = CEnumHelper.DBStrToMessageType(dataTableTmp.Rows[0][CN_MsgType].ToString());

                }
                else
                {
                    //      Debug.WriteLine(string.Format("查询电压表为空,站点{0}", stationId));
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
            finally
            {
            }
        }

        #endregion ///<PUBLIC_METHOD

        #region 帮助方法
        // 将当前所有数据写入数据库
        protected override bool AddDataToDB()
        {
            // 然后获取内存表的访问权
            m_mutexDataTable.WaitOne();

            if (m_tableDataAdded.Rows.Count <= 0)
            {
                m_mutexDataTable.ReleaseMutex();
                return true;
            }
            //清空内存表的所有内容，把内容复制到临时表tmp中
            DataTable tmp = m_tableDataAdded.Copy();
            m_tableDataAdded.Rows.Clear();

            // 释放内存表的互斥量
            m_mutexDataTable.ReleaseMutex();

            // 先获取对数据库的唯一访问权
            m_mutexWriteToDB.WaitOne();

            Dictionary<string, DataTable> tmpDic = new Dictionary<string, DataTable>();

            for (int i = 0; i < tmp.Rows.Count; i++)
            {
                DateTime time = DateTime.Parse(tmp.Rows[i][CN_DataTime].ToString());
                string tname = "voltage" + time.Year.ToString() + time.Month.ToString() + (time.Day > 15 ? "B" : "A");
                if (!tmpDic.ContainsKey(tname))
                {
                    DataTable tmpDt = tmp.Copy();
                    tmpDt.Clear();
                    tmpDic.Add(tname, tmpDt);
                }
                tmpDic[tname].Rows.Add(tmp.Rows[i].ItemArray);
            }

            foreach (var td in tmpDic)
            {
                BulkTableCopy(td.Key, td.Value);
            }

            m_mutexWriteToDB.ReleaseMutex();
            return true;

        }

        protected void InsertSqlBulk(DataTable dt)
        {
            // 然后获取内存表的访问权
            m_mutexDataTable.WaitOne();

            if (dt.Rows.Count <= 0)
            {
                m_mutexDataTable.ReleaseMutex();
                return;
            }
            //清空内存表的所有内容，把内容复制到临时表tmp中
            DataTable tmp = dt.Copy();
            m_tableDataAdded.Rows.Clear();

            // 释放内存表的互斥量
            m_mutexDataTable.ReleaseMutex();

            Dictionary<string, DataTable> tmpDic = new Dictionary<string, DataTable>();

            for (int i = 0; i < tmp.Rows.Count; i++)
            {
                DateTime time = DateTime.Parse(tmp.Rows[i][CN_DataTime].ToString());
                string tname = "voltage" + time.Year.ToString() + time.Month.ToString() + (time.Day > 15 ? "B" : "A");
                if (!tmpDic.ContainsKey(tname))
                {
                    DataTable tmpDt = tmp.Copy();
                    tmpDt.Clear();
                    tmpDic.Add(tname, tmpDt);
                }
                tmpDic[tname].Rows.Add(tmp.Rows[i].ItemArray);
            }

            foreach (var td in tmpDic)
            {
                BulkTableCopy(td.Key, td.Value);
            }
            return;
        }


        protected void BatchInsert()
        {
            if (batchInsertVoltageList == null || batchInsertVoltageList.Count == 0)
            {
                return;
            }
            List<CEntityVoltage> voltageList = new List<CEntityVoltage>();
            lock (batchInsertVoltageList)
            {
                voltageList.AddRange(batchInsertVoltageList);
                batchInsertVoltageList.Clear();
            }

            if (voltageList.Count <= 500)
            {
                Dictionary<string, object> param = new Dictionary<string, object>();
                string suffix = "/voltage/insertVoltage";
                string url = "http://" + urlPrefix + suffix;
                //string url = "http://127.0.0.1:8088/voltage/insertVoltage";
                Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
                //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
                timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
                string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(voltageList, Newtonsoft.Json.Formatting.None, timeConverter);
                //string jsonStr = HttpHelper.ObjectToJson(voltages);
                //param["voltage"] = "[{\"ChannelType\":6,\"MessageType\":1,\"StationID\":\"0229\",\"TimeCollect\":\"2019-3-13 18:00:00\",\"TimeRecieved\":\"2019-3-13 18:15:00\",\"Voltage\":4,\"VoltageID\":0,\"state\":1,\"type\":null}]";
                param["voltage"] = jsonStr;
                try
                {
                    string resultJson = HttpHelper.Post(url, param);
                    CDBLog.Instance.AddInfo(string.Format("添加{0}行到电压表", voltageList.Count));
                }
                catch (Exception e)
                {
                    Debug.WriteLine("添加电压信息失败");
                    CDBLog.Instance.AddInfo(string.Format("添加电压信息失败！", voltageList.Count));
                    return;
                }
                finally
                {
                    voltageList.Clear();
                }
            }
            else
            {
                CDBLog.Instance.AddInfo(string.Format("数据量大于500条，则分批插入", voltageList.Count));
                List<CEntityVoltage> voltages = new List<CEntityVoltage>();

                for (int i = 0; i < voltageList.Count; i++)
                {
                    voltages.Add(voltageList[i]);
                    if ((i+1) % 500 == 0)
                    {
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        string suffix = "/voltage/insertVoltage";
                        string url = "http://" + urlPrefix + suffix;
                        //string url = "http://127.0.0.1:8088/voltage/insertVoltage";
                        Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
                        //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
                        timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
                        string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(voltages, Newtonsoft.Json.Formatting.None, timeConverter);
                        //string jsonStr = HttpHelper.ObjectToJson(voltages);
                        //param["voltage"] = "[{\"ChannelType\":6,\"MessageType\":1,\"StationID\":\"0229\",\"TimeCollect\":\"2019-3-13 18:00:00\",\"TimeRecieved\":\"2019-3-13 18:15:00\",\"Voltage\":4,\"VoltageID\":0,\"state\":1,\"type\":null}]";
                        param["voltage"] = jsonStr;
                        try
                        {
                            string resultJson = HttpHelper.Post(url, param);
                            CDBLog.Instance.AddInfo(string.Format("添加{0}行到电压表", voltages.Count));
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("添加电压信息失败");
                            CDBLog.Instance.AddInfo(string.Format("添加电压信息失败！", voltages.Count));
                            return;

                        }
                        finally
                        {
                            voltages.Clear();
                        }
                    }
                    if (i == voltageList.Count - 1)
                    {
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        string suffix = "/voltage/insertVoltage";
                        string url = "http://" + urlPrefix + suffix;
                        //string url = "http://127.0.0.1:8088/voltage/insertVoltage";
                        Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
                        //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
                        timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
                        string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(voltages, Newtonsoft.Json.Formatting.None, timeConverter);
                        //string jsonStr = HttpHelper.ObjectToJson(voltages);
                        //param["voltage"] = "[{\"ChannelType\":6,\"MessageType\":1,\"StationID\":\"0229\",\"TimeCollect\":\"2019-3-13 18:00:00\",\"TimeRecieved\":\"2019-3-13 18:15:00\",\"Voltage\":4,\"VoltageID\":0,\"state\":1,\"type\":null}]";
                        param["voltage"] = jsonStr;
                        try
                        {
                            string resultJson = HttpHelper.Post(url, param);
                            CDBLog.Instance.AddInfo(string.Format("添加{0}行到电压表", voltages.Count));
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("添加电压信息失败");
                            CDBLog.Instance.AddInfo(string.Format("添加电压信息失败！", voltages.Count));
                            return;
                        }
                        finally
                        {
                            voltages.Clear();
                            voltageList.Clear();
                        }
                    }
                }
                return;
            }
        }
        protected void BulkTableCopy(string tname, DataTable dt)
        {
            try
            {
                //将临时表中的内容写入数据库
                //SqlConnection conn = CDBManager.GetInstacne().GetConnection();
                //conn.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(CDBManager.GetInstacne().GetConnectionString()))
                {
                    // 电压表有插入触发器
                    //bulkCopy.BatchSize = 1;
                    bulkCopy.BulkCopyTimeout = 1800;
                    bulkCopy.DestinationTableName = tname;
                    bulkCopy.ColumnMappings.Add(CN_StationId, CN_StationId);
                    bulkCopy.ColumnMappings.Add(CN_DataTime, CN_DataTime);
                    bulkCopy.ColumnMappings.Add(CN_Voltage, CN_Voltage);
                    bulkCopy.ColumnMappings.Add(CN_TransType, CN_TransType);
                    bulkCopy.ColumnMappings.Add(CN_MsgType, CN_MsgType);
                    bulkCopy.ColumnMappings.Add(CN_RecvDataTime, CN_RecvDataTime);
                    bulkCopy.ColumnMappings.Add(CN_State, CN_State);

                    try
                    {
                        bulkCopy.WriteToServer(dt);
                        Debug.WriteLine("###{0} :add {1} lines to voltage db", DateTime.Now, dt.Rows.Count);
                        CDBLog.Instance.AddInfo(string.Format("添加{0}行到电压表", dt.Rows.Count));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        //如果出现异常，SqlBulkCopy 会使数据库回滚，所有Table中的记录都不会插入到数据库中，
                        //此时，把Table折半插入，先插入一半，再插入一半。如此递归，直到只有一行时，如果插入异常，则返回。
                        if (dt.Rows.Count == 1)
                            return;
                        int middle = dt.Rows.Count / 2;
                        DataTable table = dt.Clone();
                        for (int i = 0; i < middle; i++)
                            table.ImportRow(dt.Rows[i]);

                        BulkTableCopy(tname, table);

                        table.Clear();
                        for (int i = middle; i < dt.Rows.Count; i++)
                            table.ImportRow(dt.Rows[i]);
                        BulkTableCopy(tname, table);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                //   m_mutexWriteToDB.ReleaseMutex();
                return;
            }
        }

        // 恢复初始状态
        private void ResetAll()
        {
            m_mutexDataTable.WaitOne();
            m_iPageCount = -1;
            m_mapDataTable.Clear(); //清空所有记录
            m_mutexDataTable.ReleaseMutex();
        }

        // 根据当前条件查询统计数据
        private void DoCountQuery()
        {
            CTF_TableName = "voltage" + m_startTime.Year.ToString() + m_startTime.Month.ToString() + (m_startTime.Day < 15 ? "A" : "B");
            CTT_TableName = "voltage" + m_endTime.Year.ToString() + m_endTime.Month.ToString() + (m_endTime.Day < 15 ? "A" : "B");
            CFF_TableName = "voltage" + m_startTime.Year.ToString() + (m_startTime.Day <= 15 ? (m_startTime.Month.ToString() + "B") : ((m_startTime.Month + 1).ToString() + "A"));
            string sql = "select count(*) count from " + CM_TableName + " " +
                "where " + CN_StationId + " = " + m_strStaionId + " " +
                "and " + TimeSelectString + CN_DataTime + "  between " + DateTimeToDBStr(m_startTime) +
                 "and " + DateTimeToDBStr(m_endTime);
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                DataTable dataTableTmp = new DataTable();
                adapter.Fill(dataTableTmp);
                m_iRowCount = Int32.Parse((dataTableTmp.Rows[0])[0].ToString());
                m_iPageCount = (int)Math.Ceiling((double)m_iRowCount / CDBParams.GetInstance().UIPageRowCount); //向上取整
            }
            catch (System.Exception ex)
            {
                // 超时异常等等
                Debug.WriteLine(ex.ToString());
            }

        }

        // 将Map中由key指定的DataTable,从startRow开始返回界面最大行数的集合
        private List<CEntityVoltage> CopyDataToList(int key, int startRow)
        {
            List<CEntityVoltage> result = new List<CEntityVoltage>();
            // 取最小值 ，保证不越界
            int endRow = Math.Min(m_mapDataTable[key].Rows.Count, startRow + CDBParams.GetInstance().UIPageRowCount);
            DataTable table = m_mapDataTable[key];
            for (; startRow < endRow; ++startRow)
            {
                CEntityVoltage voltage = new CEntityVoltage();
                //   voltage.VoltageID = long.Parse(table.Rows[startRow][CN_VoltageID].ToString());
                voltage.StationID = table.Rows[startRow][CN_StationId].ToString();
                voltage.TimeCollect = DateTime.Parse(table.Rows[startRow][CN_DataTime].ToString());
                if (!table.Rows[startRow][CN_Voltage].ToString().Equals(""))
                {
                    voltage.Voltage = Decimal.Parse(table.Rows[startRow][CN_Voltage].ToString());

                }
                else
                {
                    voltage.Voltage = -9999;
                }
                if (table.Rows[startRow][CN_State].ToString() != "")
                {
                    try
                    {
                        voltage.state = int.Parse(table.Rows[startRow][CN_State].ToString());
                    }
                    catch (Exception ex) { }
                }
                else
                {
                    voltage.state = 1;
                }
                voltage.TimeRecieved = DateTime.Parse(table.Rows[startRow][CN_RecvDataTime].ToString());
                voltage.ChannelType = CEnumHelper.DBStrToChannelType(table.Rows[startRow][CN_TransType].ToString());
                voltage.MessageType = CEnumHelper.DBStrToMessageType(table.Rows[startRow][CN_MsgType].ToString());
                result.Add(voltage);
            }
            return result;
        }

        #endregion ///< 帮助方法

        //1009gm
        public List<CEntityVoltage> QueryForRateTable(CEntityStation station, DateTime date)
        {
            List<CEntityVoltage> results = new List<CEntityVoltage>();
            DateTime startTime = date;
            DateTime endTime = startTime.AddHours(23).AddMinutes(59).AddSeconds(59);
            string stationID = station.StationID;
            try
            {
                CTF_TableName = "voltage" + startTime.Year.ToString() + startTime.Month.ToString() + (startTime.Day < 15 ? "A" : "B");
                CTT_TableName = "voltage" + endTime.Year.ToString() + endTime.Month.ToString() + (endTime.Day < 15 ? "A" : "B");
                string sql = "select * from " + CF_TableName + " where stationid = " + stationID + " and convert(VARCHAR, " + CN_DataTime + ", 120) LIKE '%00:00%' and datatime between '" + startTime + "' and '" + endTime + "'and messageType = 8;";
                SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                DataTable dataTableTmp = new DataTable();
                adapter.Fill(dataTableTmp);
                for (int i = 0; i < dataTableTmp.Rows.Count; ++i)
                {
                    CEntityVoltage res = new CEntityVoltage();
                    res.TimeCollect = DateTime.Parse(dataTableTmp.Rows[i][CN_DataTime].ToString());
                    res.type = dataTableTmp.Rows[i][CN_TransType].ToString();
                    results.Add(res);
                }
            }
            catch
            {

            }
            return results;
        }

        public List<CEntityVoltage> QueryForRateMonthTable(CEntityStation station, DateTime startTime, DateTime endTime)
        {
            List<CEntityVoltage> results = new List<CEntityVoltage>();
            string stationID = station.StationID;
            try
            {
                CTF_TableName = "voltage" + startTime.Year.ToString() + startTime.Month.ToString() + (startTime.Day < 15 ? "A" : "B");
                CTT_TableName = "voltage" + endTime.Year.ToString() + endTime.Month.ToString() + (endTime.Day < 15 ? "A" : "B");
                string sql = "select * from " + CF_TableName + " where stationid = " + stationID + " and convert(VARCHAR, " + CN_DataTime + ", 120) LIKE '%00:00%' and datatime between '" + startTime + "' and '" + endTime + "' and messagetype = 8;";
                SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                DataTable dataTableTmp = new DataTable();
                adapter.Fill(dataTableTmp);
                for (int i = 0; i < dataTableTmp.Rows.Count; ++i)
                {
                    CEntityVoltage res = new CEntityVoltage();
                    res.TimeCollect = DateTime.Parse(dataTableTmp.Rows[i][CN_DataTime].ToString());
                    res.type = dataTableTmp.Rows[i][CN_TransType].ToString();
                    results.Add(res);
                }
            }
            catch
            {

            }
            return results;
        }

        public bool createTable(string name)
        {
            StringBuilder sqlcreate = new StringBuilder();
            StringBuilder sqlAlter = new StringBuilder();
            StringBuilder sqlcreate2 = new StringBuilder();
            StringBuilder sqlAlter2 = new StringBuilder();
            for (int i = 1; i <= 12; i++)
            {
                string tableName = name + i.ToString() + "A";
                sqlcreate.Clear();
                sqlAlter.Clear();
                sqlcreate.AppendFormat("if not exists (select * from sysobjects where id = object_id('{0}') and OBJECTPROPERTY(id, 'IsUserTable') = 1) CREATE TABLE [{1}] ([stationid] [char](4) NOT NULL,[datatime][datetime] NOT NULL,[data][numeric](18, 2) NULL, [transtype][char](2) NULL,[messagetype][char](1) NULL,[recvdatatime][datetime] NULL,[state][int] NULL,CONSTRAINT [{2}] PRIMARY KEY CLUSTERED ([stationid] ASC,[datatime] ASC)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]) ON[PRIMARY]; ",
                      tableName,
                      tableName,
                      "PK_" + tableName
                    );
                if (this.ExecuteSQLCommand(sqlcreate.ToString()))
                {
                    sqlAlter.AppendFormat("ALTER TABLE [{0}]  WITH NOCHECK ADD  CONSTRAINT [{1}] FOREIGN KEY([stationid]) REFERENCES [dbo].[hydlstation]([StationID]) ON DELETE NO ACTION ALTER TABLE [{2}] CHECK CONSTRAINT[{3}];",
                        tableName,
                        "FK_" + tableName + "_hydlstation",
                        tableName,
                        "FK_" + tableName + "_hydlstation"
                        );
                    sqlAlter.AppendFormat("CREATE INDEX [{0}] ON [dbo].[{1}] ([stationid] ASC, [datatime] ASC); ",
                        "Index_" + tableName,
                        tableName
                        );
                    this.ExecuteSQLCommand(sqlAlter.ToString());
                }

                //string tableName3 = name + i.ToString();
                //sqlcreate.Clear();
                //sqlAlter.Clear();
                //sqlcreate.AppendFormat("if not exists (select * from sysobjects where id = object_id('{0}') and OBJECTPROPERTY(id, 'IsUserTable') = 1) CREATE TABLE [{1}] ([stationid] [char](4) NOT NULL,[datatime][datetime] NOT NULL,[data][numeric](18, 2) NULL, [transtype][char](2) NULL,[messagetype][char](1) NULL,[recvdatatime][datetime] NULL,[state][int] NULL,CONSTRAINT [{2}] PRIMARY KEY CLUSTERED ([stationid] ASC,[datatime] ASC)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]) ON[PRIMARY]; ",
                //      tableName3,
                //      tableName3,
                //      "PK_" + tableName3
                //    );
                //if (this.ExecuteSQLCommand(sqlcreate.ToString()))
                //{
                //    sqlAlter.AppendFormat("ALTER TABLE [{0}]  WITH NOCHECK ADD  CONSTRAINT [{1}] FOREIGN KEY([stationid]) REFERENCES [dbo].[hydlstation]([StationID]) ON DELETE NO ACTION ALTER TABLE [{2}] CHECK CONSTRAINT[{3}];",
                //        tableName3,
                //        "FK_" + tableName3 + "_hydlstation",
                //        tableName3,
                //        "FK_" + tableName3 + "_hydlstation"
                //        );
                //    this.ExecuteSQLCommand(sqlAlter.ToString());
                //}

                string tableName2 = name + i.ToString() + "B";
                sqlcreate2.Clear();
                sqlAlter2.Clear();
                sqlcreate2.AppendFormat("if not exists (select * from sysobjects where id = object_id('{0}') and OBJECTPROPERTY(id, 'IsUserTable') = 1)  CREATE TABLE [{1}] ([stationid] [char](4) NOT NULL,[datatime][datetime] NOT NULL,[data][numeric](18, 2) NULL, [transtype][char](2) NULL,[messagetype][char](1) NULL,[recvdatatime][datetime] NULL,[state][int] NULL,CONSTRAINT [{2}] PRIMARY KEY CLUSTERED ([stationid] ASC,[datatime] ASC)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]) ON[PRIMARY]; ",
                      tableName2,
                      tableName2,
                      "PK_" + tableName2
                    );
                if (this.ExecuteSQLCommand(sqlcreate2.ToString()))
                {
                    sqlAlter2.AppendFormat("ALTER TABLE [{0}]  WITH NOCHECK ADD  CONSTRAINT [{1}] FOREIGN KEY([stationid]) REFERENCES [dbo].[hydlstation]([StationID]) ON DELETE NO ACTION ALTER TABLE [{2}] CHECK CONSTRAINT[{3}];",
                        tableName2,
                        "FK_" + tableName2 + "_hydlstation",
                        tableName2,
                        "FK_" + tableName2 + "_hydlstation"
                        );
                    sqlAlter2.AppendFormat("CREATE INDEX [{0}] ON [dbo].[{1}] ([stationid] ASC, [datatime] ASC); ",
                        "Index_" + tableName2,
                        tableName2
                        );
                    this.ExecuteSQLCommand(sqlAlter2.ToString());
                }


            }
            return true;
        }

    }
}
