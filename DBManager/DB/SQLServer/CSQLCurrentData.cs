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
    /// 提供SQL Server 数据库中对实时表的操作
    /// </summary>
    public class CSQLCurrenData : CSQLBase, ICurrentDataProxy
    {
        #region 数据成员
        private string urlPrefix = "127.0.0.1:8088";
        private const string CT_EntityName = "CEntityData";   //  数据库表Rain实体类
        public static readonly string CT_TableName = "CurrentData";
        public static readonly string CN_CName = "CName"; //站点名字
        public static readonly string CN_StationId = "StationID"; //站点ID
        public static readonly string CN_StationType = "CType"; //站点类别
        public static readonly string CN_DataTime = "DataTime";    //数据的采集时间

        public static readonly string CN_PeriodRain = "PeriodRain";  //雨量差值,即时段雨量
        public static readonly string CN_YesterdayRain = "YesterdayRain";  //昨日雨量
        public static readonly string CN_TodayRain = "TodayRain";  //今日雨量
        public static readonly string CN_WaterStage = "WaterStage";  //水位
        public static readonly string CN_WaterFlow = "WaterFlow";     //实测流量
        public static readonly string CN_Voltage = "Voltage";       //电压

        public static readonly string CN_DataState = "CurrentState";       //状态，error，warning，normal
        public static readonly string CN_TransType = "GPRSType";  //通讯方式
        public static readonly string CN_MsgType = "ReportType";  //报送类型

        private const int CN_FIELD_COUNT = 9;
        private List<long> m_listDelRows;            // 删除数据记录的链表
        private List<CEntityRealTime> m_listUpdateRows; // 更新数据记录的链表

        private string m_strStaionId;       //需要查询的测站
        private DateTime m_startTime;  //查询起始时间
        private DateTime m_endTime;    //查询结束时间

        public System.Timers.Timer m_addTimer_1;
        public System.Timers.Timer m_addTimer_2;
        List<CEntityRealTime> batchInsertRealtimeList = new List<CEntityRealTime>();

        #endregion

        public CSQLCurrenData() : base()
        {
            m_listDelRows = new List<long>();
            m_listUpdateRows = new List<CEntityRealTime>();
            // 为数据表添加列
            m_tableDataAdded.Columns.Add(CN_CName);
            m_tableDataAdded.Columns.Add(CN_StationId);
            m_tableDataAdded.Columns.Add(CN_StationType);
            m_tableDataAdded.Columns.Add(CN_YesterdayRain);
            m_tableDataAdded.Columns.Add(CN_TodayRain);
            m_tableDataAdded.Columns.Add(CN_PeriodRain);
            m_tableDataAdded.Columns.Add(CN_WaterStage);
            m_tableDataAdded.Columns.Add(CN_WaterFlow);
            m_tableDataAdded.Columns.Add(CN_Voltage);
            m_tableDataAdded.Columns.Add(CN_DataState);
            m_tableDataAdded.Columns.Add(CN_MsgType);
            m_tableDataAdded.Columns.Add(CN_TransType);
            m_tableDataAdded.Columns.Add(CN_DataTime);

            // 初始化互斥量
            m_mutexWriteToDB = CDBMutex.Mutex_TB_CurrentData;

            m_addTimer_1 = new System.Timers.Timer();
            m_addTimer_1.Elapsed += new System.Timers.ElapsedEventHandler(EHTimer_1);
            m_addTimer_1.Enabled = false;
            m_addTimer_1.Interval = CDBParams.GetInstance().AddToDbDelay;

            m_addTimer_2 = new System.Timers.Timer();
            m_addTimer_2.Elapsed += new System.Timers.ElapsedEventHandler(EHTimer_2);
            m_addTimer_2.Enabled = false;
            //m_addTimer_2.Interval = CDBParams.GetInstance().AddToDbDelay;
            m_addTimer_2.Interval = 35 * 1000;
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
        public void AddNewRow(CEntityRealTime realtime)
        {
            if (realtime == null)
            {
                return;
            }
            List<CEntityRealTime> realTimeList = new List<CEntityRealTime>();
            realTimeList.Add(realtime);
            Dictionary<string, object> param = new Dictionary<string, object>();
            string suffix = "/currentdata/insertCurrentdataCS";
            string url = "http://" + urlPrefix + suffix;
            Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(realTimeList, Newtonsoft.Json.Formatting.None, timeConverter);
            param["currentdata"] = jsonStr;
            try
            {
                string resultJson = HttpHelper.Post(url, param);
                CDBLog.Instance.AddInfo(string.Format("添加{0}行到雨量表", realTimeList.Count));
            }
            catch (Exception e)
            {
                Debug.WriteLine("添加雨量信息失败");
            }
            //// 记录超过1000条，或者时间超过1分钟，就将当前的数据写入数据库
            //m_mutexDataTable.WaitOne(); //等待互斥量
            //DataRow row = m_tableDataAdded.NewRow();
            //row[CN_CName] = realtime.StrStationName;
            //row[CN_StationId] = realtime.StrStationID;
            //row[CN_StationType] = realtime.EIStationType;
            //row[CN_YesterdayRain] = realtime.LastDayRainFall;
            //row[CN_TodayRain] = realtime.DDayRainFall;
            //row[CN_PeriodRain] = realtime.DPeriodRain;
            //row[CN_WaterStage] = realtime.DWaterYield;
            //row[CN_WaterFlow] = realtime.DWaterFlowActual;
            //row[CN_Voltage] = realtime.Dvoltage;
            //row[CN_DataState] = realtime.ERTDState;
            //row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(realtime.EIMessageType);
            //row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(realtime.EIChannelType);
            //row[CN_DataTime] = realtime.TimeDeviceGained.ToString(CDBParams.GetInstance().DBDateTimeFormat);

            //m_tableDataAdded.Rows.Add(row);
            //m_mutexDataTable.ReleaseMutex();

            //// 判断是否需要创建新分区
            ////CSQLPartitionMgr.Instance.MaintainVoltage(voltage.TimeCollect);
            //if (m_tableDataAdded.Rows.Count >= CDBParams.GetInstance().AddBufferMax)
            //{
            //    // 如果超过最大值，写入数据库
            //    NewTask(() => { AddDataToDB(); });
            //}
            //else
            //{
            //    // 没有超过缓存最大值，开启定时器进行检测,多次调用Start()会导致重新计数
            //    m_addTimer.Start();
            //}
        }

        public void AddNewRow_1(CEntityRealTime realtime)
        {
            if (realtime == null)
            {
                return;
            }
            batchInsertRealtimeList.Add(realtime);
            batchInsertRealtimeList.Add(realtime);
        }

        public bool UpdateRow(CEntityRealTime realtime)
        {
            bool result = false;
            if (realtime == null)
            {
                return true;
            }
            List<CEntityRealTime> realTimeList = new List<CEntityRealTime>();
            realTimeList.Add(realtime);
            Dictionary<string, object> param = new Dictionary<string, object>();
            string suffix = "/currentdata/updateCurrentdataCS";
            string url = "http://" + urlPrefix + suffix;
            Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(realTimeList, Newtonsoft.Json.Formatting.None, timeConverter);
            param["currentdata"] = jsonStr;
            try
            {
                string resultJson = HttpHelper.Post(url, param);
                if(resultJson == "1")
                {
                    result = true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("更新实时数据表失败");
                return false;
            }
            return result;
        }
        public void AddNewRows(List<CEntityRealTime> realtimes)
        {
            // 记录超过1000条，或者时间超过1分钟，就将当前的数据写入数据库
            m_mutexDataTable.WaitOne(); //等待互斥量
            foreach (CEntityRealTime realtime in realtimes)
            {
                if (realtime.TimeDeviceGained != DateTime.MinValue)
                {
                    DataRow row = m_tableDataAdded.NewRow();
                    row[CN_CName] = realtime.StrStationName;
                    row[CN_StationId] = realtime.StrStationID;
                    row[CN_StationType] = realtime.EIStationType;
                    row[CN_DataTime] = realtime.TimeDeviceGained.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                    row[CN_YesterdayRain] = realtime.LastDayRainFall;
                    row[CN_TodayRain] = realtime.DDayRainFall;
                    row[CN_PeriodRain] = realtime.DPeriodRain;
                    row[CN_WaterStage] = realtime.DWaterYield;
                    row[CN_WaterFlow] = realtime.DWaterFlowActual;
                    row[CN_Voltage] = realtime.Dvoltage;
                    row[CN_DataState] = realtime.ERTDState;
                    row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(realtime.EIMessageType);
                    row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(realtime.EIChannelType);
                    m_tableDataAdded.Rows.Add(row);
                }
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

        public void AddNewRows_1(List<CEntityRealTime> realtimes)
        {
            List<CEntityRealTime> AddList = new List<CEntityRealTime>();
            List<CEntityRealTime> UpdateList = new List<CEntityRealTime>();
            foreach(CEntityRealTime realtime in realtimes)
            {
                if (IsInDB(realtime)) { UpdateList.Add(realtime); }
                else { AddList.Add(realtime); }
            }

            /// 在表中的就更新
            AddNewRows(AddList);
            /// 不在表中的就加表
            UpdateRows(UpdateList);
        }

        public void AddNewRow_2(CEntityRealTime realtime)
        {
            if (IsInDB(realtime))
            {
                // 如果数据库中有，就判断是否为最新数据
                //if (IsNew(realtime))
                //{

                //}
                m_mutexDataTable.WaitOne(); //等待互斥量

                DataRow row = m_tableDataUpdated.NewRow();
                row[CN_CName] = realtime.StrStationName;
                row[CN_StationId] = realtime.StrStationID;
                row[CN_StationType] = realtime.EIStationType;
                row[CN_DataTime] = realtime.TimeDeviceGained.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                row[CN_YesterdayRain] = realtime.LastDayRainFall;
                row[CN_TodayRain] = realtime.DDayRainFall;
                row[CN_PeriodRain] = realtime.DPeriodRain;
                row[CN_WaterStage] = realtime.DWaterYield;
                row[CN_WaterFlow] = realtime.DWaterFlowActual;
                row[CN_Voltage] = realtime.Dvoltage;
                row[CN_DataState] = realtime.ERTDState;
                row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(realtime.EIMessageType);
                row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(realtime.EIChannelType);
                m_tableDataUpdated.Rows.Add(row);

                m_mutexDataTable.ReleaseMutex();

                // 判断是否需要创建新分区
                //CSQLPartitionMgr.Instance.MaintainVoltage(voltage.TimeCollect);
                if (m_tableDataUpdated.Rows.Count >= CDBParams.GetInstance().AddBufferMax)
                {
                    // 如果超过最大值，写入数据库
                    NewTask(() => { AddDataToDB(); });
                }
                else
                {
                    // 没有超过缓存最大值，开启定时器进行检测,多次调用Start()会导致重新计数
                    m_updateTimer.Start();
                }
            }
            else
            {
                // 如果数据库中没有，就将当前的数据写入数据库
                m_mutexDataTable.WaitOne(); //等待互斥量
                DataRow row = m_tableDataAdded.NewRow();
                row[CN_CName] = realtime.StrStationName;
                row[CN_StationId] = realtime.StrStationID;
                row[CN_StationType] = realtime.EIStationType;
                row[CN_DataTime] = realtime.TimeDeviceGained.ToString(CDBParams.GetInstance().DBDateTimeFormat);
                row[CN_YesterdayRain] = realtime.LastDayRainFall;
                row[CN_TodayRain] = realtime.DDayRainFall;
                row[CN_PeriodRain] = realtime.DPeriodRain;
                row[CN_WaterStage] = realtime.DWaterYield;
                row[CN_WaterFlow] = realtime.DWaterFlowActual;
                row[CN_Voltage] = realtime.Dvoltage;
                row[CN_DataState] = realtime.ERTDState;
                row[CN_MsgType] = CEnumHelper.MessageTypeToDBStr(realtime.EIMessageType);
                row[CN_TransType] = CEnumHelper.ChannelTypeToDBStr(realtime.EIChannelType);
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
        }

        public bool DeleteRows(List<CEntityRealTime> realtimes)
        {
            // 删除某条实时记录
            StringBuilder sql = new StringBuilder();
            int currentBatchCount = 0;
            foreach (CEntityRealTime realtime in realtimes)
            {
                ++currentBatchCount;
                sql.AppendFormat("delete from {0} where {1}={2} and {3}='{4}';",
                    CT_TableName,
                    CN_StationId, realtime.StrStationID,
                    CN_CName, realtime.StrStationName
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
            // 如何考虑线程同异步
            if (!ExecuteSQLCommand(sql.ToString()))
            {
                return false;
            }
            ResetAll();
            return true;
        }

        public bool UpdateRows(List<CEntityRealTime> realtimes)
        {
            // 除主键和站点信息外，其余信息随意修改
            StringBuilder sql = new StringBuilder();
            int currentBatchCount = 0;
            for (int i = 0; i < realtimes.Count; i++)
            {
                ++currentBatchCount;
                sql.AppendFormat("update {0} set {1}={2},{3}={4},{5}={6},{7}={8},{9}={10},{11}={12},{13}={14},{15}={16},{17}={18},{19}={20} where {21}={22} and {23}='{24}';",
                    CT_TableName,
                    CN_YesterdayRain, realtimes[i].LastDayRainFall,
                    CN_TodayRain, realtimes[i].DDayRainFall,
                    CN_PeriodRain, realtimes[i].DPeriodRain,
                    CN_WaterStage, realtimes[i].DWaterYield,
                    CN_WaterFlow, realtimes[i].DWaterFlowActual,
                    CN_Voltage, realtimes[i].Dvoltage,
                    CN_TransType, CEnumHelper.ChannelTypeToDBStr(realtimes[i].EIChannelType),
                    CN_MsgType, CEnumHelper.MessageTypeToDBStr(realtimes[i].EIMessageType),
                    CN_DataTime, realtimes[i].TimeDeviceGained,
                    CN_DataState, realtimes[i].EIStationType,
                    CN_StationId, realtimes[i].StrStationID,
                    CN_CName, realtimes[i].StrStationName.ToString()
                //    CN_VoltageID, voltages[i].VoltageID
                );
                //if (currentBatchCount >= CDBParams.GetInstance().UpdateBufferMax)
                //{
                //    // 更新数据库
                //    if (!this.ExecuteSQLCommand(sql.ToString()))
                //    {
                //        // 保存失败
                //        return false;
                //    }
                //    sql.Clear(); //清除以前的所有命令
                //    currentBatchCount = 0;
                //}
            }
            // 更新数据库
            if (!this.ExecuteSQLCommand(sql.ToString()))
            {
                return false;
            }
            sql.Clear(); //清除以前的所有命令
            ResetAll();
            return true;
        }


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

            try
            {
                //将临时表中的内容写入数据库
                //SqlConnection conn = CDBManager.GetInstacne().GetConnection();
                //conn.Open();
                string connstr = CDBManager.Instance.GetConnectionString();
                // //指定大容量插入是否对表激发触发器。此属性的默认值为 False。
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connstr, SqlBulkCopyOptions.FireTriggers))
                {
                    // 实时表有插入触发器
                    bulkCopy.BatchSize = 1;
                    bulkCopy.BulkCopyTimeout = 1800;
                    bulkCopy.DestinationTableName = CT_TableName;
                    bulkCopy.ColumnMappings.Add(CN_CName, CN_CName);
                    bulkCopy.ColumnMappings.Add(CN_StationId, CN_StationId);
                    bulkCopy.ColumnMappings.Add(CN_StationType, CN_StationType);
                    bulkCopy.ColumnMappings.Add(CN_YesterdayRain, CN_YesterdayRain);
                    bulkCopy.ColumnMappings.Add(CN_TodayRain, CN_TodayRain);
                    bulkCopy.ColumnMappings.Add(CN_PeriodRain, CN_PeriodRain);
                    bulkCopy.ColumnMappings.Add(CN_WaterStage, CN_WaterStage);
                    bulkCopy.ColumnMappings.Add(CN_WaterFlow, CN_WaterFlow);
                    bulkCopy.ColumnMappings.Add(CN_Voltage, CN_Voltage);
                    bulkCopy.ColumnMappings.Add(CN_DataState, CN_DataState);
                    bulkCopy.ColumnMappings.Add(CN_TransType, CN_TransType);
                    bulkCopy.ColumnMappings.Add(CN_MsgType, CN_MsgType);
                    bulkCopy.ColumnMappings.Add(CN_DataTime, CN_DataTime);
                    try
                    {
                        bulkCopy.WriteToServer(tmp);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                }
                //conn.Close();   //关闭连接
                Debug.WriteLine("###{0} :add {1} lines to currentdata db", DateTime.Now, tmp.Rows.Count);
                CDBLog.Instance.AddInfo(string.Format("添加{0}行到实时表", tmp.Rows.Count));
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
            finally
            {
                //S_MutexWriteToDB.ReleaseMutex();
                m_mutexWriteToDB.ReleaseMutex();
            }

        }

        // 将当前所有数据插入数据库
        //protected override bool UpdateToDB()
        //{
        //    // 然后获取内存表的访问权
        //    m_mutexDataTable.WaitOne();

        //    if (m_tableDataUpdated.Rows.Count <= 0)
        //    {
        //        m_mutexDataTable.ReleaseMutex();
        //        return true;
        //    }
        //    //清空内存表的所有内容，把内容复制到临时表tmp中
        //    DataTable tmp = m_tableDataUpdated.Copy();
        //    m_tableDataUpdated.Rows.Clear();

        //    // 释放内存表的互斥量
        //    m_mutexDataTable.ReleaseMutex();

        //    // 先获取对数据库的唯一访问权
        //    m_mutexWriteToDB.WaitOne();

        //    try
        //    {
        //        //将临时表中的内容写入数据库
        //        //SqlConnection conn = CDBManager.GetInstacne().GetConnection();
        //        //conn.Open();
        //        string connstr = CDBManager.Instance.GetConnectionString();
        //        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connstr))
        //        {
        //            // 实时表有插入触发器
        //            bulkCopy.BatchSize = 1;
        //            bulkCopy.BulkCopyTimeout = 1800;
        //            bulkCopy.DestinationTableName = CT_TableName;
        //            bulkCopy.ColumnMappings.Add(CN_CName, CN_CName);
        //            bulkCopy.ColumnMappings.Add(CN_StationId, CN_StationId);
        //            bulkCopy.ColumnMappings.Add(CN_StationType, CN_StationType);
        //            bulkCopy.ColumnMappings.Add(CN_YesterdayRain, CN_YesterdayRain);
        //            bulkCopy.ColumnMappings.Add(CN_TodayRain, CN_TodayRain);
        //            bulkCopy.ColumnMappings.Add(CN_PeriodRain, CN_PeriodRain);
        //            bulkCopy.ColumnMappings.Add(CN_WaterStage, CN_WaterStage);
        //            bulkCopy.ColumnMappings.Add(CN_WaterFlow, CN_WaterFlow);
        //            bulkCopy.ColumnMappings.Add(CN_Voltage, CN_Voltage);
        //            bulkCopy.ColumnMappings.Add(CN_DataState, CN_DataState);
        //            bulkCopy.ColumnMappings.Add(CN_TransType, CN_TransType);
        //            bulkCopy.ColumnMappings.Add(CN_MsgType, CN_MsgType);
        //            bulkCopy.ColumnMappings.Add(CN_DataTime, CN_DataTime);
        //            try
        //            {
        //                bulkCopy.WriteToServer(tmp);
        //            }
        //            catch (Exception e)
        //            {
        //                Debug.WriteLine(e.ToString());
        //            }
        //        }
        //        //conn.Close();   //关闭连接
        //        Debug.WriteLine("###{0} :add {1} lines to currentdata db", DateTime.Now, tmp.Rows.Count);
        //        CDBLog.Instance.AddInfo(string.Format("添加{0}行到实时表", tmp.Rows.Count));
        //        return true;
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Debug.WriteLine(ex.ToString());
        //        return false;
        //    }
        //    finally
        //    {
        //        //S_MutexWriteToDB.ReleaseMutex();
        //        m_mutexWriteToDB.ReleaseMutex();
        //    }

        //}

        //private static Boolean IsNew(CEntityRealTime realTime)
        //{
        //    String station = realTime.StrStationID;
        //    DateTime dateTime = realTime.TimeDeviceGained;
        //    String sql = "select * from " + CT_TableName + " where StationID='" + station + " And DataTime >= " + dateTime + "';";
        //    SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
        //    DataTable dataTableTemp = new DataTable();
        //    adapter.Fill(dataTableTemp);
        //    if (dataTableTemp.Rows.Count > 0) return true;
        //    return false;
        //}

        private static Boolean IsInDB(CEntityRealTime realTime)
        {
            String station = realTime.StrStationID;
            String sql = "select * from " + CT_TableName + " where StationID='" + station +"';";
            SqlDataAdapter adapter = new SqlDataAdapter(sql, CDBManager.GetInstacne().GetConnection());
            DataTable dataTableTemp = new DataTable();
            adapter.Fill(dataTableTemp);
            if (dataTableTemp.Rows.Count > 0) return true;
            return false;
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

            // 先获取对数据库的唯一访问权
            // m_mutexWriteToDB.WaitOne();

            try
            {
                //将临时表中的内容写入数据库
                //SqlConnection conn = CDBManager.GetInstacne().GetConnection();
                //conn.Open();
                // //指定大容量插入是否对表激发触发器。此属性的默认值为 False。
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(CDBManager.GetInstacne().GetConnectionString(), SqlBulkCopyOptions.FireTriggers))
                {
                    // 实时表有插入触发器
                    //bulkCopy.BatchSize = 1;
                    bulkCopy.BulkCopyTimeout = 1800;
                    bulkCopy.DestinationTableName = CT_TableName;
                    bulkCopy.ColumnMappings.Add(CN_CName, CN_CName);
                    bulkCopy.ColumnMappings.Add(CN_StationId, CN_StationId);
                    bulkCopy.ColumnMappings.Add(CN_StationType, CN_StationType);
                    bulkCopy.ColumnMappings.Add(CN_YesterdayRain, CN_YesterdayRain);
                    bulkCopy.ColumnMappings.Add(CN_TodayRain, CN_TodayRain);
                    bulkCopy.ColumnMappings.Add(CN_PeriodRain, CN_PeriodRain);
                    bulkCopy.ColumnMappings.Add(CN_WaterStage, CN_WaterStage);
                    bulkCopy.ColumnMappings.Add(CN_WaterFlow, CN_WaterFlow);
                    bulkCopy.ColumnMappings.Add(CN_Voltage, CN_Voltage);
                    bulkCopy.ColumnMappings.Add(CN_DataState, CN_DataState);
                    bulkCopy.ColumnMappings.Add(CN_TransType, CN_TransType);
                    bulkCopy.ColumnMappings.Add(CN_MsgType, CN_MsgType);
                    bulkCopy.ColumnMappings.Add(CN_DataTime, CN_DataTime);
                    try
                    {
                        bulkCopy.WriteToServer(tmp);
                        Debug.WriteLine("###{0} :add {1} lines to currentdata db", DateTime.Now, tmp.Rows.Count);
                        CDBLog.Instance.AddInfo(string.Format("添加{0}行到实时表", tmp.Rows.Count));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        //如果出现异常，SqlBulkCopy 会使数据库回滚，所有Table中的记录都不会插入到数据库中，
                        //此时，把Table折半插入，先插入一半，再插入一半。如此递归，直到只有一行时，如果插入异常，则返回。
                        if (tmp.Rows.Count == 1)
                            return;
                        int middle = tmp.Rows.Count / 2;
                        DataTable table = tmp.Clone();
                        for (int i = 0; i < middle; i++)
                            table.ImportRow(tmp.Rows[i]);

                        InsertSqlBulk(table);

                        table.Clear();
                        for (int i = middle; i < tmp.Rows.Count; i++)
                            table.ImportRow(tmp.Rows[i]);
                        InsertSqlBulk(table);
                    }
                }
                //conn.Close();   //关闭连接
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                //  m_mutexWriteToDB.ReleaseMutex();
                return;
            }

            //  m_mutexWriteToDB.ReleaseMutex();
            return;
        }


        protected void BatchInsert()
        {
            if (batchInsertRealtimeList == null || batchInsertRealtimeList.Count == 0)
            {
                return;
            }
            List<CEntityRealTime> realtimeList = new List<CEntityRealTime>();
            lock (batchInsertRealtimeList)
            {
                realtimeList.AddRange(batchInsertRealtimeList);
                batchInsertRealtimeList.Clear();
            }
            Dictionary<string, object> param = new Dictionary<string, object>();
            string suffix = "/currentdata/insertCurrentdataCS";
            string url = "http://" + urlPrefix + suffix;
            Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(realtimeList, Newtonsoft.Json.Formatting.None, timeConverter);
            param["currentdata"] = jsonStr;
            try
            {
                string resultJson = HttpHelper.Post(url, param);
                CDBLog.Instance.AddInfo(string.Format("添加{0}行到实时数据表", realtimeList.Count));
            }
            catch (Exception e)
            {
                Debug.WriteLine("添加雨量信息失败");
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
        #endregion ///< 帮助方法
    }
}