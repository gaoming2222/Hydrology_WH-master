using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Hydrology.DataMgr;
using Hydrology.Entity;

namespace Hydrology.CControls
{
    partial class CRTDSoilForm : Form, ITabPage
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
        private CDataGridTabPage m_dgvAllPage;
        private Dictionary<int, CDataGridTabPage> m_mapSubCenterPage;

        private static readonly int S_C_TimeInterval = 10 * 60 * 1000;    //10分钟刷新一次,单位为ms

        // 定时器
        private System.Timers.Timer m_timer = null;


        #endregion ///<PRIVATE_MEMBER

        #region 公共方法
        public CRTDSoilForm()
            : base()
        {
            // 初始化定时器
            m_timer = new System.Timers.Timer();
            m_timer.Elapsed += new System.Timers.ElapsedEventHandler(EHTimer);
            m_timer.Interval = S_C_TimeInterval;
            m_timer.Enabled = false;

            this.FormBorderStyle = FormBorderStyle.None;
            this.Padding = new System.Windows.Forms.Padding(0);
            InitializeComponent();
            InitUI();
            InitStation();
        }

        /// <summary>
        /// 添加实时数据
        /// </summary>
        /// <param name="entity"></param>
        public void AddRTD(CEntitySoilData entity)
        {
            // 根据ID进行分中心分发
            (m_dgvAllPage.DataGrid as CDataGridViewSoilRTD).AddRTD(entity);

            // 获取每个站点分中心的ID
           // CEntityStation station = CDBDataMgr.Instance.GetStationById(entity.StationID);

            CEntitySoilStation soilstation = CDBSoilDataMgr.Instance.GetSoilStationInfoByStationId(entity.StationID);

            if (null != soilstation && m_mapSubCenterPage.ContainsKey(soilstation.SubCenterID.Value))
            {
                (m_mapSubCenterPage[soilstation.SubCenterID.Value].DataGrid as CDataGridViewSoilRTD).AddRTD(entity);
            }
            //(m_dgvAllPage.DataGrid as CDataGridViewSoilRTD).RecalculateHeaderSize(); //计算表头宽度
        }
        /// <summary>
        /// 更新实时数据
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool UpdateRTD(CEntitySoilData entity)
        {
            // 根据ID进行分中心分发
            bool result = (m_dgvAllPage.DataGrid as CDataGridViewSoilRTD).UpdateRTD(entity);
            // 获取每个站点分中心的ID
           // CEntityStation station = CDBDataMgr.Instance.GetStationById(entity.StationID);
            CEntitySoilStation soilstation = CDBSoilDataMgr.Instance.GetSoilStationInfoByStationId(entity.StationID);

            if (null != soilstation && m_mapSubCenterPage.ContainsKey(soilstation.SubCenterID.Value))
            {
                result = result && (m_mapSubCenterPage[soilstation.SubCenterID.Value].DataGrid as CDataGridViewSoilRTD).UpdateRTD(entity);
            }
            return result;
        }

        public List<CEntitySoilData> GetRTDList()
        {
            return (m_dgvAllPage.DataGrid as CDataGridViewSoilRTD).GetRTDList();
        }

        /// <summary>
        /// 根据分中心名字显示分中心
        /// </summary>
        /// <param name="subcenterName"></param>
        public void ShowTabOfSubCenter(string subcenterName)
        {
            CEntitySubCenter subcenter = CDBDataMgr.Instance.GetSubCenterByName(subcenterName);
            if (null != subcenter)
            {
                // 显示该分中心
                if (m_mapSubCenterPage.ContainsKey(subcenter.SubCenterID))
                {
                    // 如果包含了该分中心，获取索引然后显示
                    m_tabControl.SelectedIndex = (m_mapSubCenterPage[subcenter.SubCenterID] as ITabPage).TabPageIndex;
                }
            }
        }
        #endregion ///<PUBLIC_METHOD

        #region 帮助方法
        /// <summary>
        ///  初始化自定义界面
        /// </summary>
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

