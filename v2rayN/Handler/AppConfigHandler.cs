using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using v2rayN.Mode;
using v2rayN.Base;
using System.Linq;
using v2rayN.Tool;
using System.Security.Cryptography.X509Certificates;

namespace v2rayN.Handler
{
    /// <summary>
    /// 本软件配置文件处理类
    /// </summary>
    class AppConfigHandler
    {
        private static string configRes = Global.ConfigFileName;

        /// <summary>
        /// 载入配置文件
        /// </summary>
        /// <param name="appConfig"></param>
        /// <returns></returns>
        public static int LoadConfig(ref V2rayNappConfig appConfig)
        {
            //载入配置文件 
            string result = Utils.LoadResource(Utils.GetPath(configRes));
            if (!Utils.IsNullOrEmpty(result))
            {
                //从Json加载
                appConfig = Utils.FromJson<V2rayNappConfig>(result);
            }
            if (appConfig == null)
            {
                appConfig = new V2rayNappConfig
                {
                    index = -1,
                    logEnabled = false,
                    loglevel = "warning",
                    outbound = new List<NodeItem>(),

                    //Mux
                    muxEnabled = false,

                    ////默认监听端口
                    //appConfig.pacPort = 8888;

                    // 默认不开启统计
                    enableStatistics = false,

                    // 默认中等刷新率
                    statisticsFreshRate = (int)Global.StatisticsFreshRate.medium,

                    // Sockopt
                    sockoptTag = "",

                    // 下一跳socks端口
                    socksOutboundIP = "127.0.0.1",
                    socksOutboundPort = 10086,

                    // tls hello 分片
                    tlsHelloFgmLength = "40-60",
                    tlsHelloFgmInterval = "30-50"
                };
            }

            //本地监听
            if (appConfig.inbound == null)
            {
                appConfig.inbound = new List<InItem>();
                InItem inItem = new InItem
                {
                    protocol = Global.InboundSocks,
                    localPort = 10808,
                    udpEnabled = true,
                    sniffingEnabled = true
                };

                appConfig.inbound.Add(inItem);

            }
            else
            {
                //http协议不由core提供,只保留socks
                if (appConfig.inbound.Count > 0)
                {
                    appConfig.inbound[0].protocol = Global.InboundSocks;
                }
            }
            //路由规则
            if (Utils.IsNullOrEmpty(appConfig.domainStrategy))
            {
                appConfig.domainStrategy = "IPIfNonMatch";
            }
            if (Utils.IsNullOrEmpty(appConfig.routingMode))
            {
                appConfig.routingMode = "0";
            }
            if (appConfig.useragent == null)
            {
                appConfig.useragent = new List<string>();
            }
            if (appConfig.userdirect == null)
            {
                appConfig.userdirect = new List<string>();
            }
            if (appConfig.userblock == null)
            {
                appConfig.userblock = new List<string>();
            }
            //kcp
            if (appConfig.kcpItem == null)
            {
                appConfig.kcpItem = new KcpItem
                {
                    mtu = 1350,
                    tti = 50,
                    uplinkCapacity = 12,
                    downlinkCapacity = 100,
                    readBufferSize = 2,
                    writeBufferSize = 2,
                    congestion = false
                };
            }
            if (appConfig.uiItem == null)
            {
                appConfig.uiItem = new UIItem();
            }
            if (appConfig.uiItem.mainLvColWidth == null)
            {
                appConfig.uiItem.mainLvColWidth = new Dictionary<string, int>();
            }

            //// 如果是用户升级，首次会有端口号为0的情况，不可用，这里处理
            //if (appConfig.pacPort == 0)
            //{
            //    appConfig.pacPort = 8888;
            //}
            if (Utils.IsNullOrEmpty(appConfig.speedTestUrl))
            {
                appConfig.speedTestUrl = Global.SpeedTestUrl;
            }
            if (Utils.IsNullOrEmpty(appConfig.delayTestUrl))
            {
                appConfig.delayTestUrl = Global.DelayTestUrl;
            }
            if (Utils.IsNullOrEmpty(appConfig.urlGFWList))
            {
                appConfig.urlGFWList = Global.GFWLIST_URL;
            }
            //if (Utils.IsNullOrEmpty(appConfig.remoteDNS))
            //{
            //    appConfig.remoteDNS = "1.1.1.1";
            //}

            if (appConfig.subItem == null)
            {
                appConfig.subItem = new List<SubItem>();
            }
            if (appConfig.userPacRule == null)
            {
                appConfig.userPacRule = new List<string>();
            }

            if (appConfig == null
                || appConfig.index < 0
                || appConfig.outbound.Count <= 0
                || appConfig.index > appConfig.outbound.Count - 1
                )
            {
                Global.reloadV2ray = false;
            }
            else
            {
                Global.reloadV2ray = true;

                //版本升级
                for (int i = 0; i < appConfig.outbound.Count; i++)
                {
                    NodeItem nodeItem = appConfig.outbound[i];
                    UpgradeServerVersion(ref nodeItem);
                }
            }

            return 0;
        }

