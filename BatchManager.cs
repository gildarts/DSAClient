/*
 * Create Date：2011/04/22
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 
    /// </summary>
    public class BatchManager
    {
        internal Connection _conn = null;
        private int _batch_count;

        /// <summary>
        /// 建立批次呼叫管理物件。
        /// </summary>
        /// <param name="conn"></param>
        internal BatchManager(Connection conn)
        {
            _batch_count = 0;
            _conn = conn;
            BatchID = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 取消此次 Batch 呼叫，清除 Server 的暫存資料。
        /// </summary>
        public void Cancel()
        {
            XmlHelper req = new XmlHelper();
            req.AddElement(".", "BatchID", BatchID);
            _conn.SendRequest("DS.Base.CancelBatch", new Envelope(req));
        }

        /// <summary>
        /// 完成 Batch 呼叫，Server 將確認所有呼叫。
        /// </summary>
        public void Complete()
        {
            XmlHelper req = new XmlHelper();
            req.AddElement(".", "BatchID", BatchID);
            _conn.SendRequest("DS.Base.Commit", new Envelope(req));
        }

        /// <summary>
        /// 將 Batch 資訊加入到 Header 中。
        /// </summary>
        /// <param name="env"></param>
        internal void AddBatchHeader(Envelope env)
        {
            XmlHelper header = new XmlHelper("<Parameter/>");
            header.AddElement(".", "BatchID", BatchID);
            header.AddElement(".", "BatchNumber", NextSequence());
            env.Headers.Add(header);
        }

        /// <summary>
        /// 批次識別號。
        /// </summary>
        internal string BatchID { get; set; }

        /// <summary>
        /// 取得 Batch 的順序號碼。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private string NextSequence()
        {
            return (_batch_count++).ToString();
        }
    }
}
