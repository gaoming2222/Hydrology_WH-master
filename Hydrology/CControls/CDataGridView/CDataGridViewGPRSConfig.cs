using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Protocol.Manager;
using Hydrology.DataMgr;
using Protocol.Channel.Interface;
using System.Windows.Forms;
using Hydrology.Forms;

namespace Hydrology.CControls
{
    /// <summary>
    /// GPRS配置页面
    /// </summary>
    class CDataGridViewGPRSConfig : CExDataGridView
    {
        #region 静态常量

        private static readonly string CS_PortNumber = "端口号";
        private static readonly string CS_Delete = "删除";
        private static readonly string CS_ProtocolChannel = "通讯方式";
        private static readonly string CS_ProtocolData = "数据协议";

        //private static readonly string 

        private static readonly string CS_UDPorTCP = "udp或tcp";
        private static readonly string CS_StartType = "启动方式";
        private static readonly string CS_CurrentStatus = "当前状态";
        private static readonly string CS_None = "无";

        private static readonly string CS_UDPorTCP_UDP = "UDP";
        private static readonly string CS_UDPorTCP_TCP = "TCP";

        private static readonly string CS_StartType_Auto = "自动";
        private static readonly string CS_StartType_Manaual = "手动";

        private static readonly string CS_Status_Enabled = "启用";
        private static readonly string CS_Status_Disable = "关闭";


        #endregion ///<静态常量

        #region 成员变量
        /// <summary>
        /// 所有数据协议列表
        /// </summary>
        private List<string> m_listProtocolData;

        /// <summary>
        /// GPRS协议列表
        /// </summary>
        private List<string> m_listProtocolGprs;

        private List<string> m_listProtocolTransparen;

        /// <summary>
        /// 端口协议配置，也用来保存更新后的提交数据
        /// </summary>
        private List<CPortProtocolConfig> m_listPortConfig;

        /// <summary>
        /// 是否修改过的标记
        /// </summary>
        private bool m_bHasModified;

        #endregion 成员变量

        public CDataGridViewGPRSConfig()
            : base()
        {
            m_bHasModified = false;
            InitDataSource();
        }

        /// <summary>
        /// 外部调用初始化
        /// </summary>
        public bool Init()
        {
            // 判断是否有信道协议
            if (m_listProtocolTransparen.Count <= 0 || m_listProtocolTransparen.Count <= 0)
            {
                //MessageBox.Show("请先配置GPRS通讯方式和数据协议");
                return false;
            }
            // 初始化
            InitUI();
            return true;
        }

        /// <summary>
        /// 初始化数据源，读取XML文件自动赋值
        /// </summary>
        public void InitDataSource()
        {
            m_listProtocolData = XmlDocManager.Instance.DataProtocolNames;
            m_listProtocolGprs = XmlDocManager.Instance.GPRSProtocolNames;
            m_listProtocolTransparen = XmlDocManager.Instance.NoneProtocolNames;
            // 当前的GPRS协议配置
            m_listPortConfig = XmlDocManager.Instance.GetComOrPortConfig(false);
            // 重置是否开启状态字段
            List<IGprs> listGprs = CPortDataMgr.Instance.GprsLists;
            List<ITransparen> listTransparen = CPortDataMgr.Instance.TransparenLists;

            List<int> listPortStarted = new List<int>();
            foreach (IGprs gprs in listGprs)
            {
                if (gprs.GetStarted())
                {
                    // 正在监听端口
                    
                    listPortStarted.Add(gprs.GetCurrentPort());
                }
            }
            foreach (ITransparen transparen in listTransparen)
            {
                if (transparen.GetStarted())
                {
                    // 正在监听端口

                    listPortStarted.Add(transparen.GetCurrentPort());
                }
            }
            for (int i = 0; i < m_listPortConfig.Count; ++i)
            {
                if (listPortStarted.Contains(m_listPortConfig[i].PortNumber))
                {
                    // 如果包含了，则表示已经启动了
                    m_listPortConfig[i].BStartOrNot = true;
                }
                else
                {
                    m_listPortConfig[i].BStartOrNot = false;
                }
            }
            
        }

