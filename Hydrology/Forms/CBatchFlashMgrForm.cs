using System;
using System.Diagnostics;
using System.Windows.Forms;
using Hydrology.CControls;
using Hydrology.DataMgr;
using Hydrology.Entity;
using System.Collections.Generic;
using Hydrology.DBManager.DB.SQLServer;
using Hydrology.DBManager.Interface;
using System.Text;
using System.IO;
using System.Threading;

namespace Hydrology.Forms
{
    public partial class CBatchFlashMgrForm : Form
    {

        private EChannelType m_channelType = EChannelType.GPRS;

        public CBatchFlashMgrForm()
        {
            InitializeComponent();

            Init();

            // 初始化焦点切换
            FormHelper.InitControlFocusLoop(this);
        }
        private void Init()
        {
            //   this.Text = "批量传输 - Flash数据";
            this.Text = "批量传输";
            dtp_EndTime.Format = DateTimePickerFormat.Custom;
            dtp_EndTime.CustomFormat = "yyyy-MM-dd HH";
            dtp_EndTime.Value = DateTime.Now;
            dtp_StartTime.Format = DateTimePickerFormat.Custom;
            dtp_StartTime.CustomFormat = "yyyy-MM-dd HH";
            dtp_StartTime.Value = DateTime.Now.AddDays(-1);
            //radioHour.Checked = true;

            ReloadComboBox();
            ReloadListView();

            CProtocolEventManager.BatchForUI += this.BatchForUI_EventHandler;
            CProtocolEventManager.ErrorForUI += this.ErrorForUI_EventHandler;
        }

        private void ReloadComboBox()
        {
            this.groupBox2.Controls.Remove(this.cmbStation);
            this.cmbStation = new CStationComboBox(EStationBatchType.EFlash);
            this.cmbStation.FormattingEnabled = true;
            this.cmbStation.Location = new System.Drawing.Point(172, 17);
            this.cmbStation.Name = "cmbStation";
            this.cmbStation.Size = new System.Drawing.Size(167, 20);
            this.cmbStation.TabIndex = 4;
            this.groupBox2.Controls.Add(this.cmbStation);
        }
        private void ReloadListView()
        {
            this.groupBox3.Controls.Remove(this.listView1);

            this.listView1 = new CExListView() { BHorizentalScroolVisible = false };
            this.listView1.BackColor = System.Drawing.SystemColors.Info;
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView1.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.listView1.FullRowSelect = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;

            this.listView1.Columns.Add("表头", -2, HorizontalAlignment.Left);
            this.groupBox3.Controls.Add(this.listView1);
        }

