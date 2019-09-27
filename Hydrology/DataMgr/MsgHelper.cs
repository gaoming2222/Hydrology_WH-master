/************************************************************************************
* Copyright (c) 2019 All Rights Reserved.
*命名空间：Hydrology.DataMgr
*文件名： MsgHelper
*创建人： XXX
*创建时间：2019-8-26 9:59:48
*描述
*=====================================================================
*修改标记
*修改时间：2019-8-26 9:59:48
*修改人：XXX
*描述：
************************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Hydrology.DataMgr
{
    public class MsgHelper
    {
        #region 单件模式
        private static MsgHelper m_sInstance;   //实例指针

        public static MsgHelper Instance
        {
            get { return GetInstance(); }
        }
        public static MsgHelper GetInstance()
        {
            if (m_sInstance == null)
            {
                m_sInstance = new MsgHelper();
            }
            return m_sInstance;
        }
        #endregion ///<单件模式

        #region 常量
        private string url = "http://192.168.10.126";
        private string account = "zaihou";
        private string password = "swj888888";
        #endregion

        public bool sendWarnMsg(string phones,string content)
        {
            bool flag = true;
            string channel = "1";
            string request = "account=" + account + "&password=" + password + "&phones=" + phones + "&content=" + content + "&channel=" + channel;
            try
            {
                string backinfo = HttpPost(url, request);
                return flag;
            }catch(Exception e)
            {
                return false;
            }
            //return flag;
        }

        /// <summary>
        /// 模拟HTTP发送
        /// </summary>
        /// <param name="purl"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        private  string HttpPost(string purl, string str)
        {
            try
            {
                byte[] data = System.Text.Encoding.GetEncoding("GB2312").GetBytes(str);
                // 准备请求 
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(purl);

                //设置超时
                req.Timeout = 30000;
                req.Method = "Post";
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = data.Length;
                Stream stream = req.GetRequestStream();
                // 发送数据 
                stream.Write(data, 0, data.Length);
                stream.Close();

                HttpWebResponse rep = (HttpWebResponse)req.GetResponse();
                Stream receiveStream = rep.GetResponseStream();
                Encoding encode = System.Text.Encoding.GetEncoding("GB2312");
                // Pipes the stream to a higher level stream reader with the required encoding format. 
                StreamReader readStream = new StreamReader(receiveStream, encode);

                Char[] read = new Char[256];
                int count = readStream.Read(read, 0, 256);
                StringBuilder sb = new StringBuilder("");
                while (count > 0)
                {
                    String readstr = new String(read, 0, count);
                    sb.Append(readstr);
                    count = readStream.Read(read, 0, 256);
                }

                rep.Close();
                readStream.Close();

                return sb.ToString();

            }
            catch (Exception ex)
            {
                return "posterror";
                // ForumExceptions.Log(ex);
            }
        }
    }
}