        public void DoDelete()
        {
            if (this.IsCurrentCellInEditMode)
            {
                //MessageBox.Show("请完成当前的编辑");
                this.EndEdit();
                //return false;
            }

            for (int i = 0; i < m_listMaskedDeletedRows.Count; ++i)
            {
                base.DeleteRowData(m_listMaskedDeletedRows[i]);
            }
            m_listMaskedDeletedRows.Clear(); //清空
            base.UpdateDataToUI();
            m_bHasModified = true; //更改状态为已修改
        }

        public override bool DoSave()
        {
            if (this.IsCurrentCellInEditMode)
            {
                //MessageBox.Show("请完成当前的编辑");
                this.EndEdit();
                //return false;
            }
            // 无论是否修改都重新保存
            if (((m_listEditedRows.Count <= 0) && ((m_listMaskedDeletedRows.Count <= 0)) && (!m_bHasModified)) || (!(IsDataChanged())))
            {
                MessageBox.Show("没有任何修改，无需保存");
                return true;
            }
            if (!AssertSaveData())
            {
                // 有非法数据 
                return false;
            }
            // 生成ProtocolConfig
            GeneratePortConfig();
            // 应该作为一个事物一起处理
            //return base.DoSave();
            bool result = XmlDocManager.Instance.ResetComOrPortConfig(m_listPortConfig, false);
            if (result)
            {
                //CMessageBox box = new CMessageBox();
                //box.MessageInfo = "正在更新端口";
                //box.ShowDialog();
                // 通知其他界面更新消息
                CPortDataMgr.Instance.StartGprs(false);
                CPortDataMgr.Instance.StartTransparen(false);
                //box.CloseDialog();
                MessageBox.Show("保存成功");
            }
            else
            {
                MessageBox.Show("保存失败");
            }
            // 重新加载
            ClearAllState();
            InitDataSource();

            InitUI(); //重新加载界面
            return true;
        }

        public void SetEditMode(bool bEnable)
        {
            if (bEnable)
            {
                // 设定标题栏,默认有个隐藏列
                this.Header = new string[] 
                { 
                    CS_Delete, 
                    CS_PortNumber,
                    CS_ProtocolChannel,
                    CS_UDPorTCP,
                    CS_ProtocolData,
                    CS_StartType,
                    CS_CurrentStatus
                };
                //this.HideColomns = new int[] { 7, 8, 9, 10, 11 };
                //  添加删除列
                var delCol = new DataGridViewCheckBoxColumn();
                base.SetColumnEditStyle(0, delCol);

                //  添加端口号
                DataGridViewNumericUpDownColumn portNumberCol = new DataGridViewNumericUpDownColumn();
                portNumberCol.Minimum = 1000;
                portNumberCol.Maximum = 65535;
                base.SetColumnEditStyle(1, portNumberCol);

                //  添加通讯方式
                var ChannelCol = new DataGridViewComboBoxColumn();
                ChannelCol.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                var channelNames = m_listProtocolGprs;
                ChannelCol.Items.Add(CS_None);
                foreach (var item in channelNames)
                {
                    ChannelCol.Items.Add(item);
                }
                foreach (var item in m_listProtocolTransparen)
                {
                    ChannelCol.Items.Add(item);
                }

                base.SetColumnEditStyle(2, ChannelCol);

                //  添加UDP/TCP协议选择
                var udpOrTcpCol = new DataGridViewComboBoxColumn();
                udpOrTcpCol.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                udpOrTcpCol.Items.AddRange(new object[] { CS_UDPorTCP_UDP, CS_UDPorTCP_TCP });
                base.SetColumnEditStyle(3, udpOrTcpCol);

                //  添加数据协议
                var dataCol = new DataGridViewComboBoxColumn();
                dataCol.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                var dataNames = m_listProtocolData;
                dataCol.Items.Add(CS_None);
                foreach (var item in dataNames)
                {
                    dataCol.Items.Add(item);
                }
                base.SetColumnEditStyle(4, dataCol);

                //启动方式
                var startedCol = new DataGridViewComboBoxColumn();
                startedCol.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                startedCol.Items.AddRange(new object[] {CS_StartType_Auto, CS_StartType_Manaual  });
                base.SetColumnEditStyle(5, startedCol);

                //当前状态
                var statusCol = new DataGridViewComboBoxColumn();
                statusCol.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                statusCol.Items.AddRange(new object[] { CS_Status_Enabled, CS_Status_Disable });
                base.SetColumnEditStyle(6, statusCol);

                this.Columns[0].Width = 20; //删除列
            }
        }