        /// <summary>
        /// 添加服务器或编辑
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="nodeItem"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int AddServer(ref V2rayNappConfig appConfig, NodeItem nodeItem, int index)
        {
            nodeItem.configVersion = 2;
            nodeItem.configType = (int)EConfigType.Vmess;

            nodeItem.address = nodeItem.address.TrimEx();
            nodeItem.id = nodeItem.id.TrimEx();
            nodeItem.security = nodeItem.security.TrimEx();
            nodeItem.network = nodeItem.network.TrimEx();
            nodeItem.headerType = nodeItem.headerType.TrimEx();
            nodeItem.requestHost = nodeItem.requestHost.TrimEx();
            nodeItem.path = nodeItem.path.TrimEx();
            nodeItem.streamSecurity = nodeItem.streamSecurity.TrimEx();

            if (index >= 0)
            {
                //修改
                appConfig.outbound[index] = nodeItem;
                if (appConfig.index.Equals(index))
                {
                    Global.reloadV2ray = true;
                }
            }
            else
            {
                //添加
                if (Utils.IsNullOrEmpty(nodeItem.allowInsecure))
                {
                    nodeItem.allowInsecure = appConfig.defAllowInsecure.ToString();
                }
                appConfig.outbound.Add(nodeItem);
                if (appConfig.outbound.Count == 1)
                {
                    appConfig.index = 0;
                    Global.reloadV2ray = true;
                }
            }

            ToJsonFile(appConfig);

            return 0;
        }

        /// <summary>
        /// 移除服务器
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int RemoveServer(ref V2rayNappConfig appConfig, int index)
        {
            if (index < 0 || index > appConfig.outbound.Count - 1)
            {
                return -1;
            }

            //删除
            appConfig.outbound.RemoveAt(index);


            //移除的是活动的
            if (appConfig.index.Equals(index))
            {
                if (appConfig.outbound.Count > 0)
                {
                    appConfig.index = 0;
                }
                else
                {
                    appConfig.index = -1;
                }
                Global.reloadV2ray = true;
            }
            else if (index < appConfig.index)//移除活动之前的
            {
                appConfig.index--;
                Global.reloadV2ray = true;
            }

            ToJsonFile(appConfig);

            return 0;
        }

        /// <summary>
        /// 克隆服务器
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int CopyServer(ref V2rayNappConfig appConfig, int index)
        {
            if (index < 0 || index > appConfig.outbound.Count - 1)
            {
                return -1;
            }

            NodeItem vmessItem = Utils.DeepCopy(appConfig.outbound[index]);
            vmessItem.remarks = string.Format("{0}-clone", appConfig.outbound[index].remarks);
            /*
            NodeItem nodeItem = new NodeItem
            {
                configVersion = appConfig.outbound[index].configVersion,
                address = appConfig.outbound[index].address,
                port = appConfig.outbound[index].port,
                id = appConfig.outbound[index].id,
                alterId = appConfig.outbound[index].alterId,
                security = appConfig.outbound[index].security,
                network = appConfig.outbound[index].network,
                remarks = string.Format("{0}-clone", appConfig.outbound[index].remarks),
                headerType = appConfig.outbound[index].headerType,
                requestHost = appConfig.outbound[index].requestHost,
                path = appConfig.outbound[index].path,
                streamSecurity = appConfig.outbound[index].streamSecurity,
                allowInsecure = appConfig.outbound[index].allowInsecure,
                configType = appConfig.outbound[index].configType
            };
            */

            appConfig.outbound.Insert(index + 1, vmessItem); // 插入到下一项

            ToJsonFile(appConfig);

            return index + 1;
        }

        /// <summary>
        /// 设置活动服务器
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int SetDefaultServer(ref V2rayNappConfig appConfig, int index)
        {
            if (index < 0 || index > appConfig.outbound.Count - 1)
            {
                return -1;
            }

            ////和现在相同
            //if (appConfig.index.Equals(index))
            //{
            //    return -1;
            //}
            appConfig.index = index;
            Global.reloadV2ray = true;

            ToJsonFile(appConfig);

            return 0;
        }

