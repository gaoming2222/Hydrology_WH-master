using System;
using Hydrology.Entity;
using Hydrology.Forms;
using System.Collections.Generic;
using System.Windows.Forms;
using Protocol.Manager;
using Hydrology.Utils;
using Hydrology.DataMgr;
using System.Data;
using Hydrology.CControls;
using System.Collections;

namespace Hydrology.CControls
{
    class CDataGridViewGPRSNew : CExDataGridView
    {
        //常量
        private static readonly string CS_Port = "分中心";
        private static readonly string CS_StationName = "站名";
        private static readonly string CS_StationID = "站点ID";
        private static readonly string CS_UserId = "用户ID";
        private static readonly string CS_Telephone = "SIM卡号";
        private static readonly string CS_DynamicIP = "动态IP";
        private static readonly string CS_ConnectionTime = "连接时间";
        private static readonly string CS_RecvTime = "刷新时间";
        private string CS_OnlineOrOffline = "在线状态";
        private string CS_OnlineOrOfflineFlag = "在线状态记录";
        // 数据内容
        public DataTable m_dataTable_1;
        // public CDataGridViewGPRS Gprs_Copy;

        private ToolStripMenuItem m_itemOnlineFirst;
        private ToolStripMenuItem m_itemOfflineFirst;
        private ToolStripMenuItem m_itemRecoverDefault; //恢复默认

        private int m_onlineGprsCount = 0;
        public int m_totalGprsCount = 0;

        public int TotalGprsCount
        {
            get { return m_totalGprsCount; }
            set { m_totalGprsCount = value; }
        }
        public int OnlineGprsCount
        {
            get { return m_onlineGprsCount; }
            set { m_onlineGprsCount = value; }
        }
        public int OfflineGprsCount
        {
            get { return m_totalGprsCount - m_onlineGprsCount; }
        }
        public string cS_OnlineOrOffline
        {
            get { return CS_OnlineOrOffline; }
            set { CS_OnlineOrOffline = value; }
        }


        public CDataGridViewGPRSNew()
            : base()
        {
            //InitializeComponent();
            base.Header = new string[] { CS_Port,CS_StationName, CS_StationID, CS_UserId, CS_Telephone, CS_DynamicIP, CS_ConnectionTime, CS_RecvTime, CS_OnlineOrOffline, CS_OnlineOrOfflineFlag };
            m_dataTable_1 = new DataTable();
            m_dataTable_1.Columns.Add(CS_Port);
            m_dataTable_1.Columns.Add(CS_StationName);
            m_dataTable_1.Columns.Add(CS_StationID);
            m_dataTable_1.Columns.Add(CS_UserId);
            m_dataTable_1.Columns.Add(CS_Telephone);
            m_dataTable_1.Columns.Add(CS_DynamicIP);
            m_dataTable_1.Columns.Add(CS_ConnectionTime);
            m_dataTable_1.Columns.Add(CS_RecvTime);
            m_dataTable_1.Columns.Add(CS_OnlineOrOffline);
            m_dataTable_1.Columns.Add(CS_OnlineOrOfflineFlag);
            m_dataTable_1.Columns.Add("state");


            this.Columns[3].Visible = false;
            this.Columns[4].Visible = false;
            this.Columns[5].Visible = false;
            this.Columns[9].Visible = false;
            //  this.ReCalculateSize();
            //调整列宽
            AutoSizeColumn(this);
            this.Columns[6].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.Columns[7].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            //设置只能选中整行  不能选中单元格，防止刷新时出错
            this.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            // 是否启用分页功能，默认启用
            // base.BPartionPageEnable = false;
            // base.CausesValidation = false; //取消验证数据 
        }


