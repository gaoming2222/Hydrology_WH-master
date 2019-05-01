using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Hydrology.DataMgr;
using Hydrology.Entity;

namespace Hydrology.CControls
{
    public partial class CStationStateForm : Form, ITabPage
    {

        #region  ITABPAGE
        // 页面关闭事件
        public event EventHandler TabClosed;

        private string m_strTitle;
        public string Title
        {
            get
            {
                return m_strTitle;
            }
            set
            {
                m_strTitle = value;  //设置标题的值
            }
        }
        public ETabType TabType
        {
            get;
            set;
        }
        private Boolean m_bClosable;
        public Boolean BTabRectClosable
        {
            get
            {
                return m_bClosable;
            }
            set
            {
                m_bClosable = value;
            }
        }
        private int m_iTabIndex;
        public int TabPageIndex
        {
            get { return m_iTabIndex; }
            set { m_iTabIndex = value; }
        }
        public void CloseTab()
        {
            if (TabClosed != null)
            {
                this.TabClosed(this, new EventArgs());
            }
        }
        #endregion ITABPAGE
        #region 数据成员
        private CExTabControl m_tabControl;
        private CListViewTabPage m_lvStationStateAllPage;
        private Dictionary<int, CListViewTabPage> m_mapSubCenterPage;

        private TimeSpan m_spanRed; //红色，两个半小时
        private TimeSpan m_spanYellow; // 黄色，一个小时

        private static readonly int S_C_TimeInterval = 10 * 60 * 1000;    //10分钟刷新一次,单位为ms

        // 定时器
        private System.Timers.Timer m_timer = null;

        List<CEntityStation> stationList;
        #endregion ///<数据成员
        public CStationStateForm()
        {
            m_spanRed = new TimeSpan(2, 30, 0);
            m_spanYellow = new TimeSpan(1, 30, 0);

            // 初始化定时器
            m_timer = new System.Timers.Timer();
            m_timer.Elapsed += new System.Timers.ElapsedEventHandler(EHTimer);
            m_timer.Interval = S_C_TimeInterval;
            m_timer.Enabled = false;

            this.FormBorderStyle = FormBorderStyle.None;
            this.Padding = new System.Windows.Forms.Padding(0);
            InitializeComponent();
            InitUI();
            InitSubCenterLayout();
            InitStationState();
            CreateMsgBinding();
            //GenerateTestData();
        }



        private void InitUI()
        {
            this.SuspendLayout();
            m_tabControl = new CExTabControl();
            this.Controls.Add(m_tabControl);

            m_tabControl.Dock = DockStyle.Fill;
            m_tabControl.Alignment = TabAlignment.Bottom;
            //m_tabControl.ItemSelectedColor = Color.FromArgb(255, 0, 0);
            m_tabControl.ItemUnSelectedColor = Color.Transparent;
            //m_tabControl.WhitePlaceFillColor = Color.Red;
            m_tabControl.TabContentSpacePixel = 3;
            m_tabControl.TabRightSpacePixel = -1; //消除完全对比线
            m_tabControl.Margin = new System.Windows.Forms.Padding(0);
            this.ResumeLayout(false);
        }

        private void InitSubCenterLayout()
        {
            m_mapSubCenterPage = new Dictionary<int, CListViewTabPage>();
            m_tabControl.SuspendLayout();
            m_lvStationStateAllPage = new CListViewTabPage() { Title = "所有站点", BTabRectClosable = false };
            m_lvStationStateAllPage.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
            m_lvStationStateAllPage.ListView = new CListViewStationState() { BHorizentalScroolVisible = false };//, PageRowCount = 300 };

            m_tabControl.AddPage(m_lvStationStateAllPage);
            // 添加分中心
            List<CEntitySubCenter> listSubCenter = CDBDataMgr.Instance.GetAllSubCenter();
            for (int i = 0; i < listSubCenter.Count; ++i)
            {
                CListViewTabPage tmp = new CListViewTabPage() { Title = listSubCenter[i].SubCenterName, BTabRectClosable = false };
                tmp.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
                tmp.ListView = new CListViewStationState() { BHorizentalScroolVisible = false };//, PageRowCount = 300 };

                m_mapSubCenterPage.Add(listSubCenter[i].SubCenterID, tmp);
                m_tabControl.AddPage(tmp);
            }

            m_tabControl.ResumeLayout(false);
        }

