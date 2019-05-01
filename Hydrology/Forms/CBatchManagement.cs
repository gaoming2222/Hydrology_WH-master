using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Hydrology.CControls;
using Hydrology.DataMgr;
using Hydrology.Entity;
using System.Diagnostics;
using System.Threading;
using Protocol.Channel.Interface;

namespace Hydrology.Forms
{
    public partial class CBatchManagement : Form
    {
        #region 静态常量
        private readonly string CS_All_Station = "所有站点";
        #endregion 静态常量
        #region 成员变量
        /// <summary>
        /// 系统对时的表格
        /// </summary>
        private CDataGridViewSystemClock m_dgvClock;
        //存储水位
        private CDataGridViewStorageWater m_dgvStorageWater;
        //实测水位
        private CDataGridViewRealityWater m_dgvRealityWater;
        //雨量
        private CDataGridViewRainManage m_dgvRainManage;
        //墒情
        private CDataGridViewSoilDataManage m_dgvSoilDataManage;

        private CEntityStation m_currentStation;

        private CEntitySoilStation m_currentSoilStation;

        private bool isRefreshDgvWhenPageFirstLoad;

        private List<CEntityStation> m_listStations;//站点实体

        private List<CEntityStation> m_listStations_1;//水位和水文站点实体

        private List<CEntityStation> m_listStations_2;//雨量和水文站点实体

        private DateTime m_preRefreshTime;  // 上一次的刷新时间

        private TimeSpan m_timeSpanRefresh; // 刷新的时间间隔

        //public static List<CEntityStation> ListOnlineStation = new List<CEntityStation>();
        public static List<CEntityStation> ListOnlineStation1 = new List<CEntityStation>();
        public static List<CEntityStation> ListOnlineStation2 = new List<CEntityStation>();
        public static List<CEntityStation> ListOnlineStation3 = new List<CEntityStation>();
        public static List<CEntityStation> ListOnlineStation4 = new List<CEntityStation>();

        #endregion 成员变量

        public CBatchManagement()
        {
            InitializeComponent();
            InitUI();
            InitListView();
            InitListView2();
            InitDataSource();
            this.groupBox2.Hide();
            this.btn_StartAdjust.Enabled = true;
            this.button1.Enabled = false;
            CProtocolEventManager.ErrorForUI += this.ErrorForUI_EventHandler;
            CProtocolEventManager.GPRS_TimeOut4UI += CProtocolEventManager_GPRS_TimeOut4UI;
            CProtocolEventManager.GPRS_OffLine4UI += new EventHandler(CProtocolEventManager_GPRS_OffLine4UI);
            //10.09
            // CProtocolEventManager.ModemInfoDataReceived += gprs_ModemInfoClockDataReceived;
        }

        void CProtocolEventManager_GPRS_OffLine4UI(object sender, EventArgs e)
        {
            if (this.IsHandleCreated && null != m_currentStation)
            {
                AddLog(String.Format("站点{0}对时失败！", m_currentStation.StationName));
                m_dgvClock.Hide();
                m_dgvClock.UpdateStationStatus(m_currentStation.StationID, CDataGridViewSystemClock.EStationClockState.EAdjustFailed);
                m_dgvClock.UpdateDataToUI();
                m_dgvClock.Show();
            }
        }

        //private void gprs_ModemInfoClockDataReceived(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!this.IsHandleCreated)
        //            return;
        //        var gprs = sender as IGprs;
        //        if (gprs == null)
        //            return;
        //        if (!gprs.GetStarted())
        //            return;
        //        if (gprs.DTUList == null)
        //            return;
        //        ushort portValue = gprs.GetListenPort();
        //        //if (gprs.DTUList.Count() == 0)
        //        //    return;
        //        if (isRefreshDgvWhenPageFirstLoad)
        //        {
        //            m_dgvClock.ClearAllRows();
        //            m_dgvStorageWater.ClearAllRows();
        //            m_dgvRealityWater.ClearAllRows();
        //            m_dgvRainManage.ClearAllRows();
        //            m_dgvSoilDataManage.ClearAllRows();
        //            //  添加数据库中所有已经配置的GPRS号码
        //            m_dgvClock.RefreshGPRSInfo(this.m_listStations);
        //            m_dgvStorageWater.RefreshGPRSInfo(this.m_listStations_1);
        //            m_dgvRealityWater.RefreshGPRSInfo(this.m_listStations_2);
        //            m_dgvRainManage.RefreshGPRSInfo(this.m_listStations);
        //            m_dgvSoilDataManage.RefreshGPRSInfo(this.m_listStations);