        /// <summary>
        /// 使DataGridView的列自适应宽度
        /// </summary>
        /// <param name="dgViewFiles"></param>
        private void AutoSizeColumn(DataGridView dgViewFiles)
        {
            int width = 0;
            //使列自使用宽度
            //对于DataGridView的每一个列都调整
            for (int i = 0; i < dgViewFiles.Columns.Count; i++)
            {
                //将每一列都调整为自动适应模式
                dgViewFiles.AutoResizeColumn(i, DataGridViewAutoSizeColumnMode.AllCells);
                //记录整个DataGridView的宽度
                width += dgViewFiles.Columns[i].Width;
            }
            //判断调整后的宽度与原来设定的宽度的关系，如果是调整后的宽度大于原来设定的宽度，
            //则将DataGridView的列自动调整模式设置为显示的列即可，
            //如果是小于原来设定的宽度，将模式改为填充。
            if (width > dgViewFiles.Size.Width)
            {
                dgViewFiles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            }
            else
            {
                dgViewFiles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            //冻结某列 从左开始 0，1，2
        }

        public void ReCalculateSize()
        {
            //for (int i = 0; i < 8; ++i)
            //{
            //    this.Columns[i].Width = 20; //设定宽度
            //}      
        }

        public void RefreshGPRSInfo(string port, string stationName, string stationID, ModemInfoStruct dtu)
        {
            try
            {
                //  m_mutexDataTable.WaitOne();
                //string uid = ((uint)dtu.m_modemId).ToString("X").PadLeft(8, '0');
                string uid = dtu.m_modemId.ToString();
                for (int i = 10; i >= uid.Length; i--)
                {
                    uid = "0" + uid;
                }
                string phoneno = "---";
                string dynIP = CGprsUtil.Byte4ToIP(dtu.m_dynip, 0);
                
                DateTime connetime = CGprsUtil.ULongToDatetime(dtu.m_conn_time);
                DateTime refreshTime = CGprsUtil.ULongToDatetime(dtu.m_refresh_time);
                //如果计算机当前时间与刷新时间间隔超过一小时，标记为红色
                //现在改为10分钟，不在线，红色标记
                EDataState state = EDataState.ENormal;

                //  if ((DateTime.Now - refreshTime).TotalMinutes > 60)
                //超过10分钟，表示不在线
                if ((DateTime.Now - refreshTime).TotalMinutes > 10)
                {
                    this.cS_OnlineOrOffline = "离线";
                    this.CS_OnlineOrOfflineFlag = "0";
                    state = EDataState.EError;
                }
                else
                {
                    this.cS_OnlineOrOffline = "在线";
                    this.CS_OnlineOrOfflineFlag = "2";
                    this.m_onlineGprsCount += 1;
                }

                // 判断是否在列中存在，如果不存在，则新建列
                // 先找到ID所在的行
                // this.Hide();
                // m_dataTable_1 = base.m_dataTable;
                string a = m_dataTable_1.Rows[0][CS_StationID].ToString();
                string b = m_dataTable_1.Rows[0][CS_StationID].ToString();
                this.BeginInvoke(new System.Action(() =>
                {
                    for (int i = 0; i < m_dataTable_1.Rows.Count; ++i)
                    {
                        if (m_dataTable_1.Rows[i][CS_StationID].ToString() == uid)
                        {
                            //m_dataTable_1.Rows.Add(new string[] { port.ToString(), stationName, uid, phoneno, dynIP, connetime.ToString(), refreshTime.ToString(), CS_OnlineOrOffline.ToString(), CS_OnlineOrOfflineFlag, Convert.ToString((int)state) });
                            DataRow newRow = m_dataTable_1.NewRow();
                            newRow[CS_Port] = port.ToString();
                            newRow[CS_StationName] = stationName;
                            newRow[CS_StationID] = stationID;
                            newRow[CS_UserId] = uid;
                            newRow[CS_Telephone] = phoneno;
                            newRow[CS_DynamicIP] = dynIP;
                            newRow[CS_ConnectionTime] = connetime.ToString();
                            newRow[CS_RecvTime] = refreshTime.ToString();
                            newRow["在线状态"] = this.cS_OnlineOrOffline.ToString();
                            newRow["在线状态记录"] = CS_OnlineOrOfflineFlag;
                            newRow["state"] = Convert.ToString((int)state);
                            m_dataTable_1.Rows.InsertAt(newRow, i);
                            m_dataTable_1.Rows.RemoveAt(i + 1);
                            // 找到匹配，更新行的内容
                            //Gprs_Copy.UpdateRowData(i, new string[] { port.ToString(), stationName, uid, phoneno, dynIP, connetime.ToString(), refreshTime.ToString(), CS_OnlineOrOffline.ToString(), CS_OnlineOrOfflineFlag }, state);
                            //m_mutexDataTable.ReleaseMutex();
                            //return;
                        }
                    }
                    //DataView dataView = m_dataTable_1.DefaultView;
                    //dataView.Sort = "用户ID asc";
                    //m_dataTable_1 = dataView.ToTable();  
                    //for (int i = 0; i < base.m_dataTable.Columns.Count; ++i)
                    //{
                    //    Console.WriteLine(base.m_dataTable.Columns[i].ColumnName + "...");
                    //}
                    //for (int i = 0; i < 1; ++i)
                    //{
                    //    for (int j = 0; j < base.m_dataTable.Columns.Count; ++j)
                    //    {
                    //        Console.WriteLine(base.m_dataTable.Rows[i][j] + "...");
                    //    }
                    //}
                    // m_dataTable_1.DefaultView.Sort="用户ID asc";
                    base.DataSource = m_dataTable_1.DefaultView;
                    return;
                }));
                //  this.Show();
                // 没有找到匹配，添加新的行记录
                //  m_mutexDataTable.ReleaseMutex();
                return;
                //base.AddRow(new string[] { port.ToString(), stationName, uid, phoneno, dynIP, connetime.ToString(), refreshTime.ToString(), CS_OnlineOrOffline.ToString(), CS_OnlineOrOfflineFlag }, state);
                //this.m_totalGprsCount += 1;
            }
            catch (Exception ex)
            {
                this.Hide();
                MessageBox.Show("刷新出错！" + ex.ToString());
                this.Show();
            }
        }

        public void RefreshGPRSInfo(List<CEntityStation> stations)
        {
            try
            {
                if (null == stations || stations.Count == 0)
                {
                    return;
                }
                // 判断是否在列中存在，如果不存在，则新建列
                // 先找到ID所在的行
                m_mutexDataTable.WaitOne();
                // this.Hide();

                foreach (var station in stations)
                {
                    if (!string.IsNullOrEmpty(station.GPRS))
                    {
                        this.m_totalGprsCount += 1;
                        //既不离线也不在线为1
                        base.AddRow(new string[] { "---", station.StationName, station.StationID, station.GPRS, "---", "---", "---", "---", "---", "1" }, EDataState.ENormal);
                        m_dataTable_1.Rows.Add(new string[] { "---", station.StationName, station.StationID, station.GPRS, "---", "---", "---", "---", "---", "1", "0" });
                        //Gprs_Copy.AddRow(new string[] { "---", station.StationName, station.GPRS, "---", "---", "---", "---", "---", "1" }, EDataState.ENormal);
                    }
                }
                //Gprs_Copy.UpdateDataToUI();
                //Console.WriteLine(this.Rows.Count + "...");
                //Console.WriteLine(Gprs_Copy.Rows.Count+"....");
                //   this.Show();
                m_mutexDataTable.ReleaseMutex();
            }
            catch (Exception ex)
            {
                this.Hide();
                MessageBox.Show("刷新出错！" + ex.ToString());
                this.Show();
            }
        }
        public void RefreshGPRSInfoSoil(List<CEntitySoilStation> stations)
        {
            try
            {
                if (null == stations || stations.Count == 0)
                {
                    return;
                }
                // 判断是否在列中存在，如果不存在，则新建列
                // 先找到ID所在的行
                m_mutexDataTable.WaitOne();
                // this.Hide();

                foreach (var station in stations)
                {
                    if (!string.IsNullOrEmpty(station.GPRS))
                    {
                        this.m_totalGprsCount += 1;
                        //既不离线也不在线为1
                        this.AddRow(new string[] { "---", station.StationName, station.StationID, station.GPRS, "---", "---", "---", "---", "---", "1" }, EDataState.ENormal);
                        m_dataTable_1.Rows.Add(new string[] { "---", station.StationName, station.StationID, station.GPRS, "---", "---", "---", "---", "---", "1", "0" });
                        //Gprs_Copy.AddRow(new string[] { "---", station.StationName, station.GPRS, "---", "---", "---", "---", "---", "1" }, EDataState.ENormal);
                    }
                }
                //   this.Show();
                //Gprs_Copy.UpdateDataToUI();
                m_mutexDataTable.ReleaseMutex();
            }
            catch (Exception ex)
            {
                this.Hide();
                MessageBox.Show("刷新出错！" + ex.ToString());
                this.Show();
            }
        }


        protected override void OnSizeChanged(object sender, EventArgs e)
        {
            base.OnSizeChanged(sender, e);
            this.Columns[6].Width = 100;
            this.Columns[7].Width = 100;
        }


        //public DataTable GetDgvToTable(DataGridView dgv)
        //{
        //    DataTable dt = new DataTable();

        //    // 列强制转换
        //    for (int count = 0; count < dgv.Columns.Count; count++)
        //    {
        //        DataColumn dc = new DataColumn(dgv.Columns[count].Name.ToString());
        //        dt.Columns.Add(dc);
        //    }

        //    // 循环行
        //    for (int count = 0; count < dgv.Rows.Count; count++)
        //    {
        //        DataRow dr = dt.NewRow();
        //        for (int countsub = 0; countsub < dgv.Columns.Count; countsub++)
        //        {
        //            dr[countsub] = Convert.ToString(dgv.Rows[count].Cells[countsub].Value);
        //        }
        //        dt.Rows.Add(dr);
        //    }
        //    return dt;
        //}
        // 初始化右键菜单项
        protected override void InitContextMenu()
        {
            base.InitContextMenu();

            // 添加警告优先，正常优先，以及筛选器选项
            m_itemOnlineFirst = new ToolStripMenuItem() { Text = "在线优先" };
            m_itemOfflineFirst = new ToolStripMenuItem() { Text = "离线优先" };
            m_itemRecoverDefault = new ToolStripMenuItem() { Text = "恢复默认" };
            // ToolStripMenuItem itemFilter = new ToolStripMenuItem() { Text = "筛选条件..." };

            ToolStripSeparator seperator = new ToolStripSeparator();
            m_contextMenu.Items.Add(seperator);
            m_contextMenu.Items.Add(m_itemOnlineFirst);
            m_contextMenu.Items.Add(m_itemOfflineFirst);
            m_contextMenu.Items.Add(m_itemRecoverDefault);
            seperator = new ToolStripSeparator();
            //m_contextMenu.Items.Add(seperator);
            //m_contextMenu.Items.Add(itemFilter);

            // 绑定消息
            m_itemOnlineFirst.Click += EHOnlineFirst;
            m_itemOfflineFirst.Click += EHOfflineFirst;
            m_itemRecoverDefault.Click += EHRecoverDefault;
        }

        //在线优先事件
        private void EHOnlineFirst(object sender, EventArgs e)
        {
            //在线优先
            m_itemOnlineFirst.Checked = true;
            m_itemOfflineFirst.Checked = false;
            m_itemRecoverDefault.Checked = false;
            if (this.Columns.Count > 0)
            {
                //for (int i = 0; i < this.RowCount; i++)
                //{
                //    if (this.Rows[i].Cells[8].Value.ToString() == "3")
                //    {
                //        this.Rows[i].Cells[8].Value = "0";
                //    }
                //}
                // 排序
                //this.Columns[2].SortMode = DataGridViewColumnSortMode.Automatic; 
                //this.Sort(this.Columns[8], System.ComponentModel.ListSortDirection.Descending);
                m_dataTable_1.DefaultView.Sort = "在线状态记录 desc,用户id asc";
                base.DataSource = m_dataTable_1.DefaultView;
                //if(this.Rows[][8])
                //this.Sort(this.Columns[2], System.ComponentModel.ListSortDirection.Descending);
                //Comparison<DataGridViewRow> compare = (a, b) =>
                //{
                //    if (a.Cells[8].Value.Equals(b.Cells[8].Value))
                //    {          
                //        return a.Cells[2].Value.ToString().CompareTo(b.Cells[2].Value.ToString());
                //    }
                //    return a.Cells[8].Value.ToString().CompareTo(b.Cells[8].Value.ToString());
                //};
                //this.Sort(compare);
            }
        }
        //离线优先事件
        private void EHOfflineFirst(object sender, EventArgs e)
        {
            //离线优先
            m_itemOnlineFirst.Checked = false;
            m_itemOfflineFirst.Checked = true;
            m_itemRecoverDefault.Checked = false;
            if (this.Columns.Count > 0)
            {
                //for (int i = 0; i < this.RowCount; i++)
                //{
                //    if (this.Rows[i].Cells[8].Value.ToString() == "0")
                //    {
                //        this.Rows[i].Cells[8].Value = "3";
                //    }
                //}
                // 排序
                // this.Columns[2].SortMode = DataGridViewColumnSortMode.Automatic; 
                //this.Sort(this.Columns[8], System.ComponentModel.ListSortDirection.Ascending);
                m_dataTable_1.DefaultView.Sort = "在线状态记录 asc,用户id asc";
                base.DataSource = m_dataTable_1.DefaultView;
                //this.Sort();
                //this.Sort(this.Columns[2], System.ComponentModel.ListSortDirection.Ascending);
            }
        }

        //恢复默认
        private void EHRecoverDefault(object sender, EventArgs e)
        {
            //恢复默认
            m_itemOnlineFirst.Checked = false;
            m_itemOfflineFirst.Checked = false;
            m_itemRecoverDefault.Checked = true;
            if (this.Columns.Count > 0)
            {
                // 排序
                this.Sort(this.Columns[CS_UserId], System.ComponentModel.ListSortDirection.Ascending);
                //this.Sort(this.Columns[2], System.ComponentModel.ListSortDirection.Ascending);
                //DataRow[] rows = base.m_dataTable.Select(string.Format("{0} LIKE '*'", CS_StationId), string.Format("Convert({0},'System.Int32') ASC", CS_StationId));
            }
        }

        /// <summary>
        /// 根据分中心站点来加载测站,如果为空，或者NULL,则加载所有分中心
        /// </summary>
        public void SetSubCenterName(string subcenterName)
        {
            try
            {
                if (subcenterName == null || subcenterName.Equals(""))
                {
                    // 加载所有的用户分中心
                    // List<CEntityStation> listStation = CDBDataMgr.Instance.GetAllStation();
                    // 根据分中心查找测站
                    List<CEntityStation> listAllStation = CDBDataMgr.Instance.GetAllStation();
                    List<CEntitySoilStation> listAllSoilStation = CDBSoilDataMgr.Instance.GetAllSoilStation();
                    this.SuspendLayout();//暂停VIEW的刷新（datagridview的方法
                    this.Hide();
                    m_dataTable_1.Clear();
                    m_mutexDataTable.WaitOne();
                    RefreshGPRSInfo(listAllStation);
                    RefreshGPRSInfoSoil(listAllSoilStation);
                    this.Show();
                    m_mutexDataTable.ReleaseMutex();
                    this.ResumeLayout();
                }
                else
                {
                    // 根据分中心查找测站
                    List<CEntityStation> listAllStation = CDBDataMgr.Instance.GetAllStation();
                    List<CEntitySoilStation> listAllSoilStation = CDBSoilDataMgr.Instance.GetAllSoilStation();
                    CEntitySubCenter subcenter = CDBDataMgr.Instance.GetSubCenterByName(subcenterName);
                    if (null != subcenter)
                    {
                        // 如果不为空
                        List<CEntityStation> listUseStation = new List<CEntityStation>();
                        List<CEntitySoilStation> listUseStation_1 = new List<CEntitySoilStation>();
                        for (int i = 0; i < listAllStation.Count; ++i)
                        {
                            if (listAllStation[i].SubCenterID == subcenter.SubCenterID)
                            {
                                listUseStation.Add(listAllStation[i]);
                            }
                        }
                        for (int i = 0; i < listAllSoilStation.Count; ++i)
                        {
                            if (listAllSoilStation[i].SubCenterID == subcenter.SubCenterID)
                            {
                                listUseStation_1.Add(listAllSoilStation[i]);
                            }
                        }
                        this.SuspendLayout();//暂停VIEW的刷新（datagridview的方法
                        this.Hide();
                        this.ClearAllRows();
                        m_dataTable_1.Clear();
                        m_mutexDataTable.WaitOne();
                        RefreshGPRSInfo(listUseStation);
                        RefreshGPRSInfoSoil(listUseStation_1);
                        this.Show();
                        m_mutexDataTable.ReleaseMutex();
                        this.ResumeLayout();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Error");
                    }
                }
                this.UpdateDataToUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show("分中心切换有误!" + ex.ToString());
            }
        }



    }

    public static class MySort
    {
        public static void Sort(this DataGridView dgv, Comparison<DataGridViewRow> comparison)
        {
            dgv.Sort(new RowCompare(comparison));
        }
        public class RowCompare : IComparer
        {
            Comparison<DataGridViewRow> comparison;
            public RowCompare(Comparison<DataGridViewRow> comparison)
            {
                this.comparison = comparison;
            }

            #region IComparer 成员

            public int Compare(object x, object y)
            {
                return comparison((DataGridViewRow)x, (DataGridViewRow)y);
            }

            #endregion
        }
    }
}

