using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using v2rayN.Base;
using v2rayN.Mode;

namespace v2rayN.Handler
{
    /// <summary>
    /// v2ray配置文件处理类
    /// </summary>
    class V2rayConfigHandler
    {
        private static string SampleClient = Global.v2raySampleClient;
        private static string SampleServer = Global.v2raySampleServer;

        #region 生成客户端配置

        /// <summary>
        /// 生成v2ray的客户端配置文件
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="fileName"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static int GenerateClientConfig(V2rayNappConfig appConfig, string fileName, bool blExport, out string msg)
        {
            try
            {
                //检查GUI设置
                if (appConfig == null
                    || appConfig.index < 0
                    || appConfig.outbound.Count <= 0
                    || appConfig.index > appConfig.outbound.Count - 1
                    )
                {
                    msg = UIRes.I18N("CheckServerSettings");
                    return -1;
                }

                msg = UIRes.I18N("InitialConfiguration");
                if (appConfig.configType() == (int)EConfigType.Custom)
                {
                    return GenerateClientCustomConfig(appConfig, fileName, out msg);
                }

                //取得默认配置
                string result = Utils.GetEmbedText(SampleClient);
                if (Utils.IsNullOrEmpty(result))
                {
                    msg = UIRes.I18N("FailedGetDefaultConfiguration");
                    return -1;
                }

                //从Json加载
                V2rayClientConfig v2rayConfig = Utils.FromJson<V2rayClientConfig>(result);
                if (v2rayConfig == null)
                {
                    msg = UIRes.I18N("FailedGenDefaultConfiguration");
                    return -1;
                }

                //开始修改配置
                log(appConfig, ref v2rayConfig, blExport);

                //本地端口
                inbound(appConfig, ref v2rayConfig);

                //路由
                routing(appConfig, ref v2rayConfig);

                //outbound
                outbound(appConfig, ref v2rayConfig);

                //dns
                dns(appConfig, ref v2rayConfig);

                // Sockopt
                sockopt(appConfig, ref v2rayConfig);

                // TODO: 统计配置
                statistic(appConfig, ref v2rayConfig);

                Utils.ToJsonFile(v2rayConfig, fileName, false);

                msg = string.Format(UIRes.I18N("SuccessfulConfiguration"), appConfig.getSummary());
            }
            catch
            {
                msg = UIRes.I18N("FailedGenDefaultConfiguration");
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// 日志
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="v2rayConfig"></param>
        /// <returns></returns>
        private static int log(V2rayNappConfig appConfig, ref V2rayClientConfig v2rayConfig, bool blExport)
        {
            try
            {
                if (blExport)
                {
                    if (appConfig.logEnabled)
                    {
                        v2rayConfig.log.loglevel = appConfig.loglevel;
                    }
                    else
                    {
                        v2rayConfig.log.loglevel = appConfig.loglevel;
                        v2rayConfig.log.access = "";
                        v2rayConfig.log.error = "";
                    }
                }
                else
                {
                    if (appConfig.logEnabled)
                    {
                        v2rayConfig.log.loglevel = appConfig.loglevel;
                        v2rayConfig.log.access = Utils.GetPath(v2rayConfig.log.access);
                        v2rayConfig.log.error = Utils.GetPath(v2rayConfig.log.error);
                    }
                    else
                    {
                        v2rayConfig.log.loglevel = appConfig.loglevel;
                        v2rayConfig.log.access = "";
                        v2rayConfig.log.error = "";
                    }
                }
            }
            catch
            {
            }
            return 0;
        }

        /// <summary>
        /// 本地端口
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="v2rayConfig"></param>
        /// <returns></returns>
        private static int inbound(V2rayNappConfig appConfig, ref V2rayClientConfig v2rayConfig)
        {
            try
            {
                Inbounds inbound = v2rayConfig.inbounds[0];
                //端口
                inbound.port = appConfig.inbound[0].localPort;
                inbound.protocol = appConfig.inbound[0].protocol;
                if (appConfig.allowLANConn)
                {
                    inbound.listen = "0.0.0.0";
                }
                else
                {
                    inbound.listen = Global.Loopback;
                }
                //开启udp
                inbound.settings.udp = appConfig.inbound[0].udpEnabled;
                inbound.sniffing.enabled = appConfig.inbound[0].sniffingEnabled;
            }
            catch
            {
            }
            return 0;
        }

        /// <summary>
        /// 路由
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="v2rayConfig"></param>
        /// <returns></returns>
        private static int routing(V2rayNappConfig appConfig, ref V2rayClientConfig v2rayConfig)
        {
            try
            {
                if (v2rayConfig.routing != null
                  && v2rayConfig.routing.rules != null)
                {
                    v2rayConfig.routing.domainStrategy = appConfig.domainStrategy;

                    //自定义
                    //需代理
                    routingUserRule(appConfig.useragent, Global.agentTag, ref v2rayConfig);
                    //直连
                    routingUserRule(appConfig.userdirect, Global.directTag, ref v2rayConfig);
                    //阻止
                    routingUserRule(appConfig.userblock, Global.blockTag, ref v2rayConfig);


                    switch (appConfig.routingMode)
                    {
                        case "0":
                            break;
                        case "1":
                            routingGeo("ip", "private", Global.directTag, ref v2rayConfig);
                            break;
                        case "2":
                            routingGeo("", "cn", Global.directTag, ref v2rayConfig);
                            break;
                        case "3":
                            routingGeo("ip", "private", Global.directTag, ref v2rayConfig);
                            routingGeo("", "cn", Global.directTag, ref v2rayConfig);
                            break;
                    }

                }
            }
            catch
            {
            }
            return 0;
        }
        private static int routingUserRule(List<string> userRule, string tag, ref V2rayClientConfig v2rayConfig)
        {
            try
            {
                if (userRule != null
                    && userRule.Count > 0)
                {
                    //Domain
                    RulesItem rulesDomain = new RulesItem
                    {
                        type = "field",
                        outboundTag = tag,
                        domain = new List<string>()
                    };

                    //IP
                    RulesItem rulesIP = new RulesItem
                    {
                        type = "field",
                        outboundTag = tag,
                        ip = new List<string>()
                    };

                    foreach (string u in userRule)
                    {
                        string url = u.TrimEx();
                        if (Utils.IsNullOrEmpty(url))
                        {
                            continue;
                        }
                        if (Utils.IsIP(url) || url.StartsWith("geoip:"))
                        {
                            rulesIP.ip.Add(url);
                        }
                        else if (Utils.IsDomain(url)
                            || url.StartsWith("geosite:")
                            || url.StartsWith("regexp:")
                            || url.StartsWith("domain:")
                            || url.StartsWith("full:"))
                        {
                            rulesDomain.domain.Add(url);
                        }
                    }
                    if (rulesDomain.domain.Count > 0)
                    {
                        v2rayConfig.routing.rules.Add(rulesDomain);
                    }
                    if (rulesIP.ip.Count > 0)
                    {
                        v2rayConfig.routing.rules.Add(rulesIP);
                    }
                }
            }
            catch
            {
            }
            return 0;
        }


        private static int routingGeo(string ipOrDomain, string code, string tag, ref V2rayClientConfig v2rayConfig)
        {
            try
            {
                if (!Utils.IsNullOrEmpty(code))
                {
                    //IP
                    if (ipOrDomain == "ip" || ipOrDomain == "")
                    {
                        RulesItem rulesItem = new RulesItem
                        {
                            type = "field",
                            outboundTag = Global.directTag,
                            ip = new List<string>()
                        };
                        rulesItem.ip.Add($"geoip:{code}");

                        v2rayConfig.routing.rules.Add(rulesItem);
                    }

                    if (ipOrDomain == "domain" || ipOrDomain == "")
                    {
                        RulesItem rulesItem = new RulesItem
                        {
                            type = "field",
                            outboundTag = Global.directTag,
                            domain = new List<string>()
                        };
                        rulesItem.domain.Add($"geosite:{code}");
                        v2rayConfig.routing.rules.Add(rulesItem);
                    }
                }
            }
            catch
            {
            }
            return 0;
        }

        /// <summary>
        /// vmess协议服务器配置
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="v2rayConfig"></param>
        /// <returns></returns>
        private static int outbound(V2rayNappConfig appConfig, ref V2rayClientConfig v2rayConfig)
        {
            try
            {
                Outbounds outbound = v2rayConfig.outbounds[0];
                if (appConfig.configType() == (int)EConfigType.Vmess)
                {
                    VnextItem vnextItem;
                    if (outbound.settings.vnext.Count <= 0)
                    {
                        vnextItem = new VnextItem();
                        outbound.settings.vnext.Add(vnextItem);
                    }
                    else
                    {
                        vnextItem = outbound.settings.vnext[0];
                    }
                    //远程服务器地址和端口
                    vnextItem.address = appConfig.address();
                    vnextItem.port = appConfig.port();

                    UsersItem usersItem;
                    if (vnextItem.users.Count <= 0)
                    {
                        usersItem = new UsersItem();
                        vnextItem.users.Add(usersItem);
                    }
                    else
                    {
                        usersItem = vnextItem.users[0];
                    }
                    //远程服务器用户ID
                    usersItem.id = appConfig.id();
                    usersItem.alterId = appConfig.alterId();
                    usersItem.email = Global.userEMail;
                    usersItem.security = appConfig.security();

                    //Mux
                    outbound.mux.enabled = appConfig.muxEnabled;
                    outbound.mux.concurrency = appConfig.muxEnabled ? 8 : -1;

                    //远程服务器底层传输配置
                    StreamSettings streamSettings = outbound.streamSettings;
                    boundStreamSettings(appConfig, "out", ref streamSettings);

                    outbound.protocol = Global.vmessProtocolLite;
                    outbound.settings.servers = null;
                }
                else if (appConfig.configType() == (int)EConfigType.Shadowsocks)
                {
                    ServersItem serversItem;
                    if (outbound.settings.servers.Count <= 0)
                    {
                        serversItem = new ServersItem();
                        outbound.settings.servers.Add(serversItem);
                    }
                    else
                    {
                        serversItem = outbound.settings.servers[0];
                    }
                    //远程服务器地址和端口
                    serversItem.address = appConfig.address();
                    serversItem.port = appConfig.port();
                    serversItem.password = appConfig.id();
                    serversItem.method = appConfig.security();

                    serversItem.ota = false;
                    serversItem.level = 1;

                    outbound.mux.enabled = false;
                    outbound.mux.concurrency = -1;


                    outbound.protocol = Global.ssProtocolLite;
                    outbound.settings.vnext = null;
                }
                else if (appConfig.configType() == (int)EConfigType.Socks)
                {
                    ServersItem serversItem;
                    if (outbound.settings.servers.Count <= 0)
                    {
                        serversItem = new ServersItem();
                        outbound.settings.servers.Add(serversItem);
                    }
                    else
                    {
                        serversItem = outbound.settings.servers[0];
                    }
                    //远程服务器地址和端口
                    serversItem.address = appConfig.address();
                    serversItem.port = appConfig.port();
                    serversItem.method = null;
                    serversItem.password = null;

                    if (!Utils.IsNullOrEmpty(appConfig.security())
                        && !Utils.IsNullOrEmpty(appConfig.id()))
                    {
                        SocksUsersItem socksUsersItem = new SocksUsersItem
                        {
                            user = appConfig.security(),
                            pass = appConfig.id(),
                            level = 1
                        };

                        serversItem.users = new List<SocksUsersItem>() { socksUsersItem };
                    }

                    outbound.mux.enabled = false;
                    outbound.mux.concurrency = -1;

                    outbound.protocol = Global.socksProtocolLite;
                    outbound.settings.vnext = null;
                }
                else if (appConfig.configType() == (int)EConfigType.VLESS)
                {
                    VnextItem vnextItem;
                    if (outbound.settings.vnext.Count <= 0)
                    {
                        vnextItem = new VnextItem();
                        outbound.settings.vnext.Add(vnextItem);
                    }
                    else
                    {
                        vnextItem = outbound.settings.vnext[0];
                    }
                    //远程服务器地址和端口
                    vnextItem.address = appConfig.address();
                    vnextItem.port = appConfig.port();

                    UsersItem usersItem;
                    if (vnextItem.users.Count <= 0)
                    {
                        usersItem = new UsersItem();
                        vnextItem.users.Add(usersItem);
                    }
                    else
                    {
                        usersItem = vnextItem.users[0];
                    }
                    //远程服务器用户ID
                    usersItem.id = appConfig.id();
                    usersItem.alterId = 0;
                    usersItem.flow = string.Empty;
                    usersItem.email = Global.userEMail;
                    usersItem.encryption = appConfig.security();

                    //Mux
                    outbound.mux.enabled = appConfig.muxEnabled;
                    outbound.mux.concurrency = appConfig.muxEnabled ? 8 : -1;

                    //远程服务器底层传输配置
                    StreamSettings streamSettings = outbound.streamSettings;
                    boundStreamSettings(appConfig, "out", ref streamSettings);

                    //if xtls
                    if (appConfig.streamSecurity() == Global.StreamSecurityReality)
                    {
                        if (Utils.IsNullOrEmpty(appConfig.flow()))
                        {
                            usersItem.flow = "xtls-rprx-vision";
                        }
                        else
                        {
                            usersItem.flow = appConfig.flow();
                        }

                        outbound.mux.enabled = false;
                        outbound.mux.concurrency = -1;
                    }

                    outbound.protocol = Global.vlessProtocolLite;
                    outbound.settings.servers = null;
                }
                else if (appConfig.configType() == (int)EConfigType.Trojan)
                {
                    ServersItem serversItem;
                    if (outbound.settings.servers.Count <= 0)
                    {
                        serversItem = new ServersItem();
                        outbound.settings.servers.Add(serversItem);
                    }
                    else
                    {
                        serversItem = outbound.settings.servers[0];
                    }
                    //远程服务器地址和端口
                    serversItem.address = appConfig.address();
                    serversItem.port = appConfig.port();
                    serversItem.password = appConfig.id();

                    serversItem.ota = false;
                    serversItem.level = 1;

                    outbound.mux.enabled = false;
                    outbound.mux.concurrency = -1;


                    //远程服务器底层传输配置
                    StreamSettings streamSettings = outbound.streamSettings;
                    boundStreamSettings(appConfig, "out", ref streamSettings);

                    outbound.protocol = Global.trojanProtocolLite;
                    outbound.settings.vnext = null;
                }
                else if (appConfig.configType() == (int)EConfigType.Hysteria2)
                {
                    // 从默认的配置文件 SampleClientConfig.txt 加载的 server 里面有 hy2 不需要的配置项
                    // 为了对原项目的改动最小, 这里把sever删空. 然后再接下来的代码会新建一个server
                    outbound.settings.servers.Clear();
                    // 但是即使如此, 因为从 C# 类到Json序列化的关系, bool变量和int变量是有默认值的, 所以会生成到最终的json结构中
                    // "ota": false,
                    // "level": 0

                    ServersItem serversItem;
                    if (outbound.settings.servers.Count <= 0)
                    {
                        serversItem = new ServersItem();
                        outbound.settings.servers.Add(serversItem);
                    }
                    else
                    {
                        serversItem = outbound.settings.servers[0];
                    }
                    //远程服务器地址和端口
                    serversItem.address = appConfig.address();
                    serversItem.port = appConfig.port();

                    //远程服务器底层传输配置
                    StreamSettings streamSettings = outbound.streamSettings;
                    boundStreamSettings(appConfig, "out", ref streamSettings);

                    outbound.protocol = Global.hy2ProtocolLite;
                    outbound.settings.vnext = null;
                }
            }
            catch
            {
            }
            return 0;
        }

        /// <summary>
        /// vmess协议远程服务器底层传输配置
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="iobound"></param>
        /// <param name="streamSettings"></param>
        /// <returns></returns>
        private static int boundStreamSettings(V2rayNappConfig appConfig, string iobound, ref StreamSettings streamSettings)
        {
            try
            {
                //远程服务器底层传输配置
                streamSettings.network = appConfig.network();
                string host = appConfig.requestHost();
                string sni = appConfig.sni();
                
                //if tls
                if (appConfig.streamSecurity() == Global.StreamSecurity)
                {
                    streamSettings.security = appConfig.streamSecurity();

                    TlsSettings tlsSettings = new TlsSettings
                    {
                        allowInsecure = appConfig.allowInsecure()
                    };
                    if (!string.IsNullOrWhiteSpace(host))
                    {
                        tlsSettings.serverName = Utils.String2List(host)[0];
                    }
                    streamSettings.tlsSettings = tlsSettings;
                }

                //if Reality
                if (appConfig.streamSecurity() == Global.StreamSecurityReality)
                {
                    streamSettings.security = appConfig.streamSecurity();

                    TlsSettings realitySettings = new TlsSettings()
                    {
                        fingerprint = appConfig.fingerprint().IsNullOrEmpty() ? "auto" : appConfig.fingerprint(),
                        serverName = sni,
                        publicKey = appConfig.publicKey(),
                        shortId = appConfig.shortId(),
                        spiderX = appConfig.spiderX(),
                    };

                    streamSettings.realitySettings = realitySettings;
                }

                //streamSettings
                switch (appConfig.network())
                {
                    //kcp基本配置暂时是默认值，用户能自己设置伪装类型
                    case "kcp":
                        KcpSettings kcpSettings = new KcpSettings
                        {
                            mtu = appConfig.kcpItem.mtu,
                            tti = appConfig.kcpItem.tti
                        };
                        if (iobound.Equals("out"))
                        {
                            kcpSettings.uplinkCapacity = appConfig.kcpItem.uplinkCapacity;
                            kcpSettings.downlinkCapacity = appConfig.kcpItem.downlinkCapacity;
                        }
                        else if (iobound.Equals("in"))
                        {
                            kcpSettings.uplinkCapacity = appConfig.kcpItem.downlinkCapacity; ;
                            kcpSettings.downlinkCapacity = appConfig.kcpItem.downlinkCapacity;
                        }
                        else
                        {
                            kcpSettings.uplinkCapacity = appConfig.kcpItem.uplinkCapacity;
                            kcpSettings.downlinkCapacity = appConfig.kcpItem.downlinkCapacity;
                        }

                        kcpSettings.congestion = appConfig.kcpItem.congestion;
                        kcpSettings.readBufferSize = appConfig.kcpItem.readBufferSize;
                        kcpSettings.writeBufferSize = appConfig.kcpItem.writeBufferSize;
                        kcpSettings.header = new Header
                        {
                            type = appConfig.headerType()
                        };
                        if (!Utils.IsNullOrEmpty(appConfig.path()))
                        {
                            kcpSettings.seed = appConfig.path();
                        }
                        streamSettings.kcpSettings = kcpSettings;
                        break;
                    //ws
                    case "ws":
                        WsSettings wsSettings = new WsSettings
                        {
                        };

                        string path = appConfig.path();
                        if (!string.IsNullOrWhiteSpace(host))
                        {
                            wsSettings.headers = new Headers
                            {
                                Host = host
                            };
                        }
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            wsSettings.path = path;
                        }
                        streamSettings.wsSettings = wsSettings;

                        break;
                    //h2
                    case "h2":
                        HttpSettings httpSettings = new HttpSettings();

                        if (!string.IsNullOrWhiteSpace(host))
                        {
                            httpSettings.host = Utils.String2List(host);
                        }
                        httpSettings.path = appConfig.path();

                        streamSettings.httpSettings = httpSettings;

                        break;
                    //quic
                    case "quic":
                        QuicSettings quicsettings = new QuicSettings
                        {
                            security = host,
                            key = appConfig.path(),
                            header = new Header
                            {
                                type = appConfig.headerType()
                            }
                        };
                        streamSettings.quicSettings = quicsettings;
                        if (appConfig.streamSecurity() == Global.StreamSecurity)
                        {
                            streamSettings.tlsSettings.serverName = appConfig.address();
                        }
                        break;
                    // hy2
                    case "hysteria2":
                        Hy2Settings hy2Settings = new Hy2Settings
                        {
                            password = appConfig.id()
                        };
                        streamSettings.hy2Settings = hy2Settings;
                        break;
                    default:
                        //tcp带http伪装
                        if (appConfig.headerType().Equals(Global.TcpHeaderHttp))
                        {
                            TcpSettings tcpSettings = new TcpSettings
                            {
                                header = new Header
                                {
                                    type = appConfig.headerType()
                                }
                            };

                            if (iobound.Equals("out"))
                            {
                                //request填入自定义Host
                                string request = Utils.GetEmbedText(Global.v2raySampleHttprequestFileName);
                                string[] arrHost = host.Split(',');
                                string host2 = string.Join("\",\"", arrHost);
                                request = request.Replace("$requestHost$", string.Format("\"{0}\"", host2));

                                //填入自定义Path
                                string pathHttp = @"/";
                                if (!Utils.IsNullOrEmpty(appConfig.path()))
                                {
                                    string[] arrPath = appConfig.path().Split(',');
                                    pathHttp = string.Join("\",\"", arrPath);
                                }
                                request = request.Replace("$requestPath$", string.Format("\"{0}\"", pathHttp));
                                tcpSettings.header.request = Utils.FromJson<object>(request);
                            }
                            else if (iobound.Equals("in"))
                            {
                                //string response = Utils.GetEmbedText(Global.v2raySampleHttpresponseFileName);
                                //tcpSettings.header.response = Utils.FromJson<object>(response);
                            }

                            streamSettings.tcpSettings = tcpSettings;
                        }
                        break;
                }
            }
            catch
            {
            }

            return 0;
        }

        /// <summary>
        /// remoteDNS
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="v2rayConfig"></param>
        /// <returns></returns>
        private static int dns(V2rayNappConfig appConfig, ref V2rayClientConfig v2rayConfig)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(appConfig.remoteDNS))
                {
                    return 0;
                }
                List<string> servers = new List<string>();