        public void AddNewPort()
        {
            base.AddRow(new string[] 
                {
                    "False",
                    "",
                    CS_None,
                    CS_UDPorTCP_TCP,
                    CS_None,
                    CS_StartType_Auto,
                    CS_Status_Disable
                }, EDataState.ENormal, false);
            base.m_listEditedRows.Add(m_dataTable.Rows.Count - 1);
            base.ClearSelection(); //????
            base.UpdateDataToUI();
            m_bHasModified = true; //设置修改模式
        }

        public bool Close()
        {
            if (this.IsCurrentCellInEditMode)
            {
                //MessageBox.Show("请完成当前的编辑");
                this.EndEdit();
                //return false;
            }
            if (!AssertSaveData())
            {
                // 不让退出
                DialogResult result = MessageBox.Show("是否强行退出？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (DialogResult.Yes == result)
                {
                    return true;
                }
                return false;
            }
            // 关闭方法
            if (((m_listEditedRows.Count > 0) || (m_listMaskedDeletedRows.Count > 0) || (m_bHasModified)) && IsDataChanged())
            {
                DialogResult result = MessageBox.Show("当前所做修改尚未保存，是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (DialogResult.Cancel == result)
                {
                    return false;
                }
                else if (DialogResult.Yes == result)
                {
                    // 保存当前修改
                    DoSave();
                }
                else if (DialogResult.No == result)
                {
                    return true;
                }
            }
            return true;
        }

        public void Revert()
        {
            // 清空所有状态
            this.ClearAllState();
            InitDataSource();
            InitUI();
            m_bHasModified = false;
        }

        /// <summary>
        /// 生成m_listPortConfig,并清空状态m_listEditedRows， m_listMaskedDeletedRows 
        /// </summary>
        private void GeneratePortConfig()
        {
            // 标记为删除的就不需要添加到结果集中了
            m_listPortConfig = new List<CPortProtocolConfig>();
            // 获取修改了的所有数据
            for (int i = 0; i < m_dataTable.Rows.Count; ++i)
            {
                if (!m_listMaskedDeletedRows.Contains(i))
                {
                    // 不包含才需要判断
                    CPortProtocolConfig config = new CPortProtocolConfig();
                    config.PortNumber = int.Parse(m_dataTable.Rows[i][CS_PortNumber].ToString());
                    config.ProtocolChannelName = m_dataTable.Rows[i][CS_ProtocolChannel].ToString();
                    //if (CS_UDPorTCP_UDP == m_dataTable.Rows[i][CS_UDPorTCP].ToString())
                    //{
                    //    // 自动启动
                    //    config. = true;
                    //}
                    //else
                    //{
                    //    config.BAutoStart = false;
                    //}
                    config.ProtocolDataName = m_dataTable.Rows[i][CS_ProtocolData].ToString();
                    if (config.ProtocolDataName == CS_None)
                    {
                        config.ProtocolDataName = ""; //空数据协议
                    }

                    if (CS_StartType_Auto == m_dataTable.Rows[i][CS_StartType].ToString())
                    {
                        // 自动启动
                        config.BAutoStart = true;
                    }
                    else
                    {
                        config.BAutoStart = false;
                    }
                    if (CS_Status_Enabled == m_dataTable.Rows[i][CS_CurrentStatus].ToString())
                    {
                        // 启动状态
                        config.BStartOrNot = true;
                    }
                    else
                    {
                        config.BStartOrNot = false;
                    }
                    m_listPortConfig.Add(config);
                }// end of if 
            }
            m_listEditedRows.Clear();
            m_listMaskedDeletedRows.Clear();
        }


        private bool AssertSaveData()
        {
            // 判断当前数据是否合法
            List<int> listEditRows = new List<int>();
            foreach (int item in base.m_listEditedRows)
            {
                if (!m_listMaskedDeletedRows.Contains(item))
                {
                    listEditRows.Add(item);
                }
            }
            // 将去重后的项赋给编辑项
            base.m_listEditedRows = listEditRows;

            // 判断串口号不能为空,以及不能重复
            for (int i = 0; i < listEditRows.Count; ++i)
            {
                // 没有删除，并且不是新增的

                if (base.m_dataTable.Rows[listEditRows[i]][CS_PortNumber].Equals(""))
                {
                    MessageBox.Show("端口号不能为空");
                    return false;
                }
                int port = int.Parse(base.m_dataTable.Rows[listEditRows[i]][CS_PortNumber].ToString());
                //if (portnumber.Contains(port))
                //{
                //    MessageBox.Show("串口号不能重复");
                //    return false;
                //}
                //portnumber.Add(port);
            }
            // 判断端口号是否重复，以及信道协议是否为空
            List<int> allPortNumber = new List<int>();
            for (int i = 0; i < m_dataTable.Rows.Count; ++i)
            {
                if (!m_listMaskedDeletedRows.Contains(i))
                {
                    allPortNumber.Add(int.Parse(m_dataTable.Rows[i][CS_PortNumber].ToString()));
                    if (CS_None == m_dataTable.Rows[i][CS_ProtocolChannel].ToString())
                    {
                        MessageBox.Show("通讯方式不能为空");
                        return false;
                    }
                }

            }

            for (int i = 0; i < m_dataTable.Rows.Count; ++i)
            {
                int port = int.Parse(m_dataTable.Rows[i][CS_PortNumber].ToString());
                if (!m_listMaskedDeletedRows.Contains(i))
                {
                    // 不包含才需要判断
                    if (allPortNumber.IndexOf(port) !=
                    allPortNumber.LastIndexOf(port))
                    {
                        MessageBox.Show(string.Format("端口号\"{0}\"不能重复", port));
                        return false;
                    }
                }

            }
            return true;
        }

        #region 重写

        // 重写Cell值改变事件
        protected override void EHValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (base.m_arrayStrHeader[e.ColumnIndex] == CS_Delete)
            {
                // 删除列
                if (base.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Equals("True"))
                {
                    // 删除项
                    base.MarkRowDeletedOrNot(e.RowIndex, true);
                }
                else
                {
                    base.MarkRowDeletedOrNot(e.RowIndex, false);
                }
            }
            else
            {
                // 非删除列
                base.EHValueChanged(sender, e);
            }
            base.UpdateDataToUI();
        }

        // 重写双击事件
        protected override void OnCellMouseDoubleClick(DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }
            if (base.m_listMaskedDeletedRows.Contains(e.RowIndex))
            {
                if (base.m_arrayStrHeader[e.ColumnIndex] == CS_Delete)
                {
                    //开启编辑
                    base.OnCellMouseDoubleClick(e);
                }
                else
                {
                    //不编辑
                }
            }
            else
            {
                // 自动启动方式与手动启动方式，只是针对下一次系统加载时候的说法
                //if (base.m_arrayStrHeader[e.ColumnIndex] == CS_CurrentStatus)
                //{
                //    // 点击了当前状态列，判断是否可修改，仅当是自动启动状态才能启用编辑
                //    if (base.m_dataTable.Rows[e.RowIndex][CS_StartType].ToString() == CS_StartType_Manaual)
                //    {
                //        // 开启编辑
                //        base.OnCellMouseDoubleClick(e);
                //    }
                //    else
                //    {
                //        // 不开启编辑
                //    }
                //}
                //else
                //{
                // 开启编辑
                base.OnCellMouseDoubleClick(e);
                //}
            }
        }

