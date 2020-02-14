/************************************************************************************
* Copyright (c) 2018 All Rights Reserved.
*命名空间：Entity.Protocol.Channel
*文件名： CRouter
*创建人： XXX
*创建时间：2018-12-11 10:46:07
*描述
*=====================================================================
*修改标记
*修改时间：2018-12-11 10:46:07
*修改人：XXX
*描述：
************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Entity.Protocol.Channel
{
    public class CRouter
    {
        public int dataLength { get; set; }

        public string dutid { get; set; }

        public string sessionid { get; set; }

        public string data { get; set; }

        public byte[] rawData { get; set; }

        public byte[] receiveTime { get; set; }

        public int port { get; set; }
    }
}