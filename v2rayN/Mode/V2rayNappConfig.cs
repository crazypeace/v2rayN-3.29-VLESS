﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using v2rayN.Base;
using v2rayN.HttpProxyHandler;


namespace v2rayN.Mode
{
    /// <summary>
    /// 本软件配置文件实体类
    /// </summary>
    [Serializable]
    public class V2rayNappConfig
    {
        /// <summary>
        /// 本地监听
        /// </summary>
        public List<InItem> inbound { get; set; }

        /// <summary>
        /// 允许日志
        /// </summary>
        public bool logEnabled { get; set; }

        /// <summary>
        /// 日志等级
        /// </summary>
        public string loglevel { get; set; }

        /// <summary>
        /// 活动配置序号
        /// </summary>
        public int index { get; set; }

        /// <summary>
        /// 节点服务器信息
        /// </summary>
        public List<NodeItem> outbound { get; set; }

        /// <summary>
        /// 允许Mux多路复用
        /// </summary>
        public bool muxEnabled { get; set; }

        /// <summary>
        /// 域名解析策略
        /// </summary>
        public string domainStrategy { get; set; }

        /// <summary>
        /// 路由模式
        /// </summary>
        public string routingMode { get; set; }

        /// <summary>
        /// 用户自定义需代理的网址或ip
        /// </summary>
        public List<string> useragent { get; set; }

        /// <summary>
        /// 用户自定义直连的网址或ip
        /// </summary>
        public List<string> userdirect { get; set; }

        /// <summary>
        /// 用户自定义阻止的网址或ip
        /// </summary>
        public List<string> userblock { get; set; }

        /// <summary>
        /// KcpItem
        /// </summary>
        public KcpItem kcpItem { get; set; }

        /// <summary>
        /// 监听状态
        /// </summary>
        public ListenerType listenerType { get; set; }

        /// <summary>
        /// 自定义服务器下载测速url
        /// </summary>
        public string speedTestUrl { get; set; }
        /// <summary>
        /// 自定义“服务器真连接延迟”测试url
        /// </summary>
        public string delayTestUrl { get; set; }
        /// <summary>
        /// 自定义GFWList url
        /// </summary>
        public string urlGFWList { get; set; }

        /// <summary>
        /// 允许来自局域网的连接
        /// </summary>
        public bool allowLANConn { get; set; }

        /// <summary>
        /// 启用实时网速和流量统计
        /// </summary>
        public bool enableStatistics { get; set; }

        /// <summary>
        /// 去重时优先保留较旧（顶部）节点
        /// </summary>
        public bool keepOlderDedupl { get; set; }

        /// <summary>
        /// 视图刷新率
        /// </summary>
        public int statisticsFreshRate { get; set; }


        /// <summary>
        /// 自定义远程DNS
        /// </summary>
        public string remoteDNS { get; set; }

        /// <summary>
        /// 是否允许不安全连接
        /// </summary>
        public bool defAllowInsecure { get; set; }

        /// <summary>
        /// 订阅
        /// </summary>
        public List<SubItem> subItem { get; set; }
        /// <summary>
        /// UI
        /// </summary>
        public UIItem uiItem { get; set; }

        public List<string> userPacRule { get; set; }

        // sockopt
        public string sockoptTag { get; set; }

        /// SocksOut tag: "tunnel"
        public string socksOutboundIP { get; set; }
        public int socksOutboundPort { get; set; }

        /// tlsHelloFragment tag: "fragment"
        public string tlsHelloFgmLength { get; set; }
        public string tlsHelloFgmInterval { get; set; }

        #region 函数

        public string address()
        {
            if (index < 0)
            {
                return string.Empty;
            }
            return outbound[index].address.TrimEx();
        }

        public int port()
        {
            if (index < 0)
            {
                return 10808;
            }
            return outbound[index].port;
        }

        public string id()
        {
            if (index < 0)
            {
                return string.Empty;
            }
            return outbound[index].id.TrimEx();
        }

        public int alterId()
        {
            if (index < 0)
            {
                return 0;
            }
            return outbound[index].alterId;
        }

        public string security()
        {
            if (index < 0)
            {
                return string.Empty;
            }
            return outbound[index].security.TrimEx();
        }