                string[] arrDNS = appConfig.remoteDNS.Split(',');
                foreach (string str in arrDNS)
                {
                    //if (Utils.IsIP(str))
                    //{
                    servers.Add(str);
                    //}
                }
                //servers.Add("localhost");
                v2rayConfig.dns = new Mode.Dns
                {
                    servers = servers
                };
            }
            catch
            {
            }
            return 0;
        }

        /// <summary>
        /// sockopt
        /// </summary> 从v2rayN程序的配置项 转换为 节点的参数
        /// <param name="appConfig"></param>
        /// <param name="v2rayConfig"></param>
        /// <returns></returns>
        private static int sockopt(V2rayNappConfig appConfig, ref V2rayClientConfig v2rayConfig)
        {

            try
            {
                if (appConfig.sockoptTag == "")
                {
                    v2rayConfig.outbounds[0].streamSettings.sockopt = null;
                }
                else
                {
                    // 对应 SampleClientConfig 里的配置, outbounds[3] 就是下一跳Socks出口
                    Outbounds nextSocks = v2rayConfig.outbounds[3];
                    ServersItem server = nextSocks.settings.servers[0];
                    server.address = appConfig.socksOutboundIP;
                    server.port = appConfig.socksOutboundPort;

                    // 对应 SampleClientConfig 里的配置, outbounds[4] 就是tls hello分片
                    Outbounds tlsHelloFrg = v2rayConfig.outbounds[4];
                    tlsHelloFrg.settings.fragment.length = appConfig.tlsHelloFgmLength;
                    tlsHelloFrg.settings.fragment.interval = appConfig.tlsHelloFgmInterval;

                    v2rayConfig.outbounds[0].streamSettings.sockopt.dialerProxy = appConfig.sockoptTag;
                }
            }
            catch
            {
            }
            return 0;
        }

        public static int statistic(V2rayNappConfig appConfig, ref V2rayClientConfig v2rayConfig)
        {
            if (appConfig.enableStatistics)
            {
                string tag = Global.InboundAPITagName;
                API apiObj = new API();
                Policy policyObj = new Policy();
                SystemPolicy policySystemSetting = new SystemPolicy();

                string[] services = { "StatsService" };

                v2rayConfig.stats = new Stats();

                apiObj.tag = tag;
                apiObj.services = services.ToList();
                v2rayConfig.api = apiObj;

                policySystemSetting.statsInboundDownlink = true;
                policySystemSetting.statsInboundUplink = true;
                policyObj.system = policySystemSetting;
                v2rayConfig.policy = policyObj;

                if (!v2rayConfig.inbounds.Exists(item => { return item.tag == tag; }))
                {
                    Inbounds apiInbound = new Inbounds();
                    Inboundsettings apiInboundSettings = new Inboundsettings();
                    apiInbound.tag = tag;
                    apiInbound.listen = Global.Loopback;
                    apiInbound.port = Global.statePort;
                    apiInbound.protocol = Global.InboundAPIProtocal;
                    apiInboundSettings.address = Global.Loopback;
                    apiInbound.settings = apiInboundSettings;
                    v2rayConfig.inbounds.Add(apiInbound);
                }

                if (!v2rayConfig.routing.rules.Exists(item => { return item.outboundTag == tag; }))
                {
                    RulesItem apiRoutingRule = new RulesItem
                    {
                        inboundTag = new List<string> { tag },
                        outboundTag = tag,
                        type = "field"
                    };
                    v2rayConfig.routing.rules.Add(apiRoutingRule);
                }
            }
            return 0;
        }

        /// <summary>
        /// 生成v2ray的客户端配置文件(自定义配置)
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="fileName"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static int GenerateClientCustomConfig(V2rayNappConfig appConfig, string fileName, out string msg)
        {
            try
            {
                //检查GUI设置
                if (appConfig == null
                    || appConfig.index < 0
                    || appConfig.outbound.Count <= 0
                    || appConfig.index > appConfig.outbound.Count - 1
                    )
                {
                    msg = UIRes.I18N("CheckServerSettings");
                    return -1;
                }

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                string addressFileName = appConfig.address();
                if (!File.Exists(addressFileName))
                {
                    addressFileName = Path.Combine(Utils.GetTempPath(), addressFileName);
                }
                if (!File.Exists(addressFileName))
                {
                    msg = UIRes.I18N("FailedGenDefaultConfiguration");
                    return -1;
                }
                File.Copy(addressFileName, fileName);

                msg = string.Format(UIRes.I18N("SuccessfulConfiguration"), appConfig.getSummary());
            }
            catch
            {
                msg = UIRes.I18N("FailedGenDefaultConfiguration");
                return -1;
            }
            return 0;
        }

        #endregion

        #region 生成服务端配置

        /// <summary>
        /// 生成v2ray的服务端配置文件
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="fileName"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static int GenerateServerConfig(V2rayNappConfig appConfig, string fileName, out string msg)
        {
            try
            {
                //检查GUI设置
                if (appConfig == null
                    || appConfig.index < 0
                    || appConfig.outbound.Count <= 0
                    || appConfig.index > appConfig.outbound.Count - 1
                    )
                {
                    msg = UIRes.I18N("CheckServerSettings");
                    return -1;
                }

                msg = UIRes.I18N("InitialConfiguration");

                //取得默认配置
                string result = Utils.GetEmbedText(SampleServer);
                if (Utils.IsNullOrEmpty(result))
                {
                    msg = UIRes.I18N("FailedGetDefaultConfiguration");
                    return -1;
                }

                //从Json加载
                V2rayClientConfig v2rayConfig = Utils.FromJson<V2rayClientConfig>(result);
                if (v2rayConfig == null)
                {
                    msg = UIRes.I18N("FailedGenDefaultConfiguration");
                    return -1;
                }

                ////开始修改配置
                log(appConfig, ref v2rayConfig, true);

                //vmess协议服务器配置
                ServerInbound(appConfig, ref v2rayConfig);

                //传出设置
                ServerOutbound(appConfig, ref v2rayConfig);

                Utils.ToJsonFile(v2rayConfig, fileName, false);

                msg = string.Format(UIRes.I18N("SuccessfulConfiguration"), appConfig.getSummary());
            }
            catch
            {
                msg = UIRes.I18N("FailedGenDefaultConfiguration");
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// vmess协议服务器配置
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="v2rayConfig"></param>
        /// <returns></returns>
        private static int ServerInbound(V2rayNappConfig appConfig, ref V2rayClientConfig v2rayConfig)
        {
            try
            {
                Inbounds inbound = v2rayConfig.inbounds[0];
                UsersItem usersItem;
                if (inbound.settings.clients.Count <= 0)
                {
                    usersItem = new UsersItem();
                    inbound.settings.clients.Add(usersItem);
                }
                else
                {
                    usersItem = inbound.settings.clients[0];
                }
                //远程服务器端口
                inbound.port = appConfig.port();

                //远程服务器用户ID
                usersItem.id = appConfig.id();
                usersItem.email = Global.userEMail;

                if (appConfig.configType() == (int)EConfigType.Vmess)
                {
                    inbound.protocol = Global.vmessProtocolLite;
                    usersItem.alterId = appConfig.alterId();

                }
                else if (appConfig.configType() == (int)EConfigType.VLESS)
                {
                    inbound.protocol = Global.vlessProtocolLite;
                    usersItem.alterId = 0;
                    usersItem.flow = appConfig.flow();
                    inbound.settings.decryption = appConfig.security();
                }

                //远程服务器底层传输配置
                StreamSettings streamSettings = inbound.streamSettings;
                boundStreamSettings(appConfig, "in", ref streamSettings);
            }
            catch
            {
            }
            return 0;
        }

        /// <summary>
        /// 传出设置
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="v2rayConfig"></param>
        /// <returns></returns>
        private static int ServerOutbound(V2rayNappConfig appConfig, ref V2rayClientConfig v2rayConfig)
        {
            try
            {
                if (v2rayConfig.outbounds[0] != null)
                {
                    v2rayConfig.outbounds[0].settings = null;
                }
            }
            catch
            {
            }
            return 0;
        }
        #endregion

        #region 导入(导出)客户端/服务端配置

        /// <summary>
        /// 导入v2ray客户端配置
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static NodeItem ImportFromClientConfig(string fileName, out string msg)
        {
            msg = string.Empty;
            NodeItem nodeItem = new NodeItem();

            try
            {
                //载入配置文件 
                string result = Utils.LoadResource(fileName);
                if (Utils.IsNullOrEmpty(result))
                {
                    msg = UIRes.I18N("FailedReadConfiguration");
                    return null;
                }

                //从Json加载
                V2rayClientConfig v2rayConfig = Utils.FromJson<V2rayClientConfig>(result);
                if (v2rayConfig == null)
                {
                    msg = UIRes.I18N("FailedConversionConfiguration");
                    return null;
                }

                if (v2rayConfig.outbounds == null
                 || v2rayConfig.outbounds.Count <= 0)
                {
                    msg = UIRes.I18N("IncorrectClientConfiguration");
                    return null;
                }

                Outbounds outbound = v2rayConfig.outbounds[0];
                if (outbound == null
                    || Utils.IsNullOrEmpty(outbound.protocol)
                    //|| outbound.protocol != Global.vmessProtocolLite
                    || outbound.settings == null
                    || outbound.settings.vnext == null
                    || outbound.settings.vnext.Count <= 0
                    || outbound.settings.vnext[0].users == null
                    || outbound.settings.vnext[0].users.Count <= 0)
                {
                    msg = UIRes.I18N("IncorrectClientConfiguration");
                    return null;
                }

                nodeItem.security = Global.DefaultSecurity;
                nodeItem.network = Global.DefaultNetwork;
                nodeItem.headerType = Global.None;
                nodeItem.address = outbound.settings.vnext[0].address;
                nodeItem.port = outbound.settings.vnext[0].port;
                nodeItem.id = outbound.settings.vnext[0].users[0].id;
                nodeItem.alterId = outbound.settings.vnext[0].users[0].alterId;
                nodeItem.remarks = string.Format("import@{0}", DateTime.Now.ToShortDateString());

                //tcp or kcp
                if (outbound.streamSettings != null
                    && outbound.streamSettings.network != null
                    && !Utils.IsNullOrEmpty(outbound.streamSettings.network))
                {
                    nodeItem.network = outbound.streamSettings.network;
                }

                //tcp伪装http
                if (outbound.streamSettings != null
                    && outbound.streamSettings.tcpSettings != null
                    && outbound.streamSettings.tcpSettings.header != null
                    && !Utils.IsNullOrEmpty(outbound.streamSettings.tcpSettings.header.type))
                {
                    if (outbound.streamSettings.tcpSettings.header.type.Equals(Global.TcpHeaderHttp))
                    {
                        nodeItem.headerType = outbound.streamSettings.tcpSettings.header.type;
                        string request = Convert.ToString(outbound.streamSettings.tcpSettings.header.request);
                        if (!Utils.IsNullOrEmpty(request))
                        {
                            V2rayTcpRequest v2rayTcpRequest = Utils.FromJson<V2rayTcpRequest>(request);
                            if (v2rayTcpRequest != null
                                && v2rayTcpRequest.headers != null
                                && v2rayTcpRequest.headers.Host != null
                                && v2rayTcpRequest.headers.Host.Count > 0)
                            {
                                nodeItem.requestHost = v2rayTcpRequest.headers.Host[0];
                            }
                        }
                    }
                }
                //kcp伪装
                if (outbound.streamSettings != null
                    && outbound.streamSettings.kcpSettings != null
                    && outbound.streamSettings.kcpSettings.header != null
                    && !Utils.IsNullOrEmpty(outbound.streamSettings.kcpSettings.header.type))
                {
                    nodeItem.headerType = outbound.streamSettings.kcpSettings.header.type;
                }

                //ws
                if (outbound.streamSettings != null
                    && outbound.streamSettings.wsSettings != null)
                {
                    if (!Utils.IsNullOrEmpty(outbound.streamSettings.wsSettings.path))
                    {
                        nodeItem.path = outbound.streamSettings.wsSettings.path;
                    }
                    if (outbound.streamSettings.wsSettings.headers != null
                      && !Utils.IsNullOrEmpty(outbound.streamSettings.wsSettings.headers.Host))
                    {
                        nodeItem.requestHost = outbound.streamSettings.wsSettings.headers.Host;
                    }
                }

                //h2
                if (outbound.streamSettings != null
                    && outbound.streamSettings.httpSettings != null)
                {
                    if (!Utils.IsNullOrEmpty(outbound.streamSettings.httpSettings.path))
                    {
                        nodeItem.path = outbound.streamSettings.httpSettings.path;
                    }
                    if (outbound.streamSettings.httpSettings.host != null
                        && outbound.streamSettings.httpSettings.host.Count > 0)
                    {
                        nodeItem.requestHost = Utils.List2String(outbound.streamSettings.httpSettings.host);
                    }
                }

                //tls
                if (outbound.streamSettings != null
                    && outbound.streamSettings.security != null
                    && outbound.streamSettings.security == Global.StreamSecurity)
                {
                    nodeItem.streamSecurity = Global.StreamSecurity;
                }

                //VLESS Reality
                if (outbound.streamSettings != null
                    && outbound.streamSettings.security != null
                    && outbound.streamSettings.security == Global.StreamSecurityReality)
                {
                    nodeItem.streamSecurity = Global.StreamSecurityReality;

                    //nodeItem.sni = outbound.sni;
                    nodeItem.fingerprint = outbound.streamSettings.realitySettings.fingerprint;
                    nodeItem.publicKey = outbound.streamSettings.realitySettings.publicKey;
                    nodeItem.shortId = outbound.streamSettings.realitySettings.shortId;
                    nodeItem.spiderX = outbound.streamSettings.realitySettings.spiderX;
                }
            }
            catch
            {
                msg = UIRes.I18N("IncorrectClientConfiguration");
                return null;
            }

            return nodeItem;
        }

        /// <summary>
        /// 导入v2ray服务端配置
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static NodeItem ImportFromServerConfig(string fileName, out string msg)
        {
            msg = string.Empty;
            NodeItem nodeItem = new NodeItem();

            try
            {
                //载入配置文件 
                string result = Utils.LoadResource(fileName);
                if (Utils.IsNullOrEmpty(result))
                {
                    msg = UIRes.I18N("FailedReadConfiguration");
                    return null;
                }

                //从Json加载
                V2rayClientConfig v2rayConfig = Utils.FromJson<V2rayClientConfig>(result);
                if (v2rayConfig == null)
                {
                    msg = UIRes.I18N("FailedConversionConfiguration");
                    return null;
                }

                if (v2rayConfig.inbounds == null
                 || v2rayConfig.inbounds.Count <= 0)
                {
                    msg = UIRes.I18N("IncorrectServerConfiguration");
                    return null;
                }

                Inbounds inbound = v2rayConfig.inbounds[0];
                if (inbound == null
                    || Utils.IsNullOrEmpty(inbound.protocol)
                    || inbound.protocol != Global.vmessProtocolLite
                    || inbound.settings == null
                    || inbound.settings.clients == null
                    || inbound.settings.clients.Count <= 0)
                {
                    msg = UIRes.I18N("IncorrectServerConfiguration");
                    return null;
                }

                nodeItem.security = Global.DefaultSecurity;
                nodeItem.network = Global.DefaultNetwork;
                nodeItem.headerType = Global.None;
                nodeItem.address = string.Empty;
                nodeItem.port = inbound.port;
                nodeItem.id = inbound.settings.clients[0].id;
                nodeItem.alterId = inbound.settings.clients[0].alterId;

                nodeItem.remarks = string.Format("import@{0}", DateTime.Now.ToShortDateString());

                //tcp or kcp
                if (inbound.streamSettings != null
                    && inbound.streamSettings.network != null
                    && !Utils.IsNullOrEmpty(inbound.streamSettings.network))
                {
                    nodeItem.network = inbound.streamSettings.network;
                }

                //tcp伪装http
                if (inbound.streamSettings != null
                    && inbound.streamSettings.tcpSettings != null
                    && inbound.streamSettings.tcpSettings.header != null
                    && !Utils.IsNullOrEmpty(inbound.streamSettings.tcpSettings.header.type))
                {
                    if (inbound.streamSettings.tcpSettings.header.type.Equals(Global.TcpHeaderHttp))
                    {
                        nodeItem.headerType = inbound.streamSettings.tcpSettings.header.type;
                        string request = Convert.ToString(inbound.streamSettings.tcpSettings.header.request);
                        if (!Utils.IsNullOrEmpty(request))
                        {
                            V2rayTcpRequest v2rayTcpRequest = Utils.FromJson<V2rayTcpRequest>(request);
                            if (v2rayTcpRequest != null
                                && v2rayTcpRequest.headers != null
                                && v2rayTcpRequest.headers.Host != null
                                && v2rayTcpRequest.headers.Host.Count > 0)
                            {
                                nodeItem.requestHost = v2rayTcpRequest.headers.Host[0];
                            }
                        }
                    }
                }
                //kcp伪装
                //if (v2rayConfig.outbound.streamSettings != null
                //    && v2rayConfig.outbound.streamSettings.kcpSettings != null
                //    && v2rayConfig.outbound.streamSettings.kcpSettings.header != null
                //    && !Utils.IsNullOrEmpty(v2rayConfig.outbound.streamSettings.kcpSettings.header.type))
                //{
                //    cmbHeaderType.Text = v2rayConfig.outbound.streamSettings.kcpSettings.header.type;
                //}

                //ws
                if (inbound.streamSettings != null
                    && inbound.streamSettings.wsSettings != null)
                {
                    if (!Utils.IsNullOrEmpty(inbound.streamSettings.wsSettings.path))
                    {
                        nodeItem.path = inbound.streamSettings.wsSettings.path;
                    }
                    if (inbound.streamSettings.wsSettings.headers != null
                      && !Utils.IsNullOrEmpty(inbound.streamSettings.wsSettings.headers.Host))
                    {
                        nodeItem.requestHost = inbound.streamSettings.wsSettings.headers.Host;
                    }
                }

                //h2
                if (inbound.streamSettings != null
                    && inbound.streamSettings.httpSettings != null)
                {
                    if (!Utils.IsNullOrEmpty(inbound.streamSettings.httpSettings.path))
                    {
                        nodeItem.path = inbound.streamSettings.httpSettings.path;
                    }
                    if (inbound.streamSettings.httpSettings.host != null
                        && inbound.streamSettings.httpSettings.host.Count > 0)
                    {
                        nodeItem.requestHost = Utils.List2String(inbound.streamSettings.httpSettings.host);
                    }
                }

                //tls
                if (inbound.streamSettings != null
                    && inbound.streamSettings.security != null
                    && inbound.streamSettings.security == Global.StreamSecurity)
                {
                    nodeItem.streamSecurity = Global.StreamSecurity;
                }
            }
            catch
            {
                msg = UIRes.I18N("IncorrectClientConfiguration");
                return null;
            }
            return nodeItem;
        }

        /// <summary>
        /// 从剪贴板导入URL
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static NodeItem ImportFromClipboardConfig(string clipboardData, out string msg)
        {
            msg = string.Empty;
            NodeItem nodeItem = new NodeItem();

            try
            {
                //载入配置文件 
                string result = clipboardData.TrimEx();// Utils.GetClipboardData();
                if (Utils.IsNullOrEmpty(result))
                {
                    msg = UIRes.I18N("FailedReadConfiguration");
                    return null;
                }

                if (result.StartsWith(Global.vmessProtocol))
                {
                    int indexSplit = result.IndexOf("?");
                    if (indexSplit > 0)
                    {
                        nodeItem = ResolveStdVmess(result) ?? ResolveVmess4Kitsunebi(result);
                    }
                    else
                    {
                        nodeItem.configType = (int)EConfigType.Vmess;
                        result = result.Substring(Global.vmessProtocol.Length);
                        result = Utils.Base64Decode(result);

                        //转成Json
                        VmessQRCode vmessQRCode = Utils.FromJson<VmessQRCode>(result);
                        if (vmessQRCode == null)
                        {
                            msg = UIRes.I18N("FailedConversionConfiguration");
                            return null;
                        }
                        nodeItem.security = Global.DefaultSecurity;
                        nodeItem.network = Global.DefaultNetwork;
                        nodeItem.headerType = Global.None;


                        nodeItem.configVersion = Utils.ToInt(vmessQRCode.v);
                        nodeItem.remarks = Utils.ToString(vmessQRCode.ps);
                        nodeItem.address = Utils.ToString(vmessQRCode.add);
                        nodeItem.port = Utils.ToInt(vmessQRCode.port);
                        nodeItem.id = Utils.ToString(vmessQRCode.id);
                        nodeItem.alterId = Utils.ToInt(vmessQRCode.aid);

                        if (!Utils.IsNullOrEmpty(vmessQRCode.net))
                        {
                            nodeItem.network = vmessQRCode.net;
                        }
                        if (!Utils.IsNullOrEmpty(vmessQRCode.type))
                        {
                            nodeItem.headerType = vmessQRCode.type;
                        }

                        nodeItem.requestHost = Utils.ToString(vmessQRCode.host);
                        nodeItem.path = Utils.ToString(vmessQRCode.path);
                        nodeItem.streamSecurity = Utils.ToString(vmessQRCode.tls);
                    }

                    AppConfigHandler.UpgradeServerVersion(ref nodeItem);
                }
                else if (result.StartsWith(Global.ssProtocol))
                {
                    msg = UIRes.I18N("ConfigurationFormatIncorrect");

                    nodeItem = ResolveSSLegacy(result);
                    if (nodeItem == null)
                    {
                        nodeItem = ResolveSip002(result);
                    }
                    if (nodeItem == null)
                    {
                        return null;
                    }
                    if (nodeItem.address.Length == 0 || nodeItem.port == 0 || nodeItem.security.Length == 0 || nodeItem.id.Length == 0)
                    {
                        return null;
                    }

                    nodeItem.configType = (int)EConfigType.Shadowsocks;
                }
                else if (result.StartsWith(Global.socksProtocol))
                {
                    msg = UIRes.I18N("ConfigurationFormatIncorrect");

                    nodeItem.configType = (int)EConfigType.Socks;
                    result = result.Substring(Global.socksProtocol.Length);
                    //remark
                    int indexRemark = result.IndexOf("#");
                    if (indexRemark > 0)
                    {
                        try
                        {
                            nodeItem.remarks = WebUtility.UrlDecode(result.Substring(indexRemark + 1, result.Length - indexRemark - 1));
                        }
                        catch { }
                        result = result.Substring(0, indexRemark);
                    }
                    //part decode
                    int indexS = result.IndexOf("@");
                    if (indexS > 0)
                    {
                    }
                    else
                    {
                        result = Utils.Base64Decode(result);
                    }

                    string[] arr1 = result.Split('@');
                    if (arr1.Length != 2)
                    {
                        return null;
                    }
                    string[] arr21 = arr1[0].Split(':');
                    //string[] arr22 = arr1[1].Split(':');
                    int indexPort = arr1[1].LastIndexOf(":");
                    if (arr21.Length != 2 || indexPort < 0)
                    {
                        return null;
                    }
                    nodeItem.address = arr1[1].Substring(0, indexPort);
                    nodeItem.port = Utils.ToInt(arr1[1].Substring(indexPort + 1, arr1[1].Length - (indexPort + 1)));
                    nodeItem.security = arr21[0];
                    nodeItem.id = arr21[1];
                }
                else if (result.StartsWith(Global.trojanProtocol))
                {
                    msg = UIRes.I18N("ConfigurationFormatIncorrect");

                    nodeItem.configType = (int)EConfigType.Trojan;

                    Uri uri = new Uri(result);
                    nodeItem.address = uri.IdnHost;
                    nodeItem.port = uri.Port;
                    nodeItem.id = uri.UserInfo;

                    var qurery = HttpUtility.ParseQueryString(uri.Query);
                    nodeItem.requestHost = qurery["sni"] ?? "";

                    var remarks = uri.Fragment.Replace("#", "");
                    if (Utils.IsNullOrEmpty(remarks))
                    {
                        nodeItem.remarks = "NONE";
                    }
                    else
                    {
                        nodeItem.remarks = WebUtility.UrlDecode(remarks);
                    }                     
                }
                else if (result.StartsWith(Global.vlessProtocol))
                {
                    nodeItem.configType = (int)EConfigType.VLESS;
                    nodeItem.security = "none";

                    Uri url = new Uri(result);

                    nodeItem.address = url.IdnHost;
                    nodeItem.port = url.Port;
                    nodeItem.remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
                    nodeItem.id = url.UserInfo;

                    var query = HttpUtility.ParseQueryString(url.Query);
                    nodeItem.security = query["encryption"] ?? "none";
                    nodeItem.streamSecurity = query["security"] ?? "";

                    ResolveStdTransport(query, ref nodeItem);

                }
                else if (result.StartsWith(Global.hy2Protocol))
                {
                    nodeItem.configType = (int)EConfigType.Hysteria2;
                    nodeItem.network = "hysteria2";

                    Uri url = new Uri(result);
                    nodeItem.address = url.IdnHost;
                    nodeItem.port = url.Port;
                    nodeItem.remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
                    nodeItem.id = url.UserInfo;

                    nodeItem.streamSecurity = "tls";

                    var query = HttpUtility.ParseQueryString(url.Query);
                    if (query["insecure"] == "1")
                    {
                        nodeItem.allowInsecure = "true";
                    }
                    else if (query["insecure"] == "0")
                    {
                        nodeItem.allowInsecure = "false";
                    }
                }
                else
                {
                    msg = UIRes.I18N("NonvmessOrssProtocol");
                    return null;
                }
            }
            catch
            {
                msg = UIRes.I18N("Incorrectconfiguration");
                return null;
            }

            return nodeItem;
        }

        private static int ResolveStdTransport(NameValueCollection query, ref NodeItem item)
        {
            item.flow = query["flow"] ?? "";
            item.streamSecurity = query["security"] ?? "";

            item.sni = query["sni"] ?? "";
            //item.alpn = WebUtility.UrlDecode(query["alpn"] ?? "");
            item.fingerprint = WebUtility.UrlDecode(query["fp"] ?? "");
            item.publicKey = WebUtility.UrlDecode(query["pbk"] ?? "");
            item.shortId = WebUtility.UrlDecode(query["sid"] ?? "");
            item.spiderX = WebUtility.UrlDecode(query["spx"] ?? "");

            item.network = query["type"] ?? "tcp";
            switch (item.network)
            {
                case "tcp":
                    item.headerType = query["headerType"] ?? "none";
                    item.requestHost = WebUtility.UrlDecode(query["host"] ?? "");

                    break;
                case "kcp":
                    item.headerType = query["headerType"] ?? "none";
                    item.path = WebUtility.UrlDecode(query["seed"] ?? "");
                    break;

                case "ws":
                    item.requestHost = WebUtility.UrlDecode(query["host"] ?? "");
                    item.path = WebUtility.UrlDecode(query["path"] ?? "/");
                    break;

                case "http":
                case "h2":
                    item.network = "h2";
                    item.requestHost = WebUtility.UrlDecode(query["host"] ?? "");
                    item.path = WebUtility.UrlDecode(query["path"] ?? "/");
                    break;

                case "quic":
                    item.headerType = query["headerType"] ?? "none";
                    item.requestHost = query["quicSecurity"] ?? "none";
                    item.path = WebUtility.UrlDecode(query["key"] ?? "");
                    break;
                default:
                    break;
            }
            return 0;
        }


        /// <summary>
        /// 导出为客户端配置
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="fileName"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static int Export2ClientConfig(V2rayNappConfig appConfig, string fileName, out string msg)
        {
            return GenerateClientConfig(appConfig, fileName, true, out msg);
        }

        /// <summary>
        /// 导出为服务端配置
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="fileName"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static int Export2ServerConfig(V2rayNappConfig appConfig, string fileName, out string msg)
        {
            return GenerateServerConfig(appConfig, fileName, out msg);
        }

        private static NodeItem ResolveVmess4Kitsunebi(string result)
        {
            NodeItem nodeItem = new NodeItem
            {
                configType = (int)EConfigType.Vmess
            };
            result = result.Substring(Global.vmessProtocol.Length);
            int indexSplit = result.IndexOf("?");
            if (indexSplit > 0)
            {
                result = result.Substring(0, indexSplit);
            }
            result = Utils.Base64Decode(result);

            string[] arr1 = result.Split('@');
            if (arr1.Length != 2)
            {
                return null;
            }
            string[] arr21 = arr1[0].Split(':');
            string[] arr22 = arr1[1].Split(':');
            if (arr21.Length != 2 || arr21.Length != 2)
            {
                return null;
            }

            nodeItem.address = arr22[0];
            nodeItem.port = Utils.ToInt(arr22[1]);
            nodeItem.security = arr21[0];
            nodeItem.id = arr21[1];

            nodeItem.network = Global.DefaultNetwork;
            nodeItem.headerType = Global.None;
            nodeItem.remarks = "Alien";
            nodeItem.alterId = 0;

            return nodeItem;
        }

        private static NodeItem ResolveSip002(string result)
        {
            Uri parsedUrl;
            try
            {
                parsedUrl = new Uri(result);
            }
            catch (UriFormatException)
            {
                return null;
            }
            NodeItem server = new NodeItem
            {
                remarks = parsedUrl.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
                address = parsedUrl.IdnHost,
                port = parsedUrl.Port,
            };

            // parse base64 UserInfo
            string rawUserInfo = parsedUrl.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped);
            string base64 = rawUserInfo.Replace('-', '+').Replace('_', '/');    // Web-safe base64 to normal base64
            string userInfo;
            try
            {
                userInfo = Encoding.UTF8.GetString(Convert.FromBase64String(
                base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=')));
            }
            catch (FormatException)
            {
                return null;
            }
            string[] userInfoParts = userInfo.Split(new char[] { ':' }, 2);
            if (userInfoParts.Length != 2)
            {
                return null;
            }
            server.security = userInfoParts[0];
            server.id = userInfoParts[1];

            NameValueCollection queryParameters = HttpUtility.ParseQueryString(parsedUrl.Query);
            if (queryParameters["plugin"] != null)
            {
                return null;
            }

            return server;
        }

        private static readonly Regex SSUrlFinder = new Regex(@"ss://(?<base64>[A-Za-z0-9+-/=_]+)(?:#(?<tag>\S+))?", RegexOptions.IgnoreCase);
        private static readonly Regex DetailsParser = new Regex(@"^((?<method>.+?):(?<password>.*)@(?<hostname>.+?):(?<port>\d+?))$", RegexOptions.IgnoreCase);

        private static NodeItem ResolveSSLegacy(string result)
        {
            var match = SSUrlFinder.Match(result);
            if (!match.Success)
                return null;

            NodeItem server = new NodeItem();
            var base64 = match.Groups["base64"].Value.TrimEnd('/');
            var tag = match.Groups["tag"].Value;
            if (!tag.IsNullOrEmpty())
            {
                server.remarks = HttpUtility.UrlDecode(tag, Encoding.UTF8);
            }
            Match details;
            try
            {
                details = DetailsParser.Match(Encoding.UTF8.GetString(Convert.FromBase64String(
                    base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='))));
            }
            catch (FormatException)
            {
                return null;
            }
            if (!details.Success)
                return null;
            server.security = details.Groups["method"].Value;
            server.id = details.Groups["password"].Value;
            server.address = details.Groups["hostname"].Value;
            server.port = int.Parse(details.Groups["port"].Value);
            return server;
        }


        private static readonly Regex StdVmessUserInfo = new Regex(
            @"^(?<network>[a-z]+)(\+(?<streamSecurity>[a-z]+))?:(?<id>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})-(?<alterId>[0-9]+)$");

        private static NodeItem ResolveStdVmess(string result)
        {
            NodeItem item = new NodeItem
            {
                configType = (int)EConfigType.Vmess,
                security = "auto"
            };

            Uri u = new Uri(result);

            item.address = u.IdnHost;
            item.port = u.Port;
            item.remarks = u.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
            var q = HttpUtility.ParseQueryString(u.Query);

            var m = StdVmessUserInfo.Match(u.UserInfo);
            if (!m.Success) return null;

            item.id = m.Groups["id"].Value;
            if (!int.TryParse(m.Groups["alterId"].Value, out int aid))
            {
                return null;
            }
            item.alterId = aid;

            if (m.Groups["streamSecurity"].Success)
            {
                item.streamSecurity = m.Groups["streamSecurity"].Value;
            }
            switch (item.streamSecurity)
            {
                case "tls":
                    // TODO tls appConfig
                    break;
                default:
                    if (!string.IsNullOrWhiteSpace(item.streamSecurity))
                        return null;
                    break;
            }

            item.network = m.Groups["network"].Value;
            switch (item.network)
            {
                case "tcp":
                    string t1 = q["type"] ?? "none";
                    item.headerType = t1;
                    // TODO http option

                    break;
                case "kcp":
                    item.headerType = q["type"] ?? "none";
                    // TODO kcp seed
                    break;

                case "ws":
                    string p1 = q["path"] ?? "/";
                    string h1 = q["host"] ?? "";
                    item.requestHost = h1;
                    item.path = p1;
                    break;

                case "http":
                    item.network = "h2";
                    string p2 = q["path"] ?? "/";
                    string h2 = q["host"] ?? "";
                    item.requestHost = h2;
                    item.path = p2;
                    break;

                case "quic":
                    string s = q["security"] ?? "none";
                    string k = q["key"] ?? "";
                    string t3 = q["type"] ?? "none";
                    item.headerType = t3;
                    item.requestHost = s;
                    item.path = k;
                    break;

                default:
                    return null;
            }

            return item;
        }
        #endregion

        #region Gen speedtest config


        public static string GenerateClientSpeedtestConfigString(V2rayNappConfig appConfig, List<int> selecteds, out string msg)
        {
            try
            {
                if (appConfig == null
                    || appConfig.index < 0
                    || appConfig.outbound.Count <= 0
                    || appConfig.index > appConfig.outbound.Count - 1
                    )
                {
                    msg = UIRes.I18N("CheckServerSettings");
                    return "";
                }

                msg = UIRes.I18N("InitialConfiguration");

                V2rayNappConfig appConfigCopy = Utils.DeepCopy(appConfig);

                //取得默认配置
                string result = Utils.GetEmbedText(SampleClient);
                if (Utils.IsNullOrEmpty(result))
                {
                    msg = UIRes.I18N("FailedGetDefaultConfiguration");
                    return "";
                }

                //从Json加载
                V2rayClientConfig v2rayConfig = Utils.FromJson<V2rayClientConfig>(result);
                if (v2rayConfig == null)
                {
                    msg = UIRes.I18N("FailedGenDefaultConfiguration");
                    return "";
                }

                log(appConfigCopy, ref v2rayConfig, false);

                dns(appConfigCopy, ref v2rayConfig);

                // Sockopt
                sockopt(appConfigCopy, ref v2rayConfig);

                v2rayConfig.inbounds.RemoveAt(0); // Remove "proxy" service for speedtest, avoiding port conflicts.

                int httpPort = appConfigCopy.GetLocalPort("speedtest");
                foreach (int index in selecteds)
                {
                    if (appConfigCopy.outbound[index].configType == (int)EConfigType.Custom)
                    {
                        continue;
                    }

                    appConfigCopy.index = index;

                    Inbounds inbound = new Inbounds
                    {
                        listen = Global.Loopback,
                        port = httpPort + index,
                        protocol = Global.InboundHttp
                    };
                    inbound.tag = Global.InboundHttp + inbound.port.ToString();
                    v2rayConfig.inbounds.Add(inbound);


                    V2rayClientConfig v2rayConfigCopy = Utils.FromJson<V2rayClientConfig>(result);
                    outbound(appConfigCopy, ref v2rayConfigCopy);

                    // Sockopt
                    sockopt(appConfigCopy, ref v2rayConfigCopy);

                    v2rayConfigCopy.outbounds[0].tag = Global.agentTag + inbound.port.ToString();
                    v2rayConfig.outbounds.Add(v2rayConfigCopy.outbounds[0]);

                    RulesItem rule = new RulesItem
                    {
                        inboundTag = new List<string> { inbound.tag },
                        outboundTag = v2rayConfigCopy.outbounds[0].tag,
                        type = "field"
                    };
                    v2rayConfig.routing.rules.Add(rule);
                }

                msg = string.Format(UIRes.I18N("SuccessfulConfiguration"), appConfigCopy.getSummary());
                return Utils.ToJson(v2rayConfig);
            }
            catch
            {
                msg = UIRes.I18N("FailedGenDefaultConfiguration");
                return "";
            }
        }

        #endregion

    }
}