        private void InitStationState()
        {
            // 初始化默认所有的站点都为正常
            m_tabControl.SuspendLayout();
            List<CEntityStation> listStation = CDBDataMgr.Instance.GetAllStation();

            foreach (CEntityStation station in listStation)
            {
                (m_lvStationStateAllPage.ListView as CListViewStationState).AddStationWithoutNotify(station.StationID,
                    station.StationName, CListViewStationState.EStationState.ENormal);
                // 添加到子页面中
                if (m_mapSubCenterPage.ContainsKey(station.SubCenterID.Value))
                {
                    (m_mapSubCenterPage[station.SubCenterID.Value].ListView as CListViewStationState).AddStationWithoutNotify(
                        station.StationID, station.StationName, CListViewStationState.EStationState.ENormal);
                }
            }
            // 刷新界面
            (m_lvStationStateAllPage.ListView as CListViewStationState).RefreshUI();
            foreach (KeyValuePair<int, CListViewTabPage> item in m_mapSubCenterPage)
            {
                (item.Value.ListView as CListViewStationState).RefreshUI();
            }

            m_tabControl.ResumeLayout(false);

            // 刷新一下站点状态
            RefreshStationState();
            // 收到RTD消息
            CDBDataMgr.Instance.RecvedRTD += new EventHandler<CEventSingleArgs<CEntityRealTime>>(this.EHRecvRTDState);
            m_timer.Start(); //开启定时器
        }

        private void EHRecvRTDState(object sender, CEventSingleArgs<CEntityRealTime> e)
        {
            // 如果更新失败，那么就添加
            //if (!UpdateRTD(e.Value))
            //{
            //    AddRTD(e.Value);
            //}
                RefreshStationState_1(e.Value);
        }

        private void EHSubCenterChanged(object sender, EventArgs e)
        {
            // 重新刷新界面
            m_tabControl.SuspendLayout();
            Dictionary<int, CListViewTabPage> oldMap = m_mapSubCenterPage;
            m_mapSubCenterPage = new Dictionary<int, CListViewTabPage>();
            // 删除原先的分中心页面
            foreach (KeyValuePair<int, CListViewTabPage> item in oldMap)
            {
                m_tabControl.RemovePage(item.Value);
            }
            // 建立新的分中心页面
            // 添加分中心
            List<CEntitySubCenter> listSubCenter = CDBDataMgr.Instance.GetAllSubCenter();
            for (int i = 0; i < listSubCenter.Count; ++i)
            {
                CListViewTabPage tmp = new CListViewTabPage() { Title = listSubCenter[i].SubCenterName, BTabRectClosable = false };
                tmp.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
                if (oldMap.ContainsKey(listSubCenter[i].SubCenterID))
                {
                    tmp.ListView = oldMap[listSubCenter[i].SubCenterID].ListView;
                }
                else
                {
                    tmp.ListView = new CListViewStationState() { BHorizentalScroolVisible = false };//, PageRowCount = 300 };
                }
                m_mapSubCenterPage.Add(listSubCenter[i].SubCenterID, tmp);
                m_tabControl.AddPage(tmp);
            }
            m_tabControl.ResumeLayout(false);
        }

        private void EHStationChanged(object sender, EventArgs e)
        {
            // 站点个数发生改变的消息
            // 清空所有站点
            (m_lvStationStateAllPage.ListView as CListViewStationState).ClearAllStations();
            // 删除原先的分中心站点
            foreach (KeyValuePair<int, CListViewTabPage> item in m_mapSubCenterPage)
            {
                (item.Value.ListView as CListViewStationState).ClearAllStations();
            }

            InitStationState(); //重新加载站点内容
        }

        private void CreateMsgBinding()
        {
            // 绑定消息，分中心变更消息
            CDBDataMgr.Instance.SubCenterUpdated += new EventHandler(this.EHSubCenterChanged);
            // 站点信息变更消息
            CDBDataMgr.Instance.StationUpdated += new EventHandler(this.EHStationChanged);
        }

        private void EHTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            // 每十分钟刷新界面
            //RefreshStationState();
            //CSystemInfoMgr.Instance.AddInfo("刷新站点状态");
        }