        public string remarks()
        {
            if (index < 0)
            {
                return string.Empty;
            }
            return outbound[index].remarks.TrimEx();
        }
        public string network()
        {
            if (index < 0 || Utils.IsNullOrEmpty(outbound[index].network))
            {
                return Global.DefaultNetwork;
            }
            return outbound[index].network.TrimEx();
        }
        public string headerType()
        {
            if (index < 0 || Utils.IsNullOrEmpty(outbound[index].headerType))
            {
                return Global.None;
            }
            return outbound[index].headerType.Replace(" ", "").TrimEx();
        }
        public string requestHost()
        {
            if (index < 0 || Utils.IsNullOrEmpty(outbound[index].requestHost))
            {
                return string.Empty;
            }
            return outbound[index].requestHost.Replace(" ", "").TrimEx();
        }
        public string path()
        {
            if (index < 0 || Utils.IsNullOrEmpty(outbound[index].path))
            {
                return string.Empty;
            }
            return outbound[index].path.Replace(" ", "").TrimEx();
        }
        public string streamSecurity()
        {
            if (index < 0 || Utils.IsNullOrEmpty(outbound[index].streamSecurity))
            {
                return string.Empty;
            }
            return outbound[index].streamSecurity;
        }
        public bool allowInsecure()
        {
            if (index < 0 || Utils.IsNullOrEmpty(outbound[index].allowInsecure))
            {
                return defAllowInsecure;
            }
            return Convert.ToBoolean(outbound[index].allowInsecure);
        }

        public int GetLocalPort(string protocol)
        {
            if (protocol == Global.InboundHttp)
            {
                return GetLocalPort(Global.InboundSocks) + 1;
            }
            else if (protocol == "pac")
            {
                return GetLocalPort(Global.InboundSocks) + 2;
            }
            else if (protocol == "speedtest")
            {
                return GetLocalPort(Global.InboundSocks) + 103;
            }

            int localPort = 0;
            foreach (InItem inItem in inbound)
            {
                if (inItem.protocol.Equals(protocol))
                {
                    localPort = inItem.localPort;
                    break;
                }
            }
            return localPort;
        }

        public int configType()
        {
            if (index < 0)
            {
                return 0;
            }
            return outbound[index].configType;
        }

        public string getSummary()
        {
            if (index < 0)
            {
                return string.Empty;
            }
            return outbound[index].getSummary();
        }

        public string getItemId()
        {
            if (index < 0)
            {
                return string.Empty;
            }

            return outbound[index].getItemId();
        }
        public string flow()
        {
            if (index < 0)
            {
                return string.Empty;
            }
            return outbound[index].flow.TrimEx();
        }

        public string fingerprint()
        {
            if (index < 0)
            {
                return string.Empty;
            }
            return outbound[index].fingerprint.TrimEx();
        }

        public string sni()
        {
            if (index < 0)
            {
                return string.Empty;
            }
            return outbound[index].sni.TrimEx();
        }

        public string publicKey()
        {
            if (index < 0)
            {
                return string.Empty;
            }
            return outbound[index].publicKey.TrimEx();
        }

        public string shortId()
        {
            if (index < 0)
            {
                return string.Empty;
            }
            return outbound[index].shortId.TrimEx();
        }

        public string spiderX()
        {
            if (index < 0)
            {
                return string.Empty;
            }
            return outbound[index].spiderX.TrimEx();
        }
        #endregion

    }

    [Serializable]
    public class NodeItem
    {
        public NodeItem()
        {
            configVersion = 1;
            address = string.Empty;
            port = 0;
            id = string.Empty;
            alterId = 0;
            security = string.Empty;
            network = string.Empty;
            remarks = string.Empty;
            headerType = string.Empty;
            requestHost = string.Empty;
            path = string.Empty;
            streamSecurity = string.Empty;
            allowInsecure = string.Empty;
            configType = (int)EConfigType.Vmess;
            testResult = string.Empty;
            subid = string.Empty;
            flow = string.Empty;
        }

        public string getSummary()
        {
            string summary = string.Format("[{0}] ", ((EConfigType)configType).ToString());
            string[] arrAddr = address.Split('.');
            string addr;
            if (arrAddr.Length > 2)
            {
                addr = $"{arrAddr[0]}***{arrAddr[arrAddr.Length - 1]}";
            }
            else if (arrAddr.Length > 1)
            {
                addr = $"***{arrAddr[arrAddr.Length - 1]}";
            }
            else
            {
                addr = address;
            }
            switch (configType)
            {
                case (int)EConfigType.Vmess:
                case (int)EConfigType.Shadowsocks:
                case (int)EConfigType.Socks:
                case (int)EConfigType.VLESS:
                case (int)EConfigType.Trojan:
                case (int)EConfigType.Hysteria2:
                    summary += string.Format("{0}({1}:{2})", remarks, addr, port);
                    break;

                default:
                    summary += string.Format("{0}", remarks);
                    break;
            }
            return summary;
        }
        public string getSubRemarks(V2rayNappConfig appConfig)
        {
            string subRemarks = string.Empty;
            if (Utils.IsNullOrEmpty(subid))
            {
                return subRemarks;
            }
            foreach (SubItem sub in appConfig.subItem)
            {
                if (sub.id.EndsWith(subid))
                {
                    return sub.remarks;
                }
            }
            if (subid.Length <= 4)
            {
                return subid;
            }
            return subid.Substring(0, 4);
        }

