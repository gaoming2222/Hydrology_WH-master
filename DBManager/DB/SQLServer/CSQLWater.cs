using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Hydrology.DBManager.Interface;
using Hydrology.Entity;
using System.Threading;

namespace Hydrology.DBManager.DB.SQLServer
{
    public class CSQLWater : CSQLBase, IWaterProxy
    {
        #region 静态常量
        private string urlPrefix = "127.0.0.1:8088";
        private const string CT_EntityName = "CEntityWater";   //  数据库表Water实体类
        //public static readonly string CT_TableName = "water";      //数据库中水量表的名字
        public static string CT_TableName
        {
            get { return "water" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + (DateTime.Now.Day > 15 ? "B" : "A"); }
        }

        //数据库查询的水位表名
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
        // public static readonly string CN_WaterID = "WID";            //水位表的唯一ID
        public static readonly string CN_StationId = "stationid"; //站点ID
        public static readonly string CN_DataTime = "datatime";    //数据的采集时间
        public static readonly string CN_WaterStage = "waterstage";  //水位

        public static readonly string CN_WaterFlow = "waterflow";     //流量
        public static readonly string CN_TransType = "transtype";  //通讯方式
        public static readonly string CN_MsgType = "messagetype";  //报送类型
        public static readonly string CN_RecvDataTime = "recvdatatime";    //接收到数据的时间
        public static readonly string CN_State = "state";
        private const int CN_FIELD_COUNT = 7;
        #endregion

        #region 私有成员

        private List<long> m_listDelRows;            // 删除水量记录的链表
        private List<CEntityWater> m_listUpdateRows; // 更新水量录的链表

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
        List<CEntityWater> batchInsertWaterList = new List<CEntityWater>();
        #endregion ///<PRIVATE_DATAMEMBER

        #region 公共方法

