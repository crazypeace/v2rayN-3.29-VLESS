using System;
using System.Collections.Generic;
using System.Windows.Forms;
using v2rayN.Handler;
using v2rayN.Base;
using v2rayN.HttpProxyHandler;

namespace v2rayN.Forms
{
    public partial class OptionSettingForm : BaseForm
    {
        public OptionSettingForm()
        {
            InitializeComponent();
        }

        private void OptionSettingForm_Load(object sender, EventArgs e)
        {
            int screenWidth = Screen.FromHandle(this.Handle).WorkingArea.Width;
            int screenHeight = Screen.FromHandle(this.Handle).WorkingArea.Height;

            // 设置窗口的尺寸不大于屏幕的尺寸
            if (this.Width > screenWidth)
            {
                this.Width = screenWidth;
            }
            if (this.Height > screenHeight)
            {
                this.Height = screenHeight;
            }

            InitBase();

            InitRouting();

            InitKCP();

            InitGUI();

            InitUserPAC();

            InitSocksOut();
        }

        /// <summary>
        /// 初始化基础设置
        /// </summary>
        private void InitBase()
        {
            //日志
            chklogEnabled.Checked = appConfig.logEnabled;
            cmbloglevel.Text = appConfig.loglevel;

            //Mux
            chkmuxEnabled.Checked = appConfig.muxEnabled;

            //本地监听
            if (appConfig.inbound.Count > 0)
            {
                txtlocalPort.Text = appConfig.inbound[0].localPort.ToString();
                cmbprotocol.Text = appConfig.inbound[0].protocol.ToString();
                chkudpEnabled.Checked = appConfig.inbound[0].udpEnabled;
                chksniffingEnabled.Checked = appConfig.inbound[0].sniffingEnabled;

                txtlocalPort2.Text = $"{appConfig.inbound[0].localPort + 1}";
                cmbprotocol2.Text = Global.InboundHttp;

                if (appConfig.inbound.Count > 1)
                {
                    txtlocalPort2.Text = appConfig.inbound[1].localPort.ToString();
                    cmbprotocol2.Text = appConfig.inbound[1].protocol.ToString();
                    chkudpEnabled2.Checked = appConfig.inbound[1].udpEnabled;
                    chksniffingEnabled2.Checked = appConfig.inbound[1].sniffingEnabled;
                    chkAllowIn2.Checked = true;
                }
                else
                {
                    chkAllowIn2.Checked = false;
                }
                chkAllowIn2State();
            }

            //remoteDNS
            txtremoteDNS.Text = appConfig.remoteDNS;

            cmblistenerType.SelectedIndex = (int)appConfig.listenerType;

            chkdefAllowInsecure.Checked = appConfig.defAllowInsecure;
        }

        /// <summary>
        /// 初始化路由设置
        /// </summary>
        private void InitRouting()
        {
            //路由
            cmbdomainStrategy.Text = appConfig.domainStrategy;
            int.TryParse(appConfig.routingMode, out int routingMode);
            cmbroutingMode.SelectedIndex = routingMode;

            txtUseragent.Text = Utils.List2String(appConfig.useragent, true);
            txtUserdirect.Text = Utils.List2String(appConfig.userdirect, true);
            txtUserblock.Text = Utils.List2String(appConfig.userblock, true);
        }

        /// <summary>
        /// 初始化KCP设置
        /// </summary>
        private void InitKCP()
        {
            txtKcpmtu.Text = appConfig.kcpItem.mtu.ToString();
            txtKcptti.Text = appConfig.kcpItem.tti.ToString();
            txtKcpuplinkCapacity.Text = appConfig.kcpItem.uplinkCapacity.ToString();
            txtKcpdownlinkCapacity.Text = appConfig.kcpItem.downlinkCapacity.ToString();
            txtKcpreadBufferSize.Text = appConfig.kcpItem.readBufferSize.ToString();
            txtKcpwriteBufferSize.Text = appConfig.kcpItem.writeBufferSize.ToString();
            chkKcpcongestion.Checked = appConfig.kcpItem.congestion;
        }