        //            CBatchManagement.ListOnlineStation.Clear();
        //            CBatchManagement.ListOnlineStation1.Clear();
        //            CBatchManagement.ListOnlineStation2.Clear();
        //            CBatchManagement.ListOnlineStation3.Clear();
        //            CBatchManagement.ListOnlineStation4.Clear();
        //            //  更新已经上线的GPRS号码
        //            foreach (var dtu in gprs.DTUList)
        //            {
        //                var stationName = QueryStationNameByUserID(dtu);

        //                m_dgvClock.RefreshGPRSInfo(portValue, stationName, dtu);
        //                m_dgvStorageWater.RefreshGPRSInfo(portValue, stationName, dtu);
        //                m_dgvRealityWater.RefreshGPRSInfo(portValue, stationName, dtu);
        //                m_dgvRainManage.RefreshGPRSInfo(portValue, stationName, dtu);
        //                m_dgvSoilDataManage.RefreshGPRSInfo(portValue, stationName, dtu);
        //            }
        //            m_dgvClock.UpdateDataToUI();
        //            m_dgvStorageWater.UpdateDataToUI();
        //            m_dgvRealityWater.UpdateDataToUI();
        //            m_dgvRainManage.UpdateDataToUI();
        //            m_dgvSoilDataManage.UpdateDataToUI();
        //            isRefreshDgvWhenPageFirstLoad = false;
        //            // 更新时间
        //            m_preRefreshTime = DateTime.Now;
        //        }
        //        else
        //        {
        //            TimeSpan span = DateTime.Now - m_preRefreshTime;
        //            if (span.TotalSeconds >= m_timeSpanRefresh.TotalSeconds)
        //            {
        //                //MessageBox.Show("Update Second : " + DateTime.Now + "     " + dgvDTUList.IsHandleCreated);

        //                //System.Diagnostics.Debug.WriteLine("Update Gprs !");
        //                m_dgvClock.ClearAllRows();
        //                m_dgvStorageWater.ClearAllRows();
        //                m_dgvRealityWater.ClearAllRows();
        //                m_dgvRainManage.ClearAllRows();
        //                m_dgvSoilDataManage.ClearAllRows();
        //                //m_dgvClock.OnlineGprsCount = 0;
        //                //m_dgvClock.TotalGprsCount = 0;
        //                //  添加数据库中所有已经配置的GPRS号码
        //                m_dgvClock.RefreshGPRSInfo(this.m_listStations);
        //                m_dgvStorageWater.RefreshGPRSInfo(this.m_listStations_1);
        //                m_dgvRealityWater.RefreshGPRSInfo(this.m_listStations_2);
        //                m_dgvRainManage.RefreshGPRSInfo(this.m_listStations);
        //                m_dgvSoilDataManage.RefreshGPRSInfo(this.m_listStations);

