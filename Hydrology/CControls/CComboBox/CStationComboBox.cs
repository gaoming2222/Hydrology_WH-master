using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Hydrology.Entity;
using Hydrology.DataMgr;
using System.Drawing;

namespace Hydrology.CControls
{
    public class CStationComboBox : ComboBox
    {
        /// <summary>
        /// 选择了站点的消息
        /// </summary>
        public event EventHandler<CEventSingleArgs<CEntityStation>> StationSelected;

        /// <summary>
        /// 列表选择框
        /// </summary>
        public ListBox m_listBoxStation;


        private List<CEntityStation> m_listStations; //站点列表
        private CEntityStation m_currentStation;  //当前选中的测站
        public CStationComboBox()
            : base()
        {
            InitComponent();
            InitUI();
            m_currentStation = null;
        }

        public CStationComboBox(EStationBatchType batchType)
            : base()
        {
            InitComponent();
            InitUI(batchType);
            m_currentStation = null;
        }
        /// <summary>
        /// 获取当前选择的站点
        /// </summary>
        /// <returns>如果为空，表示当前没有选择站点</returns>
        public CEntityStation GetStation()
        {
            return m_currentStation;
        }
        public void SetCurrentStation(CEntityStation station, bool bNotify = true)
        {
            // 设置当前的站点
            if (station == null)
            {
                return;
            }
            m_currentStation = station;
            this.TextChanged -= cmbStation_TextChanged;
            this.Text = GetDisplayStationName(station);
            this.TextChanged += cmbStation_TextChanged;
            if (bNotify)
            {
                // 发消息
                if (StationSelected != null)
                {
                    StationSelected.Invoke(this, new CEventSingleArgs<CEntityStation>(m_currentStation));
                }
            }
        }

        #region 重写
        //protected override void WndProc(ref Message m)
        //{
        //    if (!this.DesignMode)
        //    {
        //        //if (m.Msg == WM_PARENTNOTIFY)
        //        //{
        //            if (m.WParam.ToInt32() == WM_LBUTTONDOWN)
        //            {
        //                //do you staff
        //                Debug.WriteLine(string.Format("x:{0},y{1}",this.Cursor.HotSpot.X,this.Cursor.HotSpot.Y));
        //            }
        //        //}
        //    }
        //    base.WndProc(ref m);
        //}
        #endregion 重写

        #region 事件响应
        /// <summary>
        /// 当用户点击向下箭头时候的响应消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EHControlClick(object sender, EventArgs e)
        {
            // 不显示下列表
            //this.DroppedDown = false;
            //Debug.WriteLine("mouse click");
            this.ShowListBox();
        }

        /// <summary>
        /// 选择了列表框中的某一个站点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EHListBoxSelected(object sender, EventArgs e)
        {
            this.TextChanged -= cmbStation_TextChanged;
            //this.Items.Clear();
            //this.Items.Add(m_listBoxStation.SelectedItem);
            //this.SelectedIndex = 0;
            this.Text = m_listBoxStation.SelectedItem.ToString();
            string[] idName = m_listBoxStation.SelectedItem.ToString().Split('|');

            // 匹配当前的站点赋值
            string stationId = idName[0].Substring(1).Trim();
            for (int i = 0; i < m_listStations.Count; ++i)
            {
                if (stationId == m_listStations[i].StationID)
                {
                    m_currentStation = m_listStations[i];
                    // 发消息
                    if (StationSelected != null)
                    {
                        StationSelected.Invoke(this, new CEventSingleArgs<CEntityStation>(m_currentStation));
                    }
                    break;
                }
            }
            this.TextChanged += new EventHandler(cmbStation_TextChanged);
            HideListBox();
        }

        /// <summary>
        /// 失去焦点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EHControlLeaveFocuse(object sender, EventArgs e)
        {
            // 并且是不列表框获得了焦点
            if (!m_listBoxStation.Focused)
            {
                HideListBox();
            }

        }

        private void EHLoseFocus(object sender, EventArgs e)
        {
            // 并且是不列表框获得了焦点
            if (!m_listBoxStation.Focused)
            {
                HideListBox();
            }

        }