        /// <summary>
        /// 初始化v2rayN GUI设置
        /// </summary>
        private void InitGUI()
        {
            //开机自动启动
            chkAutoRun.Checked = Utils.IsAutoRun();

            //自定义GFWList
            txturlGFWList.Text = appConfig.urlGFWList;

            chkAllowLANConn.Checked = appConfig.allowLANConn;
            chkEnableStatistics.Checked = appConfig.enableStatistics;
            chkKeepOlderDedupl.Checked = appConfig.keepOlderDedupl;




            ComboItem[] cbSource = new ComboItem[]
            {
                new ComboItem{ID = (int)Global.StatisticsFreshRate.quick, Text = UIRes.I18N("QuickFresh")},
                new ComboItem{ID = (int)Global.StatisticsFreshRate.medium, Text = UIRes.I18N("MediumFresh")},
                new ComboItem{ID = (int)Global.StatisticsFreshRate.slow, Text = UIRes.I18N("SlowFresh")},
            };
            cbFreshrate.DataSource = cbSource;

            cbFreshrate.DisplayMember = "Text";
            cbFreshrate.ValueMember = "ID";

            switch (appConfig.statisticsFreshRate)
            {
                case (int)Global.StatisticsFreshRate.quick:
                    cbFreshrate.SelectedItem = cbSource[0];
                    break;
                case (int)Global.StatisticsFreshRate.medium:
                    cbFreshrate.SelectedItem = cbSource[1];
                    break;
                case (int)Global.StatisticsFreshRate.slow:
                    cbFreshrate.SelectedItem = cbSource[2];
                    break;
            }

        }

        private void InitUserPAC()
        {
            txtuserPacRule.Text = Utils.List2String(appConfig.userPacRule, true);
        }

        private void InitSocksOut()
        {
            chkSocksOut.Checked = appConfig.socksOutboundEnable;    
            txtSocksOutboundIP.Text = appConfig.socksOutboundIP; 
            txtSocksOutboundPort.Text = Utils.ToString(appConfig.socksOutboundPort);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (SaveBase() != 0)
            {
                return;
            }

            if (SaveRouting() != 0)
            {
                return;
            }

            if (SaveKCP() != 0)
            {
                return;
            }

            if (SaveGUI() != 0)
            {
                return;
            }

            if (SaveUserPAC() != 0)
            {
                return;
            }

            if (SaveSocksOut() != 0)
            {
                return;
            }

            if (AppConfigHandler.SaveConfig(ref appConfig) == 0)
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                UI.ShowWarning(UIRes.I18N("OperationFailed"));
            }
        }

        /// <summary>
        /// 保存基础设置
        /// </summary>
        /// <returns></returns>
        private int SaveBase()
        {
            //日志
            bool logEnabled = chklogEnabled.Checked;
            string loglevel = cmbloglevel.Text.TrimEx();

            //Mux
            bool muxEnabled = chkmuxEnabled.Checked;

            //本地监听
            string localPort = txtlocalPort.Text.TrimEx();
            string protocol = cmbprotocol.Text.TrimEx();
            bool udpEnabled = chkudpEnabled.Checked;
            bool sniffingEnabled = chksniffingEnabled.Checked;
            if (Utils.IsNullOrEmpty(localPort) || !Utils.IsNumberic(localPort))
            {
                UI.Show(UIRes.I18N("FillLocalListeningPort"));
                return -1;
            }
            if (Utils.IsNullOrEmpty(protocol))
            {
                UI.Show(UIRes.I18N("PleaseSelectProtocol"));
                return -1;
            }
            appConfig.inbound[0].localPort = Utils.ToInt(localPort);
            appConfig.inbound[0].protocol = protocol;
            appConfig.inbound[0].udpEnabled = udpEnabled;
            appConfig.inbound[0].sniffingEnabled = sniffingEnabled;

            //本地监听2
            string localPort2 = txtlocalPort2.Text.TrimEx();
            string protocol2 = cmbprotocol2.Text.TrimEx();
            bool udpEnabled2 = chkudpEnabled2.Checked;
            bool sniffingEnabled2 = chksniffingEnabled2.Checked;
            if (chkAllowIn2.Checked)
            {
                if (Utils.IsNullOrEmpty(localPort2) || !Utils.IsNumberic(localPort2))
                {
                    UI.Show(UIRes.I18N("FillLocalListeningPort"));
                    return -1;
                }
                if (Utils.IsNullOrEmpty(protocol2))
                {
                    UI.Show(UIRes.I18N("PleaseSelectProtocol"));
                    return -1;
                }
                if (appConfig.inbound.Count < 2)
                {
                    appConfig.inbound.Add(new Mode.InItem());
                }
                appConfig.inbound[1].localPort = Utils.ToInt(localPort2);
                appConfig.inbound[1].protocol = protocol2;
                appConfig.inbound[1].udpEnabled = udpEnabled2;
                appConfig.inbound[1].sniffingEnabled = sniffingEnabled2;
            }
            else
            {
                if (appConfig.inbound.Count > 1)
                {
                    appConfig.inbound.RemoveAt(1);
                }
            }

            //日志     
            appConfig.logEnabled = logEnabled;
            appConfig.loglevel = loglevel;

            //Mux
            appConfig.muxEnabled = muxEnabled;

            //remoteDNS
            appConfig.remoteDNS = txtremoteDNS.Text.TrimEx();

            appConfig.listenerType = (ListenerType)Enum.ToObject(typeof(ListenerType), cmblistenerType.SelectedIndex);

            appConfig.defAllowInsecure = chkdefAllowInsecure.Checked;

            return 0;
        }

