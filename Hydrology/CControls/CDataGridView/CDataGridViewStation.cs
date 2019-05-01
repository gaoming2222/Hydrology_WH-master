using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Hydrology.Entity;
using Hydrology.DBManager.Interface;
using Hydrology.DataMgr;
using System.Windows.Forms;
using Hydrology.Forms;
using Protocol.Channel.Interface;
using Protocol.Manager;

namespace Hydrology.CControls
{
    class CDataGridViewStation : CExDataGridView
    {
        #region 事件定义
        public event EventHandler<CEventSingleArgs<int>> E_StationCountChanged;
        #endregion 事件定义

        #region 静态常量
        public static readonly string CS_Delete = "删除";
        public static readonly string CS_StationID = "站号";
        public static readonly string CS_StationName = "站名";
        public static readonly string CS_SubCenterID = "分中心";
        public static readonly string CS_Stationtype = "站类";
        public static readonly string CS_WBase = "水位基值";
        public static readonly string CS_WMax = "水位上限";
        public static readonly string CS_WMin = "水位下限";
        public static readonly string CS_Change = "水位阀值";
        public static readonly string CS_RAccuracy = "雨量精度";
        public static readonly string CS_RChange = "雨量阀值";
        public static readonly string CS_Gsm = "GSM号码";
        public static readonly string CS_Gprs = "GPRS号码";
        public static readonly string CS_BDsatellite = "设备密码";
        public static readonly string CS_BDmember = "中心站地址";
        public static readonly string CS_M30 = "电压阀值";
        public static readonly string CS_Maintran = "主信道";
        public static readonly string CS_Subtran = "备信道";
        public static readonly string CS_Dataprotocol = "数据协议";
        public static readonly string CS_Watersensor = "水位传感器";
        public static readonly string CS_Rainsensor = "雨量传感器";
        public static readonly string CS_Reportinterval = "报汛段次";
        public static readonly string CS_PreStationId_None = "---";

#pragma warning disable CS0414 // 字段“CDataGridViewStation.CS_None”已被赋值，但从未使用过它的值
        private static readonly string CS_None = "无";
#pragma warning restore CS0414 // 字段“CDataGridViewStation.CS_None”已被赋值，但从未使用过它的值
        #endregion 静态常量

        #region 成员变量
        public IStationProxy m_proxyStation;   // 数据库连接的代理
        public ISoilStationProxy m_proxySoilStation;
        private List<CEntityStation> m_listUpdatedStation;    // 更新水情站的集合
        private List<string> m_listDeletedStation;                       // 删除水情站的集合
        public List<CEntityStation> m_listAddedStation;      // 添加水情站的集合

        List<CEntityStation> m_listStation;             //数据库中站点列表
        List<CEntitySubCenter> m_listSubCenter;

        ///// <summary>
        ///// 所有数据协议列表
        ///// </summary>
        //private List<string> m_listProtocolData;

        ///// <summary>
        ///// GPRS协议列表
        ///// </summary>
        //private List<string> m_listProtocolGprs;

        /// <summary>
        /// 所有数据协议列表
        /// </summary>
        private List<string> m_listProtocolData;
        private XmlDllCollections m_dllCollections;

#pragma warning disable CS0169 // 从不使用字段“CDataGridViewStation.m_listPortConfig”
        /// <summary>
        /// 端口协议配置，也用来保存更新后的提交数据
        /// </summary>
        private List<CPortProtocolConfig> m_listPortConfig;
#pragma warning restore CS0169 // 从不使用字段“CDataGridViewStation.m_listPortConfig”


        #endregion ///<成员变量