        //                CBatchManagement.ListOnlineStation.Clear();
        //                CBatchManagement.ListOnlineStation1.Clear();
        //                CBatchManagement.ListOnlineStation2.Clear();
        //                CBatchManagement.ListOnlineStation3.Clear();
        //                CBatchManagement.ListOnlineStation4.Clear();
        //                //  更新已经上线的GPRS号码
        //                foreach (var dtu in gprs.DTUList)
        //                {
        //                    var stationName = QueryStationNameByUserID(dtu);
        //                    m_dgvClock.RefreshGPRSInfo(portValue, stationName, dtu);
        //                    m_dgvStorageWater.RefreshGPRSInfo(portValue, stationName, dtu);
        //                    m_dgvRealityWater.RefreshGPRSInfo(portValue, stationName, dtu);
        //                    m_dgvRainManage.RefreshGPRSInfo(portValue, stationName, dtu);
        //                    m_dgvSoilDataManage.RefreshGPRSInfo(portValue, stationName, dtu);
        //                }
        //                m_dgvClock.UpdateDataToUI();
        //                m_dgvStorageWater.UpdateDataToUI();
        //                m_dgvRealityWater.UpdateDataToUI();
        //                m_dgvRainManage.UpdateDataToUI();
        //                m_dgvSoilDataManage.UpdateDataToUI();
        //                //this.lblToolTip.Invoke((Action)delegate
        //                //{
        //                //    this.lblToolTip.Text = string.Format("GPRS站点在线状态统计  在线:{0}个，离线:{1}个，共:{2}个,上次刷新时间{3}.", (dgvDTUList as CDataGridViewGPRSNew).OnlineGprsCount, (dgvDTUList as CDataGridViewGPRSNew).OfflineGprsCount, (dgvDTUList as CDataGridViewGPRSNew).TotalGprsCount, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        //                //});
        //                // 更新时间
        //                m_preRefreshTime = DateTime.Now;
        //            }
        //        }
        //    }
        //    catch (Exception exp)
        //    {
        //        System.Diagnostics.Debug.WriteLine(exp.Message);
        //        //MessageBox.Show("Update Third Exception : " + DateTime.Now + "     " + dgvDTUList.IsHandleCreated);
        //    }

        //}

        //通过用户ID查询站名

        private string QueryStationNameByUserID(ModemInfoStruct dtu)
        {
            string uid = ((uint)dtu.m_modemId).ToString("X").PadLeft(8, '0');
            if (this.m_listStations != null)
            {
                foreach (var station in this.m_listStations)
                {
                    if (station.GPRS.Trim() == uid.Trim())
                    {
                        return station.StationName;
                    }
                }
            }
            return "---";
        }

        private void InitListView()
        {
            // 初始化listView
            listView1.Columns.Add("表头", -2, HorizontalAlignment.Left);
            listView1.HeaderStyle = ColumnHeaderStyle.None;

            // 设置ListView的行高
            ImageList imgList = new ImageList();
            imgList.ImageSize = new Size(1, 12);//设置 ImageList 的宽和高
            listView1.SmallImageList = imgList;
            listView1.View = View.Details;
            listView1.Dock = DockStyle.Fill;
        }

        private void InitListView2()
        {
            // 初始化listView
            listView2.Columns.Add("表头", -2, HorizontalAlignment.Left);
            listView2.HeaderStyle = ColumnHeaderStyle.None;

            // 设置ListView的行高
            ImageList imgList = new ImageList();
            imgList.ImageSize = new Size(1, 12);//设置 ImageList 的宽和高
            listView2.SmallImageList = imgList;
            listView2.View = View.Details;
            listView2.Dock = DockStyle.Fill;
        }

        void CProtocolEventManager_GPRS_TimeOut4UI(object sender, ReceivedTimeOutEventArgs e)
        {
            if (this.IsHandleCreated && null != m_currentStation)
            {
                AddLog(String.Format("站点{0}对时失败！", m_currentStation.StationName));
                m_dgvClock.UpdateStationStatus(m_currentStation.StationID, CDataGridViewSystemClock.EStationClockState.EAdjustFailed);
                m_dgvClock.UpdateDataToUI();
            }
        }

        private void ErrorForUI_EventHandler(object sender, ReceiveErrorEventArgs e)
        {
            string msg = e.Msg;
            //   if (this.IsHandleCreated && null != this.m_currentStation)
            if (this.IsHandleCreated)
            {
                try
                {
                    if (msg.Contains("TRU"))
                    {
                        AddLog(msg);
                        string[] str = msg.Split(' ');
                        string str1 = str[1];
                        //  uint str1 = uint.Parse(str[1]);
                        //   string strgprs=str1.ToString("X").PadLeft(8, '0');
                        //       item.m_modemId.ToString("X").PadLeft(8, '0') 
                        CEntityStation entity = new CEntityStation();
                        entity = CDBDataMgr.Instance.GetStationByGprs_1(str1);
                        AddLog(String.Format("站点{0}对时成功！", entity.StationName));

                        m_dgvClock.UpdateStationStatus(entity.StationID, CDataGridViewSystemClock.EStationClockState.EAjustSuccess);
                    }
                    m_dgvClock.UpdateDataToUI();
                }
                catch (Exception exp) { Debug.WriteLine(exp.Message); }
            }
        }