        /// <summary>
        /// 保存路由设置
        /// </summary>
        /// <returns></returns>
        private int SaveRouting()
        {
            //路由            
            string domainStrategy = cmbdomainStrategy.Text;
            string routingMode = cmbroutingMode.SelectedIndex.ToString();

            string useragent = txtUseragent.Text.TrimEx();
            string userdirect = txtUserdirect.Text.TrimEx();
            string userblock = txtUserblock.Text.TrimEx();

            appConfig.domainStrategy = domainStrategy;
            appConfig.routingMode = routingMode;

            appConfig.useragent = Utils.String2List(useragent);
            appConfig.userdirect = Utils.String2List(userdirect);
            appConfig.userblock = Utils.String2List(userblock);

            return 0;
        }

        /// <summary>
        /// 保存KCP设置
        /// </summary>
        /// <returns></returns>
        private int SaveKCP()
        {
            string mtu = txtKcpmtu.Text.TrimEx();
            string tti = txtKcptti.Text.TrimEx();
            string uplinkCapacity = txtKcpuplinkCapacity.Text.TrimEx();
            string downlinkCapacity = txtKcpdownlinkCapacity.Text.TrimEx();
            string readBufferSize = txtKcpreadBufferSize.Text.TrimEx();
            string writeBufferSize = txtKcpwriteBufferSize.Text.TrimEx();
            bool congestion = chkKcpcongestion.Checked;

            if (Utils.IsNullOrEmpty(mtu) || !Utils.IsNumberic(mtu)
                || Utils.IsNullOrEmpty(tti) || !Utils.IsNumberic(tti)
                || Utils.IsNullOrEmpty(uplinkCapacity) || !Utils.IsNumberic(uplinkCapacity)
                || Utils.IsNullOrEmpty(downlinkCapacity) || !Utils.IsNumberic(downlinkCapacity)
                || Utils.IsNullOrEmpty(readBufferSize) || !Utils.IsNumberic(readBufferSize)
                || Utils.IsNullOrEmpty(writeBufferSize) || !Utils.IsNumberic(writeBufferSize))
            {
                UI.Show(UIRes.I18N("FillKcpParameters"));
                return -1;
            }
            appConfig.kcpItem.mtu = Utils.ToInt(mtu);
            appConfig.kcpItem.tti = Utils.ToInt(tti);
            appConfig.kcpItem.uplinkCapacity = Utils.ToInt(uplinkCapacity);
            appConfig.kcpItem.downlinkCapacity = Utils.ToInt(downlinkCapacity);
            appConfig.kcpItem.readBufferSize = Utils.ToInt(readBufferSize);
            appConfig.kcpItem.writeBufferSize = Utils.ToInt(writeBufferSize);
            appConfig.kcpItem.congestion = congestion;

            return 0;
        }