        #region 公共方法
        public CDataGridViewStation()
            : base()
        {
            // 关闭分页
            base.BPartionPageEnable = false;
            m_listAddedStation = new List<CEntityStation>();
            m_listDeletedStation = new List<string>();
            m_listUpdatedStation = new List<CEntityStation>();
            SetStyle(ControlStyles.DoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }

        /// <summary>
        /// 初始化数据源，连接数据库
        /// </summary>
        /// <param name="proxy"></param>
        public void InitDataSource()
        {
            m_proxyStation = CDBDataMgr.GetInstance().GetStationProxy();
            m_proxySoilStation = CDBSoilDataMgr.GetInstance().GetSoilStationProxy();
            m_listStation = CDBDataMgr.GetInstance().GetAllStation();
            m_listSubCenter = CDBDataMgr.GetInstance().GetAllSubCenter();

            m_listProtocolData = XmlDocManager.Instance.DataProtocolNames;
            //m_listProtocolGprs = XmlDocManager.Instance.GPRSProtocolNames;
            //// 当前的GPRS协议配置
            //m_listPortConfig = XmlDocManager.Instance.GetComOrPortConfig(false);
            // 判断是否有信道协议
            //if (m_listProtocolGprs.Count <= 0 || m_listProtocolData.Count <= 0)
            if (m_listProtocolData.Count <= 0)
            {
                MessageBox.Show("请先配置数据协议");
                //  return false;
            }
        }
        /// <summary>
        /// 将数据标记为删除，从集合中删除当前行
        /// </summary>
        public void DoDelete()
        {
            if (this.IsCurrentCellInEditMode)
            {
                //MessageBox.Show("请完成当前的编辑");
                this.EndEdit();
                //return false;
            }
            //GetUpdatedData();
            // 获取数据
            for (int i = 0; i < base.m_listMaskedDeletedRows.Count; ++i)
            {
                // -1表示新添加的用户
                string strPreStationId = base.Rows[m_listMaskedDeletedRows[i]].Cells[CS_StationID].Value.ToString();

                // 并且从编辑项中减去这一行
                m_listEditedRows.Remove(m_listMaskedDeletedRows[i]);

                //加入到删除站list
                m_listDeletedStation.Add(strPreStationId);

            }
            // 将某些行标记为不可见
            for (int i = 0; i < base.m_listMaskedDeletedRows.Count; ++i)
            {
                //base.Rows[i].Visible = false;
                base.DeleteRowData(m_listMaskedDeletedRows[i]);
            }

            m_listMaskedDeletedRows.Clear(); //清空

            base.UpdateDataToUI();

            UpdateStationCount(base.m_dataTable.Rows.Count);
        }

        /// <summary>
        /// 关闭事件，是否取消关闭
        /// </summary>
        /// <returns>false的话，表示取消关闭</returns>
        public bool Close()
        {
            if (this.IsCurrentCellInEditMode)
            {
                //MessageBox.Show("请完成当前的编辑");
                this.EndEdit();
                //return false;
            }
            // 关闭方法
            if ((base.m_listEditedRows.Count > 0 || m_listUpdatedStation.Count > 0 || base.m_listMaskedDeletedRows.Count > 0) && (base.IsDataChanged()))
            {
                try
                {
                    this.Hide();
                    DialogResult result = MessageBox.Show("当前所做修改尚未保存，是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (DialogResult.Cancel == result)
                    {
                        return false;
                    }
                    else if (DialogResult.Yes == result)
                    {
                        // 保存当前修改
                        return DoSave();
                    }
                    else if (DialogResult.No == result)
                    {
                        return true;
                    }
                }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
                catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
                {
                    //MessageBox.Show(e.ToString());
                    //this.Hide();
                    //MessageBox.Show("操作");
                    //this.Show();
                    return false;
                }
            }
            return true;
        }

        public void Revert()
        {
            try
            {
                // 清空所有状态
                this.ClearAllState();
                this.Hide();
                this.LoadData_1();
                this.Show();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public bool LoadData()
        {
            try
            {
                if (m_proxyStation != null)
                {
                    this.SetStation(CDBDataMgr.Instance.GetAllStation());
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
        }

        public bool LoadData_1()
        {
            try
            {
                if (m_proxyStation != null)
                {
                    this.SetStation(CDBDataMgr.Instance.GetAllStationData());
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
        }

        /// <summary>
        /// 获取更新过的数据,包括增加的用户记录
        /// </summary>
        public void GetUpdatedData()
        {
            try
            {
                // 标记为删除的就不需要添加的修改或者添加的分中心中了
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

                for (int i = 0; i < base.m_listEditedRows.Count; ++i)
                {
                    CEntityStation Station = new CEntityStation();
                    Station.StationID = base.Rows[m_listEditedRows[i]].Cells[CS_StationID].Value.ToString();
                    Station.SubCenterID = CDBDataMgr.Instance.GetSubCenterByName(base.Rows[m_listEditedRows[i]].Cells[CS_SubCenterID].Value.ToString()).SubCenterID;
                    Station.StationName = base.Rows[m_listEditedRows[i]].Cells[CS_StationName].Value.ToString();
                    Station.StationType = CEnumHelper.UIStrToStationType(base.Rows[m_listEditedRows[i]].Cells[CS_Stationtype].Value.ToString());
                    if (Station.StationType == EStationType.ERainFall)
                    {
                        Station.DWaterBase = null;
                        Station.DWaterMax = null;
                        Station.DWaterMin = null;
                        Station.DWaterChange = null;
                        try
                        {
                            Station.DRainAccuracy = float.Parse(base.Rows[m_listEditedRows[i]].Cells[CS_RAccuracy].Value.ToString());
                        }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
                        catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
                        {
                            Station.DRainAccuracy = 0.5f;
                        }
                        Station.DRainChange = CStringFromatHelper.ConvertToNullableDecimal(base.Rows[m_listEditedRows[i]].Cells[CS_RChange].Value.ToString());
                        Station.GSM = base.Rows[m_listEditedRows[i]].Cells[CS_Gsm].Value.ToString();
                        Station.GPRS = base.Rows[m_listEditedRows[i]].Cells[CS_Gprs].Value.ToString();
                        Station.BDSatellite = base.Rows[m_listEditedRows[i]].Cells[CS_BDsatellite].Value.ToString();
                        Station.BDMemberSatellite = base.Rows[m_listEditedRows[i]].Cells[CS_BDmember].Value.ToString();
                        Station.DVoltageMin = CStringFromatHelper.ConvertToNullableFloat(base.Rows[m_listEditedRows[i]].Cells[CS_M30].Value.ToString());
                        Station.Maintran = base.Rows[m_listEditedRows[i]].Cells[CS_Maintran].Value.ToString();
                        Station.Subtran = base.Rows[m_listEditedRows[i]].Cells[CS_Subtran].Value.ToString();
                        Station.Datapotocol = base.Rows[m_listEditedRows[i]].Cells[CS_Dataprotocol].Value.ToString();
                        Station.Rainsensor = base.Rows[m_listEditedRows[i]].Cells[CS_Rainsensor].Value.ToString();
                        Station.Watersensor = base.Rows[m_listEditedRows[i]].Cells[CS_Watersensor].Value.ToString();
                        Station.Reportinterval = base.Rows[m_listEditedRows[i]].Cells[CS_Reportinterval].Value.ToString();
                    }
                    else if (Station.StationType == EStationType.ERiverWater)
                    {
                        Station.DWaterBase = CStringFromatHelper.ConvertToNullableDecimal(base.Rows[m_listEditedRows[i]].Cells[CS_WBase].Value.ToString());
                        Station.DWaterMax = CStringFromatHelper.ConvertToNullableDecimal(base.Rows[m_listEditedRows[i]].Cells[CS_WMax].Value.ToString());
                        Station.DWaterMin = CStringFromatHelper.ConvertToNullableDecimal(base.Rows[m_listEditedRows[i]].Cells[CS_WMin].Value.ToString());
                        Station.DWaterChange = CStringFromatHelper.ConvertToNullableDecimal(base.Rows[m_listEditedRows[i]].Cells[CS_Change].Value.ToString());
                        Station.DRainAccuracy = 2;
                        Station.DRainChange = CStringFromatHelper.ConvertToNullableDecimal(base.Rows[m_listEditedRows[i]].Cells[CS_RChange].Value.ToString());
                        Station.GSM = base.Rows[m_listEditedRows[i]].Cells[CS_Gsm].Value.ToString();
                        Station.GPRS = base.Rows[m_listEditedRows[i]].Cells[CS_Gprs].Value.ToString();
                        Station.BDSatellite = base.Rows[m_listEditedRows[i]].Cells[CS_BDsatellite].Value.ToString();
                        Station.BDMemberSatellite = base.Rows[m_listEditedRows[i]].Cells[CS_BDmember].Value.ToString();
                        Station.DVoltageMin = CStringFromatHelper.ConvertToNullableFloat(base.Rows[m_listEditedRows[i]].Cells[CS_M30].Value.ToString());
                        Station.Maintran = base.Rows[m_listEditedRows[i]].Cells[CS_Maintran].Value.ToString();
                        Station.Subtran = base.Rows[m_listEditedRows[i]].Cells[CS_Subtran].Value.ToString();
                        Station.Datapotocol = base.Rows[m_listEditedRows[i]].Cells[CS_Dataprotocol].Value.ToString();
                        Station.Rainsensor = base.Rows[m_listEditedRows[i]].Cells[CS_Rainsensor].Value.ToString();
                        Station.Watersensor = base.Rows[m_listEditedRows[i]].Cells[CS_Watersensor].Value.ToString();
                        Station.Reportinterval = base.Rows[m_listEditedRows[i]].Cells[CS_Reportinterval].Value.ToString();
                    }
                    else
                    {
                        Station.DWaterBase = CStringFromatHelper.ConvertToNullableDecimal(base.Rows[m_listEditedRows[i]].Cells[CS_WBase].Value.ToString());
                        Station.DWaterMax = CStringFromatHelper.ConvertToNullableDecimal(base.Rows[m_listEditedRows[i]].Cells[CS_WMax].Value.ToString());
                        Station.DWaterMin = CStringFromatHelper.ConvertToNullableDecimal(base.Rows[m_listEditedRows[i]].Cells[CS_WMin].Value.ToString());
                        Station.DWaterChange = CStringFromatHelper.ConvertToNullableDecimal(base.Rows[m_listEditedRows[i]].Cells[CS_Change].Value.ToString());
                        try
                        {
                            Station.DRainAccuracy = 1f;
                        }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
                        catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
                        {
                            Station.DRainAccuracy = 1f;
                        }
                        Station.DRainChange = CStringFromatHelper.ConvertToNullableDecimal(base.Rows[m_listEditedRows[i]].Cells[CS_RChange].Value.ToString());
                        Station.GSM = base.Rows[m_listEditedRows[i]].Cells[CS_Gsm].Value.ToString();
                        Station.GPRS = base.Rows[m_listEditedRows[i]].Cells[CS_Gprs].Value.ToString();
                        Station.BDSatellite = base.Rows[m_listEditedRows[i]].Cells[CS_BDsatellite].Value.ToString();
                        Station.BDMemberSatellite = base.Rows[m_listEditedRows[i]].Cells[CS_BDmember].Value.ToString();
                        Station.DVoltageMin = CStringFromatHelper.ConvertToNullableFloat(base.Rows[m_listEditedRows[i]].Cells[CS_M30].Value.ToString());
                        Station.Maintran = base.Rows[m_listEditedRows[i]].Cells[CS_Maintran].Value.ToString();
                        Station.Subtran = base.Rows[m_listEditedRows[i]].Cells[CS_Subtran].Value.ToString();
                        Station.Datapotocol = base.Rows[m_listEditedRows[i]].Cells[CS_Dataprotocol].Value.ToString();
                        if(base.Rows[m_listEditedRows[i]].Cells[CS_Rainsensor].Value.ToString() == "翻斗雨量"){
                            Station.Rainsensor = "0";
                        }
                        if (base.Rows[m_listEditedRows[i]].Cells[CS_Rainsensor].Value.ToString() == "雨雪量计")
                        {
                            Station.Rainsensor = "1";
                        }
                        //Station.Rainsensor = base.Rows[m_listEditedRows[i]].Cells[CS_Rainsensor].Value.ToString();
                        if (base.Rows[m_listEditedRows[i]].Cells[CS_Watersensor].Value.ToString() == "浮子水位")
                        {
                            Station.Watersensor = "0";
                        }
                        if (base.Rows[m_listEditedRows[i]].Cells[CS_Watersensor].Value.ToString() == "气泡水位")
                        {
                            Station.Watersensor = "1";
                        }
                        if (base.Rows[m_listEditedRows[i]].Cells[CS_Watersensor].Value.ToString() == "压阻水位")
                        {
                            Station.Watersensor = "2";
                        }
                        if (base.Rows[m_listEditedRows[i]].Cells[CS_Watersensor].Value.ToString() == "雷达水位")
                        {
                            Station.Watersensor = "3";
                        }
                        //Station.Watersensor = base.Rows[m_listEditedRows[i]].Cells[CS_Watersensor].Value.ToString();
                        Station.Reportinterval = base.Rows[m_listEditedRows[i]].Cells[CS_Reportinterval].Value.ToString();
                    }
                    m_listUpdatedStation.Add(Station);
                }
                m_listEditedRows.Clear();   //清空此次记录
            }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
            catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
            {
                //MessageBox.Show(e.ToString());
                this.Hide();
                MessageBox.Show("请在同一界面完成修改");
                this.Show();
            }
        }

        /// <summary>
        /// 是否开启编辑模式
        /// </summary>
        /// <param name="bEnable"></param>
        public void SetEditMode(bool bEnable)
        {
            try
            {
                if (bEnable)
                {
                    // 设定标题栏
                    this.Header = new string[]
                {
                      CS_Delete, CS_StationID,CS_StationName,CS_Stationtype,
                      CS_SubCenterID,CS_WBase,CS_WMax, CS_WMin,
                      CS_Change,CS_RAccuracy,CS_RChange,CS_Gsm,
                      CS_Gprs,CS_BDsatellite,CS_BDmember, CS_M30,
                      CS_Maintran,CS_Subtran, CS_Dataprotocol ,CS_Watersensor,
                      CS_Rainsensor,CS_Reportinterval
                };
                    this.HideColomns = new int[] { 18, 19, 20 };
                    List<string> listHeader = new List<string>(this.Header);
                    ////开启编辑模式,设置可编辑列
                    DataGridViewCheckBoxColumn deleteCol = new DataGridViewCheckBoxColumn();
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_Delete), deleteCol);

                    //// 设置站名可编辑
                    DataGridViewTextBoxColumn stationNumber = new DataGridViewTextBoxColumn();
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_StationName), stationNumber);

                    //// 设置站点类型下拉列表
                    var cmb_StationType = new DataGridViewComboBoxColumn();
                    cmb_StationType.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    //// 初始化站点类型
                    cmb_StationType.Items.Add(CEnumHelper.StationTypeToUIStr(EStationType.EHydrology));
                    cmb_StationType.Items.Add(CEnumHelper.StationTypeToUIStr(EStationType.EH));
                    cmb_StationType.Items.Add(CEnumHelper.StationTypeToUIStr(EStationType.GT));
                    cmb_StationType.Items.Add(CEnumHelper.StationTypeToUIStr(EStationType.RE));
                    cmb_StationType.Items.Add(CEnumHelper.StationTypeToUIStr(EStationType.RP));
                    cmb_StationType.Items.Add(CEnumHelper.StationTypeToUIStr(EStationType.ERiverWater));
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_Stationtype), cmb_StationType);


                    //所属分中心下拉列表
                    var subcenterId = new DataGridViewComboBoxColumn();
                    subcenterId.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                    List<CEntitySubCenter> listSubCenter = CDBDataMgr.GetInstance().GetAllSubCenter();
                    // 初始化分中心
                    foreach (CEntitySubCenter subcenter in listSubCenter)
                    {
                        subcenterId.Items.Add(subcenter.SubCenterName);
                    }
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_SubCenterID), subcenterId);


                    //// 设置水位基值,水位上限,水位下限，水位阀值的编辑列
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_WBase), GenerateColumnStype());
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_WMax), GenerateColumnStype());
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_WMin), GenerateColumnStype());
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_Change), GenerateColumnStype());

                    //// 设置雨量精度,雨量阀值,GSM号码的编辑列
                    //  base.SetColumnEditStyle(listHeader.IndexOf(CS_RAccuracy), GenerateColumnStype());
                    var cmb_RAccuracy = new DataGridViewComboBoxColumn();
                    cmb_RAccuracy.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                    cmb_RAccuracy.Items.Add("无");
                    cmb_RAccuracy.Items.Add("0.1");
                    cmb_RAccuracy.Items.Add("0.2");
                    cmb_RAccuracy.Items.Add("0.5");
                    cmb_RAccuracy.Items.Add("1");

                    base.SetColumnEditStyle(listHeader.IndexOf(CS_RAccuracy), cmb_RAccuracy);

                    base.SetColumnEditStyle(listHeader.IndexOf(CS_RChange), GenerateColumnStype());

                    DataGridViewTextBoxColumn gsmNumber = new DataGridViewTextBoxColumn();
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_Gsm), gsmNumber);

                    //// 设置GPRS号码,北斗卫星终端号,北斗卫星成员号,电压阀值的编辑列
                    DataGridViewTextBoxColumn gprsNumber = new DataGridViewTextBoxColumn();
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_Gprs), gprsNumber);

                    DataGridViewTextBoxColumn beidouNumber = new DataGridViewTextBoxColumn();
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_BDsatellite), beidouNumber);

                    DataGridViewTextBoxColumn beidouMemberNumber = new DataGridViewTextBoxColumn();
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_BDmember), beidouMemberNumber);

                    DataGridViewTextBoxColumn CS_M30Number = new DataGridViewTextBoxColumn();
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_M30), GenerateColumnStype());

                    //主信道,备信道，通信协议
                    XmlDocManager.Instance.ReadFromXml();
                    m_dllCollections = Protocol.Manager.XmlDocManager.Instance.DllInfo;
                    var MaintranNumber = new DataGridViewComboBoxColumn();
                    var SubtranNumber = new DataGridViewComboBoxColumn();
                    MaintranNumber.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                    SubtranNumber.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                    foreach (XmlDllInfo info in m_dllCollections.Infos)
                    {
                        //  不显示已经被禁用的协议
                        if (!info.Enabled)
                            continue;
                        // 显示信道协议
                        if (info.Type == "channel")
                        {
                            MaintranNumber.Items.Add(info.Name);
                            SubtranNumber.Items.Add(info.Name);
                        }
                    }
                    SubtranNumber.Items.Add("无");
                    SubtranNumber.Items.Add("无");
                    MaintranNumber.Items.Add("未知通讯协议");
                    SubtranNumber.Items.Add("未知通讯协议");

                    base.SetColumnEditStyle(listHeader.IndexOf(CS_Maintran), MaintranNumber);
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_Subtran), SubtranNumber);

                    var DataprotocolNumber = new DataGridViewComboBoxColumn();
                    DataprotocolNumber.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                    m_listProtocolData = XmlDocManager.Instance.DataProtocolNames;
                    for (int i = 0; i < m_listProtocolData.Count; i++)
                    {
                        DataprotocolNumber.Items.Add(m_listProtocolData[i].ToString());
                    }
                    // DataprotocolNumber.Items.Add("LN");
                    DataprotocolNumber.Items.Add("未知数据协议");
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_Dataprotocol), DataprotocolNumber);


                    var WatersensorNumber = new DataGridViewComboBoxColumn();
                    WatersensorNumber.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                    WatersensorNumber.Items.Add("无");
                    WatersensorNumber.Items.Add("浮子水位");
                    WatersensorNumber.Items.Add("气泡水位");
                    WatersensorNumber.Items.Add("压阻水位");
                    WatersensorNumber.Items.Add("雷达水位");
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_Watersensor), WatersensorNumber);

                    var RainsensorNumber = new DataGridViewComboBoxColumn();
                    RainsensorNumber.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                    RainsensorNumber.Items.Add("无");
                    RainsensorNumber.Items.Add("翻斗雨量");
                    RainsensorNumber.Items.Add("雨雪量计");
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_Rainsensor), RainsensorNumber);

                    var ReportintervalNumber = new DataGridViewComboBoxColumn();
                    ReportintervalNumber.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                    ReportintervalNumber.Items.Add("1");
                    ReportintervalNumber.Items.Add("4");
                    ReportintervalNumber.Items.Add("8");
                    ReportintervalNumber.Items.Add("12");
                    ReportintervalNumber.Items.Add("24");
                    ReportintervalNumber.Items.Add("48");
                    base.SetColumnEditStyle(listHeader.IndexOf(CS_Reportinterval), ReportintervalNumber);
                }
                else
                {

                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }

        /// <summary>
        /// 获取删除过的数据
        /// </summary>
        private void GetDeletedData()
        {
            try
            {
                for (int i = 0; i < base.m_listMaskedDeletedRows.Count; ++i)
                {
                    // -1是新增加的用户
                    string strPreStationId = base.Rows[m_listMaskedDeletedRows[i]].Cells[CS_StationID].Value.ToString();

                    m_listDeletedStation.Add(strPreStationId);

                }
                m_listMaskedDeletedRows.Clear();   //清空此次记录
            }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
            catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
            {
                //MessageBox.Show(e.ToString());
                this.Hide();
                MessageBox.Show("请在同一界面完成修改");
                this.Show();
            }
        }

        /// <summary>
        /// 判断数据数据是否合法
        /// </summary>
        /// <returns></returns>
        private bool AssertInputData(List<CEntityStation> listStation)
        {
            foreach (CEntityStation station in listStation)
            {
                //  1.站名不能为空，不能超过50个字符
                if (station.StationName.Equals(""))
                {
                    this.Hide();
                    MessageBox.Show(station.StationID + "站名不能为空");
                    this.Show();
                    return false;
                }
                if (System.Text.Encoding.Default.GetByteCount(station.StationName) > 50)
                {
                    this.Hide();
                    MessageBox.Show(station.StationID + "站名不能超过50个字符");
                    this.Show();
                    return false;
                }
                // 1115 gm
                //判断GPRS是否重复
                //List<string> gprs = m_proxyStation.getAllGprs();
                //List<string> soilgprs = m_proxySoilStation.getAllGprs();
                //for (int i = 0; i < soilgprs.Count; i++)
                //{
                //    gprs.Add(soilgprs[i]);
                //}
                //if (station.GPRS != "" && gprs.Contains(station.GPRS))
                //{
                //    this.Hide();
                //    MessageBox.Show(station.StationID + "站点GPRS不能重复");
                //    this.Show();
                //    return false;
                //}
                //else
                //{
                //    gprs.Add(station.GPRS);
                //}
                //List<CEntityStation> gprsStation = m_proxyStation.getAllGprs_1();
                //List<CEntitySoilStation> gprsSoilStation = m_proxySoilStation.getAllGprs_1();
                List<CEntityStation> gprsStation = CDBDataMgr.Instance.GetAllStation();
                //List<CEntitySoilStation> gprsSoilStation = CDBSoilDataMgr.Instance.GetAllSoilStation();
                List<String> gprs = new List<string>();
                for (int i = 0; i < gprsStation.Count; i++)
                {
                    if (gprsStation[i].StationID != station.StationID)
                    {
                        gprs.Add(gprsStation[i].GPRS);
                    }
                }
                //for (int j = 0; j < gprsSoilStation.Count; j++)
                //{

                //    gprs.Add(gprsSoilStation[j].GPRS);

                //}
                if (station.GPRS != "" && gprs.Contains(station.GPRS))
                {
                    this.Hide();
                    MessageBox.Show(station.StationID + "站点GPRS不能重复！");
                    this.Show();
                    return false;
                }
                else
                {
                    gprs.Add(station.GPRS);
                }

                if (station.StationType == EStationType.ERainFall)
                {
                    // 2. 雨量精度不能为空，如果是水文站或者雨量站
                    if (station.DRainAccuracy.ToString() == "无")
                    {
                        this.Hide();
                        MessageBox.Show("雨量站 " + station.StationID + " 的雨量精度不能为无！");
                        this.Show();
                        return false;
                    }
                    // 3. 雨量变化合法
                    if (!station.DRainAccuracy.ToString().Equals(""))
                    {
                        try
                        {
                            if (Decimal.Parse(station.DRainAccuracy.ToString()) < 0)
                            {
                                this.Hide();
                                MessageBox.Show(station.StationID + "请输入正确的雨量变化值!");
                                this.Show();
                                return false;
                            }
                        }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (System.Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            this.Hide();
                            MessageBox.Show(station.StationID + "请输入正确的雨量变化值!");
                            this.Show();
                            return false;
                        }
                    }
                }
                else if (station.StationType == EStationType.ERiverWater)
                {
                    // 6. 水位基值合法
                    if (!station.DWaterBase.ToString().Equals(""))
                    {
                        try
                        {
                            this.Hide();
                            Decimal.Parse(station.DWaterBase.ToString());
                            this.Show();
                        }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (System.Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            this.Hide();
                            MessageBox.Show(station.StationID + "请输入正确的水位基值！");
                            this.Show();
                            return false;
                        }
                    }
                    // 7. 水位变化合法
                    if (!station.DWaterChange.ToString().Equals(""))
                    {
                        try
                        {
                            if (Decimal.Parse(station.DWaterChange.ToString()) < 0)
                            {
                                this.Hide();
                                MessageBox.Show(station.StationID + "请输入正确的水位变化值！");
                                this.Show();
                                return false;
                            }
                        }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (System.Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            this.Hide();
                            MessageBox.Show(station.StationID + "请输入正确的水位变化值！");
                            this.Show();
                            return false;
                        }
                    }
                    // 8. 水位最大值合法
                    if (!station.DWaterMax.ToString().Equals(""))
                    {
                        try
                        {
                            this.Hide();
                            Decimal.Parse(station.DWaterMax.ToString());
                            this.Show();
                        }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (System.Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            this.Hide();
                            MessageBox.Show(station.StationID + "请输入正确的水位最大值！");
                            this.Show();
                            return false;
                        }
                    }
                    // 9. 水位最小值
                    if (!station.DWaterMin.ToString().Equals(""))
                    {
                        try
                        {
                            this.Hide();
                            Decimal.Parse(station.DWaterMin.ToString());
                            this.Show();
                        }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (System.Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            this.Hide();
                            MessageBox.Show(station.StationID + "请输入正确的水位最小值!");
                            this.Show();
                            return false;
                        }
                    }
                }
                else if (station.StationType == EStationType.EHydrology  || station.StationType == EStationType.EH)
                {
                    if (station.DRainAccuracy.ToString() == "无")
                    {
                        this.Hide();
                        MessageBox.Show("水文站 " + station.StationID + " 的雨量精度不能为无！");
                        this.Show();
                        return false;
                    }
                    // 3. 雨量变化合法
                    if (!station.DRainAccuracy.ToString().Equals(""))
                    {
                        try
                        {
                            if (Decimal.Parse(station.DRainAccuracy.ToString()) < 0)
                            {
                                this.Hide();
                                MessageBox.Show(station.StationID + "请输入正确的雨量变化值!");
                                this.Show();
                                return false;
                            }
                        }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (System.Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            this.Hide();
                            MessageBox.Show(station.StationID + "请输入正确的雨量变化值!");
                            this.Show();
                            return false;
                        }
                    }
                    // 6. 水位基值合法
                    if (!station.DWaterBase.ToString().Equals(""))
                    {
                        try
                        {
                            this.Hide();
                            Decimal.Parse(station.DWaterBase.ToString());
                            this.Show();
                        }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (System.Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            this.Hide();
                            MessageBox.Show(station.StationID + "请输入正确的水位基值！");
                            this.Show();
                            return false;
                        }
                    }
                    // 7. 水位变化合法
                    if (!station.DWaterChange.ToString().Equals(""))
                    {
                        try
                        {
                            if (Decimal.Parse(station.DWaterChange.ToString()) < 0)
                            {
                                this.Hide();
                                MessageBox.Show(station.StationID + "请输入正确的水位变化值！");
                                this.Show();
                                return false;
                            }
                        }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (System.Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            this.Hide();
                            MessageBox.Show(station.StationID + "请输入正确的水位变化值！");
                            this.Show();
                            return false;
                        }
                    }
                    // 8. 水位最大值合法
                    if (!station.DWaterMax.ToString().Equals(""))
                    {
                        try
                        {
                            this.Hide();
                            Decimal.Parse(station.DWaterMax.ToString());
                            this.Show();
                        }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (System.Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            this.Hide();
                            MessageBox.Show(station.StationID + "请输入正确的水位最大值！");
                            this.Show();
                            return false;
                        }
                    }
                    // 9. 水位最小值
                    if (!station.DWaterMin.ToString().Equals(""))
                    {
                        try
                        {
                            this.Hide();
                            Decimal.Parse(station.DWaterMin.ToString());
                            this.Show();
                        }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (System.Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            this.Hide();
                            MessageBox.Show(station.StationID + "请输入正确的水位最小值!");
                            this.Show();
                            return false;
                        }
                    }
                }

                // 11. 电压下限合法
                if (!station.DVoltageMin.ToString().Equals(""))
                {
                    try
                    {

                        float.Parse(station.DVoltageMin.ToString());
                    }
                    catch (System.Exception)
                    {
                        this.Hide();
                        MessageBox.Show(station.StationID + "请输入正确的电压阀值");
                        this.Show();
                        return false;
                    }
                }
                if (station.Maintran.ToString() == "未知通讯协议")
                {
                    this.Hide();
                    MessageBox.Show(station.StationID + "请配置正确的主信道！");
                    this.Show();
                    return false;
                }
                if (station.Subtran.ToString() == "未知通讯协议")
                {
                    this.Hide();
                    MessageBox.Show(station.StationID + "请配置正确的备信道！");
                    this.Show();
                    return false;
                }
            }
            return true;
        }

        private DataGridViewNumericUpDownColumn GenerateColumnStype()
        {
            DataGridViewNumericUpDownColumn C10_4 = new DataGridViewNumericUpDownColumn()
            {
                Minimum = -9999999,
                Maximum = 9999999,
                // DecimalPlaces = 4, /*好像是设置小数点后面的位数*/
                DecimalPlaces = 1, /*好像是设置小数点后面的位数*/
                // Increment = (Decimal)0.0001 /*增量*/
                Increment = (Decimal)0.1 /*增量*/
            };
            return C10_4;
        }
        #endregion 公共方法

        #region 帮助方法
        /// <summary>
        /// 设置显示的数据
        /// </summary>
        /// <param name="listSoilStation"></param>
        public void SetStation(List<CEntityStation> listStation)
        {
            try
            {
                base.m_dataTable.Rows.Clear();
                XmlDocManager.Instance.ReadFromXml();
                m_dllCollections = Protocol.Manager.XmlDocManager.Instance.DllInfo;
                List<string> xmlDll = new List<string>();
                foreach (XmlDllInfo info in m_dllCollections.Infos)
                {
                    xmlDll.Add(info.Name);
                }
                // 将用户信息显示到表格上
                foreach (CEntityStation station in listStation)
                {
                    // var s = CDBDataMgr.Instance.GetStationById(station.StationID);
                    string subcerterName = "无";
                    string accuracy = "无";
                    int reportInterval = 24;
                    var s = station;
                    // 1103 gm
                    //限制DRainAccuracy的取值范围
                    if (s.DRainAccuracy.ToString().Trim() == "0.1" || s.DRainAccuracy.ToString().Trim() == "0.2" || s.DRainAccuracy.ToString().Trim() == "0.5" || s.DRainAccuracy.ToString().Trim() == "1")
                    {
                        accuracy = s.DRainAccuracy.ToString();
                    }
                    else
                    {
                        accuracy = "无";
                    }
                    //限制subcentername的取值范围
                    for (int i = 0; i < m_listSubCenter.Count; i++)
                    {
                        if (s.SubCenterID == m_listSubCenter[i].SubCenterID)
                        {
                            subcerterName = m_listSubCenter[i].SubCenterName;
                            break;
                        }
                    }
                    //限制报讯段次的取值范围
                    try
                    {
                        int report = int.Parse(s.Reportinterval);
                        if (report == 1 || report == 4 || report == 8 || report == 12 || report == 24 || report == 48)
                        {
                            reportInterval = report;
                        }
                        else
                        {
                            reportInterval = 24;
                        }
                    }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
                    catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
                    {
                        reportInterval = 24;
                    }
                    // 限制雨量传感器类型
                    int m = 5;
                    try
                    {
                        if (s.Rainsensor.ToString() != "")
                        {
                            m = int.Parse(s.Rainsensor);
                        }
                        // int n = int.Parse(s.Watersensor);
                    }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
                    catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
                    {
                        //s.Watersensor = "5";
                        // s.Rainsensor = "5";
                    }
                    // 限制水位传感器类型
                    int n = 5;
                    try
                    {
                        //int m = int.Parse(s.Rainsensor);
                        if (s.Watersensor.Trim() != "")
                        {
                            n = int.Parse(s.Watersensor.Trim());
                        }
                    }
#pragma warning disable CS0168 // 声明了变量“e”，但从未使用过
                    catch (Exception e)
#pragma warning restore CS0168 // 声明了变量“e”，但从未使用过
                    {
                        // s.Watersensor = "5";
                    }
                    //限制主信道的类型
                    if (!xmlDll.Contains(s.Maintran))
                    {
                        List<string> listHeader = new List<string>(this.Header);
                        s.Maintran = "未知通讯协议";
                    }
                    if (!xmlDll.Contains(s.Subtran) && s.Subtran != "无")
                    {
                        s.Subtran = "未知通讯协议";
                    }
                    if (s.StationType == EStationType.ERainFall)
                    {
                        
                            base.AddRow(new string[]
                          {
                        "False",s.StationID.ToString(), s.StationName.ToString(),CEnumHelper.StationTypeToUIStr(s.StationType),
                        //gm 1103
                        subcerterName,"","","",
                        "",accuracy,s.DRainChange.ToString(),s.GSM.ToString(),
                        s.GPRS.ToString(),s.BDSatellite.ToString(),s.BDMemberSatellite.ToString(),s.DVoltageMin.ToString(),
                        s.Maintran.ToString(),s.Subtran.ToString(),s.Datapotocol.ToString(),"无",
                        CEnumHelper.RainSensorTypeToUIStr(m),reportInterval.ToString()
                                          }, EDataState.ENormal);
                        
                    }
                    else if (s.StationType == EStationType.ERiverWater)
                    {
                        base.AddRow(new string[]
                    {
                        "False", s.StationID.ToString(), s.StationName.ToString(), CEnumHelper.StationTypeToUIStr(s.StationType),
                        subcerterName, s.DWaterBase.ToString(), s.DWaterMax.ToString(), s.DWaterMin.ToString(),
                        s.DWaterChange.ToString(), "无", "", s.GSM.ToString(),
                        s.GPRS.ToString(), s.BDSatellite.ToString(), s.BDMemberSatellite.ToString(), s.DVoltageMin.ToString(),
                        s.Maintran.ToString(), s.Subtran.ToString(), s.Datapotocol.ToString(), CEnumHelper.WaterSensorTypeToUIStr(n),
                        "无", s.Reportinterval.ToString()
                                        }, EDataState.ENormal);
                    }
                    else
                    {
                        base.AddRow(new string[]
                    {
                        "False",s.StationID.ToString(), s.StationName.ToString(),CEnumHelper.StationTypeToUIStr(s.StationType),
                        //gm 1103
                        subcerterName,s.DWaterBase.ToString(),s.DWaterMax.ToString(),s.DWaterMin.ToString(),
                        s.DWaterChange.ToString(),accuracy,s.DRainChange.ToString(),s.GSM.ToString(),
                        s.GPRS.ToString(),s.BDSatellite.ToString(),s.BDMemberSatellite.ToString(),s.DVoltageMin.ToString(),
                        s.Maintran.ToString(),s.Subtran.ToString(),s.Datapotocol.ToString(),CEnumHelper.WaterSensorTypeToUIStr(n),
                        CEnumHelper.RainSensorTypeToUIStr(m),reportInterval.ToString()
                    }, EDataState.ENormal);
                    }
                }
                UpdateDataToUI();
                UpdateStationCount(listStation.Count);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        /// <summary>
        /// 更新配置个数
        /// </summary>
        /// <param name="userCount"></param>
        private void UpdateStationCount(int userCount)
        {
            if (E_StationCountChanged != null)
            {
                E_StationCountChanged.Invoke(this, new CEventSingleArgs<int>(userCount));
            }
        }

        /// <summary>
        /// 根据分中心站点来加载测站,如果为空，或者NULL,则加载所有分中心
        /// </summary>
        public void SetSubCenterName(string subcenterName)
        {
            if (subcenterName == null || subcenterName.Equals(""))
            {
                // 加载所有的用户分中心
                // List<CEntityStation> listStation = CDBDataMgr.Instance.GetAllStation();
                // 根据分中心查找测站
                List<CEntityStation> listAllStation = CDBDataMgr.Instance.GetAllStation();
                this.SuspendLayout();//暂停VIEW的刷新（datagridview的方法
                this.Hide();
                SetStation(listAllStation);
                this.Show();
                this.ResumeLayout();
            }
            else
            {
                // 根据分中心查找测站
                List<CEntityStation> listAllStation = CDBDataMgr.Instance.GetAllStation();
                CEntitySubCenter subcenter = CDBDataMgr.Instance.GetSubCenterByName(subcenterName);
                if (null != subcenter)
                {
                    // 如果不为空
                    List<CEntityStation> listUseStation = new List<CEntityStation>();
                    for (int i = 0; i < listAllStation.Count; ++i)
                    {
                        if (listAllStation[i].SubCenterID == subcenter.SubCenterID)
                        {
                            listUseStation.Add(listAllStation[i]);
                        }
                    }
                    this.SuspendLayout();//暂停VIEW的刷新（datagridview的方法
                    this.Hide();
                    this.SetStation(listUseStation);
                    this.Show();
                    this.ResumeLayout();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("测站管理分中心 Error");
                }
            }
            this.UpdateDataToUI();
        }

        #endregion

        #region 重写
        // 重写Cell值改变事件
        protected override void EHValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            int iPreValue = this.FirstDisplayedScrollingRowIndex;
            try
            {
                if (e.ColumnIndex >= 0)
                {
                    if (base.m_arrayStrHeader[e.ColumnIndex] == CS_Delete)
                    {
                        // 删除列
                        if (base.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Equals("True"))
                        {
                            // 删除项
                            base.MarkRowDeletedOrNot(e.RowIndex, true);
                            //base.UpdateDataToUI();

                        }
                        else
                        {
                            base.MarkRowDeletedOrNot(e.RowIndex, false);
                            //base.UpdateDataToUI();
                            //FocusOnRow(e.RowIndex);
                        }
                    }
                    else if (base.m_arrayStrHeader[e.ColumnIndex] == CS_StationID)
                    {
                        // 站点id发生变化，站名跟着变化
                        string stationId = base.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                        CEntityStation station = CDBDataMgr.Instance.GetStationById(stationId);
                        base.Rows[e.RowIndex].Cells[CS_StationName].Value = station.StationName;
                    }
                    else
                    {
                        // 非删除列
                        base.EHValueChanged(sender, e);
                    }
                    base.UpdateDataToUI();
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex) { }
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            //FocusOnRow(this.FirstDisplayedScrollingRowIndex);
            FocusOnRow(iPreValue, false);
            //base.Rows[e.RowIndex].Selected = true;
            //this.VerticalScrollBar.Value = iPreValue;
        }

        // 清空所有数据，恢复到刚加载的状态
        protected override void ClearAllState()
        {
            base.ClearAllState();
            m_listAddedStation.Clear();
            m_listDeletedStation.Clear();
            m_listUpdatedStation.Clear();
        }

        //保存
        public override bool DoSave()
        {
            try
            {
                if (this.IsCurrentCellInEditMode)
                {
                    //MessageBox.Show("请完成当前的编辑");
                    this.EndEdit();
                    //return false;
                }

                GetUpdatedData();

                if (!AssertInputData(m_listUpdatedStation))
                {
                    m_listUpdatedStation.Clear();
                    return false;
                }


                GetDeletedData();

                if ((m_listAddedStation.Count > 0 || m_listDeletedStation.Count > 0 || m_listUpdatedStation.Count > 0) &&
                    (IsDataChanged()))
                {
                    // 应该作为一个事物一起处理
                    //return base.DoSave();
                    // 删除
                    bool bResults = m_proxyStation.DeleteRange(m_listDeletedStation);
                    // 增加
                    bResults = bResults && m_proxyStation.AddRange(m_listAddedStation);
                    // 更新
                    bResults = bResults && m_proxyStation.UpdateRange(m_listUpdatedStation);

                    this.Hide();
                    if (bResults)
                    {
                        this.Hide();
                        MessageBox.Show("保存成功", "提示 ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Show();
                    }
                    else
                    {
                        this.Hide();
                        MessageBox.Show("保存失败,请先删除该站点下的雨量、电压、水位数据！", "提示 ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Show();
                        return false;
                    }
                    this.Show();
                    // 重新加载
                    CDBDataMgr.Instance.UpdateAllStation();
                    ClearAllState();
                    this.SuspendLayout();//暂停VIEW的刷新（datagridview的方法
                    this.Hide();
                    LoadData_1();

                    this.Show();
                    this.ResumeLayout();
                    UpdateDataToUI();
                }
                else
                {
                    //this.Revert();
                    this.Hide();
                    MessageBox.Show("没有任何修改，无需保存");
                    this.Show();
                }
                return true;
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                this.Hide();
                MessageBox.Show("请在同一界面完成修改");
                this.Show();
                return false;
            }

        }

        // 重写双击事件
        protected override void OnCellMouseDoubleClick(DataGridViewCellMouseEventArgs e)
        {
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
                // 开启编辑
                base.OnCellMouseDoubleClick(e);
            }
        }

        // 单击事件
        protected override void OnCellClick(DataGridViewCellEventArgs e)
        {
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
                // 开启编辑
                base.OnCellClick(e);
            }
        }

        #endregion
    }
}
