using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Hydrology.CControls;
using Protocol.Channel.Interface;
using Hydrology.DataMgr;
using Hydrology.Entity;
using System.Threading;

namespace Hydrology.Forms
{
    public partial class CGprsNewMgrForm : Form
    {
        private CDataGridViewGPRSNew dgvDTUList;  ///< GPRS状态信息的DGV
        private CDataGridViewGPRSConfig m_dgvGprsConfig;  ///< GPRS配置的DGV

        private List<CEntitySubCenter> m_listSubcenters;//所有水情站点实体
        private List<CEntityStation> m_listStations;//所有水情站点实体
        private List<CEntitySoilStation> m_listSoilStations;//所有墒情情站点实体

        private DateTime m_preRefreshTime;  // 上一次的刷新时间
        private TimeSpan m_timeSpanRefresh; // 刷新的时间间隔

        private static readonly string CS_Station_All = "全部";

        public static String SubcenterString;

        // 添加数据互斥量，保证线程安全
        protected /*static*/ Mutex m_mutexDataTable_1;

        public CGprsNewMgrForm()
        {
            try
            {
                SubcenterString = "全部";
                m_mutexDataTable_1 = new Mutex();
                dgvDTUList = new CDataGridViewGPRSNew();
                InitializeComponent();
                Init();
                InitSubCenter();
                cmb_SubCenter.SelectedIndex = 0;
                // 绑定消息
                this.FormClosing += new FormClosingEventHandler(EHFormClosing);
                this.tsButAdd.Click += new EventHandler(EHButtonAdd);
                this.tsButDelete.Click += new EventHandler(EHButtonDelete);
                this.tsButRevert.Click += new EventHandler(EHButtonRevert);
                this.tsButExit.Click += new EventHandler(EHButtonExit);
                this.tsButSave.Click += new EventHandler(EHButtonSave);
                this.cmb_SubCenter.SelectedIndexChanged += new EventHandler(EHSubCenterChanged);
                //this.button1.Click += new EventHandler(gprs_ModemInfoDataReceived);

                m_preRefreshTime = DateTime.Now;
                m_preRefreshTime = m_preRefreshTime.AddSeconds(-40);
                m_timeSpanRefresh = new TimeSpan(0, 0, 300); // 1分钟刷新
                //m_timeSpanRefresh = new TimeSpan(0, 0, 180); // 3分钟刷新
                //m_timeSpanRefresh = new TimeSpan(0, 0, 30); // 30秒刷新
                //MessageBox.Show("Init");
            }
            catch (Exception ex) { }
        }
        #region 事件响应


        private void EHButtonSave(object sender, EventArgs e)
        {
            dgvDTUList.Hide();
            //  如果保存成功，刷新用户在线列表
            if (m_dgvGprsConfig.DoSave())
            {
                // dgvDTUList.ClearAllRows();
            }
            dgvDTUList.Show();
            // dgvDTUList;
        }

        private void EHButtonExit(object sender, EventArgs e)
        {
            this.Close();
        }

        private void EHButtonRevert(object sender, EventArgs e)
        {
            m_dgvGprsConfig.Revert();
        }

        private void EHButtonDelete(object sender, EventArgs e)
        {
            m_dgvGprsConfig.DoDelete();
        }

        private void EHButtonAdd(object sender, EventArgs e)
        {
            m_dgvGprsConfig.AddNewPort();
        }

