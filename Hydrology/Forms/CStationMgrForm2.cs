using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Hydrology.DataMgr;
using Hydrology.DBManager.Interface;
using Hydrology.Entity;
using Hydrology.Utils;
using Protocol.Manager;

namespace Hydrology.Forms
{
    //定义一个需要string类型参数的委托
    public delegate void MyDelegate(CEntityStation station);
    public partial class CStationMgrForm2 : Form
    {
        List<CEntitySubCenter> m_listSubCenter;
        public List<CEntityStation> m_listStationCombination;    //根据增加和修改以及删除，拼接的内存的数据表
        List<CEntityStation> m_listStation;             //数据库中水情站点列表
        List<CEntitySoilStation> m_listSoilStation;             //数据库中墒情站点列表
        public List<CEntityStation> m_listStationAdded;     //新增的站点列表
        //定义该委托的事件
        public event MyDelegate AddStationEvent;
        /// <summary>
        /// 所有数据协议列表
        /// </summary>
        private List<string> m_listProtocolData;
        private XmlDllCollections m_dllCollections;

        public CStationMgrForm2()
        {
            try
            {
                InitializeComponent();
                InitDataSource();
                InitUI();
            }
            catch (Exception e)
            {
            }
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitDataSource()
        {
            try
            {
                m_listSubCenter = CDBDataMgr.Instance.GetAllSubCenter();
                m_listStation = CDBDataMgr.GetInstance().GetAllStation();
                //m_listSoilStation = CDBSoilDataMgr.GetInstance().GetAllSoilStation();
                m_listStationCombination = new List<CEntityStation>(m_listStation.ToArray()); //可以的啦
            }
            catch (Exception ex) { }
        }

        public void InitUI()
        {
            try
            {
                //// 初始化分中心
                for (int i = 0; i < m_listSubCenter.Count; ++i)
                {
                    cmb_SubCenter.Items.Add(m_listSubCenter[i].SubCenterName);
                }
                cmb_SubCenter.SelectedIndex = 0;
                // 初始化站点类型
                cmb_StationType.Items.Add(CEnumHelper.StationTypeToUIStr(EStationType.EHydrology));
                cmb_StationType.Items.Add(CEnumHelper.StationTypeToUIStr(EStationType.EH));
                cmb_StationType.Items.Add(CEnumHelper.StationTypeToUIStr(EStationType.RE));
                cmb_StationType.Items.Add(CEnumHelper.StationTypeToUIStr(EStationType.RP));
                cmb_StationType.Items.Add(CEnumHelper.StationTypeToUIStr(EStationType.GT));
                cmb_StationType.SelectedIndex = 0;

                // 初始化雨量精度
                cmb_RainAccuracy.Items.Add("0.1");
                cmb_RainAccuracy.Items.Add("0.2");
                cmb_RainAccuracy.Items.Add("0.5");
                cmb_RainAccuracy.Items.Add("1.0");
                cmb_RainAccuracy.SelectedIndex = 0;

                //浮子水位、气泡水位、压阻水位、雷达水位

                comb_WaterSensor.Items.Add("浮子水位");
                comb_WaterSensor.Items.Add("气泡水位");
                comb_WaterSensor.Items.Add("压阻水位");
                comb_WaterSensor.Items.Add("雷达水位");
                //  comb_WaterSensor.Items.Add("无");
                comb_WaterSensor.SelectedIndex = 0;

                //翻斗雨量、雨雪量计
                comb_RainSensor.Items.Add("翻斗雨量");
                comb_RainSensor.Items.Add("雨雪量计");
                //  comb_RainSensor.Items.Add("无");
                comb_RainSensor.SelectedIndex = 0;
                //comb_RainSensor.Enabled = false;

                //自报段次
                comb_Paragraph.Items.Add("1");
                comb_Paragraph.Items.Add("4");
                comb_Paragraph.Items.Add("8");
                comb_Paragraph.Items.Add("12");
                comb_Paragraph.Items.Add("24");
                comb_Paragraph.Items.Add("48");
                comb_Paragraph.SelectedIndex = 4;

                //报警信息初始化
                textBox_Voltage.Text = "11";
                
                //数值参数初始化
                

                if (cmb_StationType.Text == "雨量站")
                {
                    cmb_RainAccuracy.Enabled = true;
                    cmb_RainAccuracy.Items.Remove("无");
                    cmb_RainAccuracy.SelectedIndex = 2;

                    comb_WaterSensor.Enabled = false;
                    comb_WaterSensor.Items.Add("无");
                    comb_WaterSensor.SelectedIndex = 4;

                    comb_RainSensor.Enabled = true;
                    comb_RainSensor.Items.Remove("无");
                    comb_RainSensor.SelectedIndex = 0;

                    textBox_WaterMin.Enabled = false;
                    //textBox_WaterMin.Text = "0.1";

                    textBox_WaterMax.Enabled = false;
                    //textBox_WaterMax.Text = "100";

                    textBox_WaterChange.Enabled = false;
                    //textBox_WaterChange.Text = "2.2";

                    textBox_RainChange.Enabled = true;
                    textBox_RainChange.Text = "80.0";

                    textBox_WaterBase.Enabled = false;
                    //textBox_WaterBase.Text = "0.0";
                }

                if (cmb_StationType.Text == "水位站")
                {
                    cmb_RainAccuracy.Enabled = false;
                    cmb_RainAccuracy.Items.Add("无");
                    cmb_RainAccuracy.SelectedIndex = 4;

                    comb_WaterSensor.Enabled = true;
                    comb_WaterSensor.Items.Remove("无");
                    comb_WaterSensor.SelectedIndex = 0;

                    comb_RainSensor.Enabled = false;
                    comb_RainSensor.Items.Add("无");
                    comb_RainSensor.SelectedIndex = 2;

                    textBox_WaterMin.Enabled = true;
                    textBox_WaterMin.Text = "0";

                    textBox_WaterMax.Enabled = true;
                    textBox_WaterMax.Text = "100";

                    textBox_WaterChange.Enabled = true;
                    textBox_WaterChange.Text = "1";

                    textBox_RainChange.Enabled = false;
                    //textBox_RainChange.Text = "20.0";

                    textBox_WaterBase.Enabled = true;
                    textBox_WaterBase.Text = "0.0";
                }
                if (cmb_StationType.Text == "水文站")
                {
                    cmb_RainAccuracy.Enabled = true;
                    cmb_RainAccuracy.Items.Remove("无");
                    cmb_RainAccuracy.SelectedIndex = 2;

                    comb_WaterSensor.Enabled = true;
                    comb_WaterSensor.Items.Remove("无");
                    comb_WaterSensor.SelectedIndex = 0;

                    comb_RainSensor.Enabled = true;
                    comb_RainSensor.Items.Remove("无");
                    comb_RainSensor.SelectedIndex = 0;

                    textBox_WaterMin.Enabled = true;
                    textBox_WaterMin.Text = "0";

                    textBox_WaterMax.Enabled = true;
                    textBox_WaterMax.Text = "100";

                    textBox_WaterChange.Enabled = true;
                    textBox_WaterChange.Text = "1";

                    textBox_RainChange.Enabled = true;
                    textBox_RainChange.Text = "80.0";

                    textBox_WaterBase.Enabled = true;
                    textBox_WaterBase.Text = "0.0";
                }

                textBox_StationID.Text = "0001";
                textBox_StationName.Text = "测站1";
                cmb_StationType.SelectedIndex = 0;
                //comb_DataProtocol.Items.Add("辽宁");
                m_listProtocolData = XmlDocManager.Instance.DataProtocolNames;
                for (int i = 0; i < m_listProtocolData.Count; i++)
                {
                    comb_DataProtocol.Items.Add(m_listProtocolData[i]);
                }
                comb_DataProtocol.SelectedIndex = 0;

                XmlDocManager.Instance.ReadFromXml();
                m_dllCollections = Protocol.Manager.XmlDocManager.Instance.DllInfo;
                //10.12
                //comb_MainRoad.Items.Add("Gprs");
                //comb_PrepareRoad.Items.Add("Gprs");
                foreach (XmlDllInfo info in m_dllCollections.Infos)
                {
                    //  不显示已经被禁用的协议
                    if (!info.Enabled)
                        continue;
                    // 显示信道协议
                    if (info.Type == "channel")
                    {
                        //   m_mapChannelInfo.Add(info.Name, info);
                        comb_MainRoad.Items.Add(info.Name);
                        comb_PrepareRoad.Items.Add(info.Name);
                    }

                }
                comb_PrepareRoad.Items.Add("无");
                comb_MainRoad.SelectedIndex = 0;
                comb_PrepareRoad.SelectedIndex = 1;


                //通讯参数初始化
                textBox_GSM.Text = "";
                //textBox_GSM.Enabled = false;

                textBox_GPRS.Text = "";
                //textBox_GPRS.Enabled = false;

                textBox_Beidou.Text = "";
                textBox_Beidou.Enabled = false;

                textBox_BeidouMember.Text = "";
                textBox_BeidouMember.Enabled = false;


                if (comb_MainRoad.Text == "SX-GPRS" || comb_PrepareRoad.Text == "SX-GPRS")
                {
                    //通讯参数初始化
                    textBox_GPRS.Enabled = true;
                    textBox_GPRS.Text = "00000001";
                }
                if (comb_MainRoad.Text == "GSM" || comb_PrepareRoad.Text == "GSM" || comb_MainRoad.Text == "WebGSM" || comb_PrepareRoad.Text == "WebGSM")
                {
                    textBox_GSM.Text = "13211111111";
                    textBox_GSM.Enabled = true;
                }
                if (comb_MainRoad.Text == "Beidou-Normal" || comb_PrepareRoad.Text == "Beidou-Normal")
                {
                    textBox_Beidou.Text = "212880";
                    textBox_Beidou.Enabled = true;
                    textBox_BeidouMember.Text = "0";
                    textBox_BeidouMember.Enabled = true;
                }
                if (comb_MainRoad.Text == "Beidou-500" || comb_PrepareRoad.Text == "Beidou-500")
                {
                    textBox_Beidou.Text = "212880";
                    textBox_Beidou.Enabled = true;

                    textBox_BeidouMember.Text = "0";
                    textBox_BeidouMember.Enabled = true;
                }
                btn_Save.Enabled = true;
                btn_Revert.Enabled = true;
            }
            catch (Exception ex) { }
        }

        // 检验输入是否有效
        private bool AssertInputValid()
        {
            try
            {
                // 1. stationId 不能为空, 也不能重复,只能占用四个字节
                string stationId = textBox_StationID.Text.Trim();
                if (stationId.Equals(""))
                {
                    MessageBox.Show("测站编号不能为空");
                    return false;
                }
                // 测站编号不能超过10位
                if (stationId.Length > 10)
                {
                    MessageBox.Show("测站编码不能超过10位");
                    return false;
                }

                // 判断编号是否为负数
                try
                {
                    int.Parse(stationId);
                }
                catch (System.Exception)
                {
                    MessageBox.Show("站点编号不能含有非法字符");
                    return false;
                }
                for (int i = 0; i < m_listStationCombination.Count; ++i)
                {
                    if (m_listStationCombination[i].StationID.Trim() == stationId)
                    {
                        //MessageBox.Show(string.Format("测站站号不能重复！编号 \"{0}\" 与测站 \"{1}\" 重复",
                        //    stationId, m_listStationCombination[i].StationName.Trim()));
                        MessageBox.Show(string.Format("水情测站站号不能重复！已存在水情测站{0}",
                         stationId));
                        return false;
                    }
                }
                //for (int i = 0; i < m_listSoilStation.Count; ++i)
                //{
                //    if (m_listSoilStation[i].StationID.Trim() == stationId)
                //    {
                //        MessageBox.Show(string.Format("水情测站站号不能与墒情测站编号一样！已存在墒情测站{0}",
                //        stationId));
                //        return false;
                //    }
                //}
                if (comb_MainRoad.Text.ToString() == "SX-GPRS" || comb_PrepareRoad.Text.ToString() == "SX-GPRS")
                {
                    string gprs = textBox_GPRS.Text.Trim();
                    if (gprs.Equals(""))
                    {
                        MessageBox.Show("GPRS号码不能为空!");
                        return false;
                    }
                    else
                    {
                        //GPRS号码
                        //  string gprsNum = textBox_GPRS.Text.Trim();
                        if (!CStringUtil.IsDigit(gprs))
                        {
                            MessageBox.Show("GPRS号码参数不合法，必须全部是数字!");
                            return false;
                        }
                        else
                        {

                        }
                    }

                    for (int i = 0; i < m_listStationCombination.Count; ++i)
                    {
                        if (m_listStationCombination[i].GPRS.Trim() == gprs)
                        {
                            MessageBox.Show(string.Format("水情测站Gprs不能重复！与水情站{0} gprs号码一样 ",
                          m_listStationCombination[i].StationID.Trim()));
                            return false;
                        }
                    }
                    //for (int i = 0; i < m_listSoilStation.Count; ++i)
                    //{
                    //    if (m_listSoilStation[i].GPRS.Trim() == gprs)
                    //    {
                    //        MessageBox.Show(string.Format("水情测站Gprs不能重复！与墒情站{0} gprs号码一样 ",
                    //       m_listSoilStation[i].StationID.Trim()));
                    //        return false;
                    //    }

                    //}
                }

                if (System.Text.Encoding.Default.GetByteCount(stationId) > 10)
                {
                    MessageBox.Show("测站ID字符数不能超过10个");
                    return false;
                }

                // 2. 站名不能为空，不能超过50个字符
                if (textBox_StationName.Text.Trim().Equals(""))
                {
                    MessageBox.Show("站名不能为空");
                    return false;
                }
                if (System.Text.Encoding.Default.GetByteCount(textBox_StationName.Text.Trim()) > 50)
                {
                    MessageBox.Show("站名不能超过50个字符");
                    return false;
                }
                // 3. 站类不能为空
                if (cmb_StationType.Text.Equals(""))
                {
                    MessageBox.Show("站点类型不能为空");
                    return false;
                }
                // 4. 分中心不能为空
                if (cmb_SubCenter.Text.Equals(""))
                {
                    MessageBox.Show("分中心不能为空");
                    return false;
                }
                // 5. 雨量精度不能为空，如果是水文站或者雨量站
                EStationType type = CEnumHelper.UIStrToStationType(cmb_StationType.Text);
                if (type == EStationType.EHydrology || type == EStationType.ERainFall)
                {
                    if (cmb_RainAccuracy.Text.Equals(""))
                    {
                        MessageBox.Show("雨量精度不能为空");
                        return false;
                    }
                }
                // 6. 水位基值合法
                if (!textBox_WaterBase.Text.Equals(""))
                {
                    try
                    {
                        Decimal.Parse(textBox_WaterBase.Text);
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("请输入正确的水位基值" + ex.Message);
                        return false;
                    }
                }
                // 7. 水位变化合法
                if (!textBox_WaterChange.Text.Equals(""))
                {
                    try
                    {
                        if (Decimal.Parse(textBox_WaterChange.Text) < 0)
                        {
                            MessageBox.Show("水位阀值不能为负！");
                            return false;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("请输入正确的水位变化值" + ex.Message);
                        return false;
                    }
                }
                // 8. 水位最大值合法
                if (!textBox_WaterMax.Text.Equals(""))
                {
                    try
                    {
                        Decimal.Parse(textBox_WaterMax.Text);
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("请输入正确的水位最大值" + ex.Message);
                        return false;
                    }
                }
                // 9. 水位最小值
                if (!textBox_WaterMin.Text.Equals(""))
                {
                    try
                    {
                        Decimal.Parse(textBox_WaterMin.Text);
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("请输入正确的水位最小值" + ex.Message);
                        return false;
                    }
                }
                // 10. 雨量变化合法
                if (!textBox_RainChange.Text.Equals(""))
                {
                    try
                    {
                        if (Decimal.Parse(textBox_RainChange.Text) < 0)
                        {
                            MessageBox.Show("雨量阀值不能为负！");
                            return false;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("请输入正确的雨量变化值" + ex.Message);
                        return false;
                    }
                }

                // 11. 电压下限合法
                if (!textBox_Voltage.Text.Equals(""))
                {
                    try
                    {
                        if (float.Parse(textBox_Voltage.Text) < 0)
                        {
                            MessageBox.Show("电压阀值不能为负！");
                            return false;
                        }
                    }
                    catch (System.Exception)
                    {
                        MessageBox.Show("请输入正确的电压阀值");
                        return false;
                    }
                }

                return true;

            }catch(Exception ex)
            {
                return false;
            }
            
        }

        private CEntityStation GenerateStationFromUI()
        {
            try
            {
                if (!AssertInputValid())
                {
                    return null;
                }
                // 读取界面数据，并构建一个站点实体类
                CEntityStation station = new CEntityStation();
                station.StationID = textBox_StationID.Text.Trim(); //去掉空格
                station.StationName = textBox_StationName.Text;
                station.SubCenterID = CDBDataMgr.Instance.GetSubCenterByName(cmb_SubCenter.Text).SubCenterID;
                station.StationType = CEnumHelper.UIStrToStationType(cmb_StationType.Text);
                if (!textBox_WaterBase.Text.Trim().Equals(""))
                {
                    // 设置了水位基值
                    station.DWaterBase = Decimal.Parse(textBox_WaterBase.Text.Trim());
                }
                if (!textBox_WaterChange.Text.Trim().Equals(""))
                {
                    // 设置了水位变化
                    station.DWaterChange = Decimal.Parse(textBox_WaterChange.Text.Trim());
                }
                if (!textBox_WaterMax.Text.Trim().Equals(""))
                {
                    // 设置了水位最大值
                    station.DWaterMax = Decimal.Parse(textBox_WaterMax.Text.Trim());
                }
                if (!textBox_WaterMin.Text.Trim().Equals(""))
                {
                    // 设置了水位最小值
                    station.DWaterMin = Decimal.Parse(textBox_WaterMin.Text.Trim());
                }
                if (station.StationType == EStationType.ERainFall || station.StationType == EStationType.EHydrology)
                {
                    // 雨量精度不能为空
                    try
                    {
                        station.DRainAccuracy = float.Parse(cmb_RainAccuracy.Text);
                    }
                    catch (Exception e)
                    {
                        station.DRainAccuracy = 0.5f;
                    }
                }
                if (!textBox_RainChange.Text.Trim().Equals(""))
                {
                    // 设置了雨量变化
                    station.DRainChange = Decimal.Parse(textBox_RainChange.Text.Trim());
                }
                if (!textBox_Voltage.Text.Trim().Equals(""))
                {
                    // 设置了电压阀值
                    station.DVoltageMin = float.Parse(textBox_Voltage.Text.Trim());
                }
                if (!textBox_GSM.Text.Trim().Equals(""))
                {
                    string gsmNum = textBox_GSM.Text.Trim();
                    //if (!CStringUtil.IsDigitStrWithSpecifyLength(gsmNum, 11))
                    //{
                    //    MessageBox.Show("GSM号码参数不合法，长度必须为11位，必须全部是数字!");
                    //    return null;
                    //}
                    if (!CStringUtil.IsDigit(gsmNum))
                    {
                        MessageBox.Show("GSM号码参数不合法，必须全部是数字!");
                        return null;
                    }
                    // 设置了GSM号码
                    station.GSM = gsmNum;
                }
                //if (!textBox_GPRS.Text.Trim().Equals(""))
                //{
                //    //GPRS号码
                //    string gprsNum = textBox_GPRS.Text.Trim();
                //    if (!CStringUtil.IsDigit(gprsNum))
                //    {
                //        MessageBox.Show("GPRS号码参数不合法，必须全部是数字!");
                //        return null;
                //    }
                station.GPRS = textBox_GPRS.Text.Trim();
                //}

                if (!textBox_Beidou.Text.Trim().Equals(""))
                {
                    string bdNum = textBox_Beidou.Text.Trim();
                    if (!CStringUtil.IsDigit(bdNum))
                    {
                        MessageBox.Show("北斗卫星终端号码参数不合法，必须全部是数字!");
                        return null;
                    }
                    // 北斗卫星号码
                    station.BDSatellite = bdNum;
                }


                if (!textBox_BeidouMember.Text.Trim().Equals(""))
                {
                    string bdNum = textBox_BeidouMember.Text.Trim();
                    if (!CStringUtil.IsDigit(bdNum))
                    {
                        MessageBox.Show("北斗卫星成员号码参数不合法，必须全部是数字!");
                        return null;
                    }
                    // 北斗卫星号码
                    station.BDMemberSatellite = bdNum;
                }

                station.Maintran = comb_MainRoad.Text;
                station.Subtran = comb_PrepareRoad.Text;
                station.Rainsensor = comb_RainSensor.SelectedIndex.ToString();

                station.Watersensor = comb_WaterSensor.SelectedIndex.ToString();
                station.Datapotocol = comb_DataProtocol.Text;
                //station.DRainAccuracy = float.Parse(cmb_RainAccuracy.Text.ToString());
                station.Reportinterval = comb_Paragraph.Text;
                //if (!textBox_BeidouMember.Text.Trim().Equals(""))
                //{
                //    string bdNum = textBox_BeidouMember.Text.Trim();
                //    if (!CStringUtil.IsDigit(bdNum))
                //    {
                //        MessageBox.Show("北斗卫星成员号码参数不合法，必须全部是数字!");
                //        return null;
                //    }
                //    // 北斗卫星号码
                //    station.BDMemberSatellite = bdNum;
                //}

                //if (comb_MainRoad.Text == "SX-GPRS" || comb_PrepareRoad.Text == "GPRS")
                //{
                //}
                return station;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //private void btn_Save_Click_1(object sender, EventArgs e)
        //{
        //    //// 完成添加
        //    //if (AssertInputValid())
        //    //{
        //    //    this.Close();
        //    //    this.DialogResult = DialogResult.OK;
        //    //}

        //    // 完成添加
        //    CEntityStation station = GenerateStationFromUI();
        //    if (null == station)
        //    {
        //        return;
        //    }
        //    //  m_listStationAdded.Add(station);

        //    //  m_listStationCombination.Add(station);
        //    CDBDataMgr.GetInstance().m_mapStation.Add(station.StationID, station);
        //    //触发事件，并将修改后的文本回传
        //    AddStationEvent(station);
        //    this.Close();

        //    //InnerRefreshUI();

        //    //// 修改成功
        //    //btn_Save.Enabled = false;
        //    //btn_Revert.Enabled = false;
        //}

        private void btn_Save_Click(object sender, EventArgs e)
        {
            try
            {
                //// 完成添加
                //if (AssertInputValid())
                //{
                //    this.Close();
                //    this.DialogResult = DialogResult.OK;
                //}

                // 完成添加
                CEntityStation station = GenerateStationFromUI();
                if (null == station)
                {
                    return;
                }
                //  m_listStationAdded.Add(station);

                //  m_listStationCombination.Add(station);
                CDBDataMgr.GetInstance().m_mapStation.Add(station.StationID, station);
                //触发事件，并将修改后的文本回传
                AddStationEvent(station);
                //this.Close();
                this.Hide();
                MessageBox.Show("测站添加完成！ ", "提示 ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
                //InnerRefreshUI();

                //// 修改成功
                //btn_Save.Enabled = false;
                //btn_Revert.Enabled = false;
            }
            catch (Exception ex) { }
        }

        private void comb_MainRoad_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                textBox_GPRS.Text = "";
                textBox_GSM.Text = "";
                textBox_Beidou.Text = "";
                textBox_Beidou.Enabled = false;
                textBox_BeidouMember.Text = "";
                textBox_BeidouMember.Enabled = false;
                if (comb_PrepareRoad.Text == "SX-GPRS")
                {
                    textBox_GPRS.Enabled = true;
                    textBox_GPRS.Text = "00000001";
                }
                else if (comb_PrepareRoad.Text == "GSM" || comb_PrepareRoad.Text == "WebGSM")
                {
                    textBox_GSM.Enabled = true;
                    textBox_GSM.Text = "13211111111";
                }
                else if (comb_PrepareRoad.Text == "Beidou-Normal")
                {
                    textBox_Beidou.Text = "212880";
                    textBox_Beidou.Enabled = true;
                    textBox_BeidouMember.Text = "0";
                    textBox_BeidouMember.Enabled = true;
                }
                else if (comb_PrepareRoad.Text == "Beidou-500")
                {
                    textBox_Beidou.Text = "212880";
                    textBox_Beidou.Enabled = true;
                    textBox_BeidouMember.Text = "0";
                    textBox_BeidouMember.Enabled = true;
                }

                if (comb_MainRoad.Text == "SX-GPRS")
                {
                    textBox_GPRS.Enabled = true;
                    textBox_GPRS.Text = "00000001";
                }
                else if (comb_MainRoad.Text == "GSM" || comb_PrepareRoad.Text == "WebGSM")
                {
                    textBox_GSM.Enabled = true;
                    textBox_GSM.Text = "13211111111";
                }
                else if (comb_MainRoad.Text == "Beidou-Normal")
                {
                    textBox_Beidou.Text = "212880";
                    textBox_Beidou.Enabled = true;
                    textBox_BeidouMember.Text = "0";
                    textBox_BeidouMember.Enabled = true;
                }
                else if (comb_MainRoad.Text == "Beidou-500")
                {
                    textBox_Beidou.Text = "212880";
                    textBox_Beidou.Enabled = true;
                    textBox_BeidouMember.Text = "0";
                    textBox_BeidouMember.Enabled = true;
                }
            }
            catch (Exception ex)
            {
            }
        }
    
        private void comb_PrepareRoad_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                //textBox_GPRS.Enabled = false;
                textBox_GPRS.Text = "";
                textBox_GSM.Text = "";
                //textBox_GSM.Enabled = false;
                textBox_Beidou.Text = "";
                textBox_Beidou.Enabled = false;
                textBox_BeidouMember.Text = "";
                textBox_BeidouMember.Enabled = false;
                if (comb_MainRoad.Text == "SX-GPRS")
                {
                    textBox_GPRS.Enabled = true;
                    textBox_GPRS.Text = "00000001";
                }
                else if (comb_MainRoad.Text == "GSM" || comb_PrepareRoad.Text == "WebGSM")
                {
                    textBox_GSM.Enabled = true;
                    textBox_GSM.Text = "13211111111";
                }
                else if (comb_MainRoad.Text == "Beidou-Normal")
                {
                    textBox_Beidou.Text = "212880";
                    textBox_Beidou.Enabled = true;
                    textBox_BeidouMember.Text = "0";
                    textBox_BeidouMember.Enabled = true;
                }
                else if (comb_MainRoad.Text == "Beidou-500")
                {
                    textBox_Beidou.Text = "212880";
                    textBox_Beidou.Enabled = true;
                    textBox_BeidouMember.Text = "0";
                    textBox_BeidouMember.Enabled = true;
                }

                if (comb_PrepareRoad.Text == "SX-GPRS")
                {
                    textBox_GPRS.Enabled = true;
                    textBox_GPRS.Text = "00000001";
                }
                else if (comb_PrepareRoad.Text == "GSM" || comb_PrepareRoad.Text == "WebGSM")
                {
                    textBox_GSM.Enabled = true;
                    textBox_GSM.Text = "13211111111";
                }
                else if (comb_PrepareRoad.Text == "Beidou-Normal")
                {
                    textBox_Beidou.Text = "212880";
                    textBox_Beidou.Enabled = true;
                    textBox_BeidouMember.Text = "0";
                    textBox_BeidouMember.Enabled = true;
                }
                else if (comb_PrepareRoad.Text == "Beidou-500")
                {
                    textBox_Beidou.Text = "212880";
                    textBox_Beidou.Enabled = true;
                    textBox_BeidouMember.Text = "0";
                    textBox_BeidouMember.Enabled = true;
                }
            }
            catch (Exception ex) { }
        }

        private void comb_DataProtocol_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cmb_StationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                //报警信息初始化
                //cmb_RainAccuracy.Enabled = false;
                //comb_WaterSensor.Enabled = false;
                //comb_RainSensor.Enabled = false;
                //textBox_WaterMin.Enabled = false;
                //textBox_WaterMax.Enabled = false;
                //textBox_RainChange.Enabled = false;
                textBox_Voltage.Text = "11";
                //textBox_WaterChange.Enabled = false;
                ////数值参数初始化
                //textBox_WaterBase.Enabled = false;

                if (cmb_StationType.Text == "雨量站")
                {
                    cmb_RainAccuracy.Enabled = true;
                    if (cmb_RainAccuracy.Items.Count == 5)
                    {
                        cmb_RainAccuracy.Items.Remove("无");
                    }
                    cmb_RainAccuracy.SelectedIndex = 2;

                    comb_WaterSensor.Enabled = false;
                    if (cmb_RainAccuracy.Items.Count == 4)
                    {
                        comb_WaterSensor.Items.Add("无");
                        comb_WaterSensor.SelectedIndex = 4;
                    }

                    comb_RainSensor.Enabled = true;
                    if (comb_RainSensor.Items.Count == 3)
                    {
                        comb_RainSensor.Items.Remove("无");
                        comb_RainSensor.SelectedIndex = 0;
                    }

                    textBox_WaterMin.Enabled = false;
                    //textBox_WaterMin.Text = "0.1";

                    textBox_WaterMax.Enabled = false;
                    //textBox_WaterMax.Text = "100";

                    textBox_WaterChange.Enabled = false;
                    //textBox_WaterChange.Text = "2.2";

                    textBox_RainChange.Enabled = true;
                    textBox_RainChange.Text = "80.0";

                    textBox_WaterBase.Enabled = false;
                    //textBox_WaterBase.Text = "0.0";
                }

                if (cmb_StationType.Text == "水位站")
                {
                    cmb_RainAccuracy.Enabled = false;
                    if (cmb_RainAccuracy.Items.Count == 4)
                    {
                        cmb_RainAccuracy.Items.Add("无");
                        cmb_RainAccuracy.SelectedIndex = 4;
                    }

                    comb_WaterSensor.Enabled = true;
                    if (comb_WaterSensor.Items.Count == 5)
                    {
                        comb_WaterSensor.Items.Remove("无");
                        comb_WaterSensor.SelectedIndex = 0;
                    }

                    comb_RainSensor.Enabled = false;
                    if (comb_RainSensor.Items.Count == 2)
                    {
                        comb_RainSensor.Items.Add("无");
                        comb_RainSensor.SelectedIndex = 2;
                    }

                    textBox_WaterMin.Enabled = true;
                    textBox_WaterMin.Text = "0";

                    textBox_WaterMax.Enabled = true;
                    textBox_WaterMax.Text = "100";

                    textBox_WaterChange.Enabled = true;
                    textBox_WaterChange.Text = "1";

                    textBox_RainChange.Enabled = false;
                    //textBox_RainChange.Text = "20.0";

                    textBox_WaterBase.Enabled = true;
                    textBox_WaterBase.Text = "0.0";
                }
                if (cmb_StationType.Text == "水文站" || cmb_StationType.Text == "河道站") 
                {
                    cmb_RainAccuracy.Enabled = true;
                    cmb_RainAccuracy.Items.Remove("无");
                    cmb_RainAccuracy.SelectedIndex = 2;

                    comb_WaterSensor.Enabled = true;
                    comb_WaterSensor.Items.Remove("无");
                    comb_WaterSensor.SelectedIndex = 0;

                    comb_RainSensor.Enabled = true;
                    comb_RainSensor.Items.Remove("无");
                    comb_RainSensor.SelectedIndex = 0;

                    textBox_WaterMin.Enabled = true;
                    textBox_WaterMin.Text = "0";

                    textBox_WaterMax.Enabled = true;
                    textBox_WaterMax.Text = "100";

                    textBox_WaterChange.Enabled = true;
                    textBox_WaterChange.Text = "1";

                    textBox_RainChange.Enabled = true;
                    textBox_RainChange.Text = "80.0";

                    textBox_WaterBase.Enabled = true;
                    textBox_WaterBase.Text = "0.0";
                }
            }
            catch (Exception ex) { }
        }

        private void btn_Revert_Click(object sender, EventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
            }
        }
    }
}