        public string getItemId()
        {
            string itemId = $"{address}{port}{requestHost}{path}";
            itemId = Utils.Base64Encode(itemId);
            return itemId;
        }

        /// <summary>
        /// 版本(现在=2)
        /// </summary>
        public int configVersion { get; set; }

        /// <summary>
        /// 远程服务器地址
        /// </summary>
        public string address { get; set; }

        /// <summary>
        /// 远程服务器端口
        /// </summary>
        public int port { get; set; }

        /// <summary>
        /// 远程服务器 UUID 或 密码
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 远程服务器额外ID
        /// </summary>
        public int alterId { get; set; }

        /// <summary>
        /// 本地安全策略
        /// </summary>
        public string security { get; set; }

        /// <summary>
        /// tcp,kcp,ws,h2,quic
        /// </summary>
        public string network { get; set; }

        /// <summary>
        /// 备注或别名
        /// </summary>
        public string remarks { get; set; }

        /// <summary>
        /// 伪装类型
        /// </summary>
        public string headerType { get; set; }

        /// <summary>
        /// 伪装的域名
        /// </summary>
        public string requestHost { get; set; }

        /// <summary>
        /// ws h2 path
        /// </summary>
        public string path { get; set; }

        /// <summary>
        /// 传输层安全
        /// </summary>
        public string streamSecurity { get; set; }

        /// <summary>
        /// 是否允许不安全连接（用于客户端）
        /// </summary>
        public string allowInsecure { get; set; }

        /// <summary>
        /// appConfig type(1=normal,2=custom)
        /// </summary>
        public int configType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string testResult { get; set; }

        /// <summary>
        /// SubItem id
        /// </summary>
        public string subid { get; set; }

        /// <summary>
        /// VLESS flow
        /// </summary>
        public string flow { get; set; }

        /// <summary>
        /// tls sni
        /// </summary>
        public string sni { get; set; }

        /// <summary>
        /// tls alpn
        /// </summary>
        //public string alpn { get; set; } = string.Empty;

        // public ECoreType coreType { get; set; }

        public int preSocksPort { get; set; }

        //public bool displayLog { get; set; } = true;

        public string fingerprint { get; set; }

        // VLESS Reality 
        public string publicKey { get; set; }
        public string shortId { get; set; }
        public string spiderX { get; set; }
    }

    [Serializable]
    public class InItem
    {
        /// <summary>
        /// 本地监听端口
        /// </summary>
        public int localPort { get; set; }

        /// <summary>
        /// 协议，默认为socks
        /// </summary>
        public string protocol { get; set; }

        /// <summary>
        /// 允许udp
        /// </summary>
        public bool udpEnabled { get; set; }

        /// <summary>
        /// 开启流量探测
        /// </summary>
        public bool sniffingEnabled { get; set; } = true;
    }

    [Serializable]
    public class KcpItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int mtu { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int tti { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int uplinkCapacity { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int downlinkCapacity  { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool congestion { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int readBufferSize { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int writeBufferSize { get; set; }
    }


    [Serializable]
    public class SubItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string remarks { get; set; }

        /// <summary>
        /// url
        /// </summary>
        public string url { get; set; }

        /// <summary>
        /// allowInsecure
        /// </summary>
        public bool allowInsecure { get; set; } = false;

        /// <summary>
        /// 是否base64解码
        /// </summary>
        public bool bBase64Decode { get; set; } = false;

        /// <summary>
        /// enable
        /// </summary>
        public bool enabled { get; set; } = true;
    }

    [Serializable]
    public class UIItem
    {
        public System.Drawing.Size mainSize { get; set; }

        public System.Drawing.Point mainLoc { get; set; }

        public Dictionary<string, int> mainLvColWidth { get; set; }
    }
}