        /// <summary>
        /// 初始化列表
        /// </summary>
        private void InitUI()
        {
            this.SuspendLayout();

            m_dgvClock = new CDataGridViewSystemClock()
            {
                // BPartionPageEnable = false
            };
            m_dgvClock.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            //m_dgvInfo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            //m_dgvInfo.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            m_dgvClock.Dock = DockStyle.Fill;
            m_dgvClock.AutoSize = false;
            //m_dataGridView.ReadOnly = true; //只读
            m_dgvClock.AllowUserToResizeRows = false;
            m_dgvClock.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            m_dgvClock.RowHeadersWidth = 50;
            m_dgvClock.ColumnHeadersHeight = 25;
            m_dgvClock.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            m_dgvClock.AllowUserToAddRows = false;
            panelBottom.Controls.Add(m_dgvClock);

            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            // 绑定消息
            this.btn_Exit.Click += new EventHandler(EHBtnExit);
            this.cmb_SubCenter.SelectedIndexChanged += new EventHandler(EHSubCenterChanged);
            this.chk_All.CheckedChanged += new EventHandler(EHCheckAllChanged);
            this.FormClosing += new FormClosingEventHandler(EHFormClosing);

            this.ResumeLayout(false);

            m_dgvStorageWater = new CDataGridViewStorageWater()
            {
                //BPartionPageEnable = false 
            };
            //    m_dgvStorageWater = new CDataGridViewSystemClock() { BPartionPageEnable = false };
            m_dgvStorageWater.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            m_dgvStorageWater.Dock = DockStyle.Fill;
            m_dgvStorageWater.AutoSize = false;
            m_dgvStorageWater.AllowUserToResizeRows = false;
            m_dgvStorageWater.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            m_dgvStorageWater.RowHeadersWidth = 50;
            m_dgvStorageWater.ColumnHeadersHeight = 25;
            m_dgvStorageWater.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            m_dgvStorageWater.AllowUserToAddRows = false;

            m_dgvRealityWater = new CDataGridViewRealityWater()
            {
                //BPartionPageEnable = false
            };
            m_dgvRealityWater.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            m_dgvRealityWater.Dock = DockStyle.Fill;
            m_dgvRealityWater.AutoSize = false;
            m_dgvRealityWater.AllowUserToResizeRows = false;
            m_dgvRealityWater.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            m_dgvRealityWater.RowHeadersWidth = 50;
            m_dgvRealityWater.ColumnHeadersHeight = 25;
            m_dgvRealityWater.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            m_dgvRealityWater.AllowUserToAddRows = false;

            m_dgvRainManage = new CDataGridViewRainManage()
            {
                //BPartionPageEnable = false 
            };
            m_dgvRainManage.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            m_dgvRainManage.Dock = DockStyle.Fill;
            m_dgvRainManage.AutoSize = false;
            m_dgvRainManage.AllowUserToResizeRows = false;
            m_dgvRainManage.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            m_dgvRainManage.RowHeadersWidth = 50;
            m_dgvRainManage.ColumnHeadersHeight = 25;
            m_dgvRainManage.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            m_dgvRainManage.AllowUserToAddRows = false;

            m_dgvSoilDataManage = new CDataGridViewSoilDataManage()
            {
                //BPartionPageEnable = false 
            };
            m_dgvSoilDataManage.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            m_dgvSoilDataManage.Dock = DockStyle.Fill;
            m_dgvSoilDataManage.AutoSize = false;
            m_dgvSoilDataManage.AllowUserToResizeRows = false;
            m_dgvSoilDataManage.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            m_dgvSoilDataManage.RowHeadersWidth = 50;
            m_dgvSoilDataManage.ColumnHeadersHeight = 25;
            m_dgvSoilDataManage.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            m_dgvSoilDataManage.AllowUserToAddRows = false;

            panel4.Controls.Add(m_dgvStorageWater);
            panel4.Controls.Add(m_dgvRealityWater);
            panel4.Controls.Add(m_dgvRainManage);
            panel4.Controls.Add(m_dgvSoilDataManage);
            m_dgvRealityWater.Hide();
            m_dgvRainManage.Hide();
            m_dgvSoilDataManage.Hide();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitDataSource()
        {
            List<CEntitySubCenter> listSubCenter = CDBDataMgr.Instance.GetAllSubCenter();

            cmb_SubCenter.Items.Add(CS_All_Station);
            for (int i = 0; i < listSubCenter.Count; ++i)
            {
                cmb_SubCenter.Items.Add(listSubCenter[i].SubCenterName);
            }
            this.m_listStations = CDBDataMgr.Instance.GetAllStation();
            m_listStations_1 = new List<CEntityStation>();
            m_listStations_2 = new List<CEntityStation>();
            //所有水位站和水文站，0，2
            for (int i = 0; i < this.m_listStations.Count; i++)
            {
                if (this.m_listStations[i].StationType == EStationType.EHydrology || this.m_listStations[i].StationType == EStationType.ERiverWater)
                {
                    this.m_listStations_1.Add(this.m_listStations[i]);
                }
                else if (this.m_listStations[i].StationType == EStationType.EHydrology || this.m_listStations[i].StationType == EStationType.ERainFall)
                {
                    this.m_listStations_2.Add(this.m_listStations[i]);
                }
            }
            cmb_SubCenter.SelectedIndex = 0;
            m_preRefreshTime = DateTime.Now;
            m_preRefreshTime = m_preRefreshTime.AddSeconds(-40);
            m_timeSpanRefresh = new TimeSpan(2, 30, 300); // 5分钟刷新
            isRefreshDgvWhenPageFirstLoad = true;

        }

        private void EHFormClosing(object sender, FormClosingEventArgs e)
        {
            //if (!btn_StartAdjust.Enabled)
            //{
            //    if (MessageBox.Show("系统正在对时，是否强行退出?", "关闭", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
            //    {
            //        // this.Close();
            //    }
            //    else
            //    {
            //        e.Cancel = true;
            //    }
            //}
        }

        //private void btn_StartAdjust_Click(object sender, EventArgs e)
        //{
        //var listStation = this.m_dgvClock.GetAllSelectedStation();
        //if (0 == listStation.Count)
        //{
        //    MessageBox.Show("请选择站点！");
        //    return;
        //}
        //this.btn_StartAdjust.Enabled = false;
        //int i = 0;
        //System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer()
        //{
        //    Interval = 40000
        //};
        //AddLog("系统正在对时，请耐心等待！");
        //timer.Tick += (s1, e1) =>
        //{
        //    if (i >= listStation.Count)
        //    {
        //        timer.Stop();
        //        m_currentStation = null;
        //        this.btn_StartAdjust.Enabled = true;
        //        return;
        //    }

        //    m_currentStation = listStation[i];
        //   // Debug.WriteLine(" i = " + i + "      Name = " + m_currentStation.StationName);
        //string sendMsg = CPortDataMgr.Instance.SendAdjustClock(m_currentStation);
        //AddLog(sendMsg);
        //    i++;
        //};
        //timer.Start();
        //}

        private void EHSubCenterChanged(object sender, EventArgs e)
        {
            int selectindex = cmb_SubCenter.SelectedIndex;
            if (0 == selectindex)
            {
                m_dgvClock.SetSubCenterName(null); //所有分中心
                m_dgvStorageWater.SetSubCenterName(null);
                m_dgvRealityWater.SetSubCenterName(null);
                m_dgvRainManage.SetSubCenterName(null);
                m_dgvSoilDataManage.SetSubCenterName(null);
            }
            else
            {
                string subcentername = cmb_SubCenter.Text;
                m_dgvClock.SetSubCenterName(subcentername);
                m_dgvStorageWater.SetSubCenterName(subcentername);
                m_dgvRealityWater.SetSubCenterName(subcentername);
                m_dgvRainManage.SetSubCenterName(subcentername);
                m_dgvSoilDataManage.SetSubCenterName(subcentername);
            }
            this.tst_Label.Text = string.Format("共{0}个站点", m_dgvClock.Rows.Count);
        }

        private void EHBtnExit(object sender, EventArgs e)
        {
            this.Close();
        }

        private void EHCheckAllChanged(object sender, EventArgs e)
        {
            if (chk_All.CheckState == CheckState.Checked)
            {
                // 全选
                m_dgvClock.SelectAllOrNot(true);
                m_dgvStorageWater.SelectAllOrNot(true);
                m_dgvRealityWater.SelectAllOrNot(true);
                m_dgvRainManage.SelectAllOrNot(true);
                m_dgvSoilDataManage.SelectAllOrNot(true);
            }
            else
            {
                // 全不选
                m_dgvClock.SelectAllOrNot(false);
                m_dgvStorageWater.SelectAllOrNot(false);
                m_dgvRealityWater.SelectAllOrNot(false);
                m_dgvRainManage.SelectAllOrNot(false);
                m_dgvSoilDataManage.SelectAllOrNot(false);
            }
        }

        private void AddLog(string text)
        {
            if (text != "")
            {
                if (this.IsHandleCreated)
                {
                    this.listView1.Invoke((Action)delegate
                    {
                        try
                        {
                            this.listView1.Items.Add(text);
                            int index = this.listView1.Items.Count - 1;
                            if (index >= 0)
                            {
                                this.listView1.Items[index].EnsureVisible();
                            }
                        }
                        catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    });
                }
            }
        }

        private void AddLog1(string text)
        {
            if (text != "")
            {
                if (this.IsHandleCreated)
                {
                    this.listView2.Invoke((Action)delegate
                    {
                        try
                        {
                            this.listView2.Items.Add(text);
                            int index = this.listView1.Items.Count - 1;
                            if (index >= 0)
                            {
                                this.listView2.Items[index].EnsureVisible();
                            }
                        }
                        catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    });
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!HasUserOnLine())
            {
                return;
            }
            //  var listStation = this.m_dgvStorageWater.GetAllSelectedStation();

            //List<CEntityStation> listOnlineStationSelected = new List<CEntityStation>();
            //List<string> result = new List<string>();
            //result=ListOnlineStation
            int tag = 0;
            if (this.radioButton1.Checked)
            {
                tag = 1;
                //for (int k = 0; k < ListOnlineStation1.Count; k++)
                //{
                //    result.Add(ListOnlineStation1[k].StationID);
                //}
            }
            else if (this.radioButton2.Checked)
            {
                tag = 2;
                //for (int k = 0; k < ListOnlineStation2.Count; k++)
                //{
                //    result.Add(ListOnlineStation2[k].StationID);
                //}
            }
            else if (this.radioButton3.Checked)
            {
                tag = 3;
                //for (int k = 0; k < ListOnlineStation3.Count; k++)
                //{
                //    result.Add(ListOnlineStation3[k].StationID);
                //}
            }
            else if (this.radioButton4.Checked)
            {
                tag = 4;
                //for (int k = 0; k < ListOnlineStation4.Count; k++)
                //{
                //    result.Add(ListOnlineStation4[k].StationID);
                //}
            }
            List<CEntityStation> listStation = new List<CEntityStation>();
            List<CEntitySoilStation> listSoilStation = new List<CEntitySoilStation>();
            switch (tag)
            {
                case 1: listStation = this.m_dgvStorageWater.GetAllSelectedStation(); break;
                case 2: listStation = this.m_dgvRealityWater.GetAllSelectedStation(); break;
                case 3: listStation = this.m_dgvRainManage.GetAllSelectedStation(); break;
                case 4: listSoilStation = this.m_dgvSoilDataManage.GetAllSelectedStation(); break;
            }
            if (tag == 1 || tag == 2 || tag == 3)
            {
                if (0 == listStation.Count)
                {
                    MessageBox.Show("请选择站点！");
                    return;
                }
            }
            else if (tag == 4)
            {
                if (0 == listSoilStation.Count)
                {
                    MessageBox.Show("请选择站点！");
                    return;
                }
            }


            this.btn_StartAdjust.Enabled = false;



            //for (int j = 0; j < listStation.Count; j++)
            //{
            //    if (result.Contains(listStation[j].StationID))
            //    {
            //        listOnlineStationSelected.Add(listStation[j]);
            //    }

            //}
            if (tag == 1)
            {
                AddLog1("--读取存储水位--");
                for (int i = 0; i < listStation.Count; i++)
                {
                    m_currentStation = listStation[i];
                    string sendMsg = CPortDataMgr.Instance.GroupStorageWaterFirst(m_currentStation);
                    AddLog1(sendMsg);
                }
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer()
                {
                    Interval = 1000
                };

                int m = 0;
                timer.Tick += (s1, e1) =>
                {
                    if (m > 0)
                    {
                        timer.Stop();
                        m_currentStation = null;
                        //       this.btn_StartAdjust.Enabled = true;
                        return;
                    }
                    for (int i = 0; i < listStation.Count; i++)
                    {
                        m_currentStation = listStation[i];
                        string sendMsg = CPortDataMgr.Instance.GroupStorageWaterFirst(m_currentStation);
                        AddLog1(sendMsg);
                    }
                    m = 1;
                };
                timer.Start();
            }
            else if (tag == 2)
            {
                AddLog1("--读取实测水位--");
                for (int i = 0; i < listStation.Count; i++)
                {
                    m_currentStation = listStation[i];
                    string sendMsg = CPortDataMgr.Instance.GroupRealityWaterFirst(m_currentStation);
                    AddLog1(sendMsg);
                }
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer()
                {
                    Interval = 1000
                };

                int m = 0;
                timer.Tick += (s1, e1) =>
                {
                    if (m > 0)
                    {
                        timer.Stop();
                        m_currentStation = null;
                        //    this.btn_StartAdjust.Enabled = true;
                        return;
                    }
                    for (int i = 0; i < listStation.Count; i++)
                    {
                        m_currentStation = listStation[i];
                        string sendMsg = CPortDataMgr.Instance.GroupRealityWaterFirst(m_currentStation);
                        AddLog1(sendMsg);
                    }
                    m = 1;
                };
                timer.Start();
            }
            else if (tag == 3)
            {
                AddLog1("--读取雨量--");
                for (int i = 0; i < listStation.Count; i++)
                {
                    m_currentStation = listStation[i];
                    string sendMsg = CPortDataMgr.Instance.GroupRainWaterFirst(m_currentStation);
                    AddLog1(sendMsg);
                }
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer()
                {
                    Interval = 1000
                };

                int m = 0;
                timer.Tick += (s1, e1) =>
                {
                    if (m > 0)
                    {
                        timer.Stop();
                        m_currentStation = null;
                        //   this.btn_StartAdjust.Enabled = true;
                        return;
                    }
                    for (int i = 0; i < listStation.Count; i++)
                    {
                        m_currentStation = listStation[i];
                        string sendMsg = CPortDataMgr.Instance.GroupRainWaterFirst(m_currentStation);
                        AddLog1(sendMsg);
                    }
                    m = 1;
                };
                timer.Start();
            }
            else if (tag == 4)
            {
                AddLog1("--读取墒情--");
                for (int i = 0; i < listSoilStation.Count; i++)
                {
                    m_currentSoilStation = listSoilStation[i];
                    string sendMsg = CPortDataMgr.Instance.GroupSoilWaterFirst_1(m_currentSoilStation);
                    AddLog1(sendMsg);
                }
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer()
                {
                    Interval = 1000
                };

                int m = 0;
                timer.Tick += (s1, e1) =>
                {
                    if (m > 0)
                    {
                        timer.Stop();
                        m_currentStation = null;
                        //    this.btn_StartAdjust.Enabled = true;
                        return;
                    }
                    for (int i = 0; i < listSoilStation.Count; i++)
                    {
                        m_currentSoilStation = listSoilStation[i];
                        string sendMsg = CPortDataMgr.Instance.GroupSoilWaterFirst_1(m_currentSoilStation);
                        AddLog1(sendMsg);
                    }
                    m = 1;
                };
                timer.Start();
            }
            //int i = 0;

            // String str = "$00010G 12\n";
            // string sendMsg = CPortDataMgr.Instance.SendAdjustClock(m_currentStation);


            // int i=0;
            //      System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer()
            //{
            //    Interval = 5000
            //};
            //AddLog1("系统正在获取，请耐心等待！");



            //timer.Tick += (s1, e1) =>
            //{
            //    //  if (i >= listStation.Count)
            //    if (i >= listOnlineStationSelected.Count)
            //    {
            //        timer.Stop();
            //        m_currentStation = null;
            //        this.btn_StartAdjust.Enabled = true;
            //        return;
            //    }

            //    m_currentStation = listOnlineStationSelected[i];
            //    // Debug.WriteLine(" i = " + i + "      Name = " + m_currentStation.StationName);
            //    if(tag == 1){

            //        string sendMsg = CPortDataMgr.Instance.GroupStorageWater(m_currentStation);
            //        AddLog1(sendMsg);
            //    }
            //    if (tag == 2)
            //    {
            //        string sendMsg = CPortDataMgr.Instance.GroupRealityWater(m_currentStation);
            //        AddLog1(sendMsg);
            //    }
            //    if (tag == 3)
            //    {
            //        string sendMsg = CPortDataMgr.Instance.GroupRainWater(m_currentStation);
            //        AddLog1(sendMsg);
            //    }
            //    if (tag == 4)
            //    {
            //        string sendMsg = CPortDataMgr.Instance.GroupSoilWater(m_currentStation);
            //        AddLog1(sendMsg);
            //    }

            //    i++;
            //};
            //timer.Start();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true)
            {
                m_dgvStorageWater.Show();
            }
            else
            {
                m_dgvStorageWater.Hide();
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked == true)
            {
                m_dgvRealityWater.Show();
            }
            else
            {
                m_dgvRealityWater.Hide();
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked == true)
            {
                m_dgvRainManage.Show();
            }
            else
            {
                m_dgvRainManage.Hide();
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked == true)
            {
                m_dgvSoilDataManage.Show();
            }
            else
            {
                m_dgvSoilDataManage.Hide();
            }
        }

        private void btn_StartAdjust_Click(object sender, EventArgs e)
        {
            var listStation = this.m_dgvClock.GetAllSelectedStation();
            List<string> result = new List<string>();

            label4.Text = "开始对时,只对当前所选择的在线站点进行对时！";

            this.btn_StartAdjust.Enabled = false;
            AddLog("系统正在对时，请耐心等待！");

            m_dgvClock.Hide();

            for (int i = 0; i < listStation.Count; i++)
            {
                m_currentStation = listStation[i];
                string sendMsg = CPortDataMgr.Instance.SendTcpBatchAdjustClock(m_currentStation);
                
            }

            //System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer()
            //{
            //    Interval = 1000
            //};

            //int m = 0;
            //timer.Tick += (s1, e1) =>
            //{
            //    if (m > 0)
            //    {
            //        timer.Stop();
            //        m_currentStation = null;
            //        this.btn_StartAdjust.Enabled = true;
            //        return;
            //    }
            //    for (int i = 0; i < listStation.Count; i++)
            //    {
            //        m_currentStation = listStation[i];
            //        string sendMsg = CPortDataMgr.Instance.SendAdjustClockFirst(m_currentStation);
            //        //    AddLog(sendMsg);
            //    }
            //    m = 1;
            //};
            //timer.Start();


            ////显示对时完成
            //System.Windows.Forms.Timer timer_1 = new System.Windows.Forms.Timer()
            //{
            //    Interval = 1000 * 60
            //};
            ////  this.label4.Text = "";
            //timer_1.Tick += (s1, e1) =>
            //{
            //    this.label4.Text = "对时完成！";
            //    AddLog("对时完成！");
            //    timer_1.Stop();
            //};
            //timer_1.Start();
            m_dgvClock.Show();
        }

        private bool HasUserOnLine()
        {
            if (CPortDataMgr.Instance.GPRSCurrentOnlineUserCount == 0)
            {
                MessageBox.Show("GPRS ： 当前用户登录个数为0，请耐心等待用户登录！");
                return false;
            }
            return true;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                this.btn_StartAdjust.Enabled = true;
                this.button1.Enabled = false;
                this.label4.Show();
                this.groupBox2.Hide();
            }
            else if (tabControl1.SelectedIndex == 1)
            {
                this.button1.Enabled = true;
                this.btn_StartAdjust.Enabled = false;
                this.label4.Hide();
                this.groupBox2.Show();
            }
        }


    }
}
