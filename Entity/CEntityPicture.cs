/************************************************************************************
* Copyright (c) 2019 All Rights Reserved.
*命名空间：Entity
*文件名： CEntityPicture
*创建人： XXX
*创建时间：2019-10-31 10:52:49
*描述
*=====================================================================
*修改标记
*修改时间：2019-10-31 10:52:49
*修改人：XXX
*描述：
************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Entity
{
    public class CEntityPicture
    {
        /// <summary>
        ///  测站中心的ID
        /// </summary>
        public string StationID { get; set; }

        public string serialNum { get; set; }

        public string picMsg { get; set; }
    }
}