using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Hydrology.CControls;
using Hydrology.DataMgr;
using Hydrology.Entity;
using Hydrology.Utils;

namespace Hydrology.Forms
{
    public partial class CReadAndSettingMgrForm : Form
    {
        private EReadOrSetStatus m_RSStatus = EReadOrSetStatus.None;
        private EChannelType m_channelType = EChannelType.TCP;
        private List<string> m_vNormalStateLists = new List<string>();
        private List<string> m_vWorkStatusLists = new List<string>();
        private List<int> m_vTimePeriodLists = new List<int>();
        private List<string> m_vTimeChoiceLists = new List<string>();
        private List<string> m_vMainChannelLists = new List<string>();
        private List<string> m_vViceChannelLists = new List<string>();
        private List<string> m_vSelectCollectionParagraphsLists = new List<string>();
        private string m_CmdStr = string.Empty;
        private string m_StatusIDStr = string.Empty;
        private CEntityStation m_CurrentStation;
        public CReadAndSettingMgrForm()
        {
            InitializeComponent();
            this.label19.Hide();
            InitComboBoxDataSource();
            Init();
            FormHelper.InitControlFocusLoop(this);
            FormHelper.InitUserModeEvent(this);
            CProtocolEventManager.DownForUI += this.DownForUI_EventHandler;
            CProtocolEventManager.ErrorForUI += this.ErrorForUI_EventHandler;
            CProtocolEventManager.GPRS_TimeOut4UI += (s, e) =>
            {
                if (null != e)
                {
                    //   SetWarningInfo(string.Format("GPRS: {0}{1}站点数据，接收数据时间超过{2}毫秒!", m_CmdStr, m_StatusIDStr, e.Second), Color.Red);
                    //   m_RSStatus = EReadOrSetStatus.None;
                }
            };
            CProtocolEventManager.GSM_TimeOut4UI += (s, e) =>
            {
                if (null != e)
                {
                    //    SetWarningInfo(string.Format("GSM: {0}{1}站点数据，接收数据时间超过{2}毫秒!", m_CmdStr, m_StatusIDStr, e.Second), Color.Red);
                    //    AddLog(string.Format("GSM: {0}{1}站点数据，接收数据时间超过{2}毫秒!", m_CmdStr, m_StatusIDStr, e.Second));
                    //    m_RSStatus = EReadOrSetStatus.None;
                }
            };
            InitListView();
        }
        //  初始化ComboBox数据源列表
        private void InitComboBoxDataSource()
        {
            //  初始化站点列表信息
            this.groupBox2.Controls.Remove(this.cmbStation);
            cmbStation = new CStationComboBox();
            this.cmbStation.FormattingEnabled = true;
            this.cmbStation.Location = new System.Drawing.Point(107, 25);
            this.cmbStation.Name = "cmbStation";
            this.cmbStation.Size = new System.Drawing.Size(130, 20);
            this.cmbStation.TabIndex = 26;
            (this.cmbStation as CStationComboBox).StationSelected += new EventHandler<CEventSingleArgs<CEntityStation>>(CReadAndSettingMgrForm_StationSelected);
            this.groupBox2.Controls.Add(this.cmbStation);

            //  初始化通讯方式列表
            var msgLists = new List<String>()
            {
                //EChannelType.GPRS.ToString(),
                //EChannelType.GSM.ToString(),
                EChannelType.TCP.ToString(),
                //EChannelType.BeiDou.ToString(),
                //EChannelType.PSTN.ToString()
            };
            this.cmbMsgType.DataSource = msgLists;

            //  初始化常规状态,并绑定控件
            foreach (var item in ProtocolMaps.NormalState4UI)
            {
                m_vNormalStateLists.Add(item.Value);
            }
            this.vNormalState.DataSource = m_vNormalStateLists;
            //  初始化工作状态,并绑定控件
            foreach (var item in ProtocolMaps.WorkStatus4UI)
            {
                m_vWorkStatusLists.Add(item.Value);
            }
            this.vWorkStatus.DataSource = m_vWorkStatusLists;
            //  初始化定时段次
            foreach (var item in ProtocolMaps.TimePeriodMap)
            {
                m_vTimePeriodLists.Add(Int32.Parse(item.Value));
            }
            this.vTimePeriod.DataSource = m_vTimePeriodLists;
            //  初始化对时选择
            foreach (var item in ProtocolMaps.TimeChoice4UI)
            {
                m_vTimeChoiceLists.Add(item.Value);
            }
            this.vTimeChoice.DataSource = m_vTimeChoiceLists;
            //  初始化主用信道
            m_vMainChannelLists = new List<String>()
            {
                EChannelType.GPRS.ToString(),
                EChannelType.GSM.ToString(),
                ProtocolMaps.ChannelType4UIMap.FindValue(EChannelType.BeiDou),
                EChannelType.PSTN.ToString(),
                EChannelType.VHF.ToString()
            };
            this.vMainChannel.DataSource = m_vMainChannelLists;
            //  初始化备用信道
            m_vViceChannelLists = new List<String>()
            {
                EChannelType.GPRS.ToString(),
                EChannelType.GSM.ToString(),
                ProtocolMaps.ChannelType4UIMap.FindValue(EChannelType.BeiDou),
                EChannelType.PSTN.ToString(),
                EChannelType.VHF.ToString(),
                ProtocolMaps.ChannelType4UIMap.FindValue(EChannelType.None)
            };
            this.vViceChannel.DataSource = m_vViceChannelLists;
            //  初始化采集端次选
            foreach (var item in ProtocolMaps.SelectCollectionParagraphs4UIMap)
            {
                m_vSelectCollectionParagraphsLists.Add(item.Value);
            }
            this.vSelectCollectionParagraphs.DataSource = m_vSelectCollectionParagraphsLists;
        }
        private void Init()
        {
            /************工作配置*****************/
            //  初始化时钟
            this.vClock.Value = DateTime.Now;
            //  初始化电压
            this.vVoltage.Value = 0;
            //  初始化站号
            this.vStationCmdID.Text = string.Empty;
            //  初始化常规状态
            this.vNormalState.SelectedIndex = 0;
            //  初始化版本号
            this.vVersionNum.Text = string.Empty;
            //  初始化工作状态
            this.vWorkStatus.SelectedIndex = 1;
            //  初始化定时段次
            this.vTimePeriod.SelectedIndex = 6;
            //  初始化对时选择
            this.vTimeChoice.SelectedIndex = 0;
            /************通訊配置*****************/
            //  初始化主备信道
            this.vMainChannel.SelectedIndex = 0;
            //  初始化备用信道
            this.vViceChannel.SelectedIndex = 1;
            //  初始化目的地手机
            this.vDestPhoneNum.Text = string.Empty;
            //  初始化SIM卡号
            this.vTeleNum.Text = string.Empty;
            //  初始化终端机号
            this.vClockTime.Text = string.Empty;
            //  初始化响应波束
            this.ICset.Text = string.Empty;
            //  初始化振铃次数
            this.vRingsNum.Value = 1;

            /************工作配置*****************/
            //  初始化测站类型
            //this.vStationType.Text = string.Empty;
            //  初始化水位
            //this.vWater.Value = 0;
            //  初始化雨量
            //this.vRain.Value = 0;
            //  初始化水位加报值
            this.vWaterPlusReportedValue.Value = 0;
            //  初始化雨量加报值
            this.vRainPlusReportedValue.Value = 0;
            //  初始化K
            this.vK.Text = string.Empty;
            //  初始化C
            this.vC.Text = string.Empty;
            //  初始化平均时间           
            this.vAvegTime.Text = string.Empty;
            //  初始化采集端次选
            this.vSelectCollectionParagraphs.SelectedIndex = 0;

            this.lblWarning.Text = string.Empty;
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

        private void ErrorForUI_EventHandler(object sender, ReceiveErrorEventArgs e)
        {
            string msg = e.Msg;
            if (this.IsHandleCreated)
            {
                try
                {
                    if (msg != "ATE0" && (!msg.Contains("超过")))
                    {
                        AddLog("接收数据 : " + msg);
                    }
                    switch (m_RSStatus)
                    {
                        case EReadOrSetStatus.None:
                            break;
                        case EReadOrSetStatus.Read:
                            //if (msg.Contains("ATE0"))
                            //{
                            //    SetWarningInfo(string.Format("{0}{1}站点数据失败!", m_CmdStr, m_StatusIDStr), Color.Red);
                            //}
                            //SetWarningInfo(String.Format("{0}{1}站点数据为ATE0!", m_CmdStr, m_StatusIDStr), Color.Red);
                            break;
                        case EReadOrSetStatus.Set:
                            //if (msg.Contains("ATE0"))
                            //{
                            //    //SetWarningInfo(string.Format("{0}{1}站点数据为ATE0!", m_CmdStr, m_StatusIDStr), Color.Red);
                            //}
                            //else 
                            if (msg.Contains("TRU"))
                            {
                                SetWarningInfo(string.Format("{0}{1}站点数据成功!", m_CmdStr, m_StatusIDStr), Color.Green);
                                m_RSStatus = EReadOrSetStatus.None;
                            }
                            break;
                        default: break;
                    }
                }
                catch (Exception exp) { Debug.WriteLine(exp.Message); }
            }
        }
        private void DownForUI_EventHandler(object sender, DownEventArgs e)
        {
            try
            {
                CReportData  info = e.rpValue.Datas[0];
                string rawData = e.RawData;
                if (info == null)
                    return;
                if (this.IsHandleCreated)
                {
                    #region 更新UI
                    //if (info.Datas.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vClock.Invoke((Action)delegate
                    //        {
                    //            this.vClock.Value = info.Clock.Value;
                    //            this.label19.Show();
                    //            //HighlightControl(this.vClock);
                    //            //this.vClock.BackColor = Color.Red;                             
                    //            //this.vClock.CalendarForeColor = Color.Red;
                    //            //this.vClock.CalendarMonthBackground = Color.Red;
                    //            //this.vClock.CalendarTitleBackColor = Color.Red;
                    //            //this.vClock.CalendarTrailingForeColor= Color.Red;
                    //            this.vClock.Invalidate();

                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (info.NormalState.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vNormalState.Invoke((Action)delegate
                    //        {
                    //            this.vNormalState.Text = ProtocolMaps.NormalState4UI.FindValue(info.NormalState.Value);
                    //            HighlightControl(this.vNormalState);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (info.Voltage.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vVoltage.Invoke((Action)delegate
                    //        {
                    //            this.vVoltage.Value = info.Voltage.Value;
                    //            HighlightControl(this.vVoltage);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (!String.IsNullOrEmpty(info.StationCmdID))
                    //{
                    //    try
                    //    {
                    //        this.vStationCmdID.Invoke((Action)delegate
                    //        {
                    //            this.vStationCmdID.Text = info.StationCmdID;
                    //            HighlightControl(this.vStationCmdID);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (info.TimeChoice.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vTimeChoice.Invoke((Action)delegate
                    //        {
                    //            this.vTimeChoice.Text = ProtocolMaps.TimeChoice4UI.FindValue(info.TimeChoice.Value);
                    //            HighlightControl(this.vTimeChoice);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (info.TimePeriod.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vTimePeriod.Invoke((Action)delegate
                    //        {
                    //            this.vTimePeriod.Text = Int32.Parse(ProtocolMaps.TimePeriodMap.FindValue(info.TimePeriod.Value)).ToString();
                    //            HighlightControl(this.vTimePeriod);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //设备运行
                    if (info.Alarm != null)
                    {
                        string normalState = info.Alarm.Substring(0, 3);
                        string binaryStr = HexString2BinString(normalState);
                        binaryStr.Replace(" ","");

                        string ACState = string.Empty;
                        string voltageState = string.Empty;
                        string waterState = string.Empty;
                        string flowState = string.Empty;
                        string qualityState = string.Empty;
                        string flowInsState = string.Empty;
                        string waterInsState = string.Empty;
                        string boxState = string.Empty;
                        string memoryState = string.Empty;
                        string ICState = string.Empty;
                        string pumpState = string.Empty;
                        string remianWaterState = string.Empty;
                        for (int i = 0; i < binaryStr.Length-1; i++)
                        {
                            string flag = binaryStr.Substring(i, 1);
                            if(i == 0)
                            {
                                if(flag == "0")
                                {
                                    ACState = "正常";
                                }
                                else
                                {
                                    ACState = "停电";
                                }
                            }
                            if (i == 1)
                            {
                                if (flag == "0")
                                {
                                    voltageState = "正常";
                                }
                                else
                                {
                                    voltageState = "电压低";
                                }
                            }
                            if (i == 2)
                            {
                                if (flag == "0")
                                {
                                    waterState = "正常";
                                }
                                else
                                {
                                    waterState = "报警";
                                }
                            }
                            if (i == 3)
                            {
                                if (flag == "0")
                                {
                                    flowState = "正常";
                                }
                                else
                                {
                                    flowState = "报警";
                                }
                            }
                            if (i == 4)
                            {
                                if (flag == "0")
                                {
                                    qualityState = "正常";
                                }
                                else
                                {
                                    qualityState = "报警";
                                }
                            }
                            if (i == 5)
                            {
                                if (flag == "0")
                                {
                                    flowInsState = "正常";
                                }
                                else
                                {
                                    flowInsState = "故障";
                                }
                            }
                            if (i == 6)
                            {
                                if (flag == "0")
                                {
                                    waterInsState = "正常";
                                }
                                else
                                {
                                    waterInsState = "故障";
                                }
                            }
                            if (i == 7)
                            {
                                if (flag == "0")
                                {
                                    boxState = "正常";
                                }
                                else
                                {
                                    boxState = "关闭";
                                }
                            }
                            if (i == 8)
                            {
                                if (flag == "0")
                                {
                                    memoryState = "正常";
                                }
                                else
                                {
                                    memoryState = "异常";
                                }
                            }
                            if (i == 9)
                            {
                                if (flag == "0")
                                {
                                    ICState = "关闭";
                                }
                                else
                                {
                                    ICState = "IC卡有效";
                                }
                            }
                            if (i == 10)
                            {
                                if (flag == "0")
                                {
                                    pumpState = "水泵工作";
                                }
                                else
                                {
                                    pumpState = "水泵停机";
                                }
                            }
                            if (i == 11)
                            {
                                if (flag == "0")
                                {
                                    remianWaterState = "未超限";
                                }
                                else
                                {
                                    remianWaterState = "水量超限";
                                }
                            }
                        }
                        try
                        {
                            //string ACState = string.Empty;
                            //string voltageState = string.Empty;
                            //string waterState = string.Empty;
                            //string flowState = string.Empty;
                            //string qualityState = string.Empty;
                            //string flowInsState = string.Empty;
                            //string waterInsState = string.Empty;
                            //string boxState = string.Empty;
                            //string memoryState = string.Empty;
                            //string ICState = string.Empty;
                            //string pumpState = string.Empty;
                            //string remianWaterState = string.Empty;
                            this.vWorkStatus.Invoke((Action)delegate
                            {
                                this.ACState.Text = ACState;
                                this.voltageState.Text = voltageState;
                                this.waterState.Text = waterState;
                                this.flowState.Text = flowState;
                                this.qualityState.Text = qualityState;
                                this.flowInsState.Text = flowInsState;
                                this.waterInsState.Text = waterInsState;
                                this.boxState.Text = boxState;
                                this.memoryState.Text = memoryState;
                                this.ICState.Text = ICState;
                                this.pumpState.Text = pumpState;
                                this.remianWaterState.Text = remianWaterState;

                                HighlightControl(this.ACState);
                                HighlightControl(this.voltageState);
                                HighlightControl(this.waterState);
                                HighlightControl(this.flowState);
                                HighlightControl(this.qualityState);
                                HighlightControl(this.flowInsState);
                                HighlightControl(this.waterInsState);
                                HighlightControl(this.boxState);
                                HighlightControl(this.memoryState);
                                HighlightControl(this.ICState);
                                HighlightControl(this.pumpState);
                                HighlightControl(this.remianWaterState);
                            });
                        }
                        catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    }
                    if (!String.IsNullOrEmpty(info.Version))
                    {
                        try
                        {
                            this.vVersionNum.Invoke((Action)delegate
                            {
                                this.vVersionNum.Text = info.Version;
                                HighlightControl(this.vVersionNum);
                            });
                        }
                        catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    }
                    if(info.Time != null && info.Time.Year > 1900)
                    {

                        try
                        {
                            this.vClockTime.Invoke((Action)delegate
                            {
                                string a = info.Time.ToString();
                                this.vClockTime.Text = info.Time.ToString();
                                HighlightControl(this.vClockTime);
                            });
                        }
                        catch (Exception e7) { }
                    }
                    //if (info.MainChannel.HasValue && info.ViceChannel.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vMainChannel.Invoke((Action)delegate
                    //        {
                    //            this.vMainChannel.Text = ProtocolMaps.ChannelType4UIMap.FindValue(info.MainChannel.Value);
                    //            HighlightControl(this.vMainChannel);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //    try
                    //    {
                    //        this.vViceChannel.Invoke((Action)delegate
                    //        {
                    //            this.vViceChannel.Text = ProtocolMaps.ChannelType4UIMap.FindValue(info.ViceChannel.Value);
                    //            HighlightControl(this.vViceChannel);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (!String.IsNullOrEmpty(info.TeleNum))
                    //{
                    //    try
                    //    {
                    //        this.vTeleNum.Invoke((Action)delegate
                    //        {
                    //            this.vTeleNum.Text = info.TeleNum;
                    //            HighlightControl(this.vTeleNum);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (info.RingsNum.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vRingsNum.Invoke((Action)delegate
                    //        {
                    //            this.vRingsNum.Value = info.RingsNum.Value;
                    //            HighlightControl(this.vRingsNum);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (!String.IsNullOrEmpty(info.DestPhoneNum))
                    //{
                    //    try
                    //    {
                    //        this.vDestPhoneNum.Invoke((Action)delegate
                    //        {
                    //            this.vDestPhoneNum.Text = info.DestPhoneNum;
                    //            HighlightControl(this.vDestPhoneNum);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (!String.IsNullOrEmpty(info.TerminalNum))
                    //{
                    //    try
                    //    {
                    //        this.vTerminalNum.Invoke((Action)delegate
                    //        {
                    //            this.vTerminalNum.Text = info.TerminalNum;
                    //            HighlightControl(this.vTerminalNum);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (!String.IsNullOrEmpty(info.RespBeam))
                    //{
                    //    try
                    //    {
                    //        this.ICset.Invoke((Action)delegate
                    //        {
                    //            this.ICset.Text = info.RespBeam;
                    //            HighlightControl(this.ICset);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (info.AvegTime.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vAvegTime.Invoke((Action)delegate
                    //        {
                    //            this.vAvegTime.Text = info.AvegTime.Value.ToString();
                    //            HighlightControl(this.vAvegTime);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (info.RainPlusReportedValue.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vRainPlusReportedValue.Invoke((Action)delegate
                    //        {
                    //            if (m_CurrentStation == null)
                    //            {
                    //                AddLog("未选择站点,或者该站点雨量精度不合法");
                    //            }
                    //            else
                    //            {
                    //                decimal rainAccuracy = (decimal)m_CurrentStation.DRainAccuracy;
                    //                this.vRainPlusReportedValue.Value = info.RainPlusReportedValue.Value * rainAccuracy;
                    //                HighlightControl(this.vRainPlusReportedValue);
                    //            }
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (!String.IsNullOrEmpty(info.KC))
                    //{
                    //    if (info.KC.Length != 20)
                    //    {
                    //        MessageBox.Show("KC值为" + info.KC);

                    //    }
                    //    else
                    //    {
                    //        try
                    //        {
                    //            this.vK.Invoke((Action)delegate
                    //            {
                    //                this.vK.Text = info.KC.Substring(0, 10);
                    //                HighlightControl(this.vK);
                    //            });
                    //        }
                    //        catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //        try
                    //        {
                    //            this.vC.Invoke((Action)delegate
                    //            {
                    //                this.vC.Text = info.KC.Substring(10, 10);
                    //                HighlightControl(this.vC);
                    //            });
                    //        }
                    //        catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //    }
                    //}
                    //if (info.Rain.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vRain.Invoke((Action)delegate
                    //        {
                    //            if (m_CurrentStation == null || m_CurrentStation.DRainAccuracy.ToString() == "无")
                    //            {
                    //                AddLog("未选择站点,或者该站点雨量精度不合法");
                    //            }
                    //            else
                    //            {
                    //                decimal rainAccuracy = (decimal)m_CurrentStation.DRainAccuracy;
                    //                this.vRain.Value = info.Rain.Value * rainAccuracy;
                    //                HighlightControl(this.vRain);
                    //            }
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (info.Water.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vWater.Invoke((Action)delegate
                    //        {
                    //            this.vWater.Value = info.Water.Value*(decimal)0.01;
                    //            HighlightControl(this.vWater);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (info.WaterPlusReportedValue.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vWaterPlusReportedValue.Invoke((Action)delegate
                    //        {
                    //            this.vWaterPlusReportedValue.Value = info.WaterPlusReportedValue.Value;
                    //            HighlightControl(this.vWaterPlusReportedValue);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (info.SelectCollectionParagraphs.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vSelectCollectionParagraphs.Invoke((Action)delegate
                    //        {
                    //            this.vSelectCollectionParagraphs.Text = ProtocolMaps.SelectCollectionParagraphs4UIMap.FindValue(info.SelectCollectionParagraphs.Value);
                    //            HighlightControl(this.vSelectCollectionParagraphs);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (info.StationType.HasValue)
                    //{
                    //    try
                    //    {
                    //        this.vStationType.Invoke((Action)delegate
                    //        {
                    //            this.vStationType.Text = ProtocolMaps.StationType4ChineseMap.FindValue(info.StationType.Value);
                    //            HighlightControl(this.vStationType);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (!String.IsNullOrEmpty(info.UserName))
                    //{
                    //    try
                    //    {
                    //        this.OperatingParaModify.Invoke((Action)delegate
                    //        {
                    //            this.OperatingParaModify.Text = info.UserName;
                    //            HighlightControl(this.OperatingParaModify);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    //if (!String.IsNullOrEmpty(info.StationName))
                    //{
                    //    try
                    //    {
                    //        this.vStationName.Invoke((Action)delegate
                    //        {
                    //            this.vStationName.Text = info.StationName;
                    //            HighlightControl(this.vStationName);
                    //        });
                    //    }
                    //    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                    //}
                    #endregion

                    AddLog("接收数据：" + rawData);
                    switch (m_RSStatus)
                    {
                        case EReadOrSetStatus.None:
                            break;
                        case EReadOrSetStatus.Read:
                            SetWarningInfo(string.Format("{0}{1}站点数据成功!", m_CmdStr, m_StatusIDStr), Color.Green);
                            m_RSStatus = EReadOrSetStatus.None;
                            break;
                        case EReadOrSetStatus.Set:
                            break;
                        default: break;
                    }
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
        }

        #region ui logic

        private bool CheckLength(EDownParam param, string text)
        {
            int length = int.Parse(ProtocolMaps.DownParamLengthMap.FindValue(param));
            if (text.Length > length)
            {
                string type = ProtocolMaps.DownParam4ChineseMap.FindValue(param);
                MessageBox.Show(String.Format("{0}不能超过{1}个字符", type, length));
                return false;
            }
            return true;
        }
        /**********  Button   Event Handler **********/
        private void btn_Read_Click(object sender, EventArgs e)
        {
            try
            {
                Init();
                ResetAllControlsBackColor();
                /// 拼接查询字符串
                /// 00010G 03 05 08#13
                ///  站点id
                var station = (this.cmbStation as CStationComboBox).GetStation();
                m_CurrentStation = station;
                if (station == null)
                {
                    MessageBox.Show("请选择站点！");
                    return;
                }
                string sid = station.StationID;
                //  配置参数
                #region 配置参数
                var cmds = new List<EDownParamGY>();
                if (this.chkClock.Checked)
                {
                    cmds.Add(EDownParamGY.Rdata);
                }
                if (this.chkVoltage.Checked)
                {
                    cmds.Add(EDownParamGY.basicConfigRead);
                }
                if (this.chkStationCmdID.Checked)
                {
                    cmds.Add(EDownParamGY.timeFrom_To);
                }
                if (this.chkNormalState.Checked)
                {
                    cmds.Add(EDownParamGY.Selement);
                }
                if (this.chkVersionNum.Checked)
                {
                    cmds.Add(EDownParamGY.version);
                }
                if (this.chkWorkStatus.Checked)
                {
                    cmds.Add(EDownParamGY.basicConfigModify);                                                                                                                                                                                                                                                                                                                                                                                          
                }
                if (this.chkTimePeriod.Checked)
                {
                    cmds.Add(EDownParamGY.operatingParaRead);
                }
                if (this.chkTimeChoice.Checked)
                {
                    cmds.Add(EDownParamGY.ArtifN);
                }
                if (this.chkMainChannel.Checked)
                {
                    cmds.Add(EDownParamGY.oldPwd);
                    cmds.Add(EDownParamGY.newPwd);
                }
                if (this.chkDestPhoneNum.Checked)
                {
                    cmds.Add(EDownParamGY.alarm);
                }
                if (this.chkTeleNum.Checked)
                {
                    cmds.Add(EDownParamGY.history);
                }
                if (this.waterYield.Checked)
                {
                    cmds.Add(EDownParamGY.waterYield);
                }
                if (this.chkTerminalNum.Checked)
                {
                    cmds.Add(EDownParamGY.clocksearch);
                }
                if (this.chkRespBeam.Checked)
                {
                    cmds.Add(EDownParamGY.ICconfig);
                }
                if (this.chkRingsNum.Checked)
                {
                    cmds.Add(EDownParamGY.Reset);
                }
                if (this.memoryReset.Checked)
                {
                    cmds.Add(EDownParamGY.memoryReset);
                }
                if (this.chkAvegTime.Checked)
                {
                    cmds.Add(EDownParamGY.clockset);
                }
                if (this.chkStationType.Checked)
                {
                    cmds.Add(EDownParamGY.pumpCtrl);
                }
                if (this.chkWater.Checked)
                {
                    cmds.Add(EDownParamGY.valveCtrl);
                }
                    //if (this.chkWaterPlusReportedValue.Checked)
                    //{
                    //    cmds.Add(EDownParam.WaterPlusReportedValue);
                    //}
                if (this.chkRain.Checked)
                {
                    cmds.Add(EDownParamGY.gateCtrl);
                }
                    //if (this.chkRainPlusReportedValue.Checked)
                    //{
                    //    cmds.Add(EDownParam.RainPlusReportedValue);
                    //}
                    //if (this.chkK.Checked)
                    //{
                    //    cmds.Add(EDownParam.KC);
                    //}
                    //if (this.chkSelectCollectionParagraphs.Checked)
                    //{
                    //    cmds.Add(EDownParam.SelectCollectionParagraphs);
                    //}
                    if (this.chkUserName.Checked)
                {
                    cmds.Add(EDownParamGY.operatingParaModify);
                }
                if (this.chkStationName.Checked)
                {
                    cmds.Add(EDownParamGY.pumpRead);
                }
                if (cmds.Count == 0)
                {
                    MessageBox.Show("请选择参数!");
                    return;
                }
                #endregion
                //string gprsNum = this.txtGprs.Text;
                //if (String.IsNullOrEmpty(gprsNum))
                //{
                //    MessageBox.Show(this.cmbMsgType.Text + "号码不能为空!");
                //    return;
                //}
                if (this.m_channelType == EChannelType.GPRS)
                {
                    if (!HasUserOnLine())
                    {
                        return;
                    }
                }
                string gprsNum = "00000000";
                string query = CPortDataMgr.Instance.SendReadMsg(gprsNum, sid, cmds, this.m_channelType);
                m_RSStatus = EReadOrSetStatus.Read;
                //  日志记录
                string logMsg = String.Format("--------读取参数    目标站点（{0:D4}）--------- ", int.Parse(sid));
                m_CmdStr = "读取";
                m_StatusIDStr = sid;
                SetWarningInfo(string.Format("{0}{1}命令发出", m_CmdStr, m_StatusIDStr), Color.Blue);
                // 写入系统日志
                CSystemInfoMgr.Instance.AddInfo(logMsg);
                AddLog(logMsg);
                AddLog(String.Format("[{0}] Send: {1,-10}  {2}", m_channelType, "", query));
            }
            catch (Exception exp)
            {
                Debug.WriteLine("参数设置模块:读取数据失败！" + exp.Message);
            }
        }
        /*private void btn_Set_Click(object sender, EventArgs e)
        {
            try
            {
                ResetAllControlsBackColor();
                //  站点id
                var station = (this.cmbStation as CStationComboBox).GetStation();
                m_CurrentStation = station;
                if (station == null)
                {
                    MessageBox.Show("请选择站点！");
                    return;
                }
                string sid = station.StationID;

                var cmds = new List<EDownParam>();

                CDownConf down = new CDownConf();
                //  配置参数
                #region 配置参数
                if (this.chkClock.Checked)
                {
                    cmds.Add(EDownParam.Clock);
                    down.Clock = this.chkLocalTime.Checked ? DateTime.Now : this.vClock.Value;
                }
                if (this.chkVoltage.Checked)
                {
                    MessageBox.Show("电压不允许设置");
                    this.chkVoltage.Checked = false;
                    return;
                }
                if (this.chkStationCmdID.Checked)
                {
                    MessageBox.Show("站号不允许设置");
                    this.chkStationCmdID.Checked = false;
                    return;
                }
                if (this.chkNormalState.Checked)
                {
                    cmds.Add(EDownParam.NormalState);
                    var normalState = this.vNormalState.Text.Trim();
                    if (this.m_vNormalStateLists.Contains(normalState))
                    {
                        down.NormalState = ProtocolMaps.NormalState4UI.FindKey(normalState);
                    }
                    else
                    {
                        MessageBox.Show("常规状态 参数不是合法的!");
                        return;
                    }
                }
                if (this.chkVersionNum.Checked)
                {
                    MessageBox.Show("版本号不允许设置");
                    this.chkVersionNum.Checked = false;
                    return;
                }
                if (this.chkWorkStatus.Checked)
                {
                    cmds.Add(EDownParam.WorkStatus);
                    string workStatus = this.vWorkStatus.Text.Trim();
                    if (this.m_vWorkStatusLists.Contains(workStatus))
                    {
                        down.WorkStatus = ProtocolMaps.WorkStatus4UI.FindKey(workStatus);
                    }
                    else
                    {
                        MessageBox.Show("工作状态 参数不是合法的!");
                        return;
                    }
                }
                if (this.chkTimePeriod.Checked)
                {
                    cmds.Add(EDownParam.TimePeriod);
                    string temp = String.Format("{0:D2}", Int16.Parse(this.vTimePeriod.Text));
                    if (this.m_vTimePeriodLists.Contains(Int16.Parse(this.vTimePeriod.Text.Trim())))
                    {
                        down.TimePeriod = ProtocolMaps.TimePeriodMap.FindKey(temp);
                    }
                    else
                    {
                        MessageBox.Show("定时段次 参数不是合法的!");
                        return;
                    }
                }
                if (this.chkTimeChoice.Checked)
                {
                    cmds.Add(EDownParam.TimeChoice);
                    string temp = this.vTimeChoice.Text.Trim();
                    if (this.m_vTimeChoiceLists.Contains(temp))
                    {
                        down.TimeChoice = ProtocolMaps.TimeChoice4UI.FindKey(temp);
                    }
                    else
                    {
                        MessageBox.Show("对时选择 参数不是合法的!");
                        return;
                    }
                }

                if (this.chkMainChannel.Checked)
                {
                    cmds.Add(EDownParam.StandbyChannel);

                    string mainChannel = this.vMainChannel.Text.Trim();
                    string viceChannel = this.vViceChannel.Text.Trim();
                    if (!this.m_vMainChannelLists.Contains(mainChannel))
                    {
                        MessageBox.Show("主用信道 参数不是合法的!");
                        return;
                    }
                    if (!this.m_vViceChannelLists.Contains(viceChannel))
                    {
                        MessageBox.Show("备用信道 参数不是合法的!");
                        return;
                    }
                    if (mainChannel == viceChannel)
                    {
                        MessageBox.Show("主用信道和备用信道不能同时设为" + vMainChannel.Text);
                        return;
                    }
                    down.MainChannel = ProtocolMaps.ChannelType4UIMap.FindKey(mainChannel);
                    down.ViceChannel = ProtocolMaps.ChannelType4UIMap.FindKey(viceChannel);
                }
                if (this.chkDestPhoneNum.Checked)
                {
                    cmds.Add(EDownParam.DestPhoneNum);
                    string temp = this.vDestPhoneNum.Text.Trim();
                    if (!CStringUtil.IsDigitStrWithSpecifyLength(temp, 11))
                    {
                        MessageBox.Show("目的地手机号码 长度必须为11位，而且是数字!");
                        return;
                    }
                    down.DestPhoneNum = temp;
                }
                if (this.chkTeleNum.Checked)
                {
                    cmds.Add(EDownParam.TeleNum);
                    string temp = this.vTeleNum.Text.Trim();
                    if (!CStringUtil.IsDigit(temp))
                    {
                        MessageBox.Show("SIM卡号 所有位必须全部为数字!");
                        return;
                    }
                    down.TeleNum = temp;
                }
                if (this.chkTerminalNum.Checked)
                {
                    cmds.Add(EDownParam.TerminalNum);
                    string temp = this.vTerminalNum.Text.Trim();
                    if (!CStringUtil.IsDigit(temp))
                    {
                        MessageBox.Show("终端机号 所有位必须全部为数字!");
                        return;
                    }
                    down.TerminalNum = temp;
                }
                if (this.chkRespBeam.Checked)
                {
                    cmds.Add(EDownParam.RespBeam);
                    string temp = this.vRespBeam.Text.Trim();
                    if (!CStringUtil.IsDigit(temp))
                    {
                        MessageBox.Show("响应波束 所有位必须全部为数字!");
                        return;
                    }
                    down.RespBeam = temp;
                }
                if (this.chkRingsNum.Checked)
                {
                    cmds.Add(EDownParam.RingsNum);
                    decimal temp = this.vRingsNum.Value;
                    if (temp <= 0)
                    {
                        MessageBox.Show("振铃次数 必须大于0!");
                        return;
                    }
                    down.RingsNum = temp;
                }

                if (this.chkAvegTime.Checked)
                {
                    cmds.Add(EDownParam.AvegTime);
                    down.AvegTime = Decimal.Parse(this.vAvegTime.Text);
                }
                if (this.chkStationType.Checked)
                {
                    // MessageBox.Show("测站类型不允许设置");
                    cmds.Add(EDownParam.StationType);
                    down.PumpCtrl = this.pumpCtrl.Text;
                    if (this.vStationType.Text == "雨量站")
                        down.StationType = EStationType.ERainFall;
                    if (this.vStationType.Text == "水位站")
                        down.StationType = EStationType.ERiverWater;
                    if (this.vStationType.Text == "水文站")
                        down.StationType = EStationType.EHydrology;
                    //this.chkStationType.Checked = false;
                    //return;
                    
                }
                if (this.chkWater.Checked)
                {
                    cmds.Add(EDownParam.Water);
                    down.Water = this.vWater.Value * (decimal)100;
                    down.ValveCtrl = this.valveCtrl.Text;
                }
                if (this.chkWaterPlusReportedValue.Checked)
                {
                    cmds.Add(EDownParam.WaterPlusReportedValue);
                    down.WaterPlusReportedValue = Decimal.Parse(this.vWaterPlusReportedValue.Text);
                }
                if (this.chkRain.Checked)
                {
                    cmds.Add(EDownParam.Rain);
                    decimal rainAcc = (decimal)m_CurrentStation.DRainAccuracy;
                    if (rainAcc == 0)
                    {
                        MessageBox.Show("站点的雨量精度不能为0!");
                        return;
                    }
                    down.Rain = this.vRain.Value / rainAcc;
                    down.GateCtrl = this.gateCtrl.Text;
                }
                if (this.chkRainPlusReportedValue.Checked)
                {
                    cmds.Add(EDownParam.RainPlusReportedValue);
                    decimal rainAcc = (decimal)m_CurrentStation.DRainAccuracy;
                    if (rainAcc == 0)
                    {
                        MessageBox.Show("站点的雨量精度不能为0!");
                        return;
                    }
                    down.RainPlusReportedValue = this.vRainPlusReportedValue.Value / rainAcc;
                }
                if (this.chkK.Checked)
                {
                    string k = this.vK.Text.Trim();
                    string c = this.vC.Text.Trim();
                    if (!CStringUtil.IsDigitStrWithSpecifyLength(k, 10))
                    {
                        MessageBox.Show("K值长度必须为10位数字!");
                        return;
                    }
                    if (!CStringUtil.IsDigitStrWithSpecifyLength(c, 10))
                    {
                        MessageBox.Show("C值长度必须为10位数字!");
                        return;
                    }
                    cmds.Add(EDownParam.KC);
                    down.KC = this.vK.Text + this.vC.Text;
                }
                if (this.chkSelectCollectionParagraphs.Checked)
                {
                    string temp = this.vSelectCollectionParagraphs.Text;
                    if (!this.m_vSelectCollectionParagraphsLists.Contains(temp))
                    {
                        MessageBox.Show("采集段次 参数不是合法的!");
                        return;
                    }
                    cmds.Add(EDownParam.SelectCollectionParagraphs);
                    down.SelectCollectionParagraphs = ProtocolMaps.SelectCollectionParagraphs4UIMap.FindKey(temp);
                }
                if (this.chkUserName.Checked)
                {
                    string temp = this.vUserName.Text;
                    if (!CStringUtil.IsDigitOrAlpha(temp))
                    {
                        MessageBox.Show("用户名只能为字母或者数字！");
                        return;
                    }
                    cmds.Add(EDownParam.UserName);
                    down.UserName = temp;
                }
                if (this.chkStationName.Checked)
                {
                    string temp = this.vStationName.Text;
                    if (!CStringUtil.IsDigitOrAlpha(temp))
                    {
                        MessageBox.Show("测站名只能为字母或者数字！");
                        return;
                    }
                    cmds.Add(EDownParam.StationName);
                    down.StationName = temp;
                }
                if (cmds.Count == 0)
                {
                    MessageBox.Show("请选择参数!");
                    return;
                }
                #endregion
                string gprsNum = this.txtGprs.Text;
                if (String.IsNullOrEmpty(gprsNum))
                {
                    MessageBox.Show(this.cmbMsgType.Text + "号码不能为空!");
                    return;
                }
                if (this.m_channelType == EChannelType.GPRS)
                {
                    if (!HasUserOnLine())
                    {
                        return;
                    }
                }

                string query = CPortDataMgr.Instance.SendSetMsg(gprsNum, sid, cmds, down, this.m_channelType);

                m_RSStatus = EReadOrSetStatus.Set;
                m_CmdStr = "设置";
                m_StatusIDStr = sid;
                SetWarningInfo(string.Format("{0}{1}命令发出", m_CmdStr, m_StatusIDStr), Color.Blue);
                // 写入系统日志
                string logMsg = String.Format("--------设置参数    目标站点（{0:D4}）--------- ", int.Parse(sid));
                CSystemInfoMgr.Instance.AddInfo(logMsg);
                this.listView1.Items.Add(logMsg);
                AddLog(String.Format("[{0}] Send: {1,-10}  {2}", m_channelType, "", query));
                m_RSStatus = EReadOrSetStatus.Set;

            }
            catch (Exception exp)
            {
                Debug.WriteLine("参数设置模块:设置参数失败！" + exp.Message);
            }
        }*/





        private void btn_Set_Click(object sender, EventArgs e)
        {
            try
            {
                ResetAllControlsBackColor();
                //  站点id
                var station = (this.cmbStation as CStationComboBox).GetStation();
                m_CurrentStation = station;
                if (station == null)
                {
                    MessageBox.Show("请选择站点！");
                    return;
                }
                string sid = station.StationID;

                var cmds = new List<EDownParam>();
                var cmdsGY = new List<EDownParamGY>();

                CDownConf down = new CDownConf();
                CDownConfGY downGY = new CDownConfGY();

                //  配置参数
                #region 配置参数
                if (this.chkClock.Checked)
                {
                    cmds.Add(EDownParam.Clock);
                    down.Clock = this.chkLocalTime.Checked ? DateTime.Now : this.vClock.Value;
                }
                if (this.chkVoltage.Checked)
                {
                    MessageBox.Show("电压不允许设置");
                    this.chkVoltage.Checked = false;
                    return;
                }
                if (this.chkStationCmdID.Checked)
                {
                    MessageBox.Show("站号不允许设置");
                    this.chkStationCmdID.Checked = false;
                    return;
                }
                if (this.chkNormalState.Checked)
                {
                    cmds.Add(EDownParam.NormalState);
                    var normalState = this.vNormalState.Text.Trim();
                    if (this.m_vNormalStateLists.Contains(normalState))
                    {
                        down.NormalState = ProtocolMaps.NormalState4UI.FindKey(normalState);
                    }
                    else
                    {
                        MessageBox.Show("常规状态 参数不是合法的!");
                        return;
                    }
                }
                if (this.chkVersionNum.Checked)
                {
                    MessageBox.Show("版本号不允许设置");
                    this.chkVersionNum.Checked = false;
                    return;
                }
                //增加定值水量控制
                if (this.waterYield.Checked)
                {
                    cmdsGY.Add(EDownParamGY.waterYield);
                    downGY.WaterYield = this.vVersionNum.Text;
                }
                if (this.chkWorkStatus.Checked)
                {
                    cmds.Add(EDownParam.WorkStatus);
                    string workStatus = this.vWorkStatus.Text.Trim();
                    if (this.m_vWorkStatusLists.Contains(workStatus))
                    {
                        down.WorkStatus = ProtocolMaps.WorkStatus4UI.FindKey(workStatus);
                    }
                    else
                    {
                        MessageBox.Show("工作状态 参数不是合法的!");
                        return;
                    }

                    cmdsGY.Add(EDownParamGY.basicConfigModify);
                    downGY.BasicConfigModify = this.basicconfig.Text;

                }
                if (this.chkTimePeriod.Checked)
                {
                    cmds.Add(EDownParam.TimePeriod);
                    string temp = String.Format("{0:D2}", Int16.Parse(this.vTimePeriod.Text));
                    if (this.m_vTimePeriodLists.Contains(Int16.Parse(this.vTimePeriod.Text.Trim())))
                    {
                        down.TimePeriod = ProtocolMaps.TimePeriodMap.FindKey(temp);
                    }
                    else
                    {
                        MessageBox.Show("定时段次 参数不是合法的!");
                        return;
                    }
                }
                if (this.chkTimeChoice.Checked)
                {
                    cmds.Add(EDownParam.TimeChoice);
                    string temp = this.vTimeChoice.Text.Trim();
                    if (this.m_vTimeChoiceLists.Contains(temp))
                    {
                        down.TimeChoice = ProtocolMaps.TimeChoice4UI.FindKey(temp);
                    }
                    else
                    {
                        MessageBox.Show("对时选择 参数不是合法的!");
                        return;
                    }
                }

                if (this.chkMainChannel.Checked)
                {
                    cmds.Add(EDownParam.StandbyChannel);

                    string mainChannel = this.vMainChannel.Text.Trim();
                    string viceChannel = this.vViceChannel.Text.Trim();
                    if (!this.m_vMainChannelLists.Contains(mainChannel))
                    {
                        MessageBox.Show("主用信道 参数不是合法的!");
                        return;
                    }
                    if (!this.m_vViceChannelLists.Contains(viceChannel))
                    {
                        MessageBox.Show("备用信道 参数不是合法的!");
                        return;
                    }
                    if (mainChannel == viceChannel)
                    {
                        MessageBox.Show("主用信道和备用信道不能同时设为" + vMainChannel.Text);
                        return;
                    }
                    down.MainChannel = ProtocolMaps.ChannelType4UIMap.FindKey(mainChannel);
                    down.ViceChannel = ProtocolMaps.ChannelType4UIMap.FindKey(viceChannel);

                    cmdsGY.Add(EDownParamGY.oldPwd);
                    downGY.OldPwd = this.oldPwd.Text;
                    cmdsGY.Add(EDownParamGY.newPwd);
                    downGY.NewPwd = this.newPwd.Text;
                }
                if (this.chkDestPhoneNum.Checked)
                {
                    cmds.Add(EDownParam.DestPhoneNum);
                    string temp = this.vDestPhoneNum.Text.Trim();
                    if (!CStringUtil.IsDigitStrWithSpecifyLength(temp, 11))
                    {
                        MessageBox.Show("目的地手机号码 长度必须为11位，而且是数字!");
                        return;
                    }
                    down.DestPhoneNum = temp;
                }
                if (this.chkTeleNum.Checked)
                {
                    cmds.Add(EDownParam.TeleNum);
                    string temp = this.vTeleNum.Text.Trim();
                    if (!CStringUtil.IsDigit(temp))
                    {
                        MessageBox.Show("SIM卡号 所有位必须全部为数字!");
                        return;
                    }
                    down.TeleNum = temp;
                }
                if (this.chkTerminalNum.Checked)
                {
                    cmds.Add(EDownParam.TerminalNum);
                    string temp = this.vClockTime.Text.Trim();
                    if (!CStringUtil.IsDigit(temp))
                    {
                        MessageBox.Show("终端机号 所有位必须全部为数字!");
                        return;
                    }
                    down.TerminalNum = temp;
                }
                if (this.chkRespBeam.Checked)
                {
                    cmds.Add(EDownParam.RespBeam);
                    string temp = this.ICset.Text.Trim();
                    if (!CStringUtil.IsDigit(temp))
                    {
                        MessageBox.Show("响应波束 所有位必须全部为数字!");
                        return;
                    }
                    down.RespBeam = temp;

                    cmdsGY.Add(EDownParamGY.ICconfig);
                    downGY.ICconfig = this.ICset.Text;
                }
                if (this.chkRingsNum.Checked)
                {
                    cmds.Add(EDownParam.RingsNum);
                    decimal temp = this.vRingsNum.Value;
                    if (temp <= 0)
                    {
                        MessageBox.Show("振铃次数 必须大于0!");
                        return;
                    }
                    down.RingsNum = temp;
                }

                if (this.chkAvegTime.Checked)
                {
                    cmds.Add(EDownParam.AvegTime);
                    //down.AvegTime = Decimal.Parse(this.vAvegTime.Text);
                    cmdsGY.Add(EDownParamGY.clockset);
                }
                if (this.chkStationType.Checked)
                {
                    // MessageBox.Show("测站类型不允许设置");
                    cmds.Add(EDownParam.StationType);

                    cmdsGY.Add(EDownParamGY.pumpCtrl);
                    downGY.PumpCtrl = this.pumpCtrl.Text;

                    //if (this.vStationType.Text == "雨量站")
                    //    down.StationType = EStationType.ERainFall;
                    //if (this.vStationType.Text == "水位站")
                    //    down.StationType = EStationType.ERiverWater;
                    //if (this.vStationType.Text == "水文站")
                    //    down.StationType = EStationType.EHydrology;
                    //this.chkStationType.Checked = false;
                    //return;

                }
                if (this.chkWater.Checked)
                {
                    //cmds.Add(EDownParam.Water);
                    //down.Water = this.vWater.Value * (decimal)100;

                    cmdsGY.Add(EDownParamGY.valveCtrl);
                    downGY.ValveCtrl = this.valveCtrl.Text;
                }
                if (this.chkWaterPlusReportedValue.Checked)
                {
                    cmds.Add(EDownParam.WaterPlusReportedValue);
                    down.WaterPlusReportedValue = Decimal.Parse(this.vWaterPlusReportedValue.Text);
                }
                if (this.chkRain.Checked)
                {
                    cmds.Add(EDownParam.Rain);
                    decimal rainAcc = (decimal)m_CurrentStation.DRainAccuracy;
                    if (rainAcc == 0)
                    {
                        MessageBox.Show("站点的雨量精度不能为0!");
                        return;
                    }
                    //down.Rain = this.vRain.Value / rainAcc;


                    cmdsGY.Add(EDownParamGY.gateCtrl);
                    downGY.GateCtrl = this.gateCtrl.Text;
                }
                if (this.chkRainPlusReportedValue.Checked)
                {
                    cmds.Add(EDownParam.RainPlusReportedValue);
                    decimal rainAcc = (decimal)m_CurrentStation.DRainAccuracy;
                    if (rainAcc == 0)
                    {
                        MessageBox.Show("站点的雨量精度不能为0!");
                        return;
                    }
                    down.RainPlusReportedValue = this.vRainPlusReportedValue.Value / rainAcc;
                }
                if (this.chkK.Checked)
                {
                    string k = this.vK.Text.Trim();
                    string c = this.vC.Text.Trim();
                    if (!CStringUtil.IsDigitStrWithSpecifyLength(k, 10))
                    {
                        MessageBox.Show("K值长度必须为10位数字!");
                        return;
                    }
                    if (!CStringUtil.IsDigitStrWithSpecifyLength(c, 10))
                    {
                        MessageBox.Show("C值长度必须为10位数字!");
                        return;
                    }
                    cmds.Add(EDownParam.KC);
                    down.KC = this.vK.Text + this.vC.Text;
                }
                if (this.chkSelectCollectionParagraphs.Checked)
                {
                    string temp = this.vSelectCollectionParagraphs.Text;
                    if (!this.m_vSelectCollectionParagraphsLists.Contains(temp))
                    {
                        MessageBox.Show("采集段次 参数不是合法的!");
                        return;
                    }
                    cmds.Add(EDownParam.SelectCollectionParagraphs);
                    down.SelectCollectionParagraphs = ProtocolMaps.SelectCollectionParagraphs4UIMap.FindKey(temp);
                }
                if (this.chkUserName.Checked)
                {
                    string temp = this.OperatingParaModify.Text;
                    if (!CStringUtil.IsDigitOrAlpha(temp))
                    {
                        MessageBox.Show("用户名只能为字母或者数字！");
                        return;
                    }
                    cmds.Add(EDownParam.UserName);
                    down.UserName = temp;

                    cmdsGY.Add(EDownParamGY.operatingParaModify);
                    downGY.OperatingParaModify = this.OperatingParaModify.Text;

                }
                if (this.chkStationName.Checked)
                {
                    string temp = this.vStationName.Text;
                    if (!CStringUtil.IsDigitOrAlpha(temp))
                    {
                        MessageBox.Show("测站名只能为字母或者数字！");
                        return;
                    }
                    cmds.Add(EDownParam.StationName);
                    down.StationName = temp;
                }
                //if (cmds.Count == 0)
                //{
                //    MessageBox.Show("请选择参数!");
                //    return;
                //}
                #endregion
                //string gprsNum = this.txtGprs.Text;
                /*if (String.IsNullOrEmpty(gprsNum))
                {
                    MessageBox.Show(this.cmbMsgType.Text + "号码不能为空!");
                    return;
                }*/
                if (this.m_channelType == EChannelType.GPRS)
                {
                    if (!HasUserOnLine())
                    {
                        return;
                    }
                }

                string query = CPortDataMgr.Instance.SendSetMsgGY("", sid, cmdsGY, downGY, this.m_channelType);

                m_RSStatus = EReadOrSetStatus.Set;
                m_CmdStr = "设置";
                m_StatusIDStr = sid;
                SetWarningInfo(string.Format("{0}{1}命令发出", m_CmdStr, m_StatusIDStr), Color.Blue);
                // 写入系统日志
                string logMsg = String.Format("--------设置参数    目标站点（{0:D4}）--------- ", int.Parse(sid));
                CSystemInfoMgr.Instance.AddInfo(logMsg);
                this.listView1.Items.Add(logMsg);
                AddLog(String.Format("[{0}] Send: {1,-10}  {2}", m_channelType, "", query));
                m_RSStatus = EReadOrSetStatus.Set;

            }
            catch (Exception exp)
            {
                Debug.WriteLine("参数设置模块:设置参数失败！" + exp.Message);
            }
        }







        private void btn_Exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private bool HasUserOnLine()
        {
            if (this.cmbMsgType.Text == "GPRS" && CPortDataMgr.Instance.GPRSCurrentOnlineUserCount == 0)
            {
                MessageBox.Show("GPRS ： 当前用户登录个数为0，请耐心等待用户登录！");
                return false;
            }
            return true;
        }

        /**********  CheckBox Event Handler **********/
        private void AllSelect_CheckedChanged(object sender, EventArgs e)
        {
            var chk = sender as CheckBox;
            bool isChecked = chk.Checked;

            foreach (var control in this.WorkGroupBox.Controls)
            {
                var chkbox = control as CheckBox;
                if (chkbox == null || chkbox.Tag == null)
                    continue;
                if (chkbox.Tag.ToString().Equals("key"))
                {
                    chkbox.Checked = isChecked;
                }
            }
            foreach (var control in this.CommGroupBox.Controls)
            {
                var chkbox = control as CheckBox;
                if (chkbox == null || chkbox.Tag == null)
                    continue;
                if (chkbox.Tag.ToString().Equals("key"))
                {
                    chkbox.Checked = isChecked;
                }
            }
            foreach (var control in this.SensorGroupBox.Controls)
            {
                var chkbox = control as CheckBox;
                if (chkbox == null || chkbox.Tag == null)
                    continue;
                if (chkbox.Tag.ToString().Equals("key"))
                {
                    chkbox.Checked = isChecked;
                }
            }
        }
        private void chkLocalTime_CheckedChanged(object sender, EventArgs e)
        {
            this.vClock.Enabled = !this.chkLocalTime.Checked;
        }

        /**********  ComboBox Event Handler **********/
        private void CReadAndSettingMgrForm_StationSelected(object sender, CEventSingleArgs<CEntityStation> e)
        {
            var station = e.Value;
            if (station == null)
                return;

            string txt = this.cmbMsgType.Text;
            //if (txt == EChannelType.GPRS.ToString())
            //    this.txtGprs.Text = station.GPRS;
            //else if (txt == EChannelType.GSM.ToString())
            //    this.txtGprs.Text = station.GSM;
            //else if (txt == EChannelType.BeiDou.ToString())
            //    this.txtGprs.Text = station.BDSatellite;
            //else if (txt == EChannelType.PSTN.ToString())
            //    ;
            //      this.txtGprs.Text = station.PSTV;
        }
        private void cmbMsgType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var station = (this.cmbStation as CStationComboBox).GetStation();
            if (station == null)
                return;
            string sid = station.StationID;

            var txt = this.cmbMsgType.Text;
            if (txt == EChannelType.GPRS.ToString())
            {
                this.lblGprs.Text = "GPRS号码:";
                m_channelType = EChannelType.GPRS;
                //this.txtGprs.Text = station.GPRS;
            }
            else if (txt == EChannelType.GSM.ToString())
            {
                this.lblGprs.Text = "GSM号码:";
                m_channelType = EChannelType.GSM;
                //this.txtGprs.Text = station.GSM;
            }
            else if (txt == EChannelType.TCP.ToString())
            {
                this.lblGprs.Text = "TCP:";
                m_channelType = EChannelType.TCP;
                //this.txtGprs.Text = station;
            }
            else if (txt == EChannelType.BeiDou.ToString())
            {
                this.lblGprs.Text = "北斗卫星号码:";
                m_channelType = EChannelType.BeiDou;
                //this.txtGprs.Text = station.BDSatellite;
            }
            else if (txt == EChannelType.PSTN.ToString())
            {
                this.lblGprs.Text = "PSTN号码:";
                this.m_channelType = EChannelType.PSTN;
                //   this.txtGprs.Text = station.PSTV;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (this.listView1.Items.Count > 0)
                this.listView1.Items.Clear();
        }
        #endregion

        private void SetWarningInfo(string text, Color color)
        {
            if (!this.IsHandleCreated)
                return;
            this.lblWarning.Invoke((Action)delegate
            {
                try
                {
                    this.lblWarning.Text = text;
                    this.lblWarning.ForeColor = color;
                }
                catch (Exception exp) { }
            });
        }
        private void AddLog(string text)
        {
            if (!this.IsHandleCreated)
                return;
            this.listView1.Invoke((Action)delegate
            {
                try
                {
                    this.listView1.Items.Add(text);
                    int index = this.listView1.Items.Count - 1;
                    if (index >= 0)
                        this.listView1.Items[index].EnsureVisible();
                }
                catch (Exception exp) { Debug.WriteLine(exp.Message); }
            });
        }

        private void ResetAllControlsBackColor()
        {
            UnHighlightGroupBox(this.SensorGroupBox);
            UnHighlightGroupBox(this.CommGroupBox);
            UnHighlightGroupBox(this.WorkGroupBox);
            this.label19.Hide();
        }

        private void UnHighlightGroupBox(GroupBox grpBox)
        {
            foreach (var item in grpBox.Controls)
            {
                Control ctl = item as Control;
                if (item != null)
                {
                    UnHighlightControl(ctl);

                }
            }
        }

        private void UnHighlightControl(Control control)
        {
            var cmb = control as CheckBox;
            if (cmb == null)
            {
                control.BackColor = System.Drawing.SystemColors.Window;
            }
            else
            {
                control.BackColor = System.Drawing.SystemColors.Control;
            }
            control.ForeColor = System.Drawing.SystemColors.WindowText;

            var lbl = control as Label;
            if (lbl != null)
            {
                lbl.BackColor = System.Drawing.SystemColors.ButtonFace;
            }

            if (Object.ReferenceEquals(control, this.vVoltage))
                this.vVoltage.Enabled = false;
            if (Object.ReferenceEquals(control, this.vStationCmdID))
                this.vStationCmdID.Enabled = false;
            if (Object.ReferenceEquals(control, this.vVersionNum))
                this.vVersionNum.Enabled = false;
            //if (Object.ReferenceEquals(control, this.vStationType))
            //    this.vStationType.Enabled = false;
        }

        private void HighlightControl(Control control)
        {
            control.BackColor = Color.Green;
            control.ForeColor = Color.White;
            if (Object.ReferenceEquals(control, this.vVoltage))
                this.vVoltage.Enabled = true;
            if (Object.ReferenceEquals(control, this.vStationCmdID))
                this.vStationCmdID.Enabled = true;
            if (Object.ReferenceEquals(control, this.vVersionNum))
                this.vVersionNum.Enabled = true;
            //if (Object.ReferenceEquals(control, this.vStationType))
            //    this.vStationType.Enabled = true;
        }

        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var cmb = sender as ComboBox;
            UnHighlightControl(cmb);
        }

        internal enum EReadOrSetStatus
        {
            Read,
            Set,
            None
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string cmd = this.vCustomCmd.Text;
            if (String.IsNullOrEmpty(cmd))
            {
                MessageBox.Show("发送命令不能为空!");
                return;
            }
            cmd += "\r\n";
            //string gprsNum = this.txtGprs.Text;
            //if (String.IsNullOrEmpty(gprsNum))
            //{
            //    MessageBox.Show(this.lblGprs.Text.Replace(":", "") + "号码不能为空!");
            //    return;
            //}

            var station = (this.cmbStation as CStationComboBox).GetStation();
            m_CurrentStation = station;
            if (station == null)
            {
                MessageBox.Show("请选择站点！");
                return;
            }
            string sid = station.StationID;

            string logMsg = String.Format("发送命令 :  {0}", cmd);
            // 写入系统日志
            CSystemInfoMgr.Instance.AddInfo(logMsg);
            AddLog(logMsg);

            CPortDataMgr.Instance.SendMsg("", sid, cmd, this.m_channelType);
        }

        private string HexString2BinString(string hexString)
        {
            string result = string.Empty;
            foreach (char c in hexString)
            {
                int v = Convert.ToInt32(c.ToString(), 16);
                int v2 = int.Parse(Convert.ToString(v, 2));
                // 去掉格式串中的空格，即可去掉每个4位二进制数之间的空格，
                result += string.Format("{0:d4} ", v2);
            }
            return result;
        }

        private void chkTimePeriod_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void chkStationCmdID_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void chkDestPhoneNum_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void chkTerminalNum_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void pumpCtrl_TextChanged(object sender, EventArgs e)
        {

        }
    }
}