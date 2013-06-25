/*
 * Create Date：2010/10/29
 * Author Name：YaoMing Huang
 */
using System.Text.RegularExpressions;
using System;
namespace FISCA.DSAClient
{
    /// <summary>
    /// 代表一個 DSA 的存取點。
    /// </summary>
    public class AccessPoint
    {
        private const string AccessPointPattern = @"^(?<prefix>\w+)://(?<path>.*)$";

        enum ResolveType
        {
            HTTP, DSA, Local, Undefined
        }

        /// <summary>
        /// 初始化 AccessPoint 的新執行個體。
        /// </summary>
        /// <param name="name">DSA 的 AccessPoint 名稱。</param>
        /// <param name="url">實體位置。</param>
        internal AccessPoint(string name, string url)
        {
            Name = name;
            Url = url;
        }

        /// <summary>
        /// DSA 在 DSNS 上的名稱。
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// AccessPoint 的實體位置。
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// 解析 DSA 的 AccessPoint 名稱。
        /// </summary>
        /// <param name="accessPoint">要解析的 AccessPoint 名稱或實體位置。</param>
        /// <param name="result">AccessPoint 的執行個體。</param>
        /// <returns>是否解析成功。</returns>
        public static bool TryParse(string accessPoint, out AccessPoint result)
        {
            result = null;
            try
            {
                result = Parse(accessPoint);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解析 DSA 的 AccessPoint 名稱。
        /// </summary>
        /// <param name="accessPoint">要解析的 AccessPoint 名稱或實體位置。</param>
        /// <returns>AccessPoint 的執行個體。</returns>
        public static AccessPoint Parse(string accessPoint)
        {
            string strUri = accessPoint;

            //如果是使用舊式的「!」，就將其取代成新式的「local://」。
            if (accessPoint.StartsWith("!"))
                strUri = accessPoint.Replace("!", "local://"); //!c:\ds -> local://c:\ds

            //如果是使用舊式的「#http://」，就將其取代成新式的「http://」。
            else if (accessPoint.StartsWith("#"))
                strUri = accessPoint.Replace("#", ""); //#http://yahoo.com -> http://yahoo.com

            AccessPoint apName = null;
            switch (ParsePrefixType(strUri))
            {
                case ResolveType.HTTP:
                    apName = new AccessPoint(strUri, strUri);
                    break;

                case ResolveType.DSA:
                    apName = new AccessPoint(accessPoint, NameService.Resolve(GetUrlString(strUri)));
                    break;

                case ResolveType.Local:
                    apName = new AccessPoint(strUri, GetUrlString(accessPoint));
                    break;

                default:
                    throw new ArgumentException(string.Format("無法識別的 Protocol 類型({0})。", accessPoint));
            }

            return apName;
        }

        private static ResolveType ParsePrefixType(string URI)
        {
            switch (GetProtocolString(URI).ToUpper())
            {
                case "HTTP":
                case "HTTPS":
                    return ResolveType.HTTP;

                case "DSA":
                    return ResolveType.DSA;

                case "LOCAL":
                    return ResolveType.Local;

                case "": //沒有指定的話就使用DSA
                    return ResolveType.DSA;

                default:
                    return ResolveType.Undefined;
            }
        }

        private static string GetProtocolString(string URI)
        {
            Regex rex = new Regex(AccessPointPattern);

            Match m = rex.Match(URI);

            if (m.Success)
                return m.Result("${prefix}");
            else
                return "";
        }

        private static string GetUrlString(string URI)
        {
            Regex rex = new Regex(AccessPointPattern);

            Match m = rex.Match(URI);

            if (m.Success)
                return m.Result("${path}");
            else
                return URI;
        }
    }
}