        private void cmbStation_TextChanged(object sender, EventArgs e)
        {
            try
            {
                ShowListBox();
                //this.Container.Add(m_listViewStation);

                //// 站点上面内容发生改变
                string filter = this.Text;
                Debug.WriteLine(string.Format("--->{0}", filter));
                m_listBoxStation.Items.Clear();
                CEntityStation lastStation = null;
                foreach (CEntityStation station in m_listStations)
                {
                    string tmp = GetDisplayStationName(station);
                    if (tmp.Contains(filter))
                    {
                        m_listBoxStation.Items.Add(tmp);
                        lastStation = station;
                    }
                }
                //this.SelectionStart = this.Text.Length;
                //// 如果只有一个的话，就不需要显示下拉列表了
                if (m_listBoxStation.Items.Count == 1)
                {
                    this.TextChanged -= cmbStation_TextChanged;
                    this.Text = GetDisplayStationName(lastStation);
                    HideListBox();
                    this.TextChanged += new EventHandler(cmbStation_TextChanged);
                    m_currentStation = lastStation;
                    // 发消息
                    if (StationSelected != null)
                    {
                        StationSelected.Invoke(this, new CEventSingleArgs<CEntityStation>(lastStation));
                    }
                }
                else if (m_listBoxStation.Items.Count == 0)
                {
                    // 如果没有符合要求的站点，初始化为所有站点
                    this.TextChanged -= cmbStation_TextChanged;
                    this.Text = "";
                    foreach (CEntityStation station in m_listStations)
                    {
                        string tmp = GetDisplayStationName(station);
                        m_listBoxStation.Items.Add(tmp);
                    }
                    this.TextChanged += new EventHandler(cmbStation_TextChanged);
                    // 发消息，当前没有选中站点
                    if (StationSelected != null)
                    {
                        StationSelected.Invoke(this, new CEventSingleArgs<CEntityStation>(null));
                    }
                    m_currentStation = null;
                }
                else
                {
                    // 没有选中的站点
                    this.DroppedDown = true; //显示下拉列表
                    // 发消息,当前没有选中站点
                    if (StationSelected != null)
                    {
                        StationSelected.Invoke(this, new CEventSingleArgs<CEntityStation>(null));
                    }
                    m_currentStation = null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

        }

        private void EHMouseCaptureChanged(object sender, EventArgs e)
        {
            // Debug.WriteLine("mouse caputure changed");
            if (!m_listBoxStation.Focused)
            {
                HideListBox();
            }
        }


        #endregion  事件响应

        #region  帮助方法
        private void InitUI(EStationBatchType type)
        {
            /// 初始化ListView
            if (m_listStations == null)
                m_listStations = new List<CEntityStation>();

            var lists = CDBDataMgr.Instance.GetAllStation();
            foreach (var station in lists)
            {
                if (station.StationType != EStationType.ESoil)
                {
                    m_listStations.Add(station);
                    //this.Items.Add(GetDisplayStationName(station));
                    m_listBoxStation.Items.Add(GetDisplayStationName(station));
                }
            }
            // 绑定消息
            this.TextChanged += cmbStation_TextChanged;
        }

        private void InitUI()
        {

            m_listStations = CDBDataMgr.Instance.GetAllStation();
            //cmbStation.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            //cmbStation.AutoCompleteSource = AutoCompleteSource.CustomSource;
            foreach (CEntityStation station in m_listStations)
            {
                //cmbStation.AutoCompleteCustomSource.Add(GetDisplayStationName(station));
                //this.Items.Add(GetDisplayStationName(station));
                m_listBoxStation.Items.Add(GetDisplayStationName(station));
            }

            // 绑定消息
            this.TextChanged += cmbStation_TextChanged;
        }

        private void SetSubcenter(String subcenterName)
        {

            m_listStations = CDBDataMgr.Instance.GetAllStation();
            //cmbStation.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            //cmbStation.AutoCompleteSource = AutoCompleteSource.CustomSource;
            foreach (CEntityStation station in m_listStations)
            {
                //cmbStation.AutoCompleteCustomSource.Add(GetDisplayStationName(station));
                //this.Items.Add(GetDisplayStationName(station));
                m_listBoxStation.Items.Add(GetDisplayStationName(station));
            }

            // 绑定消息
            this.TextChanged += cmbStation_TextChanged;
        }

        private void InitComponent()
        {
            // 初始化控件
            this.SuspendLayout();

            this.DropDownStyle = ComboBoxStyle.Simple;
            m_listBoxStation = new ListBox();
            m_listBoxStation.Height = 200;
            m_listBoxStation.Hide(); //不可见

            this.Click += new EventHandler(EHControlClick);
            this.Leave += new EventHandler(EHControlLeaveFocuse);
            //this.MouseLeave += new EventHandler(EHMouseLeave);
            this.LostFocus += new EventHandler(EHLoseFocus);
            //this.MouseCaptureChanged += new EventHandler(EHMouseCaptureChanged);
            m_listBoxStation.SelectedIndexChanged += new EventHandler(EHListBoxSelected);

            //this.DrawMode = DrawMode.OwnerDrawVariable;
            //this.DrawItem += new DrawItemEventHandler(EHDrawItem);
            //this.MeasureItem += new MeasureItemEventHandler(EHMeasureItem);


            this.ResumeLayout(false);
        }

        /// <summary>
        /// 格式化站点名字显示
        /// </summary>
        /// <param name="station"></param>
        /// <returns></returns>
        private string GetDisplayStationName(CEntityStation station)
        {
            return string.Format("({0,-4}|{1})", station.StationID, station.StationName);
        }

        /// <summary>
        /// 显示ListBox
        /// </summary>
        private bool ShowListBox()
        {
            try
            {
                // 如果为空，会异常，一般不会的
                Control control = this;
                Point point = new Point() { X = 0, Y = 0 }; //坐标
                while (control.Parent != null)
                {
                    point.X = point.X + control.Location.X;
                    point.Y = point.Y + control.Location.Y;
                    control = control.Parent;
                }
                point.Y = point.Y + this.Height; //高度
                //point.X = point.X + control.Location.X;
                //point.Y = point.Y + control.Location.Y + this.Height;
                control.Controls.Add(m_listBoxStation);
                m_listBoxStation.Width = this.Width;
                m_listBoxStation.Location = point;
                m_listBoxStation.TopIndex = 0;
                m_listBoxStation.BringToFront();
                m_listBoxStation.Visible = true;
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }

        }

        /// <summary>
        /// 隐藏ListBox
        /// </summary>
        private void HideListBox()
        {
            m_listBoxStation.Hide();
        }

        #endregion 帮助方法

    } // class CStationComboBox
}
