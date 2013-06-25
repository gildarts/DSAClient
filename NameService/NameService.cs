/*
 * Create Date：2010/10/29
 * Author Name：YaoMing Huang
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.IO;
using FISCA.DSAClient.HttpUtil;

namespace FISCA.DSAClient
{
    /// <summary>
    /// 代表 Name Server 的功能。此類別有預設一組 Name Server 清單。
    /// </summary>
    public class NameService
    {
        #region Load Name Server List
        /// <summary>
        /// 具有 Name  Service 的 AccessPoint 清單。可透過在應用程式目錄下放置一個「NameServiceProvider.txt」檔案自定清單，
        /// 裡面列出自定的 AccessPoint 位置清單，每行放置一個 HTTP Url 位置即可。如果無法從自定清單中載入任何 AccessPoint，
        /// 會自動載入預設清單。
        /// </summary>
        public static IList<string> ProviderList { get; private set; }

        static NameService()
        {
            List<string> nslist = new List<string>();
            ParallelResolve = false;
            PrintResolveMessage = false;

            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NameServiceProvider.txt");
            if (File.Exists(file))
            {
                //載入 Name Server 清單。
                using (TextReader reader = new StreamReader(new FileStream(file, FileMode.Open), Encoding.UTF8))
                {
                    while (reader.Peek() > 0) nslist.Add(reader.ReadLine());
                }
            }

            if (nslist.Count <= 0) //如果沒有載入到任何 Name Server 就載入預設的。
            {   //載入 Name Server 清單。
                using (TextReader reader = new StringReader(CommonResources.DSNSServerList))
                {
                    while (reader.Peek() > 0) nslist.Add(reader.ReadLine());
                }
            }
            ProviderList = nslist.AsReadOnly();
        }
        #endregion

        /// <summary>
        /// 取得或設定是否進行平行解析 DSNS 名稱，如果平行解析，則會同時對清單中所有 Name Server 送出解析的 Request，
        /// 然後將最先回應的結果回傳，非平行解析則會依序送出解析 Request，回傳第一個成功的解析結果，非平行解析
        /// 適用在不方便建立多執行緒的環境，例如 ASP.NET 網頁，預設值為 False。
        /// </summary>
        public static bool ParallelResolve { get; set; }

        /// <summary>
        /// 取得或設定是否輸出解析偵錯訊息(詳細資訊請參考 System.Diagnostics.Trace 類別)。
        /// 預設值為 False。
        /// </summary>
        public static bool PrintResolveMessage { get; set; }

        /// <summary>
        /// 解析 DSNS 名稱。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="DSAMultipleErrorException">當所有解析動作都失敗時丟出。</exception>
        public static string Resolve(string name)
        {
            if (ParallelResolve)
                return ResolveParallel(name);
            else
            {
                List<Exception> errors = new List<Exception>();
                foreach (string nameserver in ProviderList)
                {
                    NameResolver resolver = new NameResolver(nameserver, name);
                    resolver.Resolve();

                    if (resolver.Success)
                    {
                        string msg = string.Format("DSName Resolve：{0}:{1} on {2}",
                            name,
                            resolver.ResolveResult,
                            resolver.NameServer);
                        System.Diagnostics.Trace.WriteLineIf(PrintResolveMessage, msg);
                        return resolver.ResolveResult;
                    }
                    else
                        errors.Add(resolver.Error);
                }

                //當沒有任何一台解析成功時丟出。
                throw new DSAMultipleErrorException(string.Format("解析名稱失敗：{0}", name), errors.AsReadOnly());
            }
        }

        /// <summary>
        /// 平行解析 DSNS 名稱。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string ResolveParallel(string name)
        {
            Dictionary<WaitHandle, NameResolver> runnings = new Dictionary<WaitHandle, NameResolver>();
            Dictionary<WaitHandle, NameResolver> successes = new Dictionary<WaitHandle, NameResolver>();

            //依 AccessPointList 分別建立 NameResolver 物件。
            foreach (string nameserver in ProviderList)
            {
                NameResolver resolver = new NameResolver(nameserver, name);
                //使用 WaitHandle 屬性當作 Key 記錄在 Dictionary 之中，方便之後處理。
                runnings.Add(resolver.WaitHandle, resolver);
            }

            //使用 ThreadPool 依序非同步執行所有 Resolve 動作。
            foreach (NameResolver resolver in runnings.Values)
            {
                resolver.WaitHandle.Reset(); //先設定成未信號狀態，Resolve 完成之後會設定成信號狀態。
                ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    (state as NameResolver).Resolve();
                }, resolver);
            }

            NameResolver SuccessResolver = null; //存放完成解析的 NameResovler 物件，如果沒有任何一個解析成功，此變數保持 Null。
            List<Exception> errors = new List<Exception>(); //錯誤清單，當所有解析動作都失敗時才會 Throw。

            //開始等待解析動作。
            while (true)
            {
                WaitHandle[] handles = new List<WaitHandle>(runnings.Keys).ToArray();

                int setIndex = WaitHandle.WaitAny(handles); //等待任何一個 NameServer 回應。

                WaitHandle handle = handles[setIndex]; //取得對應的 WaitHandle 物件。

                NameResolver resolver = runnings[handle]; //透過 WaitHandle 取得先前放在 Dictionary 的 NameResolver 物件。

                if (resolver.Success) //是否解析成功。
                {
                    SuccessResolver = resolver;
                    break; //跳出。
                }
                else //解析失敗。
                    errors.Add(resolver.Error); //將錯誤放入錯誤清單中。

                //將執行完成的 NameResolver 放入 successes 集合，並從 runnings 集合移除。
                successes.Add(handle, resolver);
                runnings.Remove(handle);

                if (runnings.Count <= 0) break; //如果 runnings 集合小於0，即跳出，因為所有解析動作都已經完成。
            }

            if (SuccessResolver == null) //SuccessResolver 保持 Null 代表所有解析動作都失敗。
            {
                //當沒有任何一台解析成功時丟出。
                throw new DSAMultipleErrorException(string.Format("解析名稱失敗：{0}", name), errors.AsReadOnly());
            }
            else
            {
                string msg = string.Format("DSName Resolve：{0}->{1} on {2}",
                    name,
                    SuccessResolver.ResolveResult,
                    SuccessResolver.NameServer);

                System.Diagnostics.Trace.WriteLineIf(PrintResolveMessage, msg);

                return SuccessResolver.ResolveResult;
            }
        }
    }
}