        /// <summary>
        /// 保参数
        /// </summary>
        /// <param name="appConfig"></param>
        /// <returns></returns>
        public static int SaveConfig(ref V2rayNappConfig appConfig, bool reload = true)
        {
            Global.reloadV2ray = reload;

            ToJsonFile(appConfig);

            return 0;
        }

        /// <summary>
        /// 存储文件
        /// </summary>
        /// <param name="appConfig"></param>
        private static void ToJsonFile(V2rayNappConfig appConfig)
        {
            Utils.ToJsonFile(appConfig, Utils.GetPath(configRes));
        }

        /// <summary>
        /// 取得服务器QRCode配置
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string GetVmessQRCode(V2rayNappConfig appConfig, int index)
        {
            try
            {
                string url = string.Empty;

                NodeItem nodeItem = appConfig.outbound[index];
                if (nodeItem.configType == (int)EConfigType.Vmess)
                {
                    VmessQRCode vmessQRCode = new VmessQRCode
                    {
                        v = nodeItem.configVersion.ToString(),
                        ps = nodeItem.remarks.TrimEx(), //备注也许很长 ;
                        add = nodeItem.address,
                        port = nodeItem.port.ToString(),
                        id = nodeItem.id,
                        aid = nodeItem.alterId.ToString(),
                        net = nodeItem.network,
                        type = nodeItem.headerType,
                        host = nodeItem.requestHost,
                        path = nodeItem.path,
                        tls = nodeItem.streamSecurity
                    };

                    url = Utils.ToJson(vmessQRCode);
                    url = Utils.Base64Encode(url);
                    url = string.Format("{0}{1}", Global.vmessProtocol, url);

                }
                else if (nodeItem.configType == (int)EConfigType.Shadowsocks)
                {
                    string remark = string.Empty;
                    if (!Utils.IsNullOrEmpty(nodeItem.remarks))
                    {
                        remark = "#" + WebUtility.UrlEncode(nodeItem.remarks);
                    }
                    url = string.Format("{0}:{1}@{2}:{3}",
                        nodeItem.security,
                        nodeItem.id,
                        nodeItem.address,
                        nodeItem.port);
                    url = Utils.Base64Encode(url);
                    url = string.Format("{0}{1}{2}", Global.ssProtocol, url, remark);
                }
                else if (nodeItem.configType == (int)EConfigType.Socks)
                {
                    string remark = string.Empty;
                    if (!Utils.IsNullOrEmpty(nodeItem.remarks))
                    {
                        remark = "#" + WebUtility.UrlEncode(nodeItem.remarks);
                    }
                    url = string.Format("{0}:{1}@{2}:{3}",
                        nodeItem.security,
                        nodeItem.id,
                        nodeItem.address,
                        nodeItem.port);
                    url = Utils.Base64Encode(url);
                    url = string.Format("{0}{1}{2}", Global.socksProtocol, url, remark);
                }
                else if (nodeItem.configType == (int)EConfigType.Trojan)
                {
                    string remark = string.Empty;
                    if (!Utils.IsNullOrEmpty(nodeItem.remarks))
                    {
                        remark = "#" + WebUtility.UrlEncode(nodeItem.remarks);
                    }
                    string query = string.Empty;
                    if (!Utils.IsNullOrEmpty(nodeItem.requestHost))
                    {
                        query = string.Format("?sni={0}", nodeItem.requestHost);
                    }
                    url = string.Format("{0}@{1}:{2}",
                        nodeItem.id,
                        nodeItem.address,
                        nodeItem.port);
                    url = string.Format("{0}{1}{2}{3}", Global.trojanProtocol, url, query, remark);
                }
                else if (nodeItem.configType == (int)EConfigType.VLESS)
                {
                    string remark = string.Empty;
                    if (!Utils.IsNullOrEmpty(nodeItem.remarks))
                    {
                        remark = "#" + WebUtility.UrlEncode(nodeItem.remarks);
                    }
                    var dicQuery = new Dictionary<string, string>();
                    if (!Utils.IsNullOrEmpty(nodeItem.security))
                    {
                        dicQuery.Add("encryption", nodeItem.security);
                    }
                    else
                    {
                        dicQuery.Add("encryption", "none");
                    }
                    GetStdTransport(nodeItem, "none", ref dicQuery);
                    string query = "?" + string.Join("&", dicQuery.Select(x => x.Key + "=" + x.Value).ToArray());

                    url = string.Format("{0}@{1}:{2}",
                    nodeItem.id,
                    GetIpv6(nodeItem.address),
                    nodeItem.port);
                    url = $"{Global.vlessProtocol}{url}{query}{remark}";
                }
                else if (nodeItem.configType == (int)EConfigType.Hysteria2)
                {
                    string remark = string.Empty;
                    if (!Utils.IsNullOrEmpty(nodeItem.remarks))
                    {
                        remark = "#" + WebUtility.UrlEncode(nodeItem.remarks);
                    }
                    var dicQuery = new Dictionary<string, string>();
                    if (nodeItem.allowInsecure == "true")
                    {
                        dicQuery.Add("insecure", "1");
                    }
                    if (!string.IsNullOrWhiteSpace(nodeItem.pinSHA256))
                    {
                        dicQuery.Add("pinSHA256", nodeItem.pinSHA256);
                    }
                    string query = "?" + string.Join("&", dicQuery.Select(x => x.Key + "=" + x.Value).ToArray());

                    url = string.Format("{0}@{1}:{2}",
                    nodeItem.id,
                    GetIpv6(nodeItem.address),
                    nodeItem.port);
                    url = $"{Global.hy2Protocol}{url}{query}{remark}";
                }

                return url;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 移动服务器
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="index"></param>
        /// <param name="eMove"></param>
        /// <returns>移动后的位置</returns>
        public static int MoveServer(ref V2rayNappConfig appConfig, int index, EMove eMove)
        {
            int count = appConfig.outbound.Count;
            if (index < 0 || index > appConfig.outbound.Count - 1)
            {
                return -1;
            }
            switch (eMove)
            {
                case EMove.Top:
                    {
                        // 如果当前项已经是顶端, 则不需处理, 直接返回
                        if (index == 0)
                        {
                            return 0;
                        }
                        NodeItem nodeItem = Utils.DeepCopy(appConfig.outbound[index]);
                        appConfig.outbound.RemoveAt(index);
                        appConfig.outbound.Insert(0, nodeItem);
                        if (index < appConfig.index)
                        {
                            //
                        }
                        else if (appConfig.index == index)
                        {
                            appConfig.index = 0;
                        }
                        else
                        {
                            appConfig.index++;
                        }

                        // 移动TOP正常处理完, 当前列表项位置应该为TOP, 即 0
                        index = 0;  

                        break;
                    }
                case EMove.Up:
                    {
                        // 如果当前项已经是顶端, 则不需处理, 直接返回
                        if (index == 0)
                        {
                            return 0;
                        }
                        NodeItem nodeItem = Utils.DeepCopy(appConfig.outbound[index]);
                        appConfig.outbound.RemoveAt(index);
                        appConfig.outbound.Insert(index - 1, nodeItem);
                        if (index == appConfig.index + 1)
                        {
                            appConfig.index++;
                        }
                        else if (appConfig.index == index)
                        {
                            appConfig.index--;
                        }

                        // 移动UP正常处理完, 当前列表项位置上移一位
                        index = index - 1;

                        break;
                    }
                case EMove.Down:
                    {
                        // 如果当前项已经是底端, 则不需处理, 直接返回
                        if (index == count - 1)
                        {
                            return count - 1;
                        }
                        NodeItem noteItem = Utils.DeepCopy(appConfig.outbound[index]);
                        appConfig.outbound.RemoveAt(index);
                        appConfig.outbound.Insert(index + 1, noteItem);
                        if (index == appConfig.index - 1)
                        {
                            appConfig.index--;
                        }
                        else if (appConfig.index == index)
                        {
                            appConfig.index++;
                        }

                        // 移动DOWN正常处理完, 当前列表项位置下移一位
                        index = index + 1;

                        break;
                    }
                case EMove.Bottom:
                    {
                        // 如果当前项已经是底端, 则不需处理, 直接返回
                        if (index == count - 1)
                        {
                            return count - 1;
                        }
                        NodeItem nodeItem = Utils.DeepCopy(appConfig.outbound[index]);
                        appConfig.outbound.RemoveAt(index);
                        appConfig.outbound.Add(nodeItem);
                        if (index < appConfig.index)
                        {
                            appConfig.index--;
                        }
                        else if (appConfig.index == index)
                        {
                            appConfig.index = count - 1;
                        }
                        else
                        {
                            //
                        }

                        // 移动BOTTOM正常处理完, 当前列表项位置应该为BOTTOM, 即 数量的数字-1
                        index = count - 1;

                        break;
                    }

            }
            Global.reloadV2ray = true;

            ToJsonFile(appConfig);

            // 返回  移动操作处理完后, 列表项当前所在位置
            return index;
        }

        /// <summary>
        /// 添加自定义服务器
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static int AddCustomServer(ref V2rayNappConfig appConfig, string fileName)
        {
            string newFileName = string.Format("{0}.json", Utils.GetGUID());
            //newFileName = Path.Combine(Utils.GetTempPath(), newFileName);

            try
            {
                File.Copy(fileName, Path.Combine(Utils.GetTempPath(), newFileName));
            }
            catch
            {
                return -1;
            }

            NodeItem nodeItem = new NodeItem
            {
                address = newFileName,
                configType = (int)EConfigType.Custom,
                remarks = string.Format("import custom@{0}", DateTime.Now.ToShortDateString())
            };

            appConfig.outbound.Add(nodeItem);
            if (appConfig.outbound.Count == 1)
            {
                appConfig.index = 0;
                Global.reloadV2ray = true;
            }

            ToJsonFile(appConfig);

            return 0;
        }

        /// <summary>
        /// 添加服务器或编辑
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="nodeItem"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int EditCustomServer(ref V2rayNappConfig appConfig, NodeItem nodeItem, int index)
        {
            //修改
            appConfig.outbound[index] = nodeItem;
            if (appConfig.index.Equals(index))
            {
                Global.reloadV2ray = true;
            }

            ToJsonFile(appConfig);

            return 0;
        }

        /// <summary>
        /// 添加服务器或编辑
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="ssItem"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int AddShadowsocksServer(ref V2rayNappConfig appConfig, NodeItem ssItem, int index)
        {
            ssItem.configVersion = 2;
            ssItem.configType = (int)EConfigType.Shadowsocks;

            ssItem.address = ssItem.address.TrimEx();
            ssItem.id = ssItem.id.TrimEx();
            ssItem.security = ssItem.security.TrimEx();

            if (index >= 0)
            {
                //修改
                appConfig.outbound[index] = ssItem;
                if (appConfig.index.Equals(index))
                {
                    Global.reloadV2ray = true;
                }
            }
            else
            {
                //添加
                appConfig.outbound.Add(ssItem);
                if (appConfig.outbound.Count == 1)
                {
                    appConfig.index = 0;
                    Global.reloadV2ray = true;
                }
            }

            ToJsonFile(appConfig);

            return 0;
        }

        /// <summary>
        /// 添加服务器或编辑
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="socksItem"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int AddSocksServer(ref V2rayNappConfig appConfig, NodeItem socksItem, int index)
        {
            socksItem.configVersion = 2;
            socksItem.configType = (int)EConfigType.Socks;

            socksItem.address = socksItem.address.TrimEx();

            if (index >= 0)
            {
                //修改
                appConfig.outbound[index] = socksItem;
                if (appConfig.index.Equals(index))
                {
                    Global.reloadV2ray = true;
                }
            }
            else
            {
                //添加
                appConfig.outbound.Add(socksItem);
                if (appConfig.outbound.Count == 1)
                {
                    appConfig.index = 0;
                    Global.reloadV2ray = true;
                }
            }

            ToJsonFile(appConfig);

            return 0;
        }


        /// <summary>
        /// 添加服务器或编辑
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="trojanItem"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int AddTrojanServer(ref V2rayNappConfig appConfig, NodeItem trojanItem, int index)
        {
            trojanItem.configVersion = 2;
            trojanItem.configType = (int)EConfigType.Trojan;

            trojanItem.address = trojanItem.address.TrimEx();
            trojanItem.id = trojanItem.id.TrimEx();

            trojanItem.streamSecurity = Global.StreamSecurity;
            trojanItem.allowInsecure = "false";

            if (index >= 0)
            {
                //修改
                appConfig.outbound[index] = trojanItem;
                if (appConfig.index.Equals(index))
                {
                    Global.reloadV2ray = true;
                }
            }
            else
            {
                //添加
                appConfig.outbound.Add(trojanItem);
                if (appConfig.outbound.Count == 1)
                {
                    appConfig.index = 0;
                    Global.reloadV2ray = true;
                }
            }

            ToJsonFile(appConfig);

            return 0;
        }

        /// <summary>
        /// 配置文件版本升级
        /// </summary>
        /// <param name="vmessItem"></param>
        /// <returns></returns>
        public static int UpgradeServerVersion(ref NodeItem vmessItem)
        {
            try
            {
                if (vmessItem == null
                    || vmessItem.configVersion == 2)
                {
                    return 0;
                }
                if (vmessItem.configType == (int)EConfigType.Vmess)
                {
                    string path = "";
                    string host = "";
                    string[] arrParameter;
                    switch (vmessItem.network)
                    {
                        case "kcp":
                            break;
                        case "ws":
                            //*ws(path+host),它们中间分号(;)隔开
                            arrParameter = vmessItem.requestHost.Replace(" ", "").Split(';');
                            if (arrParameter.Length > 0)
                            {
                                path = arrParameter[0];
                            }
                            if (arrParameter.Length > 1)
                            {
                                path = arrParameter[0];
                                host = arrParameter[1];
                            }
                            vmessItem.path = path;
                            vmessItem.requestHost = host;
                            break;
                        case "h2":
                            //*h2 path
                            arrParameter = vmessItem.requestHost.Replace(" ", "").Split(';');
                            if (arrParameter.Length > 0)
                            {
                                path = arrParameter[0];
                            }
                            if (arrParameter.Length > 1)
                            {
                                path = arrParameter[0];
                                host = arrParameter[1];
                            }
                            vmessItem.path = path;
                            vmessItem.requestHost = host;
                            break;
                        default:
                            break;
                    }
                }
                vmessItem.configVersion = 2;
            }
            catch
            {
            }
            return 0;
        }

        /// <summary>
        /// 批量添加服务器
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="clipboardData"></param>
        /// <param name="subid"></param>
        /// <returns>成功导入的数量</returns>
        public static int AddBatchServers(ref V2rayNappConfig appConfig, string clipboardData, string subid = "", bool allowInsecure = false)
        {
            if (Utils.IsNullOrEmpty(clipboardData))
            {
                return -1;
            }
            //if (clipboardData.IndexOf("outbound") >= 0 && clipboardData.IndexOf("outbound") == clipboardData.LastIndexOf("outbound"))
            //{
            //    clipboardData = clipboardData.Replace("\r\n", "").Replace("\n", "");
            //}
            int countServers = 0;

            //string[] arrData = clipboardData.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            string[] arrData = clipboardData.Split(Environment.NewLine.ToCharArray());
            foreach (string str in arrData)
            {
                //maybe sub
                if (str.StartsWith(Global.httpsProtocol) || str.StartsWith(Global.httpProtocol))
                {
                    if (AddSubItem(ref appConfig, str) == 0)
                    {
                        countServers++;
                    }
                    continue;
                }
                NodeItem nodeItem = V2rayConfigHandler.ImportFromClipboardConfig(str, out string msg);
                if (nodeItem == null)
                {
                    continue;
                }
                nodeItem.subid = subid;
                if (nodeItem.configType == (int)EConfigType.Vmess)
                {
                    if (allowInsecure)
                    {
                        nodeItem.allowInsecure = "true";
                    }
                    if (AddServer(ref appConfig, nodeItem, -1) == 0)
                    {
                        countServers++;
                    }
                }
                else if (nodeItem.configType == (int)EConfigType.Shadowsocks)
                {
                    if (AddShadowsocksServer(ref appConfig, nodeItem, -1) == 0)
                    {
                        countServers++;
                    }
                }
                else if (nodeItem.configType == (int)EConfigType.Socks)
                {
                    if (AddSocksServer(ref appConfig, nodeItem, -1) == 0)
                    {
                        countServers++;
                    }
                }
                else if (nodeItem.configType == (int)EConfigType.Trojan)
                {
                    if (AddTrojanServer(ref appConfig, nodeItem, -1) == 0)
                    {
                        countServers++;
                    }
                }
                else if (nodeItem.configType == (int)EConfigType.VLESS)
                {
                    if (allowInsecure)
                    {
                        nodeItem.allowInsecure = "true";
                    }
                    if (AddVlessServer(ref appConfig, nodeItem, -1) == 0)
                    {
                        countServers++;
                    }
                }
                else if (nodeItem.configType == (int)EConfigType.Hysteria2)
                {
                    if (allowInsecure)
                    {
                        nodeItem.allowInsecure = "true";
                    }
                    if (AddHysteria2Server(ref appConfig, nodeItem, -1) == 0)
                    {
                        countServers++;
                    }
                }
            }
            return countServers;
        }

        /// <summary>
        /// add sub
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static int AddSubItem(ref V2rayNappConfig appConfig, string url)
        {
            //already exists
            foreach (SubItem sub in appConfig.subItem)
            {
                if (url == sub.url)
                {
                    return 0;
                }
            }

            SubItem subItem = new SubItem
            {
                id = string.Empty,
                remarks = "import sub",
                url = url
            };
            appConfig.subItem.Add(subItem);

            return SaveSubItem(ref appConfig);
        }

        /// <summary>
        /// save sub
        /// </summary>
        /// <param name="appConfig"></param>
        /// <returns></returns>
        public static int SaveSubItem(ref V2rayNappConfig appConfig)
        {
            if (appConfig.subItem == null || appConfig.subItem.Count <= 0)
            {
                return -1;
            }

            foreach (SubItem sub in appConfig.subItem)
            {
                if (Utils.IsNullOrEmpty(sub.id))
                {
                    sub.id = Utils.GetGUID();
                }
            }

            ToJsonFile(appConfig);
            return 0;
        }

        /// <summary>
        /// 移除服务器
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="subid"></param>
        /// <returns></returns>
        public static int RemoveServerViaSubid(ref V2rayNappConfig appConfig, string subid)
        {
            if (Utils.IsNullOrEmpty(subid) || appConfig.outbound.Count <= 0)
            {
                return -1;
            }
            for (int k = appConfig.outbound.Count - 1; k >= 0; k--)
            {
                if (appConfig.outbound[k].subid.Equals(subid))
                {
                    appConfig.outbound.RemoveAt(k);
                }
            }

            ToJsonFile(appConfig);
            return 0;
        }

        public static int AddformMainLvColWidth(ref V2rayNappConfig appConfig, string name, int width)
        {
            if (appConfig.uiItem.mainLvColWidth == null)
            {
                appConfig.uiItem.mainLvColWidth = new Dictionary<string, int>();
            }
            if (appConfig.uiItem.mainLvColWidth.ContainsKey(name))
            {
                appConfig.uiItem.mainLvColWidth[name] = width;
            }
            else
            {
                appConfig.uiItem.mainLvColWidth.Add(name, width);
            }
            return 0;
        }
        public static int GetformMainLvColWidth(ref V2rayNappConfig appConfig, string name, int width)
        {
            if (appConfig.uiItem.mainLvColWidth == null)
            {
                appConfig.uiItem.mainLvColWidth = new Dictionary<string, int>();
            }
            if (appConfig.uiItem.mainLvColWidth.ContainsKey(name))
            {
                return appConfig.uiItem.mainLvColWidth[name];
            }
            else
            {
                return width;
            }
        }

        public static int SortServers(ref V2rayNappConfig appConfig, EServerColName name, bool asc)
        {
            if (appConfig.outbound.Count <= 0)
            {
                return -1;
            }
            switch (name)
            {
                case EServerColName.configType:
                case EServerColName.remarks:
                case EServerColName.address:
                case EServerColName.port:
                case EServerColName.security:
                case EServerColName.network:
                case EServerColName.testResult:
                    break;
                default:
                    return -1;
            }
            string itemId = appConfig.getItemId();
            var items = appConfig.outbound.AsQueryable();

            if (asc)
            {
                appConfig.outbound = items.OrderBy(name.ToString()).ToList();
            }
            else
            {
                appConfig.outbound = items.OrderByDescending(name.ToString()).ToList();
            }

            var index_ = appConfig.outbound.FindIndex(it => it.getItemId() == itemId);
            if (index_ >= 0)
            {
                appConfig.index = index_;
            }

            ToJsonFile(appConfig);
            return 0;
        }

        /// <summary>
        /// 添加服务器或编辑
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="vlessItem"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int AddVlessServer(ref V2rayNappConfig appConfig, NodeItem vlessItem, int index)
        {
            vlessItem.configVersion = 2;
            vlessItem.configType = (int)EConfigType.VLESS;

            vlessItem.address = vlessItem.address.TrimEx();
            vlessItem.id = vlessItem.id.TrimEx();
            vlessItem.security = vlessItem.security.TrimEx();
            vlessItem.network = vlessItem.network.TrimEx();
            vlessItem.headerType = vlessItem.headerType.TrimEx();
            vlessItem.requestHost = vlessItem.requestHost.TrimEx();
            vlessItem.path = vlessItem.path.TrimEx();
            vlessItem.streamSecurity = vlessItem.streamSecurity.TrimEx();

            vlessItem.sni = vlessItem.sni.TrimEx();
            vlessItem.fingerprint = vlessItem.fingerprint.TrimEx();
            vlessItem.publicKey = vlessItem.publicKey.TrimEx();
            vlessItem.shortId = vlessItem.shortId.TrimEx();
            vlessItem.spiderX = vlessItem.spiderX.TrimEx();

            if (index >= 0)
            {
                //修改
                appConfig.outbound[index] = vlessItem;
                if (appConfig.index.Equals(index))
                {
                    Global.reloadV2ray = true;
                }
            }
            else
            {
                //添加
                if (Utils.IsNullOrEmpty(vlessItem.allowInsecure))
                {
                    vlessItem.allowInsecure = appConfig.defAllowInsecure.ToString();
                }
                appConfig.outbound.Add(vlessItem);
                if (appConfig.outbound.Count == 1)
                {
                    appConfig.index = 0;
                    Global.reloadV2ray = true;
                }
            }

            ToJsonFile(appConfig);

            return 0;
        }

        /// <summary>
        /// 添加服务器或编辑
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="hy2Item"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int AddHysteria2Server(ref V2rayNappConfig appConfig, NodeItem hy2Item, int index)
        {
            hy2Item.configVersion = 2;
            hy2Item.configType = (int)EConfigType.Hysteria2;

            hy2Item.address = hy2Item.address.TrimEx();
            hy2Item.id = hy2Item.id.TrimEx();
            hy2Item.network = hy2Item.network.TrimEx();
            hy2Item.streamSecurity = hy2Item.streamSecurity.TrimEx();
            hy2Item.allowInsecure = hy2Item.allowInsecure.TrimEx();
            hy2Item.pinSHA256 = hy2Item.pinSHA256.TrimEx();

            if (index >= 0)
            {
                //修改
                appConfig.outbound[index] = hy2Item;
                if (appConfig.index.Equals(index))
                {
                    Global.reloadV2ray = true;
                }
            }
            else
            {
                //添加
                appConfig.outbound.Add(hy2Item);
                if (appConfig.outbound.Count == 1)
                {
                    appConfig.index = 0;
                    Global.reloadV2ray = true;
                }
            }

            ToJsonFile(appConfig);

            return 0;
        }

        private static string GetIpv6(string address)
        {
            return Utils.IsIpv6(address) ? $"[{address}]" : address;
        }

        private static int GetStdTransport(NodeItem item, string securityDef, ref Dictionary<string, string> dicQuery)
        {
            if (!Utils.IsNullOrEmpty(item.flow))
            {
                dicQuery.Add("flow", item.flow);
            }

            if (!Utils.IsNullOrEmpty(item.streamSecurity))
            {
                dicQuery.Add("security", item.streamSecurity);
            }
            else
            {
                if (securityDef != null)
                {
                    dicQuery.Add("security", securityDef);
                }
            }

            // VLESS Reality
            if (!Utils.IsNullOrEmpty(item.sni))
            {
                dicQuery.Add("sni", item.sni);
            }
            //if (!Utils.IsNullOrEmpty(item.alpn))
            //{
            //    dicQuery.Add("alpn", WebUtility.UrlEncode(item.alpn));
            //}
            if (!Utils.IsNullOrEmpty(item.fingerprint))
            {
                dicQuery.Add("fp", WebUtility.UrlEncode(item.fingerprint));
            }
            if (!Utils.IsNullOrEmpty(item.publicKey))
            {
                dicQuery.Add("pbk", WebUtility.UrlEncode(item.publicKey));
            }
            if (!Utils.IsNullOrEmpty(item.shortId))
            {
                dicQuery.Add("sid", WebUtility.UrlEncode(item.shortId));
            }
            if (!Utils.IsNullOrEmpty(item.spiderX))
            {
                dicQuery.Add("spx", WebUtility.UrlEncode(item.spiderX));
            }

            dicQuery.Add("type", !Utils.IsNullOrEmpty(item.network) ? item.network : "tcp");

            switch (item.network)
            {
                case "tcp":
                    dicQuery.Add("headerType", !Utils.IsNullOrEmpty(item.headerType) ? item.headerType : "none");
                    if (!Utils.IsNullOrEmpty(item.requestHost))
                    {
                        dicQuery.Add("host", WebUtility.UrlEncode(item.requestHost));
                    }
                    break;
                case "kcp":
                    dicQuery.Add("headerType", !Utils.IsNullOrEmpty(item.headerType) ? item.headerType : "none");
                    if (!Utils.IsNullOrEmpty(item.path))
                    {
                        dicQuery.Add("seed", WebUtility.UrlEncode(item.path));
                    }
                    break;

                case "ws":
                    if (!Utils.IsNullOrEmpty(item.requestHost))
                    {
                        dicQuery.Add("host", WebUtility.UrlEncode(item.requestHost));
                    }
                    if (!Utils.IsNullOrEmpty(item.path))
                    {
                        dicQuery.Add("path", WebUtility.UrlEncode(item.path));
                    }
                    break;

                case "http":
                case "h2":
                    dicQuery["type"] = "http";
                    if (!Utils.IsNullOrEmpty(item.requestHost))
                    {
                        dicQuery.Add("host", WebUtility.UrlEncode(item.requestHost));
                    }
                    if (!Utils.IsNullOrEmpty(item.path))
                    {
                        dicQuery.Add("path", WebUtility.UrlEncode(item.path));
                    }
                    break;

                case "quic":
                    dicQuery.Add("headerType", !Utils.IsNullOrEmpty(item.headerType) ? item.headerType : "none");
                    dicQuery.Add("quicSecurity", WebUtility.UrlEncode(item.requestHost));
                    dicQuery.Add("key", WebUtility.UrlEncode(item.path));
                    break;
            }
            return 0;
        }
    }
}
