using System;
using System.Collections.Generic;
using Hydrology.Entity;

namespace Hydrology.Entity
{
    /// <summary>
    /// 查询的结果出来的事件
    /// </summary>
    public class CEventDBUIDataReadyArgs : EventArgs
    {
        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPageIndex { get; set; }

        /// <summary>
        /// 总共页码
        /// </summary>
        public int TotalPageCount { get; set; }

        /// <summary>
        /// 总共记录数
        /// </summary>
        public int TotalRowCount { get; set; }
    }

    /// <summary>
    /// 收到发送过来的单条数据的参数
    /// </summary>
    public class CEventRecvStationDataArgs : EventArgs
    {
        /// <summary>
        /// 测站ID,唯一编号
        /// </summary>
        public string StrStationID;

        /// <summary>
        /// 测站类型
        /// </summary>
        public EStationType EStationType;

        /// <summary>
        /// 消息类型
        /// </summary>
        public EMessageType EMessageType;

        /// <summary>
        /// 通讯方式类型
        /// </summary>
        public EChannelType EChannelType;

        /// <summary>
        /// 水位
        /// </summary>
        public Nullable<Decimal> WaterStage;

        /// <summary>
        /// 流量，仪器读取，需要计算，才能得到有用数值
        /// </summary>
        public Nullable<Decimal> TotalRain;

        /// <summary>
        /// 电压
        /// </summary>
        public Decimal Voltage;

        /// <summary>
        /// 采集时间
        /// </summary>
        public DateTime DataTime;

        /// <summary>
        /// 接收到数据的系统时间
        /// </summary>
        public DateTime RecvDataTime;

        /// <summary>
        /// 串口
        /// </summary>
        public string StrSerialPort;
    }

    /// <summary>
    /// 收到发送过来的多条数据的参数
    /// </summary>
    public class CEventRecvStationDatasArgs : EventArgs
    {
        /// <summary>
        /// 测站ID,唯一编号
        /// </summary>
        public string StrStationID;

        /// <summary>
        /// 测站类型
        /// </summary>
        public EStationType EStationType;

        /// <summary>
        /// 消息类型
        /// </summary>
        public EMessageType EMessageType;

        /// <summary>
        /// 通讯方式类型
        /// </summary>
        public EChannelType EChannelType;

        /// <summary>
        /// 数据值，最后一个的值为最新的值
        /// </summary>
        private List<CSingleStationData> m_listStationData;
        public List<CSingleStationData> Datas
        {
            get
            {
                if (m_listStationData == null)
                {
                    m_listStationData = new List<CSingleStationData>();
                }
                return m_listStationData;
            }
            set
            {
                // list 拷贝
                m_listStationData = new List<CSingleStationData>(value.ToArray());
            }
        }

        /// <summary>
        /// 接收到数据的系统时间
        /// </summary>
        public DateTime RecvDataTime;

        /// <summary>
        /// 串口
        /// </summary>
        public string StrSerialPort;
    }

    public class CTextInfo
    {
        /// <summary>
        /// 信息时间
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// 信息内容
        /// </summary>
        public String Info { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public ETextMsgState EState { get; set; }
    }
    /// <summary>
    /// 单站单条数据记录
    /// </summary>
    public class CSingleStationData
    {
        /// <summary>
        /// 水位
        /// </summary>
        public Nullable<Decimal> WaterStage;

        /// <summary>
        /// 流量
        /// </summary>
        public Nullable<Decimal> WaterFlow;

        /// <summary>
        /// 流量，仪器读取，需要计算，才能得到有用数值
        /// </summary>
        public Nullable<Decimal> TotalRain;

        /// <summary>
        /// 流量，仪器读取，需要计算，才能得到有用数值
        /// </summary>
        public Nullable<Decimal> DiffRain;

        /// <summary>
        /// 时段雨量
        /// </summary>
        public Nullable<Decimal> PeriodRain;

        /// <summary>
        /// 时段雨量
        /// </summary>
        public Nullable<Decimal> CurrentRain;

        /// <summary>
        /// 电压
        /// </summary>
        public Nullable<Decimal> Voltage;

        /// <summary>
        /// 采集时间
        /// </summary>
        public DateTime DataTime;
    }

    /// <summary>
    /// 串口状态
    /// </summary>
    public class CSerialPortState
    {
        /// <summary>
        /// 串口号
        /// </summary>
        public int PortNumber { get; set; }

        /// <summary>
        /// 是否正常
        /// </summary>
        public bool BNormal { get; set; }

        public EListeningProtType PortType { get; set; }
    }

    /// <summary>
    /// 单个数值的消息参数
    /// </summary>
    /// <typeparam name="T">实际参数类型</typeparam>
    public class CEventSingleArgs<T> : EventArgs
    {
        private T m_iValue;
        public CEventSingleArgs(T args)
            : base()
        {
            m_iValue = args;
        }
        public T Value
        {
            get { return m_iValue; }
        }
    }

    /// <summary>
    /// 当前墒情数据参数
    /// </summary>
    public class CSingleSoilDataArgs : EventArgs
    {
        /// <summary>
        /// 站点id, 需自己读数据库，将终端机号和站点ID进行映射匹配
        /// </summary>
        public string StrStationId { get; set; }

        /// <summary>
        /// 日期时间
        /// </summary>
        public DateTime DataTime { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public EMessageType EMessageType { get; set; }

        /// <summary>
        /// 通讯方式类型
        /// </summary>
        public EChannelType EChannelType { get; set; }

        ///电压值
        public Nullable<Decimal> Voltage { get; set; }

        /// <summary>
        /// 十厘米处的电压
        /// </summary>
        public Nullable<float> D10Value { get; set; }

        /// <summary>
        /// 二十厘米处的电压
        /// </summary>
        public Nullable<float> D20Value { get; set; }

        /// <summary>
        /// 三十厘米处的电压
        /// </summary>
        public Nullable<float> D30Value { get; set; }

        /// <summary>
        /// 四十厘米处的含水量
        /// </summary>
        public Nullable<float> D40Value { get; set; }

        /// <summary>
        /// 六十厘米处的电压
        /// </summary>
        public Nullable<float> D60Value { get; set; }
    }
}