        /// <summary>
        /// 保存GUI设置
        /// </summary>
        /// <returns></returns>
        private int SaveGUI()
        {
            //开机自动启动
            Utils.SetAutoRun(chkAutoRun.Checked);

            //自定义GFWList
            appConfig.urlGFWList = txturlGFWList.Text.TrimEx();

            appConfig.allowLANConn = chkAllowLANConn.Checked;

            bool lastEnableStatistics = appConfig.enableStatistics;
            appConfig.enableStatistics = chkEnableStatistics.Checked;
            appConfig.statisticsFreshRate = (int)cbFreshrate.SelectedValue;
            appConfig.keepOlderDedupl = chkKeepOlderDedupl.Checked;

            //if(lastEnableStatistics != appConfig.enableStatistics)
            //{
            //    /// https://stackoverflow.com/questions/779405/how-do-i-restart-my-c-sharp-winform-application
            //    // Shut down the current app instance.
            //    Application.Exit();

            //    // Restart the app passing "/restart [processId]" as cmd line args
            //    Process.Start(Application.ExecutablePath, "/restart " + Process.GetCurrentProcess().Id);
            //}
            return 0;
        }

        private int SaveUserPAC()
        {
            string userPacRule = txtuserPacRule.Text.TrimEx();
            userPacRule = userPacRule.Replace("\"", "");

            appConfig.userPacRule = Utils.String2List(userPacRule);

            return 0;
        }
        private int SaveSocksOut()
        {
            appConfig.socksOutboundEnable = chkSocksOut.Checked;
            appConfig.socksOutboundIP = txtSocksOutboundIP.Text.TrimEx();
            appConfig.socksOutboundPort = Utils.ToInt(txtSocksOutboundPort.Text);

            return 0;
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void chkAllowIn2_CheckedChanged(object sender, EventArgs e)
        {
            chkAllowIn2State();
        }
        private void chkAllowIn2State()
        {
            bool blAllow2 = chkAllowIn2.Checked;
            txtlocalPort2.Enabled =
            cmbprotocol2.Enabled =
            chkudpEnabled2.Enabled = blAllow2;
        }

        private void btnSetDefRountingRule_Click(object sender, EventArgs e)
        {
            txtUseragent.Text = Utils.GetEmbedText(Global.CustomRoutingFileName + Global.agentTag);
            txtUserdirect.Text = Utils.GetEmbedText(Global.CustomRoutingFileName + Global.directTag);
            txtUserblock.Text = Utils.GetEmbedText(Global.CustomRoutingFileName + Global.blockTag);
            cmbroutingMode.SelectedIndex = 3;

            List<string> lstUrl = new List<string>
            {
                Global.CustomRoutingListUrl + Global.agentTag,
                Global.CustomRoutingListUrl + Global.directTag,
                Global.CustomRoutingListUrl + Global.blockTag
            };

            List<TextBox> lstTxt = new List<TextBox>
            {
                txtUseragent,
                txtUserdirect,
                txtUserblock
            };

            for (int k = 0; k < lstUrl.Count; k++)
            {
                TextBox txt = lstTxt[k];
                DownloadHandle downloadHandle = new DownloadHandle();
                downloadHandle.UpdateCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        string result = args.Msg;
                        if (Utils.IsNullOrEmpty(result))
                        {
                            return;
                        }
                        txt.Text = result;
                    }
                    else
                    {
                        AppendText(false, args.Msg);
                    }
                };
                downloadHandle.Error += (sender2, args) =>
                {
                    AppendText(true, args.GetException().Message);
                };

                downloadHandle.WebDownloadString(lstUrl[k]);
            }
        }
        void AppendText(bool notify, string text)
        {
            labRoutingTips.Text = text;
        }

        private void linkLabelRoutingDoc_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.v2fly.org/appConfig/routing.html");
        }
    }

    class ComboItem
    {
        public int ID { get; set; }
        public string Text { get; set; }
    }
}
