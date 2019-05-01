using System;
using System.Collections.Generic;
using Hydrology.Entity;

namespace Hydrology.DBManager.Interface
{
    /// <summary>
    /// 电压表的代理接口抽象
    /// </summary>
    public interface ICurrentDataProxy : IMultiThread
    {
        void AddNewRow(CEntityRealTime realtime);

        // 异步添加新的一个实时记录
        void AddNewRows(List<CEntityRealTime> realtimes);

        // 异步添加新的一个实时记录,不需等待1分钟
        void AddNewRow_1(CEntityRealTime realtime);

        void AddNewRows_1(List<CEntityRealTime> realtimes);

        bool DeleteRows(List<CEntityRealTime> realtimes);

        /// <summary>
        /// 更新数据行
        /// </summary>
        /// <param name="rains"></param>
        /// <returns></returns>
        bool UpdateRows(List<CEntityRealTime> realtimes);

        bool UpdateRow(CEntityRealTime realtime);
    }
}