        private void EHFormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_dgvGprsConfig.Close())
            {

            }
            else
            {
                // 保存失败
                e.Cancel = true;
            }
        }

        #endregion 事件响应

        #region 帮助方法

        private void Init()
        {
            InitStationList();
            InitDtuDgv();
            InitGprsDgv();
        }
        private void InitStationList()
        {
            this.m_listSubcenters = CDBDataMgr.Instance.GetAllSubCenter();
            this.m_listStations = CDBDataMgr.Instance.GetAllStation();
            //this.m_listSoilStations = CDBSoilDataMgr.Instance.GetAllSoilStation();
            this.m_listSoilStations = new List<CEntitySoilStation>();
        }
        //状态信息
        private void InitDtuDgv()
        {
            //  初始化用户登陆列表
            this.panel5.Controls.Remove(this.dgvDTUList);
            dgvDTUList = new CDataGridViewGPRSNew();
            dgvDTUList.RefreshGPRSInfo(m_listStations);
            //dgvDTUList.RefreshGPRSInfoSoil(m_listSoilStations);
            dgvDTUList.AllowUserToAddRows = false;
            dgvDTUList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            dgvDTUList.Dock = DockStyle.Fill;
            //dgvDTUList.AllowUserToResizeRows = false;
            //dgvDTUList.AllowUserToResizeColumns = true;
            dgvDTUList.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvDTUList.RowHeadersWidth = 50;
            //dgvDTUList.ColumnHeadersHeight = 25;
            //dgvDTUList.Columns[1].Width = 100; // 接收时间
            //dgvDTUList.Columns[2].Width = 100; // 接收时间
            //dgvDTUList.Columns[4].Width = 200; // 接收时间
            //dgvDTUList.Columns[5].Width = 200; // 接收时间
            dgvDTUList.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            dgvDTUList.TotalGprsCount = 0;
            foreach (var station in m_listStations)
            {
                if (!string.IsNullOrEmpty(station.GPRS))
                {
                    dgvDTUList.m_totalGprsCount += 1;
                }
            }
            //foreach (var station in m_listSoilStations)
            //{
            //    if (!string.IsNullOrEmpty(station.GPRS))
            //    {
            //        dgvDTUList.m_totalGprsCount += 1;
            //    }
            //}

            this.lblToolTip.Text = string.Format("GPRS站点在线状态统计  在线:{0}个，离线:{1}个，共:{2}个,上次刷新时间{3}.", 0, dgvDTUList.TotalGprsCount, dgvDTUList.TotalGprsCount, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            this.panel5.Controls.Add(this.dgvDTUList);
        }

        //配置信息
        private void InitGprsDgv()
        {
            this.SuspendLayout();
            m_dgvGprsConfig = new CDataGridViewGPRSConfig();
            m_dgvGprsConfig.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            m_dgvGprsConfig.Dock = DockStyle.Fill;
            m_dgvGprsConfig.AllowUserToResizeRows = false;
            m_dgvGprsConfig.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            m_dgvGprsConfig.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            m_dgvGprsConfig.RowHeadersWidth = 40;
            m_dgvGprsConfig.ColumnHeadersHeight = 25;
            m_dgvGprsConfig.AllowUserToAddRows = false;

            m_dgvGprsConfig.SetEditMode(true); //可编辑模式

            panelPortConfig.Controls.Add(m_dgvGprsConfig);
            this.ResumeLayout(false);

            if (!m_dgvGprsConfig.Init())
            {
                //初始化失败
                
                MessageBox.Show("配置端口通讯时，请先配置端口通讯方式和数据协议");
                this.Close();
                return;
            }

            CProtocolEventManager.ModemDataReceived += gprs_ModemDataReceived;
            CProtocolEventManager.ModemInfoDataReceived += gprs_ModemInfoDataReceived;
            CProtocolEventManager.ModemInfoDataReceived1 += tcp_ModemInfoDataReceived;

        }

        private void gprs_ModemDataReceived(object sender, ModemDataEventArgs e)
        {
            var gprs = sender as IGprs;
            if (gprs == null)
            {
                return;
            }
            ushort portValue = gprs.GetListenPort();
            string msg = e.Msg;
        }
        private bool isRefreshDgvWhenPageFirstLoad = true;
        private void gprs_ModemInfoDataReceived(object sender, EventArgs e)
        {
            try
            {
                if (!this.IsHandleCreated)
                    return;
                var gprs = sender as IGprs;
                if (gprs == null)
                    return;
                if (!gprs.GetStarted())
                    return;
                if (gprs.DTUList == null)
                    return;
                ushort portValue = gprs.GetListenPort();
                //if (gprs.DTUList.Count() == 0)
                //    return;

                if (isRefreshDgvWhenPageFirstLoad)
                {
                    //MessageBox.Show("Update First : " + DateTime.Now + "     " + dgvDTUList.IsHandleCreated);
                    //dgvDTUList.ClearAllRows();
                    dgvDTUList.OnlineGprsCount = 0;
                    dgvDTUList.TotalGprsCount = 0;
                    //1025
                    //dgvDTUList.Hide();
                    //  添加数据库中所有已经配置的GPRS号码
                    // dgvDTUList.RefreshGPRSInfo(this.m_listStations);
                    foreach (var station in m_listStations)
                    {
                        if (!string.IsNullOrEmpty(station.GPRS))
                        {
                            dgvDTUList.m_totalGprsCount += 1;
                        }
                    }

                    //foreach (var station in m_listSoilStations)
                    //{
                    //    if (!string.IsNullOrEmpty(station.GPRS))
                    //    {
                    //        dgvDTUList.m_totalGprsCount += 1;
                    //    }
                    //}

                    //  更新已经上线的GPRS号码
                    foreach (var dtu in gprs.DTUList)
                    {
                        var stationName = QueryStationNameByUserID(dtu);
                        var stationID = QueryStationIDByUserID(dtu);
                        var port = QuerySubcenterNameByUserID(dtu);
                        if (stationName != "---")
                        {
                            dgvDTUList.RefreshGPRSInfo(port, stationName, stationID, dtu);
                        }
                    }
                    //   dgvDTUList.Show();
                    dgvDTUList.UpdateDataToUI();

                    this.lblToolTip.Invoke((Action)delegate
                    {
                        this.lblToolTip.Text = string.Format("GPRS站点在线状态统计  在线:{0}个，离线:{1}个，共:{2}个,上次刷新时间{3}.", dgvDTUList.OnlineGprsCount, dgvDTUList.OfflineGprsCount, dgvDTUList.TotalGprsCount, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    });

                    isRefreshDgvWhenPageFirstLoad = false;

                    // 更新时间
                    m_preRefreshTime = DateTime.Now;
                }
                else
                {
                    TimeSpan span = DateTime.Now - m_preRefreshTime;
                    //System.Diagnostics.Debug.WriteLine("Total Second : " + span.TotalSeconds);
                    if (span.TotalSeconds >= m_timeSpanRefresh.TotalSeconds)
                    {
                        //MessageBox.Show("Update Second : " + DateTime.Now + "     " + dgvDTUList.IsHandleCreated);

                        //System.Diagnostics.Debug.WriteLine("Update Gprs !");
                        // dgvDTUList.ClearAllRows();
                        m_mutexDataTable_1.WaitOne();
                        if (SubcenterString == "全部")
                        {
                            dgvDTUList.OnlineGprsCount = 0;
                            dgvDTUList.TotalGprsCount = 0;
                            //1025
                            // dgvDTUList.Hide();
                            //  添加数据库中所有已经配置的GPRS号码
                            // dgvDTUList.RefreshGPRSInfo(this.m_listStations);

                            foreach (var station in m_listStations)
                            {
                                if (!string.IsNullOrEmpty(station.GPRS))
                                {
                                    dgvDTUList.m_totalGprsCount += 1;
                                }
                            }

                            //foreach (var station in m_listSoilStations)
                            //{
                            //    if (!string.IsNullOrEmpty(station.GPRS))
                            //    {
                            //        dgvDTUList.m_totalGprsCount += 1;
                            //    }
                            //}

                            //  更新已经上线的GPRS号码
                            foreach (var dtu in gprs.DTUList)
                            {
                                var stationName = QueryStationNameByUserID(dtu);
                                var stationID = QueryStationIDByUserID(dtu);
                                var port = QuerySubcenterNameByUserID(dtu);
                                if (stationName != "---")
                                {
                                    dgvDTUList.RefreshGPRSInfo(port, stationName, stationID, dtu);
                                }
                            }
                            //   dgvDTUList.Show();
                            dgvDTUList.UpdateDataToUI();

                            this.lblToolTip.Invoke((Action)delegate
                            {
                                this.lblToolTip.Text = string.Format("GPRS站点在线状态统计  在线:{0}个，离线:{1}个，共:{2}个,上次刷新时间{3}.", dgvDTUList.OnlineGprsCount, dgvDTUList.OfflineGprsCount, dgvDTUList.TotalGprsCount, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                            });
                            //this.lblToolTip.Show();
                        }
                        else
                        {
                            dgvDTUList.OnlineGprsCount = 0;
                            dgvDTUList.TotalGprsCount = 0;
                            try
                            {
                                foreach (var station in m_listStations)
                                {
                                    if (CDBDataMgr.Instance.GetSubCenterById(int.Parse(station.SubCenterID.ToString())).SubCenterName == SubcenterString)
                                    {
                                        if (!string.IsNullOrEmpty(station.GPRS))
                                        {
                                            dgvDTUList.m_totalGprsCount += 1;
                                        }
                                    }
                                }

                                //foreach (var station in m_listSoilStations)
                                //{
                                //    if (CDBSoilDataMgr.Instance.GetSubCenterBySoilId(int.Parse(station.SubCenterID.ToString())).SubCenterName == SubcenterString)
                                //    {
                                //        if (!string.IsNullOrEmpty(station.GPRS))
                                //        {
                                //            dgvDTUList.m_totalGprsCount += 1;
                                //        }
                                //    }
                                //}
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("获取所有分中心站点错误。。。");
                            }

                            try
                            {
                                //  更新已经上线的GPRS号码
                                foreach (var dtu in gprs.DTUList)
                                {
                                    var stationName = QueryStationNameByUserID(dtu);
                                    var stationID = QueryStationIDByUserID(dtu);
                                    var station = QueryStationEntityByUserID(dtu);
                                    var soil = QuerySoilStationEntityByUserID(dtu);
                                    var port = QuerySubcenterNameByUserID(dtu);
                                    if (station != null)
                                    {
                                        if (CDBDataMgr.Instance.GetSubCenterById(int.Parse(station.SubCenterID.ToString())).SubCenterName == SubcenterString)
                                        {
                                            dgvDTUList.RefreshGPRSInfo(port, stationName, stationID, dtu);
                                        }
                                    }
                                    //else if (soil != null)
                                    //{

                                    //    if (CDBSoilDataMgr.Instance.GetSubCenterBySoilId(int.Parse(soil.SubCenterID.ToString())).SubCenterName == SubcenterString)
                                    //    {
                                    //        dgvDTUList.RefreshGPRSInfo(port, stationName, stationID, dtu);
                                    //    }
                                    //}
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("更新所有分中心站点错误。。。");
                            }
                            //   dgvDTUList.Show();
                            dgvDTUList.UpdateDataToUI();
                            this.lblToolTip.Invoke((Action)delegate
                            {
                                this.lblToolTip.Text = string.Format("GPRS站点在线状态统计  在线:{0}个，离线:{1}个，共:{2}个,上次刷新时间{3}.", dgvDTUList.OnlineGprsCount, dgvDTUList.OfflineGprsCount, dgvDTUList.TotalGprsCount, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                            });
                            //this.lblToolTip.Show();
                        }
                        m_mutexDataTable_1.ReleaseMutex();
                        // 更新时间
                        m_preRefreshTime = DateTime.Now;
                        m_timeSpanRefresh = new TimeSpan(0, 0, 300);
                    }

                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("刷新错误。。。");
                //MessageBox.Show("Update Third Exception : " + DateTime.Now + "     " + dgvDTUList.IsHandleCreated);
            }
        }
        //TCP在线信息刷新
        private void tcp_ModemInfoDataReceived(object sender, EventArgs e)
        {
            try
            {
                if (!this.IsHandleCreated)
                    return;
                var transparent = sender as ITransparen;
                if (transparent == null)
                    return;
                if (!transparent.GetStarted())
                    return;
                System.Collections.Generic.List<ModemInfoStruct> dtuList = transparent.getDTUList();
                if (dtuList == null)
                    return;
                
                ushort portValue = transparent.GetListenPort();
                //if (gprs.DTUList.Count() == 0)
                //    return;

                if (isRefreshDgvWhenPageFirstLoad)
                {
                    //MessageBox.Show("Update First : " + DateTime.Now + "     " + dgvDTUList.IsHandleCreated);
                    //dgvDTUList.ClearAllRows();
                    dgvDTUList.OnlineGprsCount = 0;
                    dgvDTUList.TotalGprsCount = 0;
                    //1025
                    //dgvDTUList.Hide();
                    //  添加数据库中所有已经配置的GPRS号码
                    // dgvDTUList.RefreshGPRSInfo(this.m_listStations);
                    foreach (var station in m_listStations)
                    {
                        if (!string.IsNullOrEmpty(station.GPRS))
                        {
                            dgvDTUList.m_totalGprsCount += 1;
                        }
                    }

                    //foreach (var station in m_listSoilStations)
                    //{
                    //    if (!string.IsNullOrEmpty(station.GPRS))
                    //    {
                    //        dgvDTUList.m_totalGprsCount += 1;
                    //    }
                    //}

                    //  更新已经上线的GPRS号码
                    foreach (var dtu in dtuList)
                    {
                        string stationId = dtu.m_modemId.ToString();
                        for(int i = 10; i >= stationId.Length; i--)
                        {
                            stationId = "0" + stationId;
                        }
                        string  stationName = QueryStationNameByID(stationId);
                        string  stationID = stationId;
                        var port = portValue;
                        if (stationName != "---")
                        {
                            dgvDTUList.RefreshGPRSInfo(port.ToString(), stationName, stationID, dtu);
                        }
                    }
                    //   dgvDTUList.Show();
                    dgvDTUList.UpdateDataToUI();

                    this.lblToolTip.Invoke((Action)delegate
                    {
                        this.lblToolTip.Text = string.Format("GPRS站点在线状态统计  在线:{0}个，离线:{1}个，共:{2}个,上次刷新时间{3}.", dgvDTUList.OnlineGprsCount, dgvDTUList.OfflineGprsCount, dgvDTUList.TotalGprsCount, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    });

                    isRefreshDgvWhenPageFirstLoad = false;

                    // 更新时间
                    m_preRefreshTime = DateTime.Now;
                }
                else
                {
                    TimeSpan span = DateTime.Now - m_preRefreshTime;
                    //System.Diagnostics.Debug.WriteLine("Total Second : " + span.TotalSeconds);
                    if (span.TotalSeconds >= m_timeSpanRefresh.TotalSeconds)
                    {
                        //MessageBox.Show("Update Second : " + DateTime.Now + "     " + dgvDTUList.IsHandleCreated);

                        //System.Diagnostics.Debug.WriteLine("Update Gprs !");
                        // dgvDTUList.ClearAllRows();
                        m_mutexDataTable_1.WaitOne();
                        if (SubcenterString == "全部")
                        {
                            dgvDTUList.OnlineGprsCount = 0;
                            dgvDTUList.TotalGprsCount = 0;
                            //1025
                            // dgvDTUList.Hide();
                            //  添加数据库中所有已经配置的GPRS号码
                            // dgvDTUList.RefreshGPRSInfo(this.m_listStations);

                            foreach (var station in m_listStations)
                            {
                                if (!string.IsNullOrEmpty(station.GPRS))
                                {
                                    dgvDTUList.m_totalGprsCount += 1;
                                }
                            }

                            //foreach (var station in m_listSoilStations)
                            //{
                            //    if (!string.IsNullOrEmpty(station.GPRS))
                            //    {
                            //        dgvDTUList.m_totalGprsCount += 1;
                            //    }
                            //}

                            //  更新已经上线的GPRS号码
                            foreach (var dtu in dtuList)
                            {
                                string stationId = dtu.m_modemId.ToString();
                                for (int i = 10; i >= stationId.Length; i--)
                                {
                                    stationId = "0" + stationId;
                                }
                                string stationName = QueryStationNameByID(stationId);
                                string stationID = stationId;
                                var port = QuerySubcenterNameByID(stationId);
                                if (stationName != "---")
                                {
                                    dgvDTUList.RefreshGPRSInfo(port, stationName, stationID, dtu);
                                }
                            }
                            //   dgvDTUList.Show();
                            dgvDTUList.UpdateDataToUI();

                            this.lblToolTip.Invoke((Action)delegate
                            {
                                this.lblToolTip.Text = string.Format("GPRS站点在线状态统计  在线:{0}个，离线:{1}个，共:{2}个,上次刷新时间{3}.", dgvDTUList.OnlineGprsCount, dgvDTUList.OfflineGprsCount, dgvDTUList.TotalGprsCount, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                            });
                            //this.lblToolTip.Show();
                        }
                        else
                        {
                            dgvDTUList.OnlineGprsCount = 0;
                            dgvDTUList.TotalGprsCount = 0;
                            try
                            {
                                foreach (var station in m_listStations)
                                {
                                    if (CDBDataMgr.Instance.GetSubCenterById(int.Parse(station.SubCenterID.ToString())).SubCenterName == SubcenterString)
                                    {
                                        if (!string.IsNullOrEmpty(station.GPRS))
                                        {
                                            dgvDTUList.m_totalGprsCount += 1;
                                        }
                                    }
                                }

                                //foreach (var station in m_listSoilStations)
                                //{
                                //    if (CDBSoilDataMgr.Instance.GetSubCenterBySoilId(int.Parse(station.SubCenterID.ToString())).SubCenterName == SubcenterString)
                                //    {
                                //        if (!string.IsNullOrEmpty(station.GPRS))
                                //        {
                                //            dgvDTUList.m_totalGprsCount += 1;
                                //        }
                                //    }
                                //}
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("获取所有分中心站点错误。。。");
                            }

                            try
                            {
                                //  更新已经上线的GPRS号码
                                foreach (var dtu in dtuList)
                                {
                                    var stationName = QueryStationNameByUserID(dtu);
                                    var stationID = QueryStationIDByUserID(dtu);
                                    var station = QueryStationEntityByUserID(dtu);
                                    var soil = QuerySoilStationEntityByUserID(dtu);
                                    var port = QuerySubcenterNameByUserID(dtu);
                                    if (station != null)
                                    {
                                        if (CDBDataMgr.Instance.GetSubCenterById(int.Parse(station.SubCenterID.ToString())).SubCenterName == SubcenterString)
                                        {
                                            dgvDTUList.RefreshGPRSInfo(port, stationName, stationID, dtu);
                                        }
                                    }
                                    //else if (soil != null)
                                    //{

                                    //    if (CDBSoilDataMgr.Instance.GetSubCenterBySoilId(int.Parse(soil.SubCenterID.ToString())).SubCenterName == SubcenterString)
                                    //    {
                                    //        dgvDTUList.RefreshGPRSInfo(port, stationName, stationID, dtu);
                                    //    }
                                    //}
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("更新所有分中心站点错误。。。");
                            }
                            //   dgvDTUList.Show();
                            dgvDTUList.UpdateDataToUI();
                            this.lblToolTip.Invoke((Action)delegate
                            {
                                this.lblToolTip.Text = string.Format("GPRS站点在线状态统计  在线:{0}个，离线:{1}个，共:{2}个,上次刷新时间{3}.", dgvDTUList.OnlineGprsCount, dgvDTUList.OfflineGprsCount, dgvDTUList.TotalGprsCount, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                            });
                            //this.lblToolTip.Show();
                        }
                        m_mutexDataTable_1.ReleaseMutex();
                        // 更新时间
                        m_preRefreshTime = DateTime.Now;
                        m_timeSpanRefresh = new TimeSpan(0, 0, 300);
                    }

                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("刷新错误。。。");
                //MessageBox.Show("Update Third Exception : " + DateTime.Now + "     " + dgvDTUList.IsHandleCreated);
            }
        }
        //通过用户ID查询站名
        private string QueryStationNameByUserID(ModemInfoStruct dtu)
        {
            //string uid = ((uint)dtu.m_modemId).ToString("X").PadLeft(8, '0');
            string uid = dtu.m_modemId.ToString();
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
            if (this.m_listSoilStations != null)
            {
                foreach (var station in this.m_listSoilStations)
                {
                    if (station.GPRS.Trim() == uid.Trim())
                    {
                        return station.StationName;
                    }
                }
            }
            return "---";
        }

        //通过站点ID查询站名
        private string QueryStationNameByID(string stationId)
        {
            
            if (this.m_listStations != null)
            {
                foreach (var station in this.m_listStations)
                {
                    if (station.StationID == stationId)
                    {
                        return station.StationName;
                    }
                }

            }
            if (this.m_listSoilStations != null)
            {
                foreach (var station in this.m_listSoilStations)
                {
                    if (station.StationID == stationId)
                    {
                        return station.StationName;
                    }
                }
            }
            return "---";
        }
        //20181010 gm 通过ID查询分中心名称
        private string QuerySubcenterNameByUserID(ModemInfoStruct dtu)
        {
            String subcenterid = null; 
            string uid = ((uint)dtu.m_modemId).ToString("X").PadLeft(8, '0');
            if (this.m_listStations != null)
            {
                foreach (var station in this.m_listStations)
                {
                    if (station.GPRS.Trim() == uid.Trim())
                    {
                        subcenterid = station.SubCenterID.ToString();
                        break;
                    }
                }

            }
            if (this.m_listSoilStations != null)
            {
                foreach (var station in this.m_listSoilStations)
                {
                    if (station.GPRS.Trim() == uid.Trim())
                    {
                        subcenterid = station.SubCenterID.ToString();
                        break;
                    }
                }
            }
            if(subcenterid != null && subcenterid.Length > 0)
            {
                if(m_listSubcenters != null)
                {
                    foreach (var subcenter in this.m_listSubcenters)
                    {
                        if (subcenter.SubCenterID.ToString().Trim() == subcenterid.Trim())
                        {
                            return subcenter.SubCenterName.ToString();
                        }
                    }
                }
            }
            return "---";
        }

        //20181010 gm 通过站点ID查询分中心名称
        private string QuerySubcenterNameByID(string stationId)
        {
            String subcenterid = null;
            
            if (this.m_listStations != null)
            {
                foreach (var station in this.m_listStations)
                {
                    if (station.StationID == stationId)
                    {
                        subcenterid = station.SubCenterID.ToString();
                        break;
                    }
                }

            }
            if (this.m_listSoilStations != null)
            {
                foreach (var station in this.m_listSoilStations)
                {
                    if (station.StationID == stationId)
                    {
                        subcenterid = station.SubCenterID.ToString();
                        break;
                    }
                }
            }
            if (subcenterid != null && subcenterid.Length > 0)
            {
                if (m_listSubcenters != null)
                {
                    foreach (var subcenter in this.m_listSubcenters)
                    {
                        if (subcenter.SubCenterID.ToString().Trim() == subcenterid.Trim())
                        {
                            return subcenter.SubCenterName.ToString();
                        }
                    }
                }
            }
            return "---";
        }

        //201704gm
        private string QueryStationIDByUserID(ModemInfoStruct dtu)
        {
            //string uid = ((uint)dtu.m_modemId).ToString("X").PadLeft(8, '0');
            string uid = dtu.m_modemId.ToString();
            if (this.m_listStations != null)
            {
                foreach (var station in this.m_listStations)
                {
                    if (station.GPRS.Trim() == uid.Trim())
                    {
                        return station.StationID;
                    }
                }

            }
            if (this.m_listSoilStations != null)
            {
                foreach (var station in this.m_listSoilStations)
                {
                    if (station.GPRS.Trim() == uid.Trim())
                    {
                        return station.StationID;
                    }
                }
            }
            return "---";
        }

        //通过用户ID查询站名
        private CEntityStation QueryStationEntityByUserID(ModemInfoStruct dtu)
        {
            string uid = ((uint)dtu.m_modemId).ToString("X").PadLeft(8, '0');
            if (this.m_listStations != null)
            {
                foreach (var station in this.m_listStations)
                {
                    if (station.GPRS.Trim() == uid.Trim())
                    {
                        return station;
                    }
                }

            }
            return null;
        }

        private CEntitySoilStation QuerySoilStationEntityByUserID(ModemInfoStruct dtu)
        {
            string uid = ((uint)dtu.m_modemId).ToString("X").PadLeft(8, '0');
            if (this.m_listSoilStations != null)
            {
                foreach (var station in this.m_listSoilStations)
                {
                    if (station.GPRS.Trim() == uid.Trim())
                    {
                        return station;
                    }
                }

            }
            return null;
        }

        #endregion 帮助方法



        //分中心的筛选功能
        private void InitSubCenter()
        {
            // 初始化分中心
            List<CEntitySubCenter> subcenter = CDBDataMgr.Instance.GetAllSubCenter();
            cmb_SubCenter.Items.Add(CS_Station_All);
            for (int i = 0; i < subcenter.Count; ++i)
            {
                cmb_SubCenter.Items.Add(subcenter[i].SubCenterName);
            }
        }

        private void EHSubCenterChanged(object sender, EventArgs e)
        {
            try
            {
                // dgvDTUList.Hide();
                m_timeSpanRefresh = new TimeSpan(0, 0, 0);
                int selectindex = cmb_SubCenter.SelectedIndex;
                SubcenterString = cmb_SubCenter.Text;
                if (0 == selectindex)
                {
                    m_mutexDataTable_1.WaitOne();
                    dgvDTUList.SetSubCenterName(null); //所有分中心
                    m_mutexDataTable_1.ReleaseMutex();
                    //    this.lblToolTip.Hide(); 
                }
                else
                {
                    string subcentername = cmb_SubCenter.Text;
                    m_mutexDataTable_1.WaitOne();
                    dgvDTUList.SetSubCenterName(subcentername);
                    m_mutexDataTable_1.ReleaseMutex();
                    // this.lblToolTip.Hide();       
                }
                panel5.Focus();
            }
            catch (Exception ex) { MessageBox.Show("切换分中心有误！" + ex.ToString()); }
            // dgvDTUList.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //    // dgvDTUList.UpdateDataToUI();
            dgvDTUList.Hide();
            m_timeSpanRefresh = new TimeSpan(0, 0, 0);
            dgvDTUList.Show();
        }




    }
}