        public CSQLWater()
            : base()
        {
            m_listDelRows = new List<long>();
            m_listUpdateRows = new List<CEntityWater>();
            // 为数据表添加列
            m_tableDataAdded.Columns.Add(CN_StationId);
            //  m_tableDataAdded.Columns.Add(CN_WaterID);
            m_tableDataAdded.Columns.Add(CN_DataTime);
            m_tableDataAdded.Columns.Add(CN_RecvDataTime);
            m_tableDataAdded.Columns.Add(CN_WaterStage);
            m_tableDataAdded.Columns.Add(CN_WaterFlow);
            m_tableDataAdded.Columns.Add(CN_TransType);
            m_tableDataAdded.Columns.Add(CN_MsgType);
            m_tableDataAdded.Columns.Add(CN_State);

            // 初始化互斥量
            m_mutexWriteToDB = CDBMutex.Mutex_TB_Water;

            m_addTimer_1 = new System.Timers.Timer();
            m_addTimer_1.Elapsed += new System.Timers.ElapsedEventHandler(EHTimer_1);
            m_addTimer_1.Enabled = false;
            m_addTimer_1.Interval = CDBParams.GetInstance().AddToDbDelay;


            m_addTimer_2 = new System.Timers.Timer();
            m_addTimer_2.Elapsed += new System.Timers.ElapsedEventHandler(EHTimer_2);
            m_addTimer_2.Enabled = false;
            m_addTimer_2.Interval = CDBParams.GetInstance().AddToDbDelay;
            m_addTimer_2.Start();

            //每隔一分钟做一次写库操作
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
        public void AddNewRow(CEntityWater water)
        {
            // 记录超过1000条，或者时间超过1分钟，就将当前的数据写入数据库
            m_mutexDataTable.WaitOne(); //等待互斥量
            DataRow row = m_tableDataAdded.NewRow();
            row[CN_StationId] = water.StationID;
            row[CN_DataTime] = water.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
            row[CN_WaterStage] = water.WaterStage;
            row[CN_WaterFlow] = water.WaterFlow;
            row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(water.MessageType);
            row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(water.ChannelType);
            row[CN_RecvDataTime] = water.TimeRecieved.ToString(CDBParams.GetInstance().DBDateTimeFormat);
            row[CN_State] = water.state;
            m_tableDataAdded.Rows.Add(row);
            m_mutexDataTable.ReleaseMutex();

            // 判断是否需要创建新分区
            //CSQLPartitionMgr.Instance.MaintainWater(water.TimeCollect);
            if (m_tableDataAdded.Rows.Count >= CDBParams.GetInstance().AddBufferMax)
            {
                // 如果超过最大值，写入数据库
                //Task task = new Task(() => { AddDataToDB(); });
                //task.Start();
                NewTask(() => { AddDataToDB(); });
            }
            else
            {
                // 没有超过缓存最大值，开启定时器进行检测,多次调用Start()会导致重新计数
                m_addTimer.Start();
            }

        }

        public void AddNewRows(List<CEntityWater> waters)
        {
            // 记录超过写入上线条，或者时间超过1分钟，就将当前的数据写入数据库
            m_mutexDataTable.WaitOne(); //等待互斥量
            foreach (CEntityWater water in waters)
            {
                DataRow row = m_tableDataAdded.NewRow();
                row[CN_StationId] = water.StationID;
                row[CN_DataTime] = water.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                row[CN_WaterStage] = water.WaterStage;
                row[CN_WaterFlow] = water.WaterFlow;
                row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(water.MessageType);
                row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(water.ChannelType);
                row[CN_RecvDataTime] = water.TimeRecieved.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                row[CN_State] = water.state;
                m_tableDataAdded.Rows.Add(row);

                // 判断是否需要创建新分区
                //CSQLPartitionMgr.Instance.MaintainWater(water.TimeCollect);
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

        public void AddNewRows_1(List<CEntityWater> waters)
        {
            if (waters.Count <= 0)
            {
                return;
            }
            batchInsertWaterList.AddRange(waters);
            
            //// 记录超过写入上线条，或者时间超过1分钟，就将当前的数据写入数据库
            //m_mutexDataTable.WaitOne(); //等待互斥量
            //foreach (CEntityWater water in waters)
            //{
            //    DataRow row = m_tableDataAdded.NewRow();
            //    row[CN_StationId] = water.StationID;
            //    row[CN_DataTime] = water.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
            //    row[CN_WaterStage] = water.WaterStage;
            //    row[CN_WaterFlow] = water.WaterFlow;
            //    row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(water.MessageType);
            //    row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(water.ChannelType);
            //    row[CN_RecvDataTime] = water.TimeRecieved.ToString(CDBParams.GetInstance().DBDateTimeFormat);
            //    row[CN_State] = water.state;
            //    m_tableDataAdded.Rows.Add(row);

            //    // 判断是否需要创建新分区
            //    //CSQLPartitionMgr.Instance.MaintainWater(water.TimeCollect);
            //}

            //// 直接写入数据库
            //NewTask(() => { AddDataToDB(); });
            //m_mutexDataTable.ReleaseMutex();
        }

        public bool DeleteRows(List<String> waters_StationId, List<String> waters_StationDate)
        {
            if (waters_StationId.Count <= 0)
            {
                return true;
            }
            List<CEntityWater> waterList = new List<CEntityWater>();
            for (int i = 0; i < waters_StationId.Count; i++)
            {
                waterList.Add(new CEntityWater()
                {
                    StationID = waters_StationId[i],
                    TimeCollect = Convert.ToDateTime(waters_StationDate[i]),
                    TimeRecieved = Convert.ToDateTime("2019/3/13 15:00:00")
                });
            }
            Dictionary<string, object> param = new Dictionary<string, object>();
            //rains_StationDate = DateTime.MinValue ? (DateTime)SqlDateTime.MinValue : rains_StationDate;
            string suffix = "/water/deleteWater";
            string url = "http://" + urlPrefix + suffix;
            Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(waterList, Newtonsoft.Json.Formatting.None, timeConverter);
            param["water"] = jsonStr;
            //string jsonStr = HttpHelper.ObjectToJson(waterList);
            //param["water"] = "[{\"ChannelType\":0,\"MessageType\":0,\"StationID\":\"3004\",\"TimeCollect\":\"2019/3/13 14:00:00\",\"TimeRecieved\":\"\\/Date(1552460400000+0800)\\/\",\"WaterFlow\":null,\"WaterID\":0,\"WaterStage\":0,\"state\":0}]";
            try
            {
                string resultJson = HttpHelper.Post(url, param);
            }
            catch (Exception e)
            {
                Debug.WriteLine("删除水位信息失败");
                return false;
            }
            return true;
            //// 删除某条水位记录
            //StringBuilder sql = new StringBuilder();
            //int currentBatchCount = 0;
            //for (int i = 0; i < waters_StationId.Count; i++)
            //{
            //    ++currentBatchCount;
            //    CTF_TableName = "water" + DateTime.Parse(waters_StationDate[i]).Year.ToString() + DateTime.Parse(waters_StationDate[i]).Month.ToString() + (DateTime.Parse(waters_StationDate[i]).Day > 15 ? "B" : "A");
            //    sql.AppendFormat("delete from {0} where {1}={2} and {3} = '{4}';",
            //        CTF_TableName,
            //        CN_StationId, waters_StationId[i].ToString(),
            //        CN_DataTime, waters_StationDate[i].ToString()
            //    );
            //    if (currentBatchCount >= CDBParams.GetInstance().UpdateBufferMax)
            //    {
            //        // 更新数据库
            //        if (!this.ExecuteSQLCommand(sql.ToString()))
            //        {
            //            return false;
            //        }
            //        sql.Clear(); //清除以前的所有命令
            //        currentBatchCount = 0;
            //    }
            //}
            //if (!ExecuteSQLCommand(sql.ToString()))
            //{
            //    return false;
            //}
            //ResetAll();
            //// 如何考虑线程同异步
            //return true;
        }

        public bool UpdateRows(List<Hydrology.Entity.CEntityWater> waters)
        {
            if (waters.Count <= 0)
            {
                return true;
            }
            Dictionary<string, object> param = new Dictionary<string, object>();
            string suffix = "/water/updateWater";
            string url = "http://" + urlPrefix + suffix;
            //string url = "http://127.0.0.1:8088/water/updateWater";
            //string jsonStr = HttpHelper.ObjectToJson(waters);
            //param["water"] = "[{\"ChannelType\":16,\"MessageType\":1,\"StationID\":\"3004\",\"TimeCollect\":\"2019-3-13 15:00:00\",\"TimeRecieved\":\"2019/3/13 15:45:44\",\"WaterFlow\":null,\"WaterID\":0,\"WaterStage\":14,\"state\":2}]";
            Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(waters, Newtonsoft.Json.Formatting.None, timeConverter);
            param["water"] = jsonStr;
            try
            {
                string resultJson = HttpHelper.Post(url, param);
            }
            catch (Exception e)
            {
                Debug.WriteLine("更新水位信息失败");
                return false;
            }
            return true;
            //// 除主键外,其余信息随意修改
            //StringBuilder sql = new StringBuilder();
            //int currentBatchCount = 0;
            //for (int i = 0; i < waters.Count; i++)
            //{
            //    ++currentBatchCount;
            //    CTF_TableName = "water" + waters[i].TimeCollect.Year.ToString() + waters[i].TimeCollect.Month.ToString() + (waters[i].TimeCollect.Day > 15 ? "B" : "A");
            //    sql.AppendFormat("update {0} set {1}={2},{3}={4},{5}={6},{7}={8},{9}={10},{11}={12} where {13}={14} and {15}='{16}';",
            //        CTF_TableName,
            //        CN_WaterStage, waters[i].WaterStage,
            //        CN_WaterFlow, (waters[i].WaterFlow.HasValue ? waters[i].WaterFlow.Value.ToString() : "null"),
            //        CN_TransType, CEnumHelper.ChannelTypeToDBStr(waters[i].ChannelType),
            //        CN_MsgType, CEnumHelper.MessageTypeToDBStr(waters[i].MessageType),
            //        CN_State, waters[i].state,
            //        CN_RecvDataTime, DateTimeToDBStr(waters[i].TimeRecieved),
            //        CN_StationId, waters[i].StationID,
            //        CN_DataTime, waters[i].TimeCollect.ToString()
            //    //   CN_WaterID, waters[i].WaterID
            //    );
            //    //if (currentBatchCount >= CDBParams.GetInstance().UpdateBufferMax)
            //    //{
            //    //    // 更新数据库
            //    //    if (!this.ExecuteSQLCommand(sql.ToString()))
            //    //    {
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
            // 设置查询条件
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
        public List<CEntityWater> SetFilterData(string stationId, DateTime timeStart, DateTime timeEnd, bool TimeSelect)
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
            List<CEntityWater> waterList = new List<CEntityWater>();
            
            string url = "http://127.0.0.1:8088/water/getWater";
            string jsonStr = HttpHelper.SerializeDictionaryToJsonString(paramInner);
            param["water"] = jsonStr;
            try
            {
                string resultJson = HttpHelper.Post(url, param);
                resultJson = HttpHelper.JsonDeserialize(resultJson);
                waterList = (List<CEntityWater>)HttpHelper.JsonToObject(resultJson, new List<CEntityWater>());
                
            }
            catch (Exception e)
            {
                Debug.WriteLine("查询水位信息失败");
                throw e;
            }
            return waterList;
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

        public List<Hydrology.Entity.CEntityWater> GetPageData(int pageIndex)
        {
            if (pageIndex <= 0 || m_startTime == null || m_endTime == null || m_strStaionId == null)
            {
                return new List<CEntityWater>();
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
                int rowidmin = topcount - CDBParams.GetInstance().DBPageRowCount;
                CTF_TableName = "water" + m_startTime.Year.ToString() + m_startTime.Month.ToString() + (m_startTime.Day > 15 ? "B" : "A");
                CTT_TableName = "water" + m_endTime.Year.ToString() + m_endTime.Month.ToString() + (m_endTime.Day > 15 ? "B" : "A");
                CFF_TableName = "water" + m_startTime.Year.ToString() + (m_startTime.Day <= 15 ? (m_startTime.Month.ToString() + "B") : ((m_startTime.Month + 1).ToString() + "A"));
                string sql = " select * from ( " +
                    "select top " + topcount.ToString() + " row_number() over( order by " + CN_DataTime + " ) as " + CN_RowId + ",* " +
                    "from " + CM_TableName + " " +
                    "where " + CN_StationId + "=" + m_strStaionId.ToString() + " " +
                    "and " + TimeSelectString + CN_DataTime + " between " + DateTimeToDBStr(m_startTime) +
                    "and " + DateTimeToDBStr(m_endTime) +
                    ") as tmp1 " +
                    "where " + CN_RowId + ">" + rowidmin.ToString() +
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
            CTF_TableName = "water" + time.Year.ToString() + time.Month.ToString() + (time.Day > 15 ? "B" : "A");
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

        public bool GetLastData(ref Nullable<Decimal> lastWaterStage, ref Nullable<Decimal> lastWaterFlow, ref Nullable<DateTime> lastDayTime, ref Nullable<EChannelType> lastChannelType, ref Nullable<EMessageType> lastMessageType, string stationId)
        {
            // 获取计算水位值所需的数据
            try
            {
                // 获取最近一条的水位值
                string sql = string.Format("select top 1 * from {0} where {1} = '{2}' order by {3} desc;",
                    CT_TableName,
                    CN_StationId, stationId,
                    CN_DataTime);
                SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
                DataTable dataTableTmp = new DataTable();
                adapter.Fill(dataTableTmp);
                if (dataTableTmp.Rows.Count > 0)
                {
                    lastWaterStage = Decimal.Parse(dataTableTmp.Rows[0][CN_WaterStage].ToString());
                    if (dataTableTmp.Rows[0][CN_WaterFlow].ToString() != "")
                    {
                        lastWaterFlow = Decimal.Parse(dataTableTmp.Rows[0][CN_WaterFlow].ToString());
                    }
                    lastDayTime = DateTime.Parse(dataTableTmp.Rows[0][CN_DataTime].ToString());
                    lastChannelType = CEnumHelper.DBStrToChannelType(dataTableTmp.Rows[0][CN_TransType].ToString());
                    lastMessageType = CEnumHelper.DBStrToMessageType(dataTableTmp.Rows[0][CN_MsgType].ToString());

                }
                else
                {
                    //      Debug.WriteLine(string.Format("查询水位表为空,站点{0}", stationId));
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
        #endregion ///<公共方法

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
                string tname = "water" + time.Year.ToString() + time.Month.ToString() + (time.Day > 15 ? "B" : "A");
                if (!tmpDic.ContainsKey(tname))
                {
                    DataTable tmpDt = tmp.Copy();
                    tmpDt.Clear();
                    tmpDic.Add(tname, tmpDt);
                }
                if (!tmpDic[tname].Rows.Contains(tmp.Rows[i]))
                {
                    tmpDic[tname].Rows.Add(tmp.Rows[i].ItemArray);
                }
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
                string tname = "water" + time.Year.ToString() + time.Month.ToString() + (time.Day > 15 ? "B" : "A");
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
            if (batchInsertWaterList == null || batchInsertWaterList.Count == 0)
            {
                return;
            }
            List<CEntityWater> waterList = new List<CEntityWater>();
            lock (batchInsertWaterList)
            {
                waterList.AddRange(batchInsertWaterList);
                batchInsertWaterList.Clear();
            }
            
            if (waterList.Count <= 1000)
            {
                Dictionary<string, object> param = new Dictionary<string, object>();
                string suffix = "/water/insertWater";
                string url = "http://" + urlPrefix + suffix;
                //string url = "http://127.0.0.1:8088/water/insertWater";
                Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
                //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
                timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
                string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(waterList, Newtonsoft.Json.Formatting.None, timeConverter);
                //string jsonStr = HttpHelper.ObjectToJson(waters);
                //param["water"] = "[{\"ChannelType\":6,\"MessageType\":1,\"StationID\":\"3004\",\"TimeCollect\":\"2019-3-13 14:00:00\",\"TimeRecieved\":\"2019-3-13 14:45:00\",\"WaterFlow\":14,\"WaterID\":0,\"WaterStage\":14,\"state\":1}]";
                param["water"] = jsonStr;
                try
                {
                    string resultJson = HttpHelper.Post(url, param);
                    CDBLog.Instance.AddInfo(string.Format("添加{0}行到水位表", waterList.Count));
                    return;
                }
                catch (Exception e)
                {
                    CDBLog.Instance.AddInfo(string.Format("添加数据到水位表失败", waterList.Count));
                    Debug.WriteLine("添加水位信息失败");
                    return;
                }
                finally
                {
                    waterList.Clear();
                }
            }
            else
            {
                CDBLog.Instance.AddInfo(string.Format("数据量大于1000条，则分批插入", waterList.Count));
                List<CEntityWater> waters = new List<CEntityWater>();
                
                for(int i = 0; i < waterList.Count; i++)
                {
                    waters.Add(waterList[i]);
                    if((i+1)% 1000 == 0)
                    {
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        string suffix = "/water/insertWater";
                        string url = "http://" + urlPrefix + suffix;
                        //string url = "http://127.0.0.1:8088/water/insertWater";
                        Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
                        //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
                        timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
                        string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(waters, Newtonsoft.Json.Formatting.None, timeConverter);
                        //string jsonStr = HttpHelper.ObjectToJson(waters);
                        //param["water"] = "[{\"ChannelType\":6,\"MessageType\":1,\"StationID\":\"3004\",\"TimeCollect\":\"2019-3-13 14:00:00\",\"TimeRecieved\":\"2019-3-13 14:45:00\",\"WaterFlow\":14,\"WaterID\":0,\"WaterStage\":14,\"state\":1}]";
                        param["water"] = jsonStr;
                        try
                        {
                            string resultJson = HttpHelper.Post(url, param);
                            CDBLog.Instance.AddInfo(string.Format("添加{0}行到水位表", waters.Count));
                        }
                        catch (Exception e)
                        {
                            CDBLog.Instance.AddInfo(string.Format("添加数据到水位表失败", waters.Count));
                            Debug.WriteLine("添加水位信息失败");
                            return;
                        }
                        finally
                        {
                            waters.Clear();
                        }
                    }
                    if(i == waterList.Count - 1)
                    {
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        string suffix = "/water/insertWater";
                        string url = "http://" + urlPrefix + suffix;
                        //string url = "http://127.0.0.1:8088/water/insertWater";
                        Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
                        //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
                        timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
                        string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(waters, Newtonsoft.Json.Formatting.None, timeConverter);
                        //string jsonStr = HttpHelper.ObjectToJson(waters);
                        //param["water"] = "[{\"ChannelType\":6,\"MessageType\":1,\"StationID\":\"3004\",\"TimeCollect\":\"2019-3-13 14:00:00\",\"TimeRecieved\":\"2019-3-13 14:45:00\",\"WaterFlow\":14,\"WaterID\":0,\"WaterStage\":14,\"state\":1}]";
                        param["water"] = jsonStr;
                        try
                        {
                            string resultJson = HttpHelper.Post(url, param);
                            CDBLog.Instance.AddInfo(string.Format("添加{0}行到水位表", waters.Count));
                            
                        }
                        catch (Exception e)
                        {
                            CDBLog.Instance.AddInfo(string.Format("添加数据到水位表失败", waters.Count));
                            Debug.WriteLine("添加水位信息失败");
                            return;
                        }
                        finally
                        {
                            waters.Clear();
                            waterList.Clear();
                            
                        }
                    }
                    Thread.Sleep(200);
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
                    // 水位表有插入触发器，如果遇到重复记录，则更新为当前的最新记录
                    // bulkCopy.BatchSize = 1;
                    //bulkCopy.BulkCopyTimeout = 1800;
                    //bulkCopy.DestinationTableName = CT_TableName;
                    //bulkCopy.ColumnMappings.Add(CN_RainID, CN_RainID);
                    bulkCopy.BatchSize = 1;
                    bulkCopy.BulkCopyTimeout = 1800;
                    bulkCopy.DestinationTableName = tname;
                    //bulkCopy.ColumnMappings.Add(CN_RainID, CN_RainID);
                    bulkCopy.ColumnMappings.Add(CN_StationId, CN_StationId);
                    bulkCopy.ColumnMappings.Add(CN_DataTime, CN_DataTime);
                    bulkCopy.ColumnMappings.Add(CN_WaterStage, CN_WaterStage);
                    bulkCopy.ColumnMappings.Add(CN_WaterFlow, CN_WaterFlow);
                    bulkCopy.ColumnMappings.Add(CN_TransType, CN_TransType);
                    bulkCopy.ColumnMappings.Add(CN_MsgType, CN_MsgType);
                    bulkCopy.ColumnMappings.Add(CN_RecvDataTime, CN_RecvDataTime);
                    bulkCopy.ColumnMappings.Add(CN_State, CN_State);

                    try
                    {
                        bulkCopy.WriteToServer(dt);
                        Debug.WriteLine("###{0} :add {1} lines to water db", DateTime.Now, dt.Rows.Count);
                        CDBLog.Instance.AddInfo(string.Format("添加{0}行到水位表", dt.Rows.Count));
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

        // 根据当前条件查询统计数据
        private void DoCountQuery()
        {
            CTF_TableName = "water" + m_startTime.Year.ToString() + m_startTime.Month.ToString() + (m_startTime.Day > 15 ? "B" : "A");
            CTT_TableName = "water" + m_endTime.Year.ToString() + m_endTime.Month.ToString() + (m_endTime.Day > 15 ? "B" : "A");
            CFF_TableName = "water" + m_startTime.Year.ToString() + (m_startTime.Day <= 15 ? (m_startTime.Month.ToString() + "B") : ((m_startTime.Month + 1).ToString() + "A"));
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
        private List<CEntityWater> CopyDataToList(int key, int startRow)
        {
            List<CEntityWater> result = new List<CEntityWater>();
            // 取最小值 ，保证不越界
            int endRow = Math.Min(m_mapDataTable[key].Rows.Count, startRow + CDBParams.GetInstance().UIPageRowCount);
            DataTable table = m_mapDataTable[key];
            for (; startRow < endRow; ++startRow)
            {
                CEntityWater water = new CEntityWater();
                // water.WaterID = long.Parse(table.Rows[startRow][CN_WaterID].ToString());
                water.StationID = table.Rows[startRow][CN_StationId].ToString();
                water.TimeCollect = DateTime.Parse(table.Rows[startRow][CN_DataTime].ToString());
                //水位
                if (!table.Rows[startRow][CN_WaterStage].ToString().Equals(""))
                {
                    water.WaterStage = Decimal.Parse(table.Rows[startRow][CN_WaterStage].ToString());
                }
                else
                {
                    //11.12
                    water.WaterStage = -9999;
                }
                //流量
                string tmp = table.Rows[startRow][CN_WaterFlow].ToString();
                if (!tmp.Equals(""))
                {
                    water.WaterFlow = Decimal.Parse(table.Rows[startRow][CN_WaterFlow].ToString());
                }
                else
                {
                    //11.12
                    water.WaterFlow = -9999;
                }
                if (table.Rows[startRow][CN_State].ToString() != "")
                {
                    try
                    {
                        water.state = int.Parse(table.Rows[startRow][CN_State].ToString());
                    }
                    catch (Exception ex) { }
                }
                else
                {
                    water.state = 1;
                }
                water.TimeRecieved = DateTime.Parse(table.Rows[startRow][CN_RecvDataTime].ToString());
                water.ChannelType = CEnumHelper.DBStrToChannelType(table.Rows[startRow][CN_TransType].ToString());
                water.MessageType = CEnumHelper.DBStrToMessageType(table.Rows[startRow][CN_MsgType].ToString());
                result.Add(water);
            }
            return result;
        }

        private void ResetAll()
        {
            m_mutexDataTable.WaitOne();
            m_iPageCount = -1;
            m_mapDataTable.Clear(); //清空所有记录
            m_mutexDataTable.ReleaseMutex();
        }

        #endregion ///< HELP_METHOD

        //1009gm
        public List<CEntityWater> QueryA(string station, DateTime time)
        {
            List<CEntityWater> results = new List<CEntityWater>();
            DateTime startTime = time;
            DateTime endTime = startTime.AddHours(23).AddMinutes(59).AddSeconds(59);
            CTF_TableName = "water" + time.Year.ToString() + time.Month.ToString() + "A";
            CTT_TableName = "water" + time.Year.ToString() + time.Month.ToString() + "B";
            String sql = "select * from " + CF_TableName + " where StationID=" + station + " and Datatime between '" + startTime + "'and '" + endTime + "';";
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
                    CEntityWater water = new CEntityWater();
                    water.StationID = dataTableTemp.Rows[rowid][CN_StationId].ToString();
                    water.TimeCollect = DateTime.Parse(dataTableTemp.Rows[rowid][CN_DataTime].ToString());
                    water.WaterStage = decimal.Parse(dataTableTemp.Rows[rowid][CN_WaterStage].ToString());
                    results.Add(water);
                }
            }
            return results;

        }

        public List<CEntityWater> QueryForYear(string station, DateTime time)
        {
            List<CEntityWater> results = new List<CEntityWater>();
            DateTime startTime = time;
            DateTime endTime = startTime.AddMonths(1);
            CTF_TableName = "water" + time.Year.ToString() + time.Month.ToString() + "A";
            CTT_TableName = "water" + time.Year.ToString() + time.Month.ToString() + "B";
            String sql = "select * from " + CF_TableName + " where StationID=" + station + " and messagetype = 8 " + " and Datatime between '" + startTime + "'and '" + endTime + "';";
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
                    CEntityWater water = new CEntityWater();
                    water.StationID = dataTableTemp.Rows[rowid][CN_StationId].ToString();
                    water.TimeCollect = DateTime.Parse(dataTableTemp.Rows[rowid][CN_DataTime].ToString());
                    if (dataTableTemp.Rows[rowid][CN_WaterStage].ToString() != "")
                    {
                        water.WaterStage = decimal.Parse(dataTableTemp.Rows[rowid][CN_WaterStage].ToString());
                    }
                    else
                    {
                        water.WaterStage = -9999;
                    }
                    results.Add(water);
                }
            }

            return results;

        }

        public bool checkWaterIsExists(string stationid, DateTime dt)
        {
            bool flag = false;
            CTF_TableName = "water" + dt.Year.ToString() + dt.Month.ToString() + (dt.Day > 15 ? "B" : "A");
            string sql = "select count(waterstage) from " + CTF_TableName + " where stationid= " + stationid + " and datatime=  '" + dt + "';";
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

        public void AddOrUpdate(List<CEntityWater> listWaters)
        {
            m_mutexDataTable.WaitOne(); //等待互斥量
            foreach (CEntityWater water in listWaters)
            {
                StringBuilder sql = new StringBuilder();
                try
                {
                    DataRow row = m_tableDataAdded.NewRow();
                    row[CN_StationId] = water.StationID;
                    row[CN_DataTime] = water.TimeCollect.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                    row[CN_WaterStage] = water.WaterStage;
                    row[CN_WaterFlow] = water.WaterFlow;
                    row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(water.MessageType);
                    row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(water.ChannelType);
                    row[CN_RecvDataTime] = water.TimeRecieved.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                    m_tableDataAdded.Rows.Add(row);

                    // 判断是否需要创建新分区
                    //CSQLPartitionMgr.Instance.MaintainWater(water.TimeCollect);
                    NewTask(() => { AddDataToDB(); });
                }
                catch (Exception e)
                {
                    CTF_TableName = "water" + water.TimeCollect.Year.ToString() + water.TimeCollect.Month.ToString() + (water.TimeCollect.Day > 15 ? "B" : "A");
                    sql.AppendFormat("update {0} set {1}={2},{3}={4},{5}={6},{7}={8},{9}={10} where {11}={12} and {13}='{14}';",
                    CTF_TableName,
                    CN_WaterStage, water.WaterStage,
                    CN_WaterFlow, (water.WaterFlow.HasValue ? water.WaterFlow.Value.ToString() : "null"),
                    CN_TransType, CEnumHelper.ChannelTypeToDBStr(water.ChannelType),
                    CN_MsgType, CEnumHelper.MessageTypeToDBStr(water.MessageType),
                    CN_RecvDataTime, DateTimeToDBStr(water.TimeRecieved),
                    CN_StationId, water.StationID,
                    CN_DataTime, water.TimeCollect.ToString()
                    );
                    if (!this.ExecuteSQLCommand(sql.ToString()))
                    {
                        return;
                    }
                    sql.Clear(); //清除以前的所有命令
                }
                ResetAll();

            }
            m_mutexDataTable.ReleaseMutex();
        }


        public List<DateTime> getExistsTime(string stationid, DateTime startTime, DateTime endTime)
        {
            List<DateTime> results = new List<DateTime>();
            CTF_TableName = "water" + startTime.Year.ToString() + startTime.Month.ToString() + (startTime.Day > 15 ? "B" : "A");
            CTT_TableName = "water" + endTime.Year.ToString() + endTime.Month.ToString() + (endTime.Day > 15 ? "B" : "A");
            string sql = "select datatime from " + CF_TableName + " where stationid= " + stationid + " and datatime between '" + startTime + "' and  '" + endTime + "' order by datatime;";
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
                sqlcreate.AppendFormat("if not exists (select * from sysobjects where id = object_id('{0}') and OBJECTPROPERTY(id, 'IsUserTable') = 1) CREATE TABLE [{1}] ([stationid] [char](4) NOT NULL,[datatime][datetime] NOT NULL,[waterstage][numeric](18, 2) NULL,[waterflow][numeric](18, 3) NULL,[transtype][char](2) NULL,[messagetype][char](1) NULL,[recvdatatime][datetime] NULL, [state][int] NULL, CONSTRAINT [{2}] PRIMARY KEY CLUSTERED ( [stationid] ASC, [datatime] ASC)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]) ON[PRIMARY]; ",
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
                //sqlcreate.AppendFormat("if not exists (select * from sysobjects where id = object_id('{0}') and OBJECTPROPERTY(id, 'IsUserTable') = 1) CREATE TABLE [{1}] ([stationid] [char](4) NOT NULL,[datatime][datetime] NOT NULL,[waterstage][numeric](18, 2) NULL,[waterflow][numeric](18, 3) NULL,[transtype][char](2) NULL,[messagetype][char](1) NULL,[recvdatatime][datetime] NULL, [state][int] NULL, CONSTRAINT [{2}] PRIMARY KEY CLUSTERED ( [stationid] ASC, [datatime] ASC)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]) ON[PRIMARY]; ",
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
                sqlcreate2.AppendFormat("if not exists (select * from sysobjects where id = object_id('{0}') and OBJECTPROPERTY(id, 'IsUserTable') = 1) CREATE TABLE [{1}] ([stationid] [char](4) NOT NULL,[datatime][datetime] NOT NULL,[waterstage][numeric](18, 2) NULL,[waterflow][numeric](18, 3) NULL,[transtype][char](2) NULL,[messagetype][char](1) NULL,[recvdatatime][datetime] NULL, [state][int] NULL, CONSTRAINT [{2}] PRIMARY KEY CLUSTERED ( [stationid] ASC, [datatime] ASC)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]) ON[PRIMARY]; ",
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