        // 单击事件
        protected override void OnCellClick(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }
            if (base.m_listMaskedDeletedRows.Contains(e.RowIndex))
            {
                if (base.m_arrayStrHeader[e.ColumnIndex] == CS_Delete)
                {
                    //开启编辑
                    base.OnCellClick(e);
                }
                else
                {
                    //不编辑
                }
            }
            else
            {
                if (base.m_arrayStrHeader[e.ColumnIndex] == CS_CurrentStatus)
                {
                    // 点击了当前状态列，判断是否可修改，仅当是自动启动状态才能启用编辑
                    if (base.m_dataTable.Rows[e.RowIndex][CS_StartType].ToString() == CS_StartType_Manaual)
                    {
                        // 开启编辑
                        base.OnCellClick(e);
                    }
                    else
                    {
                        // 不开启编辑
                    }
                }
                else
                {
                    // 开启编辑
                    base.OnCellClick(e);
                }
            }
        }

        // 清空所有数据，恢复到刚加载的状态
        protected override void ClearAllState()
        {
            base.ClearAllState();
            m_bHasModified = false;
        }

        /// <summary>
        /// 初始化UI,界面显示
        /// </summary>
        protected void InitUI()
        {
            base.ClearAllRows();
            List<string[]> listShowArray = new List<string[]>();
            List<EDataState> listState = new List<EDataState>();
            for (int i = 0; i < m_listPortConfig.Count; ++i)
            {
                List<string> listShow = new List<string>();
                listShow.Add("False");
                listShow.Add(m_listPortConfig[i].PortNumber.ToString());

                if (null != m_listPortConfig[i].ProtocolChannelName)
                {
                    listShow.Add(m_listPortConfig[i].ProtocolChannelName);
                }
                else
                {
                    listShow.Add(CS_None);
                    CPortDataMgr.Instance.StopGprs();
                }

                //if (null != m_listPortConfig[i].BTcpOrUdp)
                //{
                //    listShow.Add(m_listPortConfig[i].BTcpOrUdp);
                //}
                //else
                //{
                //    listShow.Add(CS_UDPorTCP_TCP);
                //}
                listShow.Add(CS_UDPorTCP_TCP);
                if (null != m_listPortConfig[i].ProtocolDataName)
                {
                    listShow.Add(m_listPortConfig[i].ProtocolDataName);
                }
                else
                {
                    listShow.Add(CS_None);
                    CPortDataMgr.Instance.StopGprs();
                }

                if (m_listPortConfig[i].BAutoStart.HasValue)
                {
                    // 如果有值的话
                    listShow.Add((m_listPortConfig[i].BAutoStart.Value ? CS_StartType_Auto : CS_StartType_Manaual));
                }
                else
                {
                    // 如果没有值，默认显示为自动启动,防止崩溃，其实应该是一定要有一个信道协议的
                    listShow.Add(CS_StartType_Auto);
                }

                listShow.Add((m_listPortConfig[i].BStartOrNot.Value ? CS_Status_Enabled : CS_Status_Disable));
                listShowArray.Add(listShow.ToArray());
                listState.Add(EDataState.ENormal);

            }
            base.AddRowRange(listShowArray, listState);
            base.UpdateDataToUI();
        }

        #endregion  ///<重写

    }
}