        private void CStationStateForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 停止定时器
            m_timer.Stop();
        }

        /// <summary>
        /// 刷新站点状态，根据实时数据
        /// </summary>
        private void RefreshStationState()
        {
            List<CEntityStation> stationList = CDBDataMgr.Instance.GetAllStation();
            foreach (CEntityStation station in stationList)
            {
                Nullable<DateTime> time = CDBDataMgr.Instance.GetStationLastDateTime(station.StationID);
                CListViewStationState.EStationState state = CListViewStationState.EStationState.EError;
                if (time.HasValue)
                {
                    // 计算状态
                    state = GetStationStateByTime(time.Value);
                    (m_lvStationStateAllPage.ListView as CListViewStationState).UpdateStationState(station.StationID, state);
                    if (m_mapSubCenterPage.ContainsKey(station.SubCenterID.Value))
                    {
                        (m_mapSubCenterPage[station.SubCenterID.Value].ListView as CListViewStationState).UpdateStationState(station.StationID, state);
                    }
                }
                else
                {
                    // 没有数据红色
                    (m_lvStationStateAllPage.ListView as CListViewStationState).UpdateStationState(station.StationID, state);
                    if (m_mapSubCenterPage.ContainsKey(station.SubCenterID.Value))
                    {
                        (m_mapSubCenterPage[station.SubCenterID.Value].ListView as CListViewStationState).UpdateStationState(station.StationID, state);
                    }
                }
            }// end of for

            // 更新到界面
            (m_lvStationStateAllPage.ListView as CListViewStationState).RefreshUI();
            foreach (KeyValuePair<int, CListViewTabPage> entity in m_mapSubCenterPage)
            {
                (entity.Value.ListView as CListViewStationState).RefreshUI();
            }

        }

        private void RefreshStationState_1(CEntityRealTime entity)
        {
            List<CEntityStation> stationList_1 = CDBDataMgr.Instance.GetAllStation();
            int? subcenterid = 0;
            for (int i = 0; i < stationList_1.Count; i++)
            {
                if (entity.StrStationID == stationList_1[i].StationID)
                {
                    subcenterid = stationList_1[i].SubCenterID;
                }
            }
            Nullable<DateTime> time = CDBDataMgr.Instance.GetStationLastDateTime(entity.StrStationID);
            CListViewStationState.EStationState state = CListViewStationState.EStationState.EError;
            if (time.HasValue)
            {
                // 计算状态
                state = GetStationStateByTime(time.Value);
                (m_lvStationStateAllPage.ListView as CListViewStationState).UpdateStationState(entity.StrStationID, state);
                if (m_mapSubCenterPage.ContainsKey(subcenterid.Value))
                {
                    (m_mapSubCenterPage[subcenterid.Value].ListView as CListViewStationState).UpdateStationState(entity.StrStationID, state);
                }
            }
            else
            {
                // 没有数据红色
                (m_lvStationStateAllPage.ListView as CListViewStationState).UpdateStationState(entity.StrStationID, state);
           
                if (m_mapSubCenterPage.ContainsKey(subcenterid.Value))
                {
                    (m_mapSubCenterPage[subcenterid.Value].ListView as CListViewStationState).UpdateStationState(entity.StrStationID, state);
                }
            }
        }


        #region 帮助方法
        private CListViewStationState.EStationState GetStationStateByTime(DateTime time)
        {
            TimeSpan span = DateTime.Now - time;
            if (span.TotalSeconds < 0)
            {
                // 非法时间
                return CListViewStationState.EStationState.EError;
            }
            else
            {
                if (span.TotalSeconds < m_spanYellow.TotalSeconds)
                {
                    // 绿色，正常
                    return CListViewStationState.EStationState.ENormal;
                }
                else if (span.TotalSeconds < m_spanRed.TotalSeconds)
                {
                    // 黄色，警告
                    return CListViewStationState.EStationState.EWarning;
                }
                else
                {
                    // 大于了，就是红色
                    return CListViewStationState.EStationState.EError;
                }
            }
        }
        #endregion 帮助方法
        #region 测试方法
        private void GenerateTestData()
        {
            for (int i = 0; i < 2000; ++i)
            {
                if (i % 100 == 25)
                {
                    (m_lvStationStateAllPage.ListView as CListViewStationState).AddStation(i.ToString(),
                        String.Format("站点{0}", i + 1),
                        CListViewStationState.EStationState.EError);
                }
                else if (i % 13 == 0)
                {
                    (m_lvStationStateAllPage.ListView as CListViewStationState).AddStation(i.ToString(),
                        String.Format("站点{0}", i + 1),
                        CListViewStationState.EStationState.EWarning);
                }
                else
                {
                    (m_lvStationStateAllPage.ListView as CListViewStationState).AddStation(i.ToString(),
                        String.Format("站点{0}", i + 1),
                        CListViewStationState.EStationState.ENormal);
                }
            }
        }
        #endregion 测试方法


    } // end of class

}
