using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using v2rayN.Tool;

namespace v2rayN.Handler
{
    /// <summary>
    /// 优选IP来源: 把配置项 preferredIpUrl 当作 URL 做 HTTP GET, 返回纯文本列表
    /// (逗号或换行分隔, 元素可带 # 注释), 取第 1 个合法元素。
    /// </summary>
    public class PreferredIpHandler
    {
        private static readonly Regex DomainRegex = new Regex(
            @"^(?=.{1,253}$)([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,63}$",
            RegexOptions.Compiled);

        /// <summary>
        /// 拉取并解析, 返回第 1 个合法 IP/域名; 失败/空/不合法返回 null。
        /// </summary>
        public static async Task<string> GetFirstAsync(string url)
        {
            try
            {
                if (Utils.IsNullOrEmpty(url))
                {
                    return null;
                }
                Utils.SetSecurityProtocol();
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    string body = await client.GetStringAsync(url.Trim());
                    return ParseFirst(body);
                }
            }
            catch (Exception ex)
            {
                Utils.SaveLog(ex.Message, ex);
                return null;
            }
        }

        /// <summary>
        /// 随机优选域名: 6 个小写字母 + 2 个数字 + "." + 配置的优选域名后缀。
        /// 后缀前若带 "." 会先去掉; 配置为空返回 null。
        /// </summary>
        public static string RandomPreferredDomain(string suffix)
        {
            if (Utils.IsNullOrEmpty(suffix))
            {
                return null;
            }
            string s = suffix.Trim().TrimStart('.');
            if (Utils.IsNullOrEmpty(s))
            {
                return null;
            }
            const string letters = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            byte[] buf = new byte[8];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buf);
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                sb.Append(letters[buf[i] % letters.Length]);
            }
            for (int i = 6; i < 8; i++)
            {
                sb.Append(digits[buf[i] % digits.Length]);
            }
            return sb.ToString() + "." + s;
        }

        /// <summary>
        /// 纯解析(便于单测): 按 , / 换行 切分, 取第一个非空项, 截掉 # 注释, trim, 校验。
        /// </summary>
        public static string ParseFirst(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return null;
            }
            string[] tokens = body.Split(new[] { ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                string s = token;
                int hash = s.IndexOf('#');
                if (hash >= 0)
                {
                    s = s.Substring(0, hash);
                }
                s = s.Trim();
                if (s.Length == 0)
                {
                    continue;
                }
                // 只认第 1 个非空元素: 合法则返回, 不合法视为失败
                if (IPAddress.TryParse(s, out _))
                {
                    return s;
                }
                if (DomainRegex.IsMatch(s))
                {
                    return s;
                }
                return null;
            }
            return null;
        }
    }
}