            this.FormClosing += new FormClosingEventHandler(EHFormClosing);
            this.ResumeLayout(false);
        }

        private void InitStation()
        {
            m_mapSubCenterPage = new Dictionary<int, CDataGridTabPage>();
            m_tabControl.SuspendLayout();
            m_dgvAllPage = new CDataGridTabPage() { Title = "所有站点", BTabRectClosable = false };
            m_dgvAllPage.DataGrid = new CDataGridViewSoilRTD() { AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            m_dgvAllPage.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
            m_tabControl.AddPage(m_dgvAllPage);

            // 添加分中心
            List<CEntitySubCenter> listSubCenter = CDBDataMgr.Instance.GetAllSubCenter();
            for (int i = 0; i < listSubCenter.Count; ++i)
            {
                CDataGridTabPage tmp = new CDataGridTabPage() { Title = listSubCenter[i].SubCenterName, BTabRectClosable = false };
                m_mapSubCenterPage.Add(listSubCenter[i].SubCenterID, tmp);
                tmp.DataGrid = new CDataGridViewSoilRTD() { AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                tmp.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
                m_tabControl.AddPage(tmp);
                //(tmp.DataGrid as CDataGridViewSoilRTD).RecalculateHeaderSize(); //计算表头宽度
            }

            // 绑定消息，分中心变更消息
            CDBDataMgr.Instance.SubCenterUpdated += new EventHandler(this.EHSubCenterChanged);
            // 收到RTD消息
            CDBSoilDataMgr.Instance.RecvedRTDSoilData += new EventHandler<CEventSingleArgs<CEntitySoilData>>(EHRecvRTD);
            // 收到清空RTD消息
            CDBSoilDataMgr.Instance.RTDSoilDataClear += new EventHandler(EHClearRTD);

            m_tabControl.ResumeLayout(false);

            // 开启定时器
            m_timer.Start();

            //(m_dgvAllPage.DataGrid as CDataGridViewSoilRTD).RecalculateHeaderSize();
        }

        #endregion ///<帮助方法

        private void EHSubCenterChanged(object sender, EventArgs e)
        {
            // 重新刷新界面
            m_tabControl.SuspendLayout();
            Dictionary<int, CDataGridTabPage> oldMap = m_mapSubCenterPage;
            m_mapSubCenterPage = new Dictionary<int, CDataGridTabPage>();
            // 删除原先的分中心页面
            foreach (KeyValuePair<int, CDataGridTabPage> item in oldMap)
            {
                m_tabControl.RemovePage(item.Value);
            }
            // 建立新的分中心页面
            // 添加分中心
            List<CEntitySubCenter> listSubCenter = CDBDataMgr.Instance.GetAllSubCenter();
            for (int i = 0; i < listSubCenter.Count; ++i)
            {
                CDataGridTabPage tmp = new CDataGridTabPage() { Title = listSubCenter[i].SubCenterName, BTabRectClosable = false };
                tmp.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
                m_mapSubCenterPage.Add(listSubCenter[i].SubCenterID, tmp);
                if (oldMap.ContainsKey(listSubCenter[i].SubCenterID))
                {
                    //Debug.WriteLine("before-{0}:{1}", i,oldMap[listSubCenter[i].SubCenterID].DataGrid.Width);
                    //int width = oldMap[listSubCenter[i].SubCenterID].DataGrid.Width;
                    oldMap[listSubCenter[i].SubCenterID].RemoveDataGrid();
                    // 内容不均匀分布问题
                    oldMap[listSubCenter[i].SubCenterID].DataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                    tmp.DataGrid = oldMap[listSubCenter[i].SubCenterID].DataGrid;
                    tmp.DataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    //Debug.WriteLine("after-{0}:{1}", i,tmp.DataGrid.Width);
                    //tmp.DataGrid.Width = width;
                }
                else
                {
                    // 新建数据项
                    tmp.DataGrid = new CDataGridViewSoilRTD();
                    tmp.DataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
                m_tabControl.AddPage(tmp);
            }
            m_tabControl.ResumeLayout(false);
        }

        private void EHRecvRTD(object sender, CEventSingleArgs<CEntitySoilData> e)
        {
            // 如果更新失败，那么就添加
            if (!UpdateRTD(e.Value))
            {
                AddRTD(e.Value);
            }
        }

        private void EHClearRTD(object sender, EventArgs e)
        {
            // 切换数据库时，会清空RTD数据
            m_dgvAllPage.DataGrid.ClearAllRows();
            foreach (KeyValuePair<int, CDataGridTabPage> entity in m_mapSubCenterPage)
            {
                entity.Value.DataGrid.ClearAllRows();
            }
        }

        private void EHTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            // 每十分钟刷新界面
            // 切换数据库时，会清空RTD数据
            //bool result = (m_dgvAllPage.DataGrid as CDataGridViewSoilRTD).RefreshRTDTimeOutStatus();
            //foreach (KeyValuePair<int, CDataGridTabPage> entity in m_mapSubCenterPage)
            //{
            //    result = result && (entity.Value.DataGrid as CDataGridViewSoilRTD).RefreshRTDTimeOutStatus();
            //}
            //CSystemInfoMgr.Instance.AddInfo("刷新土壤墒情实时数据状态");
        }

        private void EHFormClosing(object sender, FormClosingEventArgs e)
        {
            // 停止定时器
            m_timer.Stop();
        }

    }
}