        private void ErrorForUI_EventHandler(object sender, ReceiveErrorEventArgs e)
        {
            string msg = e.Msg;
            try
            {
                if (this.IsHandleCreated)
                {
                    this.listView1.Invoke((Action)delegate
                    {
                        try
                        {
                            if (msg != "ATE0")
                            {
                                this.listView1.Items.Add(new ListViewItem()
                                {
                                    Text = String.Format("[{0}]  接收数据 : {1}", this.m_channelType.ToString(), msg)
                                });
                            }
                        }
                        catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    });
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }
        private void BatchForUI_EventHandler(object sender, BatchEventArgs e)
        {
            //20170512改动过
            var result = e.Value;
            string rawData = e.RawData;

            BatchDataImport m_batchDataImport = new BatchDataImport();

            if (result != null)
            {
                if (this.IsHandleCreated)
                {
                    //  导入数据库
                    //BatchDataImport.Import(result);
                    DateTime DStartTime = new DateTime();
                    DateTime DEndTime = new DateTime();
                    //if (this.radioHour.Checked== true)
                    //{
                    //    DateTime tmp = this.dtp_StartTime.Value;
                    //    //DateTime tmp_1 = this.dtp_EndTime.Value;
                    //    DStartTime = new DateTime(tmp.Year, tmp.Month, tmp.Day, tmp.Hour, 0, 0);
                    //    DEndTime = this.dtp_EndTime.Value;
                    //}
                    //else if (this.radioDay.Checked == true)
                    //{
                    //    DateTime tmp = this.dtp_StartTime.Value;
                    //    DateTime tmp_1 = this.dtp_EndTime.Value;
                    //    DStartTime = new DateTime(tmp.Year, tmp.Month, tmp.Day, 8, 0, 0);
                    //    DEndTime =new DateTime(tmp_1.Year, tmp_1.Month, tmp_1.Day, 23, 59, 59);
                    //}
                    //DateTime DStartTime = this.dtp_StartTime.Value;
                    //DateTime DEndTime = this.dtp_EndTime.Value;
                    if (radioButton2.Checked)
                    {
                        m_batchDataImport.Import(result, DStartTime, DEndTime);
                    }
                    //m_batchDataImport.Import(result, DStartTime, DEndTime);
                    if (radioButtonSave.Checked)
                    {
                        if (result.StationType == EStationType.ERainFall)
                        {
                            string filePath = @"BatchData\\Rain.txt";//这里是你的已知文件
                            string strPath = Path.GetDirectoryName(filePath);
                            if (!Directory.Exists(strPath))
                            {
                                Directory.CreateDirectory(strPath);
                            }
                            StreamWriter sw = new StreamWriter(filePath, true, Encoding.GetEncoding("gb2312"));
                            sw.Write("---------------------------------------------" + "\r\n");
                            sw.Write("接收数据 : " + rawData + "\r\n");//写你的字符串。
                            sw.Write("站点ID : " + result.StationID + "\r\n");//写你的字符串。
                            sw.Write("站点类型: " + "水位" + "\r\n");//写你的字符串。
                            foreach (var item in result.Datas)
                            {
                                try
                                {
                                    sw.Write(item.Time.ToString() + "    " + item.Data + "\r\n");
                                }
                                catch (Exception exp) { Debug.WriteLine(exp.Message); }
                            }
                            sw.Close();
                        }
                        if (result.StationType == EStationType.ERiverWater)
                        {
                            string filePath = "BatchData\\Water.txt";//这里是你的已知文件
                            string strPath = Path.GetDirectoryName(filePath);
                            if (!Directory.Exists(strPath))
                            {
                                Directory.CreateDirectory(strPath);
                            }
                            StreamWriter sw = new StreamWriter(filePath, true, Encoding.GetEncoding("gb2312"));
                            sw.Write("---------------------------------------------" + "\r\n");
                            sw.Write("接收数据 : " + rawData + "\r\n");//写你的字符串。
                            sw.Write("站点ID : " + result.StationID + "\r\n");//写你的字符串。
                            sw.Write("站点类型: " + "雨量" + "\r\n");//写你的字符串。
                            foreach (var item in result.Datas)
                            {
                                try
                                {
                                    sw.Write(item.Time.ToString() + "    " + item.Data + "\r\n");
                                }
                                catch (Exception exp) { Debug.WriteLine(exp.Message); }
                            }
                            sw.Close();
                        }
                    }

                    //  更新界面

                    this.listView1.Invoke((Action)delegate
                    {
                        try
                        {
                            this.listView1.Items.Add(new ListViewItem()
                            {
                                Text = "接收数据 : " + rawData
                            });

                        }
                        catch (Exception exp) { Debug.WriteLine(exp.Message); }
                        try
                        {
                            this.listView1.Items.Add(new ListViewItem()
                            {
                                Text = "站点ID : " + result.StationID
                            });
                        }
                        catch (Exception exp) { Debug.WriteLine(exp.Message); }
                        try
                        {
                            if (result.StationType == EStationType.ERiverWater)
                            {
                                this.listView1.Items.Add(new ListViewItem()
                                {
                                    Text = "站点类型: " + "雨量"
                                });
                            }
                            else if (result.StationType == EStationType.ERainFall)
                            {
                                this.listView1.Items.Add(new ListViewItem()
                                {
                                    Text = "站点类型: " + "水位"
                                });
                            }


                        }
                        catch (Exception exp) { Debug.WriteLine(exp.Message); }
                        foreach (var item in result.Datas)
                        {
                            try
                            {
                                this.listView1.Items.Add(new ListViewItem()
                                {
                                    Text = item.Time.ToString() + "    " + item.Data
                                });
                            }
                            catch (Exception exp) { Debug.WriteLine(exp.Message); }
                        }
                    });
                }
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (this.listView1.Items.Count > 0)
                this.listView1.Items.Clear();
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void btnStartTrans_Click(object sender, EventArgs e)
        {
            var station = (this.cmbStation as CStationComboBox).GetStation();
            if (station == null)
                return;

            string sid = station.StationID;

            var stype = station.StationType;
            //ETrans trans = this.radioHour.Checked ? ETrans.ByHour : ETrans.ByDay;
            DateTime beginTime = new DateTime();
            DateTime endTime = new DateTime();
            DateTime tmp = this.dtp_StartTime.Value;
            DateTime tmp_1 = this.dtp_EndTime.Value;
            beginTime = new DateTime(tmp.Year, tmp.Month, tmp.Day, tmp.Hour, 0, 0);
            endTime = new DateTime(tmp_1.Year, tmp_1.Month, tmp_1.Day, tmp_1.Hour, 0, 0);
            if (tmp > tmp_1)
            {
                MessageBox.Show("起始时间不能大于结束时间!");
                return;
            }
            //string msg1 = beginTime.Year.ToString()
            //station.StationType = radioRain.Checked ? EStationType.ERainFall : EStationType.EHydrology;

            string logMsg = String.Format("--------批量传输    目标站点（{0:D4}）--------- ", int.Parse(sid));
            // 写入系统日志
            CSystemInfoMgr.Instance.AddInfo(logMsg);
            this.listView1.Items.Add(logMsg);
            string qry = CPortDataMgr.Instance.SendFlashMsg(station, ETrans.ByHour, beginTime, endTime,EChannelType.TCP);
            this.listView1.Items.Add(new ListViewItem()
            {
                Text = String.Format("[{0}]  发送数据:  {1}", this.m_channelType.ToString(), qry)
            });
        }
        
        private void CReadAndSettingMgrForm_StationSelected(object sender, CEventSingleArgs<CEntityStation> e)
        {
            var station = e.Value;
            if (station == null)
                return;
        }





    }

    //gm 20161016

    class BatchDataImport
    {
        public static int flag;
        private IWaterProxy m_proxyWater;
        private IRainProxy m_proxyRain;
        private IStationProxy m_proxyStation;
        List<CEntityRain> rainResults = new List<CEntityRain>();
        List<CEntityWater> waterResults = new List<CEntityWater>();


        public BatchDataImport()
        {
            flag = 0;
            m_proxyWater = new CSQLWater();
            m_proxyRain = new CSQLRain();
            m_proxyStation = new CSQLStation();


        }
        public void Import(CBatchStruct batch, DateTime DStartTime, DateTime DEndTime, bool isUpdate = false)
        {
            DateTime? lastTime = null;
            ERTDDataState tmpRTDWaterDataState = ERTDDataState.ENormal;
            ////gm  1024 $60091K02031610220800084505230800084505
            //string rawString = rawData;
            //string startTime = rawString.Substring(11, 10);
            //string endTime = rawString.Substring(27, 6);
            //string strYear = 20 + startTime.Substring(0, 2);
            //string strMonth = startTime.Substring(2, 2);
            //string strStartDay = startTime.Substring(4, 2);
            //string strStartHour = startTime.Substring(6, 2);
            //string strStartMinute = startTime.Substring(8, 2);
            //string strEndDay = endTime.Substring(0, 2);
            //string strEndHour = endTime.Substring(2, 2);
            //string strEndMinute = endTime.Substring(4, 2);

            ////DateTime DStartTime = new DateTime(int.Parse(strYear), int.Parse(strMonth), int.Parse(strStartDay), int.Parse(strStartHour), int.Parse(strStartMinute), 0);
            ////DateTime DEndTime = new DateTime(int.Parse(strYear), int.Parse(strMonth), int.Parse(strEndDay), int.Parse(strEndHour), int.Parse(strEndMinute), 0);

            List<CTimeAndData> datas = batch.Datas;
            string stationid = batch.StationID;
            EStationType type = batch.StationType;
            CEntityStation station = m_proxyStation.QueryById(stationid);
            //  if (type == EStationType.ERainFall)
            if (type == EStationType.ERiverWater)
            // if (true)
            {
                for (int i = 0; i < datas.Count; i++)
                {
                    if (lastTime == datas[i].Time)
                    {
                        lastTime = datas[i].Time;
                        continue;
                    }
                    lastTime = datas[i].Time;
                    int status = 1;
                    DateTime dt = datas[i].Time;
                    string data = datas[i].Data.ToString();
                    int RawtotalRain = int.Parse(data.Substring(0, 4));
                    Nullable<Decimal> RawrainAccuracy = decimal.Parse((data.Substring(4, 2)));
                    Nullable<Decimal> rainAccuracy = RawrainAccuracy / 10;
                    Nullable<Decimal> totalRain = RawtotalRain * rainAccuracy;
                    CEntityRain LastSharpMes = new CEntityRain();
                    CEntityRain LastDayMes = new CEntityRain();
                    //如果是第一条数据，则通过数据库获取上一条数据
                    if (i == 0)
                    {
                        Nullable<Decimal> lastTotalRain = null, lastDayTotalRain = null, lastSharpClockTotalRain = null;
                        Nullable<DateTime> lastDayTime = null;
                        Nullable<DateTime> lastClockSharpTime = null;
                        Nullable<DateTime> lastDataTime = null;
                        Nullable<Decimal> lastPeriodRain = null;
                        Nullable<EChannelType> lastChannelType = null;
                        Nullable<EMessageType> lastMessageType = null;

                        LastSharpMes = m_proxyRain.GetLastSharpRain(stationid, dt);
                        LastDayMes = m_proxyRain.GetLastDayRain(stationid, dt);

                        lastTotalRain = m_proxyRain.GetLastRain(stationid, dt).TotalRain;
                        lastSharpClockTotalRain = LastSharpMes.TotalRain;
                        lastDayTotalRain = LastDayMes.TotalRain;

                        lastClockSharpTime = LastSharpMes.TimeCollect;
                        lastDayTime = LastDayMes.TimeCollect;

                        //改动

                        // 查询成功

                        station.LastTotalRain = lastTotalRain;
                        station.LastDayTotalRain = lastDayTotalRain;
                        station.LastPeriodRain = lastPeriodRain;
                        if (lastDataTime != null && lastDataTime.HasValue)
                        {
                            station.LastDataTime = lastDataTime;
                        }
                        if (lastClockSharpTime != null && lastClockSharpTime.HasValue)
                        {
                            station.LastClockSharpTime = lastClockSharpTime;
                        }
                        if (lastDayTime != null && lastDayTime.HasValue)
                        {
                            station.LastDayTime = lastDayTime;
                        }
                        if (lastChannelType != null && lastChannelType.HasValue)
                        {
                            station.LastChannelType = lastChannelType;
                        }
                        if (lastMessageType != null && lastMessageType.HasValue)
                        {
                            station.LastMessageType = lastMessageType;
                        }

                        //int year = dt.Year;
                        //int month = dt.Month;
                        //int day = dt.Day;
                        //DateTime tmp1 = new DateTime(year, month, day, 8, 0, 0);
                        //DateTime tmp2 = tmp1.Subtract(new TimeSpan(24, 0, 0));
                        //station.LastDayTime = tmp2;
                        //lastSharpClockTotalRain = m_proxyRain.GetLastClockSharpTotalRain(stationid, dt);
                        //lastDayTotalRain = m_proxyRain.GetLastDayTotalRain(stationid, tmp2);
                        station.LastTotalRain = lastTotalRain;

                        station.LastClockSharpTotalRain = lastSharpClockTotalRain;
                        station.LastClockSharpTime = lastClockSharpTime;

                        station.LastDayTotalRain = lastDayTotalRain;
                        station.LastDayTime = lastDayTime;
                    }
                    CEntityRain rain = new CEntityRain();
                    rain.StationID = stationid;
                    rain.TimeCollect = dt;
                    rain.TotalRain = totalRain;
                    // rain.DifferneceRain = CalDifferenceRain(rainAccuracy, RawtotalRain, station.LastTotalRain, station.DRainChange, ref status);

                    if ((dt.Minute + dt.Second) == 0)
                    {
                        //rain.PeriodRain = CalPeriodRain(rainAccuracy, RawtotalRain, dt, station.LastClockSharpTotalRain);
                        rain.PeriodRain = CalPeriodRain_2(rainAccuracy, RawtotalRain, dt, station.LastClockSharpTotalRain, station.LastClockSharpTime, station.LastTotalRain);
                        station.LastClockSharpTotalRain = totalRain;
                        station.LastClockSharpTime = dt;
                    }
                    if (dt.Hour == 8)
                    {
                        //rain.DayRain = CalDayRain(rainAccuracy, RawtotalRain, dt, station.LastDayTime, station.LastDayTotalRain);
                        rain.DayRain = CalDayRain_2(rainAccuracy, RawtotalRain, dt, station.LastDayTotalRain, station.LastDayTime);
                        station.LastDayTotalRain = totalRain;
                        station.LastDayTime = dt;
                    }
                    rain.DifferneceRain = CalDifferenceRain_1(rainAccuracy, RawtotalRain, station.LastTotalRain, station.DRainChange, ref status);
                    station.LastTotalRain = totalRain;
                    // 待检测
                    rain.ChannelType = EChannelType.GPRS;
                    rain.MessageType = EMessageType.Batch;

                    rain.TimeRecieved = DateTime.Now;
                    if (status == 1)
                    {
                        rain.BState = 1;
                    }
                    else
                    {
                        rain.BState = 0;
                    }

                    rainResults.Add(rain);
                }
                List<CEntityRain> listInsert = new List<CEntityRain>();
                List<CEntityRain> listUpdate = new List<CEntityRain>();
                List<DateTime> listDateTime = m_proxyRain.getExistsTime(stationid, DStartTime, DEndTime);
                for (int i = 0; i < rainResults.Count; i++)
                {
                    if (listDateTime.Contains(rainResults[i].TimeCollect))
                    {
                        listUpdate.Add(rainResults[i]);
                    }
                    else
                    {
                        listInsert.Add(rainResults[i]);
                    }
                }
                m_proxyRain.AddNewRows(listInsert); //写入数据库
                m_proxyRain.UpdateRows(listUpdate);//更新

                //m_proxyRain.AddOrUpdate(rainResults);
            }
            //操作water表
            //   if (type == EStationType.ERiverWater)
            if (type == EStationType.ERainFall)
            {
                for (int i = 0; i < datas.Count; i++)
                {
                    decimal data = 0;
                    DateTime dt = datas[i].Time;
                    try
                    {
                        string strData = datas[i].Data.Trim();
                        //decimal data = int.Parse(datas[i].Data) / 100;
                        data = decimal.Parse(strData) / 100;
                    }
                    catch (Exception e)
                    {
                        break;
                    }
                    //操作water表
                    CEntityWater water = new CEntityWater();
                    water.StationID = station.StationID;
                    water.TimeCollect = dt;
                    water.TimeRecieved = DateTime.Now;
                    if (station.DWaterBase.HasValue)
                    {
                        // 减去水位基值
                        water.WaterStage = data + station.DWaterBase.Value;
                    }
                    else
                    {
                        water.WaterStage = data;
                    }
                    water.WaterFlow = CDBDataMgr.GetInstance().GetWaterFlowByWaterStageAndStation(stationid, data);
                    //此处 waterflow需要计算的
                    water.ChannelType = EChannelType.GPRS;
                    water.MessageType = EMessageType.Batch;
                    AssertWaterData(water, ref tmpRTDWaterDataState);
                    //if (tmpRTDWaterDataState == ERTDDataState.ENormal)
                    //{
                    //    water.state = 1;
                    //}
                    //if (tmpRTDWaterDataState == ERTDDataState.EError)
                    //{
                    //    water.state = 0;
                    //}
                    //if (tmpRTDWaterDataState == ERTDDataState.EWarning)
                    //{
                    //    water.state = 2;
                    //}

                    waterResults.Add(water);
                }
                List<CEntityWater> listInsert = new List<CEntityWater>();
                List<CEntityWater> listUpdate = new List<CEntityWater>();
                List<DateTime> listDateTime = m_proxyWater.getExistsTime(stationid, DStartTime, DEndTime);
                for (int i = 0; i < waterResults.Count; i++)
                {
                    if (listDateTime.Contains(waterResults[i].TimeCollect))
                    {
                        listUpdate.Add(waterResults[i]);
                    }
                    else
                    {
                        listInsert.Add(waterResults[i]);
                    }
                }
                // 
                m_proxyWater.AddNewRows(listInsert); //写入数据库
                m_proxyWater.UpdateRows(listUpdate);//更新数据库

                //m_proxyWater.AddOrUpdate(waterResults);
            }


        }
        // 站点类型
        //     01为水位 
        //     02为雨量
        //  若数据库中没有时间相同的记录，则更新
        //  若数据库中包含时间相同的记录，且isUpdate = true 判断是否更新
        private Nullable<Decimal> CalDifferenceRain_1(Nullable<Decimal> dRainArruracy, Nullable<Decimal> totalRain, Nullable<Decimal> lastTotalRain, Nullable<Decimal> MaxChange, ref int status)
        {
            Nullable<Decimal> dDifferenceRain = null;
            status = 1;
            // 差值雨量
            if (lastTotalRain.HasValue)
            {
                dDifferenceRain = dRainArruracy * totalRain - lastTotalRain.Value;
            }
            if (dDifferenceRain < 0)
            {
                decimal tmp = (decimal)MaxChange / (decimal)dRainArruracy;
                if ((9999 - lastTotalRain.Value / (Decimal)dRainArruracy) > tmp)
                {
                    dDifferenceRain = 0;
                    status = 0;
                }
                else
                {
                    dDifferenceRain = 9999 * (Decimal)dRainArruracy - lastTotalRain.Value + (totalRain);
                }
            }
            return dDifferenceRain;
        }
        private Nullable<Decimal> CalPeriodRain_2(Nullable<Decimal> dRainArruracy, Decimal? totalRain, DateTime datetime, Nullable<Decimal> lastSharpTotalRain, Nullable<DateTime> lastSharpTime, Nullable<Decimal> lastTotalRain)
        {
            Nullable<Decimal> dPeriodRain = null;
            DateTime tmp = DateTime.Now;
            if (lastSharpTotalRain.HasValue && lastSharpTime.HasValue)
            {
                TimeSpan timespan = datetime - lastSharpTime.Value;
                if (1 == timespan.Hours)
                {
                    dPeriodRain = dRainArruracy * totalRain - lastSharpTotalRain;
                }
                else
                {
                    //lastSharpTotalRain = m_proxyRain.GetLastClockSharpTotalRain(stationid, datetime);
                    dPeriodRain = dRainArruracy * totalRain - lastTotalRain;

                }
            }
            return dPeriodRain;

        }
        private Nullable<Decimal> CalDayRain_2(Nullable<Decimal> dRainArruracy, Nullable<Decimal> totalRain, DateTime datetime, Nullable<Decimal> lastDayTotalRain, Nullable<DateTime> LastDayTime)
        {
            Nullable<Decimal> dDayRain = null;
            // 如果时间设置的误差范围内，整点，默认m_iMutesRange为0

            int offset = (datetime.Hour - 8) * 60 + datetime.Minute;
            // 计算日雨量,日期相差一天
            if (lastDayTotalRain.HasValue && LastDayTime.HasValue)
            {
                TimeSpan timespan = datetime - LastDayTime.Value;

                // 并且日期相差一天，才计算日雨量，否则都为空
                dDayRain = dRainArruracy * totalRain - lastDayTotalRain;

            }
            //dPeriodRain = 0; //整点不符合条件，默认日雨量都为0
            // end of if minutes accepted
            return dDayRain;
        }


        // 帮助函数
        private Nullable<Decimal> CalDifferenceRain(Nullable<Decimal> dRainArruracy, Decimal totalRain, Nullable<Decimal> lastTotalRain, Nullable<Decimal> MaxChange, ref int status)
        {
            Nullable<Decimal> dDifferenceRain = null;
            status = 1;
            // 差值雨量
            if (lastTotalRain.HasValue)
            {
                dDifferenceRain = totalRain * (Decimal)dRainArruracy - lastTotalRain.Value;
            }
            if (dDifferenceRain < 0)
            {
                decimal tmp = (decimal)MaxChange / (decimal)dRainArruracy;
                if ((9999 - lastTotalRain.Value / (Decimal)dRainArruracy) > tmp)
                {
                    dDifferenceRain = 0;
                    status = 0;
                }
                else
                {
                    dDifferenceRain = 9999 * (Decimal)dRainArruracy - lastTotalRain.Value + (totalRain * (Decimal)dRainArruracy);
                }
            }
            return dDifferenceRain;
        }
        private Nullable<Decimal> CalPeriodRain(Nullable<Decimal> dRainArruracy, Decimal totalRain, DateTime datetime, Nullable<Decimal> lastSharpTotalRain)
        {
            Nullable<Decimal> dPeriodRain = null;
            DateTime tmp = DateTime.Now;

            //时段雨量
            if ((datetime.Minute + datetime.Second == 0))
            {
                dPeriodRain = totalRain * (Decimal)dRainArruracy - lastSharpTotalRain;
            }
            return dPeriodRain;
        }
        private Nullable<Decimal> CalDayRain(Nullable<Decimal> dRainArruracy, Decimal totalRain, DateTime datetime, Nullable<DateTime> lastDataRainDayTime,
            Nullable<Decimal> lastDayTotalRain)
        {
            Nullable<Decimal> dDayRain = null;
            // 如果时间设置的误差范围内，整点，默认m_iMutesRange为0
            int offset = (datetime.Hour - 8) * 60 + datetime.Minute;
            if (Math.Abs(offset) <= 30)
            {
                // 计算日雨量,日期相差一天
                if (lastDayTotalRain.HasValue && lastDataRainDayTime.HasValue)
                {
                    TimeSpan timespan = datetime - lastDataRainDayTime.Value;
                    if (1 == timespan.Days)
                    {
                        // 并且日期相差一天，才计算日雨量，否则都为空
                        dDayRain = totalRain * (Decimal)dRainArruracy - lastDayTotalRain;
                    }
                }
                //dPeriodRain = 0; //整点不符合条件，默认日雨量都为0
            }// end of if minutes accepted
            return dDayRain;
        }

        private void AssertWaterData(CEntityWater water, ref ERTDDataState rtdState, bool bNotityWarning = true)
        {
            // 判断水量信息是否合法，写入系统信息或者告警信息
            CEntityStation station = CDBDataMgr.GetInstance().GetStationById(water.StationID);
            StringBuilder errinfo = new StringBuilder();
            // 判断是否超过最大值
            rtdState = ERTDDataState.ENormal;
            if (station.DWaterMax.HasValue)
            {
                if (water.WaterStage > station.DWaterMax)
                {
                    errinfo.AppendFormat("水位 {0} 超过最大值 {1} 站点编号：{2}", water.WaterStage.ToString("0.00"), station.DWaterMax.Value.ToString("0.00"), water.StationID);
                    rtdState = ERTDDataState.EError;
                }
            }
            // 判断是否低于最小值
            if (station.DWaterMin.HasValue)
            {
                if (water.WaterStage < station.DWaterMin)
                {
                    errinfo.AppendFormat("水位 {0} 低于最小值 {1} 站点编号：{2}", water.WaterStage.ToString("0.00"), station.DWaterMin.Value.ToString("0.00"), water.StationID);
                    rtdState = ERTDDataState.EError;
                }
            }

            // 判断是否超过允许变化值,暂时还未考虑好
            if (station.DWaterChange.HasValue && station.LastWaterStage.HasValue)
            {
                Decimal change = water.WaterStage - station.LastWaterStage.Value;
                if (change > station.DWaterChange)
                {
                    errinfo.AppendFormat("水位变化 {0} 超过允许值{1} 站点编号：{2}", change.ToString("0.00"), station.DWaterChange.Value.ToString("0.00"), water.StationID);
                    rtdState = ERTDDataState.EError;
                }
            }

            // 更新水位值，便于计算水位变化
            station.LastWaterStage = water.WaterStage;

            // 通知其它页面
            if (!errinfo.ToString().Equals(""))
            {
                if (bNotityWarning)
                {
                    CSystemInfoMgr.Instance.AddInfo(errinfo.ToString(), water.TimeCollect, ETextMsgState.EError);
                    CWarningInfoMgr.Instance.AddInfo(errinfo.ToString(), water.TimeCollect, EWarningInfoCodeType.EWater, station.StationID);
                    if (flag == 0)
                    {
                        flag = 1;
                        ////MessageBoxForm mbForm = new MessageBoxForm();
                        ////mbForm.StartPosition = FormStartPosition.CenterParent;
                        ////mbForm.Text = "水位报警";
                        ////mbForm.label3.Text = water.TimeCollect.ToString();
                        ////mbForm.label1.Text = errinfo.ToString();
                        ////mbForm.label2.Text = "是否关闭报警声音?";
                        ////mbForm.TopMost = true;
                        ////mbForm.ShowDialog();
                        string info = water.TimeCollect.ToString() + "\r\n" + "\r\n" + errinfo.ToString() + "\r\n" + "\r\n" + "是否关闭报警声音？" + "\r\n";
                        Thread t = new Thread(new ParameterizedThreadStart(MessageShow));
                        t.Start(info);
                    }
                }
            }
        }
        //private void AssertWaterData(CEntityWater water, ref ERTDDataState rtdState, bool bNotityWarning = true)
        //{
        //    // 判断水量信息是否合法，写入系统信息或者告警信息
        //    CEntityStation station = CDBDataMgr.GetInstance().GetStationById(water.StationID);
        //    StringBuilder errinfo = new StringBuilder();
        //    // 判断是否超过最大值
        //    rtdState = ERTDDataState.ENormal;
        //    if (station.DWaterMax.HasValue)
        //    {
        //        if (water.WaterStage > station.DWaterMax)
        //        {
        //            errinfo.AppendFormat("水位 {0} 超过最大值 {1} 站点编号：{2}", water.WaterStage.ToString("0.00"), station.DWaterMax.Value.ToString("0.00"), water.StationID);
        //            rtdState = ERTDDataState.EError;
        //        }
        //    }
        //    // 判断是否低于最小值
        //    if (station.DWaterMin.HasValue)
        //    {
        //        if (water.WaterStage < station.DWaterMin)
        //        {
        //            errinfo.AppendFormat("水位 {0} 低于最小值 {1} 站点编号：{2}", water.WaterStage.ToString("0.00"), station.DWaterMin.Value.ToString("0.00"), water.StationID);
        //            rtdState = ERTDDataState.EError;
        //        }
        //    }

        //    // 判断是否超过允许变化值,暂时还未考虑好
        //    if (station.DWaterChange.HasValue && station.LastWaterStage.HasValue)
        //    {
        //        Decimal change = water.WaterStage - station.LastWaterStage.Value;
        //        if (change > station.DWaterChange)
        //        {
        //            errinfo.AppendFormat("水位变化 {0} 超过允许值{1} 站点编号：{2}", change.ToString("0.00"), station.DWaterChange.Value.ToString("0.00"), water.StationID);
        //            rtdState = ERTDDataState.EError;
        //        }
        //    }

        //    // 更新水位值，便于计算水位变化
        //    station.LastWaterStage = water.WaterStage;

        //    // 通知其它页面
        //    if (!errinfo.ToString().Equals(""))
        //    {
        //        if (bNotityWarning)
        //        {
        //            CSystemInfoMgr.Instance.AddInfo(errinfo.ToString(), water.TimeCollect, ETextMsgState.EError);
        //            CWarningInfoMgr.Instance.AddInfo(errinfo.ToString(), water.TimeCollect, EWarningInfoCodeType.EWater, station.StationID);
        //            if (flag == 0)
        //            {
        //                flag = 1;
        //                //MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
        //                //DialogResult dr = MessageBox.Show(errinfo + " ，是否关闭报警声音?", "水位报警", messButton);
        //                //if (dr == DialogResult.OK)
        //                //{
        //                //    CVoicePlayer.Instance.Stop();
        //                //    flag = 0;
        //                //}
        //                //string str1="水位报警";
        //                //string str2=errinfo + " ，是否关闭报警声音?";
        //                MessageBoxForm mbForm = new MessageBoxForm();
        //                mbForm.StartPosition = FormStartPosition.CenterParent;
        //                mbForm.Text = "水位报警";
        //                mbForm.label3.Text = water.TimeCollect.ToString();
        //                mbForm.label1.Text = errinfo.ToString();
        //                mbForm.label2.Text = "是否关闭报警声音?";
        //                mbForm.TopMost = true;
        //                mbForm.ShowDialog();
        //            }
        //        }
        //    }
        //}
        private void MessageShow(Object str)
        {
            try
            {
                MessageBoxForm mbForm = new MessageBoxForm();
                string info = str.ToString();
                mbForm.StartPosition = FormStartPosition.CenterParent;
                mbForm.Text = "报警信息";

                mbForm.label1.Text = info;

                mbForm.TopMost = true;
                mbForm.ShowDialog();
            }
            catch (Exception e)
            {

            }

        }

        
        
    }
}


