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
    /// <summary>
    /// 提供SQL Server 数据库中对雨量表的操作
    /// </summary>
    public class CSQLRain : CSQLBase, IRainProxy
    {
        #region 数据成员
        private string urlPrefix = "127.0.0.1:8088";
        private const string CT_EntityName = "CEntityRain";   //  数据库表Rain实体类
        //public static readonly string CT_TableName = "rain"; //数据库中雨量表的名字
        //0504

        //数据库中雨量表的名字
        public static string CT_TableName
        {
            get { return "rain" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + (DateTime.Now.Day > 15 ? "B" : "A"); }
        }

        //数据库查询的雨量表名
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


        //public static readonly string CT_TableName = "rain";
        //  public static readonly string CN_RainID = "RID";            //雨量表的唯一ID
        public static readonly string CN_StationId = "stationid"; //站点ID
        public static readonly string CN_DataTime = "datatime";    //数据的采集时间

        public static readonly string CN_PeriodRain = "periodrain";  //雨量差值,即时段雨量
        public static readonly string CN_DifferenceRain = "differencerain";  //雨量差值,即时段雨量
        public static readonly string CN_TotalRain = "totalrain";     //累积雨量
        public static readonly string CN_DayRain = "dayrain";       //日雨量

        public static readonly string CN_DataState = "state";       //状态，True or False
        public static readonly string CN_TransType = "transtype";  //通讯方式
        public static readonly string CN_MsgType = "messagetype";  //报送类型
        public static readonly string CN_RecvDataTime = "recvdatatime";    //接收到数据的时间

        private const int CN_FIELD_COUNT = 9;
        private List<long> m_listDelRows;            // 删除雨量记录的链表
        private List<CEntityRain> m_listUpdateRows; // 更新雨量录的链表

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
        List<CEntityRain> batchInsertRainList = new List<CEntityRain>();

        #endregion


        public CSQLRain()
            : base()
        {
            m_listDelRows = new List<long>();
            m_listUpdateRows = new List<CEntityRain>();
            // 为数据表添加列
            m_tableDataAdded.Columns.Add(CN_StationId);
            // m_tableDataAdded.Columns.Add(CN_RainID);
            m_tableDataAdded.Columns.Add(CN_DataTime);

            m_tableDataAdded.Columns.Add(CN_PeriodRain);
            m_tableDataAdded.Columns.Add(CN_DifferenceRain);
            //  m_tableDataAdded.Columns.Add(CN_RecvDataTime);
            m_tableDataAdded.Columns.Add(CN_TotalRain);
            m_tableDataAdded.Columns.Add(CN_DayRain);

            m_tableDataAdded.Columns.Add(CN_TransType);
            m_tableDataAdded.Columns.Add(CN_MsgType);

            //  m_tableDataAdded.Columns.Add(CN_DataState);
            m_tableDataAdded.Columns.Add(CN_RecvDataTime);
            m_tableDataAdded.Columns.Add(CN_DataState);
            // 分页查询相关
            m_strStaionId = null;

            // 初始化互斥量
            m_mutexWriteToDB = CDBMutex.Mutex_TB_Rain;

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

        public void AddNewRow(CEntityRain rain)
        {
            // 记录超过1000条，或者时间超过1分钟，就将当前的数据写入数据库
            m_mutexDataTable.WaitOne(); //等待互斥量
            DataRow row = m_tableDataAdded.NewRow();
            row[CN_StationId] = rain.StationID;
            row[CN_DataTime] = rain.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
            row[CN_PeriodRain] = rain.PeriodRain;
            row[CN_DifferenceRain] = rain.DifferneceRain;
            row[CN_TotalRain] = rain.TotalRain;
            row[CN_DayRain] = rain.DayRain;

            row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(rain.ChannelType);
            row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(rain.MessageType);

            row[CN_RecvDataTime] = rain.TimeRecieved.ToString(CDBParams.GetInstance().DBDateTimeFormat);

            row[CN_DataState] = rain.BState;
            m_tableDataAdded.Rows.Add(row);
            m_mutexDataTable.ReleaseMutex();

            // 判断是否需要创建新分区
            //CSQLPartitionMgr.Instance.MaintainRain(rain.TimeCollect);
            // 可能被多线程抢断，不过Count值总是最新的
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

        public void AddNewRows(List<CEntityRain> rains)
        {

            // 记录超过1000条，或者时间超过1分钟，就将当前的数据写入数据库
            m_mutexDataTable.WaitOne(); //等待互斥量
            foreach (CEntityRain rain in rains)
            {
                DataRow row = m_tableDataAdded.NewRow();
                row[CN_StationId] = rain.StationID;
                row[CN_DataTime] = rain.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                row[CN_PeriodRain] = rain.PeriodRain;
                row[CN_DifferenceRain] = rain.DifferneceRain;
                row[CN_TotalRain] = rain.TotalRain;
                row[CN_DayRain] = rain.DayRain;

                row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(rain.ChannelType);
                row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(rain.MessageType);

                row[CN_RecvDataTime] = rain.TimeRecieved.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                //row[CN_StationId] = rain.StationID;
                //row[CN_DataTime] = rain.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                //row[CN_PeriodRain] = rain.PeriodRain;
                //row[CN_TotalRain] = rain.TotalRain;
                //row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(rain.MessageType);
                //row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(rain.ChannelType);
                //row[CN_DayRain] = rain.DayRain;
                //row[CN_RecvDataTime] = rain.TimeRecieved.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                row[CN_DataState] = rain.BState;
                m_tableDataAdded.Rows.Add(row);

                // 判断是否需要创建新分区
                //CSQLPartitionMgr.Instance.MaintainRain(rain.TimeCollect);
            }
            if (m_tableDataAdded.Rows.Count >= CDBParams.GetInstance().AddBufferMax)
            {
                // 如果超过最大值，写入数据库
                //   NewTask(() => { AddDataToDB(); });
                NewTask(() => { InsertSqlBulk(m_tableDataAdded); });
            }
            else
            {
                // 没有超过缓存最大值，开启定时器进行检测,多次调用Start()会导致重新计数
                m_addTimer_1.Start();
            }
            // AddDataToDB(); 
            m_mutexDataTable.ReleaseMutex();
        }

        public bool DeleteRows(List<String> rains_StationId, List<String> rains_StationDate)
        {
            if (rains_StationId.Count <= 0)
            {
                return true;
            }
            List<CEntityRain> rainList = new List<CEntityRain>();
            for (int i = 0; i < rains_StationId.Count; i++)
            {
                rainList.Add(new CEntityRain()
                {
                    StationID = rains_StationId[i],
                    TimeCollect = Convert.ToDateTime(rains_StationDate[i]),
                    TimeRecieved = Convert.ToDateTime("2019/3/13 15:00:00")
                });
            }
            Dictionary<string, object> param = new Dictionary<string, object>();
            //rains_StationDate = DateTime.MinValue ? (DateTime)SqlDateTime.MinValue : rains_StationDate;
            string suffix = "/rain/deleteRain";
            string url = "http://" + urlPrefix + suffix;
            //string url = "http://127.0.0.1:8088/rain/deleteRain";
            Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(rainList, Newtonsoft.Json.Formatting.None, timeConverter);
            param["rain"] = jsonStr;
            //string jsonStr = HttpHelper.ObjectToJson(rainList);
            //param["rain"] = "[{\"BState\":0,\"ChannelType\":0,\"DayRain\":null,\"DifferneceRain\":null,\"MessageType\":0,\"PeriodRain\":null,\"RainID\":0,\"StationID\":\"3004\",\"TimeCollect\":\"2019/3/13 11:00:00\",\"TimeRecieved\":\"2019/3/13 15:00:00\",\"TotalRain\":null}]";
            try
            {
                string resultJson = HttpHelper.Post(url, param);
            }
            catch (Exception e)
            {
                Debug.WriteLine("删除雨量信息失败");
                return false;
            }
            return true;
            //// 删除某条雨量记录
            //StringBuilder sql = new StringBuilder();
            //int currentBatchCount = 0;
            //for (int i = 0; i < rains_StationId.Count; i++)
            //{
            //    ++currentBatchCount;
            //    CTF_TableName = "rain" + DateTime.Parse(rains_StationDate[i]).Year.ToString() + DateTime.Parse(rains_StationDate[i]).Month.ToString() + (DateTime.Parse(rains_StationDate[i]).Day > 15 ? "B" : "A");
            //    sql.AppendFormat("delete from {0} where {1}={2} and {3}='{4}';",
            //        CTF_TableName,
            //        CN_StationId, rains_StationId[i].ToString(),
            //        CN_DataTime, rains_StationDate[i].ToString()
            //    // CN_RainID, rains[i].ToString()
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
            //if (!ExecuteSQLCommand(sql.ToString()))
            //{
            //    ExecuteSQLCommand(sql.ToString());
            //    //return false;
            //}
            //ResetAll();
            //// 如何考虑线程同异步
            //return true;

        }

        public bool UpdateRows(List<Hydrology.Entity.CEntityRain> rains)
        {
            if (rains.Count <= 0)
            {
                return true;
            }
            Dictionary<string, object> param = new Dictionary<string, object>();
            string suffix = "/rain/updateRain";
            string url = "http://" + urlPrefix + suffix;
            //string url = "http://127.0.0.1:8088/rain/updateRain";
            Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(rains, Newtonsoft.Json.Formatting.None, timeConverter);
            param["rain"] = jsonStr;
            try
            {
                string resultJson = HttpHelper.Post(url, param);
            }
            catch (Exception e)
            {
                Debug.WriteLine("更新雨量信息失败");
                return false;
            }
            return true;
            //// 除主键外和站点外，其余信息随意修改
            //StringBuilder sql = new StringBuilder();
            //int currentBatchCount = 0;
            //for (int i = 0; i < rains.Count; i++)
            //{

            //    ++currentBatchCount;
            //    CTF_TableName = "rain" + rains[i].TimeCollect.Year.ToString() + rains[i].TimeCollect.Month.ToString() + (rains[i].TimeCollect.Day > 15 ? "B" : "A");
            //    sql.AppendFormat("update {0} set {1}={2},{3}={4},{5}={6},{7}={8},{9}={10},{11}={12},{13}='{14}',{15} ='{16}' where {17}={18} and {19}='{20}';",
            //        CTF_TableName,
            //        //  CN_DataTime, DateTimeToDBStr(rains[i].TimeCollect),
            //        CN_PeriodRain, rains[i].PeriodRain.HasValue ? rains[i].PeriodRain.Value.ToString() : "null",
            //        CN_TotalRain, rains[i].TotalRain.HasValue ? rains[i].TotalRain.Value.ToString() : "null",
            //        CN_DifferenceRain, rains[i].DifferneceRain.HasValue ? rains[i].DifferneceRain.Value.ToString() : "null",
            //        CN_DayRain, rains[i].DayRain.HasValue ? rains[i].DayRain.Value.ToString() : "null",
            //        CN_TransType, CEnumHelper.ChannelTypeToDBStr(rains[i].ChannelType),
            //        CN_MsgType, CEnumHelper.MessageTypeToDBStr(rains[i].MessageType),
            //        CN_RecvDataTime, rains[i].TimeRecieved.ToString(),
            //        CN_DataState, rains[i].BState,
            //        //CN_RainID, rains[i].RainID
            //        CN_StationId, rains[i].StationID,
            //        CN_DataTime, rains[i].TimeCollect.ToString()
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
            //// 更新数据库
            //if (!this.ExecuteSQLCommand(sql.ToString()))
            //{
            //    return false;
            //}
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
        /// 设置查询条件并查询数据
        /// </summary>
        /// <param name="stationId"></param>
        /// <param name="timeStart"></param>
        /// <param name="timeEnd"></param>
        /// <param name="TimeSelect"></param>
        public List<CEntityRain> SetFilterData(string stationId, DateTime timeStart, DateTime timeEnd, bool TimeSelect)
        {
            //传递得参数
            Dictionary<string, object> param = new Dictionary<string, object>();

            //查询条件
            Dictionary<string, string> paramInner = new Dictionary<string, string>();
            paramInner["stationid"] = stationId;
            paramInner["strttime"] = timeStart.ToString();
            paramInner["endtime"] = timeEnd.ToString();
            //返回结果
            List<CEntityRain> rainList = new List<CEntityRain>();
            string suffix = "/rain/getRain";
            string url = "http://" + urlPrefix + suffix;
            //string url = "http://127.0.0.1:8088/rain/getRain";
            string jsonStr = HttpHelper.SerializeDictionaryToJsonString(paramInner);
            param["rain"] = jsonStr;
            try
            {
                string resultJson = HttpHelper.Post(url, param);
                resultJson = HttpHelper.JsonDeserialize(resultJson);
                rainList = (List<CEntityRain>)HttpHelper.JsonToObject(resultJson, new List<CEntityRain>());
            }
            catch (Exception e)
            {
                Debug.WriteLine("查询分中心失败");
                throw e;
            }
            return rainList;
        }

        public int GetPageCount()
        {
            //select top 300 ROW_NUMBER() over(order by datatime) as rowid,* from Tsrain where datatime between CAST('2009-12-22 09:09:09' as datetime) and CAST('2010-01-01 09:09:09' as datetime);
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

        public List<Hydrology.Entity.CEntityRain> GetPageData(int pageIndex)
        {
            if (pageIndex <= 0 || m_startTime == null || m_endTime == null || m_strStaionId == null)
            {
                return new List<CEntityRain>();
            }
            // 获取某一页的数据，判断所需页面是否在内存中有值
            int startIndex = (pageIndex - 1) * CDBParams.GetInstance().UIPageRowCount + 1;
            int key = (int)(startIndex / CDBParams.GetInstance().DBPageRowCount) + 1; //对应于数据库中的索引
            int startRow = startIndex - (key - 1) * CDBParams.GetInstance().DBPageRowCount - 1;
            Debug.WriteLine("startIndex;{0} key:{1} startRow:{2}", startIndex, key, startRow);
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
                CTF_TableName = "rain" + m_startTime.Year.ToString() + m_startTime.Month.ToString() + (m_startTime.Day > 15 ? "B" : "A");
                CTT_TableName = "rain" + m_endTime.Year.ToString() + m_endTime.Month.ToString() + (m_endTime.Day > 15 ? "B" : "A");
                CFF_TableName = "rain" + m_startTime.Year.ToString() + (m_startTime.Day <= 15 ? (m_startTime.Month.ToString() + "B") : ((m_startTime.Month + 1).ToString() + "A"));
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
            CTF_TableName = "rain" + time.Year.ToString() + time.Month.ToString() + (time.Day > 15 ? "B" : "A");
            string sql = string.Format("select top 1 {0} from {1} order by {2};",
                CN_DataTime,
                CTF_TableName, CN_DataTime);
            //m_mutexWriteToDB.WaitOne();
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
                //m_mutexWriteToDB.ReleaseMutex();
            }
            return false;

        }

        public bool GetLastData(ref Nullable<Decimal> lastTotalRain, ref Nullable<DateTime> lastDataTime, ref Nullable<Decimal> lastDayTotalRain, ref Nullable<Decimal> llastDayTotalRain, ref Nullable<Decimal> lastSharpClockTotalRain, ref Nullable<DateTime> latSharpClockTime, ref Nullable<DateTime> lastDayTime, ref Nullable<EChannelType> lastChannelType, ref Nullable<EMessageType> lastMessageType, string stationId)
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
                    lastDataTime = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
                    lastTotalRain = Decimal.Parse(dataTableTmp.Rows[0][CN_TotalRain].ToString());
                    //lastPeriodRain = Decimal.Parse(dataTableTmp.Rows[0][CN_PeriodRain].ToString());
                    //lastPeriodRain = base.GetCellDecimalValue(dataTableTmp.Rows[0][CN_PeriodRain]);
                    lastChannelType = CEnumHelper.DBStrToChannelType(dataTableTmp.Rows[0][CN_TransType].ToString());
                    lastMessageType = CEnumHelper.DBStrToMessageType(dataTableTmp.Rows[0][CN_MsgType].ToString());
                }
                else
                {
                    //       Debug.WriteLine(string.Format("查询雨量表为空,站点{0}", stationId));
                }

                // 获取最近一天的雨量记录
                sql = string.Format("select top 2 {0},{1} from {2} where {3} is not null and {4}='{5}'order by {6} desc;",
                    CN_TotalRain, CN_DataTime,
                    CT_TableName,
                    CN_DayRain,
                    CN_StationId, stationId,
                    CN_DataTime);
                adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                dataTableTmp = new DataTable();
                adapter.Fill(dataTableTmp);

                ////start
                //SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                //DataTable dataTableTemp = new DataTable();
                //adapter.Fill(dataTableTemp);
                //int flag = dataTableTemp.Rows.Count;
                //if (flag == 0)
                //{
                //}
                //else
                //{
                //    for (int rowid = 0; rowid < dataTableTemp.Rows.Count; ++rowid)
                //    {
                //        CEntityRain rain = new CEntityRain();
                //        rain.StationID = dataTableTemp.Rows[rowid][CN_StationId].ToString();
                //        rain.TimeCollect = DateTime.Parse(dataTableTemp.Rows[rowid][CN_DataTime].ToString());
                //        if (dataTableTemp.Rows[rowid][CN_PeriodRain].ToString() != "")
                //        {
                //            rain.PeriodRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_PeriodRain].ToString());
                //        }
                //        else
                //        {
                //            rain.PeriodRain = -9999;
                //        }
                //        if (dataTableTemp.Rows[rowid][CN_TotalRain].ToString() != "")
                //        {
                //            rain.TotalRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_TotalRain].ToString());
                //        }
                //        else
                //        {
                //            rain.TotalRain = -9999;
                //        }
                //        results.Add(rain);
                //    }
                //}
                //return results;
                ////end
                if (dataTableTmp.Rows.Count > 0)
                {
                    for (int rowid = 0; rowid < dataTableTmp.Rows.Count; ++rowid)
                    {
                        if (rowid == 0)
                        {
                            lastDayTotalRain = Decimal.Parse(dataTableTmp.Rows[0][CN_TotalRain].ToString());
                            lastDayTime = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
                        }
                        if (rowid == 1)
                        {

                            llastDayTotalRain = Decimal.Parse(dataTableTmp.Rows[1][CN_TotalRain].ToString());
                        }

                    }

                }
                else
                {
                    //        Debug.WriteLine(string.Format("查询日雨量为空,站点{0}", stationId));
                }
                //最近正点
                sql = string.Format("select top 1 {0},{1} from {2} where {3} is not null and {4}='{5}'order by {6} desc;",
                    CN_TotalRain, CN_DataTime,
                    CT_TableName,
                    CN_PeriodRain,
                    CN_StationId, stationId,
                    CN_DataTime);
                adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                dataTableTmp = new DataTable();
                adapter.Fill(dataTableTmp);
                if (dataTableTmp.Rows.Count > 0)
                {
                    lastSharpClockTotalRain = Decimal.Parse(dataTableTmp.Rows[0][CN_TotalRain].ToString());
                    latSharpClockTime = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
                }
                else
                {
                    //       Debug.WriteLine(string.Format("查询日雨量为空,站点{0}", stationId));
                }
                //结束
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

        //public decimal GetLastDayTotalRain(string stationId, DateTime lastDay)
        //{
        //    //decimal result = 0;
        //    //string sql = "select totalrain from " + CT_TableName + " where stationid= " + stationId + " and datatime= '" + lastDay + "';";
        //    //SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
        //    //DataTable dataTableTmp = new DataTable();
        //    //adapter.Fill(dataTableTmp);
        //    //result = decimal.Parse((dataTableTmp.Rows[0])[0].ToString());
        //    decimal result = 0;
        //    return result;
        //}

        //public List<CEntityRain> GetLastClockSharp(string stationId, DateTime TodayTime)
        //{
        //    List<CEntityRain> listRain=new List<CEntityRain>();
        //    //DateTime PreTime = TodayTime.Subtract(new TimeSpan(5,0,0));
        //    //string sql = "select * from " + CT_TableName + " where stationid= " + stationId + " and datatime between  '" + PreTime + " ' and  '",+";";
        //    return listRain;
        //}
        // 会出错吗  1026
        public decimal? GetLastDayTotalRain(string stationId, DateTime dt)
        {
            decimal? result = 0;
            CTT_TableName = "rain" + dt.Year.ToString() + dt.Month.ToString() + (dt.Day > 15 ? "B" : "A");
            CTF_TableName = "rain" + dt.Year.ToString() + (dt.Day > 15 ? dt.Month.ToString() : dt.AddMonths(-1).Month.ToString()) + (dt.Day > 15 ? "A" : "B");
            string sql = "select top(1) totalrain from " + CF_TableName + " where stationid= " + stationId + " and datatime< '" + dt + "' order by datatime desc;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            if (dataTableTmp.Rows.Count == 1)
            {
                result = decimal.Parse((dataTableTmp.Rows[0])[0].ToString());
            }
            if (dataTableTmp.Rows.Count == 0)
            {
                result = null;
            }

            return result;
        }

        public CEntityRain GetLastDayRain(string stationId, DateTime dt)
        {
            CEntityRain result = new CEntityRain();
            result.TimeRecieved = DateTime.Now;
            CTT_TableName = "rain" + dt.Year.ToString() + dt.Month.ToString() + (dt.Day > 15 ? "B" : "A");
            CTF_TableName = "rain" + dt.Year.ToString() + (dt.Day > 15 ? dt.Month.ToString() : dt.AddMonths(-1).Month.ToString()) + (dt.Day > 15 ? "A" : "B");
            string sql = "select top(1) totalrain,datatime from " + CF_TableName + " where stationid= " + stationId + " and datatime<= '" + dt + "' order by datatime desc;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            if (dataTableTmp.Rows.Count == 1)
            {
                try
                {
                    result.TotalRain = decimal.Parse((dataTableTmp.Rows[0])[CN_TotalRain].ToString());
                    result.TimeCollect = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
                }
                catch (Exception e)
                {

                }

            }
            if (dataTableTmp.Rows.Count == 0)
            {

            }

            return result;
        }

        public CEntityRain GetLastRain(string stationId, DateTime dt)
        {
            CEntityRain result = new CEntityRain();
            result.TimeRecieved = DateTime.Now;
            CTT_TableName = "rain" + dt.Year.ToString() + dt.Month.ToString() + (dt.Day > 15 ? "B" : "A");
            CTF_TableName = "rain" + dt.Year.ToString() + (dt.Day > 15 ? dt.Month.ToString() : dt.AddMonths(-1).Month.ToString()) + (dt.Day > 15 ? "A" : "B");
            string sql = "select top(1) totalrain,datatime from " + CF_TableName + " where stationid= " + stationId + " and datatime< '" + dt + "' order by datatime desc;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            if (dataTableTmp.Rows.Count == 1)
            {
                try
                {
                    result.TotalRain = decimal.Parse((dataTableTmp.Rows[0])[CN_TotalRain].ToString());
                    result.TimeCollect = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
                }
                catch
                {

                }

            }
            if (dataTableTmp.Rows.Count == 0)
            {

            }

            return result;
        }

        public CEntityRain GetLastSharpRain(string stationId, DateTime dt)
        {
            CEntityRain result = new CEntityRain();
            result.TimeRecieved = DateTime.Now;
            DateTime tmp = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
            CTT_TableName = "rain" + tmp.Year.ToString() + tmp.Month.ToString() + (tmp.Day > 15 ? "B" : "A");
            CTF_TableName = "rain" + tmp.Year.ToString() + (tmp.Day > 15 ? tmp.Month.ToString() : tmp.AddMonths(-1).Month.ToString()) + (tmp.Day > 15 ? "A" : "B");
            string sql = "select top(1) totalrain,datatime from " + CF_TableName + " where stationid= " + stationId + " and datatime= '" + tmp + "' order by datatime desc;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            if (dataTableTmp.Rows.Count == 1)
            {
                try
                {
                    result.TotalRain = decimal.Parse((dataTableTmp.Rows[0])[CN_TotalRain].ToString());
                    result.TimeCollect = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
                }
                catch (Exception e)
                {

                }

            }
            else
            {
                string sql2 = "select top(1) totalrain,datatime from " + CF_TableName + " where stationid= " + stationId + " and datatime< '" + tmp + "' order by datatime desc;";
                SqlDataAdapter adapter2 = new SqlDataAdapter(sql2, CDBManager.GetInstacne().GetConnection());
                DataTable dataTableTmp2 = new DataTable();
                adapter2.Fill(dataTableTmp2);
                if (dataTableTmp2.Rows.Count == 1)
                {
                    try
                    {
                        result.TotalRain = decimal.Parse((dataTableTmp2.Rows[0])[CN_TotalRain].ToString());
                        result.TimeCollect = DateTime.Parse(dataTableTmp2.Rows[0][CN_DataTime].ToString());
                    }
                    catch (Exception e)
                    {

                    }

                }
            }
            return result;
        }

        public decimal? GetLastClockSharpTotalRain(string stationId, DateTime TodayTime)
        {
            decimal? result = 0;
            DateTime tmp = TodayTime.Subtract(new TimeSpan(1, 0, 0));
            CTT_TableName = "rain" + tmp.Year.ToString() + tmp.Month.ToString() + (tmp.Day > 15 ? "B" : "A");
            CTF_TableName = "rain" + tmp.Year.ToString() + (tmp.Day > 15 ? tmp.Month.ToString() : tmp.AddMonths(-1).Month.ToString()) + (tmp.Day > 15 ? "A" : "B");
            string sql = "select top(1) * from " + CF_TableName + " where stationid= " + stationId + " and datatime< '" + tmp + "' order by datatime desc;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            if (dataTableTmp.Rows.Count == 1)
            {
                if (dataTableTmp.Rows[0][CN_TotalRain].ToString() != null)
                {
                    try
                    {
                        result = decimal.Parse(dataTableTmp.Rows[0][CN_TotalRain].ToString());
                    }
                    catch (Exception e)
                    {
                        result = null;
                    }

                }
            }
            else
            {
                result = null;
            }
            return result;
        }


        #region 帮助方法
        // 根据当前条件查询统计数据
        private void DoCountQuery()
        {
            CTF_TableName = "rain" + m_startTime.Year.ToString() + m_startTime.Month.ToString() + (m_startTime.Day > 15 ? "B" : "A");
            CTT_TableName = "rain" + m_endTime.Year.ToString() + m_endTime.Month.ToString() + (m_endTime.Day > 15 ? "B" : "A");
            CFF_TableName = "rain" + m_startTime.Year.ToString() + (m_startTime.Day <= 15 ? (m_startTime.Month.ToString() + "B") : ((m_startTime.Month + 1).ToString() + "A"));
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
                Debug.WriteLine(ex.ToString());
            }

        }

        // 将Map中由key指定的DataTable,从startRow开始返回界面最大行数的集合
        private List<CEntityRain> CopyDataToList(int key, int startRow)
        {
            List<CEntityRain> result = new List<CEntityRain>();
            try
            {
                // 取最小值 ，保证不越界
                int endRow = Math.Min(m_mapDataTable[key].Rows.Count, startRow + CDBParams.GetInstance().UIPageRowCount);
                DataTable table = m_mapDataTable[key];
                for (; startRow < endRow; ++startRow)
                {
                    CEntityRain rain = new CEntityRain();
                    //  rain.RainID = long.Parse(table.Rows[startRow][CN_RainID].ToString());
                    rain.StationID = table.Rows[startRow][CN_StationId].ToString();
                    rain.TimeCollect = DateTime.Parse(table.Rows[startRow][CN_DataTime].ToString());
                    if (!table.Rows[startRow][CN_PeriodRain].ToString().Equals(""))
                    {
                        rain.PeriodRain = Decimal.Parse(table.Rows[startRow][CN_PeriodRain].ToString());
                    }
                    if (!table.Rows[startRow][CN_DifferenceRain].ToString().Equals(""))
                    {
                        rain.DifferneceRain = Decimal.Parse(table.Rows[startRow][CN_DifferenceRain].ToString());
                    }
                    if (!table.Rows[startRow][CN_TotalRain].ToString().Equals(""))
                    {
                        rain.TotalRain = Decimal.Parse(table.Rows[startRow][CN_TotalRain].ToString());
                    }
                    rain.TimeRecieved = DateTime.Parse(table.Rows[startRow][CN_RecvDataTime].ToString());
                    rain.ChannelType = CEnumHelper.DBStrToChannelType(table.Rows[startRow][CN_TransType].ToString());
                    rain.MessageType = CEnumHelper.DBStrToMessageType(table.Rows[startRow][CN_MsgType].ToString());
                    // 日雨量，与前一日的差值
                    if (!table.Rows[startRow][CN_DayRain].ToString().Equals(""))
                    {
                        rain.DayRain = Decimal.Parse(table.Rows[startRow][CN_DayRain].ToString());
                    }
                    if (!table.Rows[startRow][CN_DataState].ToString().Equals(""))
                    {
                        // 数据状态
                        rain.BState = int.Parse(table.Rows[startRow][CN_DataState].ToString());
                    }
                    else
                    {
                        rain.BState = 1;
                    }
                    result.Add(rain);

                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("" + e.ToString());
            }
            return result;
        }

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
                string tname = "rain" + time.Year.ToString() + time.Month.ToString() + (time.Day > 15 ? "B" : "A");
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
                string tname = "rain" + time.Year.ToString() + time.Month.ToString() + (time.Day > 15 ? "B" : "A");
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
            if (batchInsertRainList == null || batchInsertRainList.Count == 0)
            {
                return;
            }
            List<CEntityRain> rainList = new List<CEntityRain>();
            lock (batchInsertRainList)
            {
                rainList.AddRange(batchInsertRainList);
                batchInsertRainList.Clear();
            }

            if (rainList.Count <= 500)
            {
                Dictionary<string, object> param = new Dictionary<string, object>();
                string suffix = "/rain/insertRain";
                string url = "http://" + urlPrefix + suffix;
                //string url = "http://127.0.0.1:8088/rain/insertRain";
                Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
                //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
                timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
                string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(rainList, Newtonsoft.Json.Formatting.None, timeConverter);
                param["rain"] = jsonStr;
                //string jsonStr = HttpHelper.ObjectToJson(rains);
                //param["rain"] = "[{\"BState\":1,\"ChannelType\":6,\"DayRain\":null,\"DifferneceRain\":null,\"MessageType\":2,\"PeriodRain\":null,\"RainID\":0,\"StationID\":\"3004\",\"TimeCollect\":\"2019-3-13 11:00:00\",\"TimeRecieved\":\"2019-3-13 11:15:00\",\"TotalRain\":4}]";
                try
                {
                    string resultJson = HttpHelper.Post(url, param);
                    CDBLog.Instance.AddInfo(string.Format("添加{0}行到雨量表", rainList.Count));
                }
                catch (Exception e)
                {
                    Debug.WriteLine("添加雨量信息失败");
                    CDBLog.Instance.AddInfo(string.Format("添加数据到水位表失败", rainList.Count));
                    return;
                }
                finally
                {
                    rainList.Clear();
                }
            }
            else
            {
                CDBLog.Instance.AddInfo(string.Format("数据量大于500条，则分批插入", rainList.Count));
                List<CEntityRain> rains = new List<CEntityRain>();

                for (int i = 0; i < rainList.Count; i++)
                {
                    rains.Add(rainList[i]);
                    if ((i+1) % 500 == 0)
                    {
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        string suffix = "/rain/insertRain";
                        string url = "http://" + urlPrefix + suffix;
                        //string url = "http://127.0.0.1:8088/rain/insertRain";
                        Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
                        //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
                        timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
                        string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(rains, Newtonsoft.Json.Formatting.None, timeConverter);
                        param["rain"] = jsonStr;
                        //string jsonStr = HttpHelper.ObjectToJson(rains);
                        //param["rain"] = "[{\"BState\":1,\"ChannelType\":6,\"DayRain\":null,\"DifferneceRain\":null,\"MessageType\":2,\"PeriodRain\":null,\"RainID\":0,\"StationID\":\"3004\",\"TimeCollect\":\"2019-3-13 11:00:00\",\"TimeRecieved\":\"2019-3-13 11:15:00\",\"TotalRain\":4}]";
                        try
                        {
                            string resultJson = HttpHelper.Post(url, param);
                            CDBLog.Instance.AddInfo(string.Format("添加{0}行到雨量表", rains.Count));
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("添加雨量信息失败");
                            CDBLog.Instance.AddInfo(string.Format("添加雨量信息失败", rains.Count));
                            return;
                        }
                        finally
                        {
                            rains.Clear();
                        }
                    }
                    if (i == rainList.Count - 1)
                    {
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        string suffix = "/rain/insertRain";
                        string url = "http://" + urlPrefix + suffix;
                        //string url = "http://127.0.0.1:8088/rain/insertRain";
                        Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
                        //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
                        timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
                        string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(rains, Newtonsoft.Json.Formatting.None, timeConverter);
                        param["rain"] = jsonStr;
                        //string jsonStr = HttpHelper.ObjectToJson(rains);
                        //param["rain"] = "[{\"BState\":1,\"ChannelType\":6,\"DayRain\":null,\"DifferneceRain\":null,\"MessageType\":2,\"PeriodRain\":null,\"RainID\":0,\"StationID\":\"3004\",\"TimeCollect\":\"2019-3-13 11:00:00\",\"TimeRecieved\":\"2019-3-13 11:15:00\",\"TotalRain\":4}]";
                        try
                        {
                            string resultJson = HttpHelper.Post(url, param);
                            CDBLog.Instance.AddInfo(string.Format("添加{0}行到雨量表", rains.Count));
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("添加雨量信息失败");
                            CDBLog.Instance.AddInfo(string.Format("添加雨量信息失败", rains.Count));
                            return;
                        }
                        finally
                        {
                            rains.Clear();
                            rainList.Clear();
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
                    //1126
                    // 雨量表有插入触发器，如果遇到重复记录，则更新为当前的最新记录
                    //bulkCopy.BatchSize = 1;
                    bulkCopy.BulkCopyTimeout = 1800;
                    bulkCopy.DestinationTableName = tname;
                    //bulkCopy.ColumnMappings.Add(CN_RainID, CN_RainID);
                    bulkCopy.ColumnMappings.Add(CN_StationId, CN_StationId);
                    bulkCopy.ColumnMappings.Add(CN_DataTime, CN_DataTime);
                    bulkCopy.ColumnMappings.Add(CN_PeriodRain, CN_PeriodRain);
                    bulkCopy.ColumnMappings.Add(CN_DifferenceRain, CN_DifferenceRain);
                    bulkCopy.ColumnMappings.Add(CN_TotalRain, CN_TotalRain);
                    bulkCopy.ColumnMappings.Add(CN_DayRain, CN_DayRain);
                    bulkCopy.ColumnMappings.Add(CN_TransType, CN_TransType);
                    bulkCopy.ColumnMappings.Add(CN_MsgType, CN_MsgType);

                    bulkCopy.ColumnMappings.Add(CN_RecvDataTime, CN_RecvDataTime);
                    bulkCopy.ColumnMappings.Add(CN_DataState, CN_DataState);

                    try
                    {
                        bulkCopy.WriteToServer(dt);
                        Debug.WriteLine("###{0} :add {1} lines to rain db", DateTime.Now, dt.Rows.Count);
                        CDBLog.Instance.AddInfo(string.Format("添加{0}行到雨量表", dt.Rows.Count));
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

        #endregion ///<HELP_METHOD

        //1009gm
        public List<CEntityRain> QueryAccTime(DateTime time)
        {
            DateTime startTime = time;
            DateTime endTime1 = time.AddMinutes(59).AddSeconds(59);
            DateTime endTime2 = time.AddHours(24);
            List<CEntityRain> results = new List<CEntityRain>();
            //string sqlAcc = "select * from HydrologytestDB.dbo.Rain where Datatime between '2010-01-05 01:00:00.000' and '2010-01-05 01:59:59.000'; ";
            //string sql = "select * from " + CT_TableName + " where Datetime between startTime and endTime;";
            CTF_TableName = "rain" + time.Year.ToString() + time.Month.ToString() + (time.Day > 15 ? "B" : "A");
            string sql = "select * from " + CTF_TableName + " where DataTime between '2010-01-05 16:40:56' and '2010-01-05 17:40:56';";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTemp = new DataTable();
            adapter.Fill(dataTableTemp);
            int i = dataTableTemp.Columns.Count;
            foreach (DataColumn dc in dataTableTemp.Columns)
            {
                string temp = dc.ColumnName;
            }

            for (int rowid = 0; rowid < dataTableTemp.Rows.Count; ++rowid)
            {
                CEntityRain rain = new CEntityRain();
                rain.StationID = dataTableTemp.Rows[rowid][CN_StationId].ToString();
                rain.TimeCollect = DateTime.Parse(dataTableTemp.Rows[rowid][CN_DataTime].ToString());
                if (dataTableTemp.Rows[rowid][CN_PeriodRain].ToString() != "")
                {
                    rain.PeriodRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_PeriodRain].ToString());
                }
                else
                {
                    rain.PeriodRain = -9999;
                }
                if (dataTableTemp.Rows[rowid][CN_TotalRain].ToString() != "")
                {
                    rain.TotalRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_TotalRain].ToString());
                }
                else
                {
                    rain.TotalRain = -9999;
                }

                results.Add(rain);
            }
            return results;
        }
        //gm
        public List<CEntityRain> QueryAccTimeAndStation(string StationId, DateTime time)
        {
            CTF_TableName = "rain" + time.Year.ToString() + time.Month.ToString() + "A";
            CFF_TableName = "rain" + time.Year.ToString() + (time.Month + 1).ToString() + "A";
            CTT_TableName = "rain" + time.Year.ToString() + time.Month.ToString() + "B";
            string sql = "select * from " + CM_TableName + " where Datatime in('";
            for (int j = 0; j < 23; j++)
            {
                sql = sql + time + "','";
                time = time.AddHours(1);
            }
            sql = sql + time + "') and StationID=" + StationId + ";";
            //DateTime startTime = time;
            //DateTime endTime1 = time.AddMinutes(59).AddSeconds(59);
            List<CEntityRain> results = new List<CEntityRain>();
            //string sql = "select * from HydrologytestDB.dbo.Rain where DataTime= '" + startTime + "' and StationID="  + StationId + ";";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTemp = new DataTable();
            adapter.Fill(dataTableTemp);
            int flag = dataTableTemp.Rows.Count;
            if (flag == 0)
            {
            }
            else
            {
                for (int rowid = 0; rowid < dataTableTemp.Rows.Count; ++rowid)
                {
                    CEntityRain rain = new CEntityRain();
                    rain.StationID = dataTableTemp.Rows[rowid][CN_StationId].ToString();
                    rain.TimeCollect = DateTime.Parse(dataTableTemp.Rows[rowid][CN_DataTime].ToString());
                    if (dataTableTemp.Rows[rowid][CN_PeriodRain].ToString() != "")
                    {
                        rain.PeriodRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_PeriodRain].ToString());
                    }
                    else
                    {
                        rain.PeriodRain = -9999;
                    }
                    if (dataTableTemp.Rows[rowid][CN_TotalRain].ToString() != "")
                    {
                        rain.TotalRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_TotalRain].ToString());
                    }
                    else
                    {
                        rain.TotalRain = -9999;
                    }
                    results.Add(rain);
                }
            }
            return results;

        }

        public List<CEntityRain> QueryForMonthTable(string StationId, DateTime time)
        {
            CTF_TableName = "rain" + time.Year.ToString() + time.Month.ToString() + "A";
            CFF_TableName = "rain" + time.Year.ToString() + (time.Month + 1).ToString() + "A";
            CTT_TableName = "rain" + time.Year.ToString() + time.Month.ToString() + "B";
            string sql = "select * from " + CM_TableName + " where Datatime in('";
            for (int j = 0; j < 23; j++)
            {
                sql = sql + time + "','";
                time = time.AddHours(1);
            }
            sql = sql + time + "') and StationID=" + StationId + ";";
            //DateTime startTime = time;
            //DateTime endTime1 = time.AddMinutes(59).AddSeconds(59);
            List<CEntityRain> results = new List<CEntityRain>();
            //string sql = "select * from HydrologytestDB.dbo.Rain where DataTime= '" + startTime + "' and StationID="  + StationId + ";";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTemp = new DataTable();
            adapter.Fill(dataTableTemp);
            int flag = dataTableTemp.Rows.Count;
            if (flag == 0)
            {

            }
            else
            {
                for (int rowid = 0; rowid < dataTableTemp.Rows.Count; ++rowid)
                {
                    CEntityRain rain = new CEntityRain();
                    rain.StationID = dataTableTemp.Rows[rowid][CN_StationId].ToString();
                    rain.TimeCollect = DateTime.Parse(dataTableTemp.Rows[rowid][CN_DataTime].ToString());
                    if (dataTableTemp.Rows[rowid][CN_PeriodRain].ToString() != "")
                    {
                        rain.PeriodRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_PeriodRain].ToString());
                    }
                    else
                    {
                        rain.PeriodRain = -9999;
                    }

                    if (dataTableTemp.Rows[rowid][CN_TotalRain].ToString() != "")
                    {
                        rain.TotalRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_TotalRain].ToString());
                    }
                    else
                    {
                        rain.TotalRain = -9999;
                    }

                    results.Add(rain);
                }
            }
            return results;
        }

        public List<CEntityRain> QueryForYearTable(string StationId, DateTime time)
        {
            List<CEntityRain> results = new List<CEntityRain>();
            DateTime start = time;
            DateTime end = time.AddMonths(1);
            CTF_TableName = "rain" + time.Year.ToString() + time.Month.ToString() + "A";
            CFF_TableName = "rain" + time.Year.ToString() + (time.Month + 1).ToString() + "A";
            CTT_TableName = "rain" + time.Year.ToString() + time.Month.ToString() + "B";
            string sql = "select * from " + CM_TableName + " where Datatime between '" + start + "' and '" + end + "' and messagetype = 8 and stationid = " + StationId + ";";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTemp = new DataTable();
            adapter.Fill(dataTableTemp);
            int flag = dataTableTemp.Rows.Count;
            if (flag == 0)
            {

            }
            else
            {
                for (int rowid = 0; rowid < dataTableTemp.Rows.Count; ++rowid)
                {
                    CEntityRain rain = new CEntityRain();
                    rain.StationID = dataTableTemp.Rows[rowid][CN_StationId].ToString();
                    rain.TimeCollect = DateTime.Parse(dataTableTemp.Rows[rowid][CN_DataTime].ToString());
                    if (dataTableTemp.Rows[rowid][CN_PeriodRain].ToString() != "")
                    {
                        rain.PeriodRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_PeriodRain].ToString());
                    }
                    else
                    {
                        rain.PeriodRain = -9999;
                    }
                    if (dataTableTemp.Rows[rowid][CN_TotalRain].ToString() != "")
                    {
                        rain.TotalRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_TotalRain].ToString());
                    }
                    else
                    {
                        rain.TotalRain = -9999;
                    }
                    results.Add(rain);
                }
            }

            return results;
        }

        public List<CEntityRain> QueryForSoil(string StationId, DateTime start, DateTime end)
        {
            List<CEntityRain> results = new List<CEntityRain>();
            CTF_TableName = "rain" + start.Year.ToString() + start.Month.ToString() + "A";
            CFF_TableName = "rain" + start.Year.ToString() + (start.Month + 1).ToString() + "A";
            CTT_TableName = "rain" + start.Year.ToString() + start.Month.ToString() + "B";
            string sql = "select * from " + CM_TableName + " where Datatime between '" + start + "' and '" + end + "' and messagetype = 8 and stationid = " + StationId + ";";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTemp = new DataTable();
            adapter.Fill(dataTableTemp);
            int flag = dataTableTemp.Rows.Count;
            if (flag == 0)
            {

            }
            else
            {
                for (int rowid = 0; rowid < dataTableTemp.Rows.Count; ++rowid)
                {
                    CEntityRain rain = new CEntityRain();
                    rain.StationID = dataTableTemp.Rows[rowid][CN_StationId].ToString();
                    rain.TimeCollect = DateTime.Parse(dataTableTemp.Rows[rowid][CN_DataTime].ToString());
                    if (dataTableTemp.Rows[rowid][CN_PeriodRain].ToString() != "")
                    {
                        rain.PeriodRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_PeriodRain].ToString());
                    }
                    else
                    {
                        rain.PeriodRain = -9999;
                    }
                    if (dataTableTemp.Rows[rowid][CN_TotalRain].ToString() != "")
                    {
                        rain.TotalRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_TotalRain].ToString());
                    }
                    else
                    {
                        rain.TotalRain = -9999;
                    }
                    results.Add(rain);
                }
            }

            return results;
        }

        public bool checkRainIsExists(string stationid, DateTime time)
        {
            bool flag = false;
            CTF_TableName = "rain" + time.Year.ToString() + time.Month.ToString() + (time.Day > 15 ? "B" : "A");
            string sql = "select count(totalrain) from " + CTF_TableName + "where stationid= " + stationid + "and datatime= '" + time + "';";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTemp = new DataTable();
            adapter.Fill(dataTableTemp);
            for (int rowid = 0; rowid < dataTableTemp.Rows.Count; ++rowid)
            {
                int count = int.Parse(dataTableTemp.Rows[rowid][0].ToString());
                if (count > 0)
                {
                    flag = true;
                }
            }
            return flag;
        }

        public bool UpdateRows_1(List<Hydrology.Entity.CEntityRain> rains, int index)
        {
            // 除主键外和站点外，其余信息随意修改
            StringBuilder sql = new StringBuilder();
            int currentBatchCount = 0;
            for (int i = index; i < rains.Count; i++)
            {
                ++currentBatchCount;
                CTF_TableName = "rain" + rains[i].TimeCollect.Year.ToString() + rains[i].TimeCollect.Month.ToString() + (rains[i].TimeCollect.Day > 15 ? "B" : "A");
                sql.AppendFormat("update {0} set {1}={2},{3}={4},{5}={6},{7}={8}  where {9}={10} and {11}='{12}';",
                    CTF_TableName,
                    CN_PeriodRain, rains[i].PeriodRain.HasValue ? rains[i].PeriodRain.Value.ToString() : "null",
                    CN_DifferenceRain, rains[i].DifferneceRain.HasValue ? rains[i].DifferneceRain.Value.ToString() : "null",
                    CN_DayRain, rains[i].DayRain.HasValue ? rains[i].DayRain.Value.ToString() : "null",
                    CN_DataState, rains[i].BState,
                    CN_StationId, rains[i].StationID,
                    CN_DataTime, rains[i].TimeCollect.ToString()
                );
                if (currentBatchCount >= CDBParams.GetInstance().UpdateBufferMax)
                {
                    // 更新数据库
                    if (!this.ExecuteSQLCommand(sql.ToString()))
                    {
                        // 保存失败
                        return false;
                    }
                    sql.Clear(); //清除以前的所有命令
                    currentBatchCount = 0;
                }
            }
            // 更新数据库
            if (!this.ExecuteSQLCommand(sql.ToString()))
            {
                return false;
            }
            ResetAll();
            return true;
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
                sqlcreate.AppendFormat("if not exists (select * from sysobjects where id = object_id('{0}') and OBJECTPROPERTY(id, 'IsUserTable') = 1) CREATE TABLE [{1}] ([stationid][char](4) NOT NULL,[datatime][datetime] NOT NULL,[periodrain][numeric](18, 1) NULL,[differencerain][numeric](18, 1) NULL,[totalrain][numeric](18, 1) NULL,[dayrain][numeric](18, 1) NULL,[transtype][char](2) NULL,[messagetype][char](1) NULL,[recvdatatime][datetime] NULL,[state][int] NULL,CONSTRAINT [{2}] PRIMARY KEY CLUSTERED ( [stationid] ASC, [datatime] ASC)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]) ON[PRIMARY]; ",
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
                //sqlcreate.AppendFormat("if not exists (select * from sysobjects where id = object_id('{0}') and OBJECTPROPERTY(id, 'IsUserTable') = 1) CREATE TABLE [{1}] ([stationid][char](4) NOT NULL,[datatime][datetime] NOT NULL,[periodrain][numeric](18, 1) NULL,[differencerain][numeric](18, 1) NULL,[totalrain][numeric](18, 1) NULL,[dayrain][numeric](18, 1) NULL,[transtype][char](2) NULL,[messagetype][char](1) NULL,[recvdatatime][datetime] NULL,[state][int] NULL,CONSTRAINT [{2}] PRIMARY KEY CLUSTERED ( [stationid] ASC, [datatime] ASC)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]) ON[PRIMARY]; ",
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
                sqlAlter2.Clear();
                sqlcreate2.Clear();
                sqlcreate2.AppendFormat("if not exists (select * from sysobjects where id = object_id('{0}') and OBJECTPROPERTY(id, 'IsUserTable') = 1) CREATE TABLE [{1}] ([stationid][char](4) NOT NULL,[datatime][datetime] NOT NULL,[periodrain][numeric](18, 1) NULL,[differencerain][numeric](18, 1) NULL,[totalrain][numeric](18, 1) NULL,[dayrain][numeric](18, 1) NULL,[transtype][char](2) NULL,[messagetype][char](1) NULL,[recvdatatime][datetime] NULL,[state][int] NULL,CONSTRAINT [{2}] PRIMARY KEY CLUSTERED ( [stationid] ASC, [datatime] ASC)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]) ON[PRIMARY]; ",
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

        public void AddOrUpdate(List<Hydrology.Entity.CEntityRain> rains)
        {
            m_mutexDataTable.WaitOne(); //等待互斥量
            foreach (CEntityRain rain in rains)
            {
                try
                {
                    DataRow row = m_tableDataAdded.NewRow();
                    row[CN_StationId] = rain.StationID;
                    row[CN_DataTime] = rain.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                    row[CN_PeriodRain] = rain.PeriodRain;
                    row[CN_DifferenceRain] = rain.DifferneceRain;
                    row[CN_TotalRain] = rain.TotalRain;
                    row[CN_DayRain] = rain.DayRain;
                    row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(rain.ChannelType);
                    row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(rain.MessageType);
                    row[CN_RecvDataTime] = rain.TimeRecieved.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                    row[CN_DataState] = rain.BState;
                    m_tableDataAdded.Rows.Add(row);
                    // 判断是否需要创建新分区
                    //CSQLPartitionMgr.Instance.MaintainRain(rain.TimeCollect);

                    NewTask(() => { AddDataToDB(); });

                }
                catch (Exception e)
                {
                    StringBuilder sql = new StringBuilder();

                    CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
                    sql.AppendFormat("update {0} set {1}={2},{3}={4},{5}={6},{7}={8},{9}={10},{11}={12},{13}='{14}',{15} ='{16}' where {17}={18} and {19}='{20}';",
                            CTF_TableName,
                            //  CN_DataTime, DateTimeToDBStr(rains[i].TimeCollect),
                            CN_PeriodRain, rain.PeriodRain.HasValue ? rain.PeriodRain.Value.ToString() : "null",
                            CN_TotalRain, rain.TotalRain.HasValue ? rain.TotalRain.Value.ToString() : "null",
                            CN_DifferenceRain, rain.DifferneceRain.HasValue ? rain.DifferneceRain.Value.ToString() : "null",
                            CN_DayRain, rain.DayRain.HasValue ? rain.DayRain.Value.ToString() : "null",
                            CN_TransType, CEnumHelper.ChannelTypeToDBStr(rain.ChannelType),
                            CN_MsgType, CEnumHelper.MessageTypeToDBStr(rain.MessageType),
                            CN_RecvDataTime, rain.TimeRecieved.ToString(),
                            CN_DataState, rain.BState,
                            //CN_RainID, rains[i].RainID
                            CN_StationId, rain.StationID,
                            CN_DataTime, rain.TimeCollect.ToString()
                        );
                    if (!this.ExecuteSQLCommand(sql.ToString()))
                    {
                        // 保存失败
                        return;
                    }
                    sql.Clear(); //清除以前的所有命令
                }

                ResetAll();
            }

            m_mutexDataTable.ReleaseMutex();
        }

        public List<CEntityRain> getListRainForUpdate(string stationid, DateTime start, DateTime end)
        {
            List<CEntityRain> results = new List<CEntityRain>();
            CTF_TableName = "rain" + start.Year.ToString() + start.Month.ToString() + (start.Day > 15 ? "B" : "A");
            CTT_TableName = "rain" + end.Year.ToString() + end.Month.ToString() + (end.Day > 15 ? "B" : "A");
            string sql = "select * from " + CF_TableName + " where Datatime between '" + start + "' and '" + end + "' and  stationid = " + stationid + ";";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTemp = new DataTable();
            adapter.Fill(dataTableTemp);
            int flag = dataTableTemp.Rows.Count;
            for (int rowid = 0; rowid < dataTableTemp.Rows.Count; ++rowid)
            {
                CEntityRain rain = new CEntityRain();
                rain.StationID = dataTableTemp.Rows[rowid][CN_StationId].ToString();
                rain.TimeCollect = DateTime.Parse(dataTableTemp.Rows[rowid][CN_DataTime].ToString());
                rain.TotalRain = decimal.Parse(dataTableTemp.Rows[rowid][CN_TotalRain].ToString());
                results.Add(rain);
            }
            return results;
        }

        public List<string> getUpdateStations(DateTime start, DateTime end)
        {
            List<string> results = new List<string>();
            CTF_TableName = "rain" + start.Year.ToString() + start.Month.ToString() + (start.Day > 15 ? "B" : "A");
            CTT_TableName = "rain" + end.Year.ToString() + end.Month.ToString() + (end.Day > 15 ? "B" : "A");
            string sql = "select distinct stationid  from " + CF_TableName + " where Datatime between '" + start + "' and '" + end + "' and  state = 2 " + ";";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTemp = new DataTable();
            adapter.Fill(dataTableTemp);
            for (int rowid = 0; rowid < dataTableTemp.Rows.Count; ++rowid)
            {
                string result = dataTableTemp.Rows[rowid][0].ToString();
                results.Add(result);
            }
            return results;
        }

        public List<DateTime> getExistsTime(string stationid, DateTime start, DateTime end)
        {
            List<DateTime> results = new List<DateTime>();
            CTF_TableName = "rain" + start.Year.ToString() + start.Month.ToString() + (start.Day > 15 ? "B" : "A");
            CTT_TableName = "rain" + end.Year.ToString() + end.Month.ToString() + (end.Day > 15 ? "B" : "A");
            string sql = "select datatime from " + CF_TableName + " where stationid= " + stationid + " and datatime>= '" + start + "' and datatime<=  '" + end + "' order by datatime;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTemp = new DataTable();
            adapter.Fill(dataTableTemp);
            for (int rowid = 0; rowid < dataTableTemp.Rows.Count; ++rowid)
            {
                DateTime dt = DateTime.Parse(dataTableTemp.Rows[rowid][0].ToString());
                results.Add(dt);
            }
            return results;
        }

        public void AddNewRows_DataModify(List<Hydrology.Entity.CEntityRain> rains)
        {
            if (rains.Count <= 0)
            {
                return;
            }
            batchInsertRainList.AddRange(rains);

            //Dictionary<string, object> param = new Dictionary<string, object>();
            //string suffix = "/rain/insertRain";
            //string url = "http://" + urlPrefix + suffix;
            ////string url = "http://127.0.0.1:8088/rain/insertRain";
            //Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            ////这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
            //timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            //string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(rains, Newtonsoft.Json.Formatting.None, timeConverter);
            //param["rain"] = jsonStr;
            ////string jsonStr = HttpHelper.ObjectToJson(rains);
            ////param["rain"] = "[{\"BState\":1,\"ChannelType\":6,\"DayRain\":null,\"DifferneceRain\":null,\"MessageType\":2,\"PeriodRain\":null,\"RainID\":0,\"StationID\":\"3004\",\"TimeCollect\":\"2019-3-13 11:00:00\",\"TimeRecieved\":\"2019-3-13 11:15:00\",\"TotalRain\":4}]";
            //try
            //{
            //    string resultJson = HttpHelper.Post(url, param);
            //    CDBLog.Instance.AddInfo(string.Format("添加{0}行到雨量表", rains.Count));
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine("添加雨量信息失败");
            //}
            //// 记录超过1000条，或者时间超过1分钟，就将当前的数据写入数据库
            //m_mutexDataTable.WaitOne(); //等待互斥量
            //foreach (CEntityRain rain in rains)
            //{
            //    DataRow row = m_tableDataAdded.NewRow();
            //    row[CN_StationId] = rain.StationID;
            //    row[CN_DataTime] = rain.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
            //    row[CN_PeriodRain] = rain.PeriodRain;
            //    row[CN_DifferenceRain] = rain.DifferneceRain;
            //    row[CN_TotalRain] = rain.TotalRain;
            //    row[CN_DayRain] = rain.DayRain;

            //    row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(rain.ChannelType);
            //    row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(rain.MessageType);

            //    row[CN_RecvDataTime] = rain.TimeRecieved.ToString(CDBParams.GetInstance().DBDateTimeFormat);

            //    row[CN_DataState] = rain.BState;
            //    m_tableDataAdded.Rows.Add(row);

            //    // 判断是否需要创建新分区
            //    // CSQLPartitionMgr.Instance.MaintainRain(rain.TimeCollect);
            //}

            //// 如果超过最大值，写入数据库
            //NewTask(() => { AddDataToDB(); });

            //// AddDataToDB(); 
            //m_mutexDataTable.ReleaseMutex();
        }
        public bool UpdateRows_DataModify(List<CEntityRain> rains)
        {
            if (rains.Count <= 0)
            {
                return true;
            }
            Dictionary<string, object> param = new Dictionary<string, object>();
            string suffix = "/rain/updateRain";
            string url = "http://" + urlPrefix + suffix;
            //string url = "http://127.0.0.1:8088/rain/updateRain";
            Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(rains, Newtonsoft.Json.Formatting.None, timeConverter);
            //param["rain"] = "[{\"BState\":2,\"ChannelType\":15,\"DayRain\":4,\"DifferneceRain\":4,\"MessageType\":8,\"PeriodRain\":4,\"RainID\":0,\"StationID\":\"3004\",\"TimeCollect\":\"2019/3/13 11:00:00\",\"TimeRecieved\":\"2019/3/13 12:45:00\",\"TotalRain\":44}]";
            param["rain"] = jsonStr;
            try
            {
                string resultJson = HttpHelper.Post(url, param);
            }
            catch (Exception e)
            {
                Debug.WriteLine("更新雨量信息失败");
                return false;
            }
            return true;
            //// 除主键外和站点外，其余信息随意修改
            //StringBuilder sql = new StringBuilder();
            ////  int currentBatchCount = 0;
            //for (int i = 0; i < rains.Count; i++)
            //{
            //    //   ++currentBatchCount;
            //    //第一个修改，下一条最近的数据
            //    CTF_TableName = "rain" + rains[i].TimeCollect.Year.ToString() + rains[i].TimeCollect.Month.ToString() + (rains[1].TimeCollect.Day > 15 ? "B" : "A");
            //    sql.AppendFormat("update {0} set {1}={2},{3}={4},{5}={6},{7}={8},{9}={10},{11}={12},{13}='{14}',{15} ='{16}' where {17}={18} and {19}='{20}';",
            //        CTF_TableName,
            //        //  CN_DataTime, DateTimeToDBStr(rains[i].TimeCollect),
            //        CN_PeriodRain, rains[i].PeriodRain.HasValue ? rains[i].PeriodRain.Value.ToString() : "null",
            //        CN_TotalRain, rains[i].TotalRain.HasValue ? rains[i].TotalRain.Value.ToString() : "null",
            //        CN_DifferenceRain, rains[i].DifferneceRain.HasValue ? rains[i].DifferneceRain.Value.ToString() : "null",
            //        CN_DayRain, rains[i].DayRain.HasValue ? rains[i].DayRain.Value.ToString() : "null",
            //        CN_TransType, CEnumHelper.ChannelTypeToDBStr(rains[i].ChannelType),
            //        CN_MsgType, CEnumHelper.MessageTypeToDBStr(rains[i].MessageType),
            //        CN_RecvDataTime, rains[i].TimeRecieved.ToString(),
            //        CN_DataState, rains[i].BState,
            //        //CN_RainID, rains[i].RainID
            //        CN_StationId, rains[i].StationID,
            //        CN_DataTime, rains[i].TimeCollect.ToString()
            //    );

            //}
            //    if (!this.ExecuteSQLCommand(sql.ToString()))
            //    {
            //        return false;
            //    }
            //    ResetAll();

            //    for (int i = 0; i < rains.Count; i++)
            //    {
            //        if (rains[i].TimeCollect.Hour == 8 && rains[i].TimeCollect.Minute + rains[i].TimeCollect.Second == 0)
            //        {
            //            //修改当前8点整点的日雨量
            //            CEntityRain result1 = GetLastDayRain_1(rains[i]);
            //            if (result1 != null)
            //            {
            //                rains[i].DayRain = rains[i].TotalRain - result1.TotalRain;
            //                UpdateThisDayRain(rains[i]);
            //            }
            //        }
            //        if (rains[i].TimeCollect.Minute + rains[i].TimeCollect.Second == 0)
            //        {
            //            //修改本条整点的时段雨量
            //            CEntityRain result2 = GetLastSharpRain_1(rains[i]);
            //            if (result2 != null)
            //            {
            //                rains[i].PeriodRain = rains[i].TotalRain - result2.TotalRain;
            //                UpdateThisSharpRain(rains[i]);
            //            }
            //        }

            //        //修改本条数据的差值雨量
            //        CEntityRain result3 = GetLastRain_1(rains[i]);
            //        if (result3 != null)
            //        {
            //            rains[i].DifferneceRain = rains[i].TotalRain - result3.TotalRain;
            //            UpdateThisRain(rains[i]);
            //        }
            //    }
            //    return true;
        }
        public bool UpdateOtherRows_DataModify(List<CEntityRain> rains)
        {
            for (int i = 0; i < rains.Count; i++)
            {
                if (rains[i].TimeCollect.Hour == 8 && rains[i].TimeCollect.Minute + rains[i].TimeCollect.Second == 0)
                {
                    //修改下一条8点的日雨量
                    CEntityRain result1 = GetNextDayRain(rains[i]);
                    if (result1 != null)
                    {
                        result1.DayRain = result1.TotalRain - rains[i].TotalRain;
                        UpdateNextDayRain(result1);
                    }
                }
                if (rains[i].TimeCollect.Minute + rains[i].TimeCollect.Second == 0)
                {
                    //修改最近一条整点的时段雨量
                    CEntityRain result2 = GetNextSharpRain(rains[i]);
                    if (result2 != null)
                    {
                        result2.PeriodRain = result2.TotalRain - rains[i].TotalRain;
                        UpdateNextSharpRain(result2);
                    }
                }

                //修改最近一条数据的差值雨量
                CEntityRain result3 = GetNextRain(rains[i]);
                if (result3 != null)
                {
                    result3.DifferneceRain = result3.TotalRain - rains[i].TotalRain;
                    UpdateNextRain(result3);
                }

            }
            return true;
        }

        public bool UpdateOtherRows_DataModify_1(List<CEntityRain> rains)
        {
            for (int i = 0; i < rains.Count; i++)
            {
                if (rains[i].TimeCollect.Hour == 8 && rains[i].TimeCollect.Minute + rains[i].TimeCollect.Second == 0)
                {
                    //修改下一条8点的日雨量
                    CEntityRain result1 = GetNextDayRain(rains[i]);
                    CEntityRain result2 = GetLastRain_1(rains[i]);
                    if (result1 != null)
                    {
                        result1.DayRain = result1.TotalRain - result2.TotalRain;
                        UpdateNextDayRain(result1);
                    }
                }
                if (rains[i].TimeCollect.Minute + rains[i].TimeCollect.Second == 0)
                {
                    //修改最近一条整点的时段雨量
                    CEntityRain result2 = GetNextSharpRain(rains[i]);
                    CEntityRain result4 = GetLastRain_1(rains[i]);
                    if (result2 != null)
                    {
                        result2.PeriodRain = result2.TotalRain - result4.TotalRain;
                        UpdateNextSharpRain(result2);
                    }
                }

                //修改最近一条数据的差值雨量
                CEntityRain result3 = GetNextRain(rains[i]);
                CEntityRain result5 = GetLastRain_1(rains[i]);
                if (result3 != null)
                {
                    result3.DifferneceRain = result3.TotalRain - result5.TotalRain;
                    UpdateNextRain(result3);
                }

            }
            return true;
        }

        public CEntityRain GetNextRain(CEntityRain rain)
        {
            CEntityRain result = new CEntityRain();
            CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
            string sql = "select top(1) stationid,totalrain,datatime from " + CTF_TableName + " where stationid= " + rain.StationID + " and datatime> '" + rain.TimeCollect + "' order by datatime asc;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            if (dataTableTmp.Rows.Count == 1)
            {
                result.StationID = dataTableTmp.Rows[0][CN_StationId].ToString();
                result.TotalRain = decimal.Parse((dataTableTmp.Rows[0])[CN_TotalRain].ToString());
                result.TimeCollect = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
            }
            if (dataTableTmp.Rows.Count == 0)
            {
                result = null;
            }

            return result;
        }
        public CEntityRain GetNextSharpRain(CEntityRain rain)
        {
            CEntityRain result = new CEntityRain();
            // string sql = "select top(1) stationid,totalrain,datatime from " + CT_TableName + " where periodrain is not null and stationid= " + rain.StationID + " and datatime> '" + rain.TimeCollect + "' order by datatime asc;";
            CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
            string sql = "select stationid,totalrain,datatime from " + CTF_TableName + " where stationid= " + rain.StationID + " and datatime= '" + rain.TimeCollect.AddHours(1) + "' order by datatime asc;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            if (dataTableTmp.Rows.Count == 1)
            {
                result.StationID = dataTableTmp.Rows[0][CN_StationId].ToString();
                result.TotalRain = decimal.Parse((dataTableTmp.Rows[0])[CN_TotalRain].ToString());
                result.TimeCollect = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
            }
            if (dataTableTmp.Rows.Count == 0)
            {
                //string sql1 = "select stationid,totalrain,datatime from " + CT_TableName + " where stationid= " + rain.StationID + " and datatime= '" + rain.TimeCollect.AddHours(1) + "' order by datatime asc;";
                //SqlDataAdapter adapter1 = new SqlDataAdapter(sql1, CDBManager.GetInstacne().GetConnection());
                //DataTable dataTableTmp1 = new DataTable();
                //adapter.Fill(dataTableTmp);
                //if (dataTableTmp1.Rows.Count == 1)
                //{
                //    result.StationID = dataTableTmp1.Rows[0][CN_StationId].ToString();
                //    result.TotalRain = decimal.Parse((dataTableTmp1.Rows[0])[CN_TotalRain].ToString());
                //    result.TimeCollect = DateTime.Parse(dataTableTmp1.Rows[0][CN_DataTime].ToString());
                //}
                //else
                //{
                result = null;
                //}
            }
            return result;
        }
        public CEntityRain GetNextDayRain(CEntityRain rain)
        {
            CEntityRain result = new CEntityRain();
            // string sql = "select top(1) stationid,totalrain,datatime from " + CT_TableName + " where dayrain is not null and stationid= " + rain.StationID + " and datatime> '" + rain.TimeCollect + "' order by datatime asc;";
            CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
            string sql = "select top(1) stationid,totalrain,datatime from " + CTF_TableName + " where stationid= " + rain.StationID + " and datatime= '" + rain.TimeCollect.AddDays(1) + "' order by datatime asc;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            if (dataTableTmp.Rows.Count == 1)
            {
                result.StationID = dataTableTmp.Rows[0][CN_StationId].ToString();
                result.TotalRain = decimal.Parse((dataTableTmp.Rows[0])[CN_TotalRain].ToString());
                result.TimeCollect = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
            }
            if (dataTableTmp.Rows.Count == 0)
            {
                result = null;
            }
            return result;
        }

        public CEntityRain GetLastDayRain_1(CEntityRain rain)
        {
            CEntityRain result = new CEntityRain();
            // string sql = "select top(1) stationid,totalrain,datatime from " + CT_TableName + "  where dayrain is not null and stationid= " + rain.StationID + " and datatime<'" + rain.TimeCollect + "' order by datatime desc";
            CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
            string sql = "select top(1) stationid,totalrain,datatime from " + CTF_TableName + "  where  stationid= " + rain.StationID + " and datatime<='" + rain.TimeCollect.Subtract(new TimeSpan(24, 0, 0)) + "' order by datatime desc";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            if (dataTableTmp.Rows.Count == 1)
            {
                result.StationID = dataTableTmp.Rows[0][CN_StationId].ToString();
                result.TotalRain = decimal.Parse((dataTableTmp.Rows[0])[CN_TotalRain].ToString());
                result.TimeCollect = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
            }
            if (dataTableTmp.Rows.Count == 0)
            {
                result = null;
            }
            return result;
        }
        public CEntityRain GetLastSharpRain_1(CEntityRain rain)
        {
            CEntityRain result = new CEntityRain();

            // string sql = "select top(1) stationid,totalrain,datatime from " + CT_TableName + " where periodrain is not null and stationid= " + rain.StationID + " and datatime<= '" + rain.TimeCollect + "' order by datatime desc;";
            CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
            string sql = "select top(1) stationid,totalrain,datatime from " + CTF_TableName + " where  stationid= " + rain.StationID + " and datatime<= '" + rain.TimeCollect.Subtract(new TimeSpan(1, 0, 0)) + "' order by datatime desc;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            if (dataTableTmp.Rows.Count == 1)
            {
                result.StationID = dataTableTmp.Rows[0][CN_StationId].ToString();
                result.TotalRain = decimal.Parse((dataTableTmp.Rows[0])[CN_TotalRain].ToString());
                result.TimeCollect = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
            }
            if (dataTableTmp.Rows.Count == 0)
            {
                result = null;
            }
            return result;


            //CEntityRain result = new CEntityRain();
            //DateTime tmp = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
            //string sql = "select top(1) totalrain,datatime from " + CT_TableName + " where stationid= " + stationId + " and datatime= '" + tmp + "' order by datatime desc;";
            //SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            //DataTable dataTableTmp = new DataTable();
            //adapter.Fill(dataTableTmp);
            //if (dataTableTmp.Rows.Count == 1)
            //{
            //    result.TotalRain = decimal.Parse((dataTableTmp.Rows[0])[CN_TotalRain].ToString());
            //    result.TimeCollect = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
            //}
            //else
            //{
            //    string sql2 = "select top(1) totalrain,datatime from " + CT_TableName + " where stationid= " + stationId + " and datatime< '" + tmp + "' order by datatime desc;";
            //    SqlDataAdapter adapter2 = new SqlDataAdapter(sql2, CDBManager.GetInstacne().GetConnection());
            //    DataTable dataTableTmp2 = new DataTable();
            //    adapter2.Fill(dataTableTmp2);
            //    if (dataTableTmp2.Rows.Count == 1)
            //    {
            //        result.TotalRain = decimal.Parse((dataTableTmp2.Rows[0])[CN_TotalRain].ToString());
            //        result.TimeCollect = DateTime.Parse(dataTableTmp2.Rows[0][CN_DataTime].ToString());
            //    }
            //}
            //return result;
        }
        public CEntityRain GetLastRain_1(CEntityRain rain)
        {
            CEntityRain result = new CEntityRain();
            CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
            string sql = "select top(1) stationid,totalrain,datatime from " + CTF_TableName + " where stationid= " + rain.StationID + " and datatime< '" + rain.TimeCollect + "' order by datatime desc;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            if (dataTableTmp.Rows.Count == 1)
            {
                result.StationID = dataTableTmp.Rows[0][CN_StationId].ToString();
                result.TotalRain = decimal.Parse((dataTableTmp.Rows[0])[CN_TotalRain].ToString());
                result.TimeCollect = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
            }
            if (dataTableTmp.Rows.Count == 0)
            {
                result = null;
            }

            return result;
        }

        public bool UpdateThisRain(CEntityRain rain)
        {
            CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
            string sql = "update  " + CTF_TableName + " set differencerain= " + rain.DifferneceRain + " where stationid= " + rain.StationID + " and datatime= '" + rain.TimeCollect + " '; ";
            // 更新数据库
            if (!this.ExecuteSQLCommand(sql))
            {
                return false;
            }
            ResetAll();
            return true;
        }
        public bool UpdateThisSharpRain(CEntityRain rain)
        {
            CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
            string sql = "update  " + CTF_TableName + " set periodrain= " + rain.PeriodRain + " where stationid= " + rain.StationID + " and datatime= '" + rain.TimeCollect + " ' ;";
            // 更新数据库
            if (!this.ExecuteSQLCommand(sql))
            {
                return false;
            }
            ResetAll();
            return true;
        }
        public bool UpdateThisDayRain(CEntityRain rain)
        {
            CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
            string sql = "update  " + CTF_TableName + " set dayrain= " + rain.DayRain + " where stationid= " + rain.StationID + " and datatime= '" + rain.TimeCollect + " ' ;";
            // 更新数据库
            if (!this.ExecuteSQLCommand(sql))
            {
                return false;
            }
            ResetAll();
            return true;
        }

        public bool UpdateNextRain(CEntityRain rain)
        {
            CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
            string sql = "update  " + CTF_TableName + " set differencerain= " + rain.DifferneceRain + " where stationid= " + rain.StationID + " and datatime= ' " + rain.TimeCollect + " ' ;";
            // 更新数据库
            if (!this.ExecuteSQLCommand(sql))
            {
                return false;
            }
            ResetAll();
            return true;
        }
        public bool UpdateNextSharpRain(CEntityRain rain)
        {
            CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
            string sql = "update  " + CTF_TableName + " set periodrain= " + rain.PeriodRain + " where stationid= " + rain.StationID + " and datatime= '" + rain.TimeCollect + " ' ;";
            // 更新数据库
            if (!this.ExecuteSQLCommand(sql))
            {
                return false;
            }
            ResetAll();
            return true;
        }
        public bool UpdateNextDayRain(CEntityRain rain)
        {
            CTF_TableName = "rain" + rain.TimeCollect.Year.ToString() + rain.TimeCollect.Month.ToString() + (rain.TimeCollect.Day > 15 ? "B" : "A");
            string sql = "update  " + CTF_TableName + " set dayrain= " + rain.DayRain + " where stationid= " + rain.StationID + " and datatime= '" + rain.TimeCollect + " '; ";
            // 更新数据库
            if (!this.ExecuteSQLCommand(sql))
            {
                return false;
            }
            ResetAll();
            return true;
        }


        public CEntityRain getRainsForInit(string stationid, DateTime dt)
        {
            CEntityRain result = new CEntityRain();
            CTF_TableName = "rain" + dt.Year.ToString() + dt.Month.ToString() + (dt.Day > 15 ? "B" : "A");
            string sql = "select top(1) stationid,totalrain,datatime from " + CTF_TableName + " where stationid= " + stationid + " and datatime<= '" + dt + "' order by datatime desc;";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTmp = new DataTable();
            adapter.Fill(dataTableTmp);
            if (dataTableTmp.Rows.Count == 1)
            {
                result.StationID = dataTableTmp.Rows[0][CN_StationId].ToString();
                result.TotalRain = decimal.Parse((dataTableTmp.Rows[0])[CN_TotalRain].ToString());
                result.TimeCollect = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
            }
            if (dataTableTmp.Rows.Count == 0)
            {
                result = null;
            }
            return result;
        }
    }
}
