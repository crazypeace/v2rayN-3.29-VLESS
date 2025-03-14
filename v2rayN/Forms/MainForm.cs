﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using v2rayN.Handler;
using v2rayN.HttpProxyHandler;
using v2rayN.Mode;
using v2rayN.Base;
using v2rayN.Tool;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace v2rayN.Forms
{
    public partial class MainForm : BaseForm
    {
        private V2rayHandler v2rayHandler;
        private List<int> lvSelecteds = new List<int>();
        private StatisticsHandler statistics = null;

        #region Window 事件

        public MainForm()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            HideForm();
            this.Text = Utils.GetVersion();
            Global.processJob = new Job();

            Application.ApplicationExit += (sender, args) =>
            {
                v2rayHandler.V2rayStop();

                HttpProxyHandle.CloseHttpAgent(appConfig);
                PACServerHandle.Stop();

                AppConfigHandler.SaveConfig(ref appConfig);
                statistics?.SaveToFile();
                statistics?.Close();
            };
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            AppConfigHandler.LoadConfig(ref appConfig);
            v2rayHandler = new V2rayHandler();
            v2rayHandler.ProcessEvent += v2rayHandler_ProcessEvent;

            if (appConfig.enableStatistics)
            {
                statistics = new StatisticsHandler(appConfig, UpdateStatisticsHandler);
            }
        }

        private void MainForm_VisibleChanged(object sender, EventArgs e)
        {
            if (statistics == null || !statistics.Enable) return;
            if ((sender as Form).Visible)
            {
                statistics.UpdateUI = true;
            }
            else
            {
                statistics.UpdateUI = false;
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            InitServersView();
            RefreshServers();
            RestoreUI();

            LoadV2ray();

            HideForm();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                StorageUI();
                e.Cancel = true;
                HideForm();
                return;
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            //if (this.WindowState == FormWindowState.Minimized)
            //{
            //    HideForm();
            //}
            //else
            //{

            //}
        }


        //private const int WM_QUERYENDSESSION = 0x0011;
        //protected override void WndProc(ref Message m)
        //{
        //    switch (m.Msg)
        //    {
        //        case WM_QUERYENDSESSION:
        //            Utils.SaveLog("Windows shutdown UnsetProxy");

        //            AppConfigHandler.ToJsonFile(appConfig);
        //            statistics?.SaveToFile();
        //            ProxySetting.UnsetProxy();
        //            m.Result = (IntPtr)1;
        //            break;
        //        default:
        //            base.WndProc(ref m);
        //            break;
        //    }
        //}

        private void RestoreUI()
        {
            scMain.Panel2Collapsed = true;

            if (!appConfig.uiItem.mainSize.IsEmpty)
            {
                this.Width = appConfig.uiItem.mainSize.Width;
                this.Height = appConfig.uiItem.mainSize.Height;
            }
            if (!appConfig.uiItem.mainLoc.IsEmpty)
            {
                this.Left = appConfig.uiItem.mainLoc.X;
                this.Top = appConfig.uiItem.mainLoc.Y;
            }

            for (int k = 0; k < lvServers.Columns.Count; k++)
            {
                var width = AppConfigHandler.GetformMainLvColWidth(ref appConfig, ((EServerColName)k).ToString(), lvServers.Columns[k].Width);
                lvServers.Columns[k].Width = width;
            }

            // 获取当前屏幕的尺寸
            var workingArea = Screen.FromHandle(Process.GetCurrentProcess().MainWindowHandle).WorkingArea;
            var screenWidth = workingArea.Width;
            var screenHeight = workingArea.Height;
            var screenTop = workingArea.Top;
            var screenLeft = workingArea.Left;
            var screenBottom = workingArea.Bottom;
            var screenRight = workingArea.Right;

            // 设置窗口尺寸不超过当前屏幕的尺寸
            if (this.Width > screenWidth)
            {
                this.Width = screenWidth;
            }
            if (this.Height > screenHeight)
            {
                this.Height = screenHeight;
            }

            // 设置窗口不要显示在屏幕外面
            if (this.Top < screenTop)
            {
                this.Top = screenTop;
            }
            if (this.Left < screenLeft)
            {
                this.Left = screenLeft;
            }
            if (this.Top + this.Height > screenBottom)
            {
                this.Top = screenBottom - this.Height;
            }
            if (this.Left + this.Width > screenRight)
            {
                this.Left = screenRight - this.Width;
            }
        }

        private void StorageUI()
        {
            appConfig.uiItem.mainSize = new Size(this.Width, this.Height);
            appConfig.uiItem.mainLoc = new Point(this.Left, this.Top);

            for (int k = 0; k < lvServers.Columns.Count; k++)
            {
                AppConfigHandler.AddformMainLvColWidth(ref appConfig, ((EServerColName)k).ToString(), lvServers.Columns[k].Width);
            }
        }

        #endregion

        #region 显示服务器 listview 和 menu

        /// <summary>
        /// 刷新服务器
        /// </summary>
        private void RefreshServers()
        {
            RefreshServersView();
            //lvServers.AutoResizeColumns();
            RefreshServersMenu();
        }

        /// <summary>
        /// 初始化服务器列表
        /// </summary>
        private void InitServersView()
        {
            lvServers.BeginUpdate();
            lvServers.Items.Clear();

            lvServers.GridLines = true;
            lvServers.FullRowSelect = true;
            lvServers.View = View.Details;
            lvServers.Scrollable = true;
            lvServers.MultiSelect = true;
            lvServers.HeaderStyle = ColumnHeaderStyle.Clickable;

            lvServers.Columns.Add("", 30);
            lvServers.Columns.Add(UIRes.I18N("LvServiceType"), 80);
            lvServers.Columns.Add(UIRes.I18N("LvAlias"), 100);
            lvServers.Columns.Add(UIRes.I18N("LvAddress"), 120);
            lvServers.Columns.Add(UIRes.I18N("LvPort"), 50);
            lvServers.Columns.Add(UIRes.I18N("LvEncryptionMethod"), 90);
            lvServers.Columns.Add(UIRes.I18N("LvTransportProtocol"), 70);
            lvServers.Columns.Add(UIRes.I18N("LvSubscription"), 50);
            lvServers.Columns.Add(UIRes.I18N("LvTestResults"), 70, HorizontalAlignment.Right);

            if (statistics != null && statistics.Enable)
            {
                lvServers.Columns.Add(UIRes.I18N("LvTodayDownloadDataAmount"), 70);
                lvServers.Columns.Add(UIRes.I18N("LvTodayUploadDataAmount"), 70);
                lvServers.Columns.Add(UIRes.I18N("LvTotalDownloadDataAmount"), 70);
                lvServers.Columns.Add(UIRes.I18N("LvTotalUploadDataAmount"), 70);
            }
            lvServers.EndUpdate();
        }

        /// <summary>
        /// 刷新服务器列表
        /// </summary>
        private void RefreshServersView()
        {
            // 准备更新 LilstView
            lvServers.BeginUpdate();

            lvServers.Items.Clear();

            for (int k = 0; k < appConfig.outbound.Count; k++)
            {
                string def = string.Empty;
                string totalUp = string.Empty,
                        totalDown = string.Empty,
                        todayUp = string.Empty,
                        todayDown = string.Empty;
                if (appConfig.index.Equals(k))
                {
                    def = "√";
                }

                NodeItem item = appConfig.outbound[k];

                void _addSubItem(ListViewItem i, string name, string text)
                {
                    i.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = name, Text = text });
                }
                bool stats = statistics != null && statistics.Enable;
                if (stats)
                {
                    ServerStatItem sItem = statistics.Statistic.Find(item_ => item_.itemId == item.getItemId());
                    if (sItem != null)
                    {
                        totalUp = Utils.HumanFy(sItem.totalUp);
                        totalDown = Utils.HumanFy(sItem.totalDown);
                        todayUp = Utils.HumanFy(sItem.todayUp);
                        todayDown = Utils.HumanFy(sItem.todayDown);
                    }
                }
                ListViewItem lvItem = new ListViewItem(def);
                _addSubItem(lvItem, EServerColName.configType.ToString(), ((EConfigType)item.configType).ToString());
                _addSubItem(lvItem, EServerColName.remarks.ToString(), item.remarks);
                _addSubItem(lvItem, EServerColName.address.ToString(), item.address);
                _addSubItem(lvItem, EServerColName.port.ToString(), item.port.ToString());
                _addSubItem(lvItem, EServerColName.security.ToString(), item.security);
                _addSubItem(lvItem, EServerColName.network.ToString(), item.network);
                _addSubItem(lvItem, EServerColName.subRemarks.ToString(), item.getSubRemarks(appConfig));
                _addSubItem(lvItem, EServerColName.testResult.ToString(), item.testResult);
                if (stats)
                {
                    _addSubItem(lvItem, EServerColName.todayDown.ToString(), todayDown);
                    _addSubItem(lvItem, EServerColName.todayUp.ToString(), todayUp);
                    _addSubItem(lvItem, EServerColName.totalDown.ToString(), totalDown);
                    _addSubItem(lvItem, EServerColName.totalUp.ToString(), totalUp);
                }

                if (k % 2 == 1) // 隔行着色
                {
                    lvItem.BackColor = Color.WhiteSmoke;
                }
                if (appConfig.index.Equals(k))
                {
                    //lvItem.Checked = true;
                    lvItem.ForeColor = Color.DodgerBlue;
                    lvItem.Font = new Font(lvItem.Font, FontStyle.Bold);
                }

                if (lvItem != null) lvServers.Items.Add(lvItem);
            }

            lvServers.Select();
            lvServers.EndUpdate();

            //if (lvServers.Items.Count > 0)
            //{
            //    if (lvServers.Items.Count <= testConfigIndex)
            //    {
            //        testConfigIndex = lvServers.Items.Count - 1;
            //    }
            //    lvServers.Items[testConfigIndex].Selected = true;
            //    lvServers.Select();
            //}
        }

        /// <summary>
        /// 刷新托盘服务器菜单
        /// </summary>
        private void RefreshServersMenu()
        {
            menuServers.DropDownItems.Clear();

            List<ToolStripMenuItem> lst = new List<ToolStripMenuItem>();
            for (int k = 0; k < appConfig.outbound.Count; k++)
            {
                NodeItem item = appConfig.outbound[k];
                string name = item.getSummary();

                ToolStripMenuItem ts = new ToolStripMenuItem(name)
                {
                    Tag = k
                };
                if (appConfig.index.Equals(k))
                {
                    ts.Checked = true;
                }
                ts.Click += new EventHandler(ts_Click);
                lst.Add(ts);
            }
            menuServers.DropDownItems.AddRange(lst.ToArray());
        }

        private void ts_Click(object sender, EventArgs e)
        {
            try
            {
                ToolStripItem ts = (ToolStripItem)sender;
                int index = Utils.ToInt(ts.Tag);
                SetDefaultServer(index);
            }
            catch
            {
            }
        }

        private void lvServers_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = -1;
            try
            {
                if (lvServers.SelectedIndices.Count > 0)
                {
                    index = lvServers.SelectedIndices[0];
                }
            }
            catch
            {
            }
            if (index < 0)
            {
                return;
            }
            qrCodeControl.showQRCode(index, appConfig);
        }

        private void DisplayToolStatus()
        {
            toolSslSocksPort.Text =
            toolSslHttpPort.Text =
            toolSslPacPort.Text = "OFF";

            toolSslSocksPort.Text = $"{Global.Loopback}:{appConfig.inbound[0].localPort}";

            if (appConfig.listenerType != (int)ListenerType.noHttpProxy)
            {
                toolSslHttpPort.Text = $"{Global.Loopback}:{Global.httpPort}";
                if (appConfig.listenerType == ListenerType.GlobalPac ||
                    appConfig.listenerType == ListenerType.PacOpenAndClear ||
                    appConfig.listenerType == ListenerType.PacOpenOnly)
                {
                    if (PACServerHandle.IsRunning)
                    {
                        toolSslPacPort.Text = $"{HttpProxyHandle.GetPacUrl()}";
                    }
                    else
                    {
                        toolSslPacPort.Text = UIRes.I18N("StartPacFailed");
                    }
                }
            }

            notifyMain.Icon = MainFormHandler.Instance.GetNotifyIcon(appConfig, this.Icon);
        }
        private void ssMain_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (!Utils.IsNullOrEmpty(e.ClickedItem.Text))
            {
                Utils.SetClipboardData(e.ClickedItem.Text);
            }
        }

        private void lvServers_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column < 0)
            {
                return;
            }

            try
            {
                var tag = lvServers.Columns[e.Column].Tag?.ToString();
                bool asc = Utils.IsNullOrEmpty(tag) ? true : !Convert.ToBoolean(tag);
                if (AppConfigHandler.SortServers(ref appConfig, (EServerColName)e.Column, asc) != 0)
                {
                    return;
                }
                lvServers.Columns[e.Column].Tag = Convert.ToString(asc);
                RefreshServers();
            }
            catch (Exception ex)
            {
                Utils.SaveLog(ex.Message, ex);
            }

            if (e.Column < 0)
            {
                return;
            }

        }
        #endregion

        #region v2ray 操作

        /// <summary>
        /// 载入V2ray
        /// </summary>
        private void LoadV2ray()
        {
            tsbReload.Enabled = false;

            if (Global.reloadV2ray)
            {
                ClearMsg();
            }
            v2rayHandler.LoadV2ray(appConfig);
            Global.reloadV2ray = false;
            AppConfigHandler.SaveConfig(ref appConfig, false);
            statistics?.SaveToFile();

            ChangePACButtonStatus(appConfig.listenerType);

            tsbReload.Enabled = true;
        }

        /// <summary>
        /// 关闭V2ray
        /// </summary>
        private void CloseV2ray()
        {
            AppConfigHandler.SaveConfig(ref appConfig, false);
            statistics?.SaveToFile();

            ChangePACButtonStatus(0);

            v2rayHandler.V2rayStop();
        }

        #endregion

        #region 功能按钮

        private void lvServers_Click(object sender, EventArgs e)
        {
            int index = -1;
            try
            {
                if (lvServers.SelectedIndices.Count > 0)
                {
                    index = lvServers.SelectedIndices[0];
                }
            }
            catch
            {
            }
            if (index < 0)
            {
                return;
            }
            qrCodeControl.showQRCode(index, appConfig);
        }

        private void lvServers_DoubleClick(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }
            ShowServerForm(appConfig.outbound[index].configType, index);
        }
        private void ShowServerForm(int configType, int index)
        {
            BaseServerForm fm;
            switch (configType)
            {
                case (int)EConfigType.Vmess:
                    fm = new AddVmessServerForm();
                    break;
                case (int)EConfigType.Shadowsocks:
                    fm = new AddShadowSocksServerForm();
                    break;
                case (int)EConfigType.Socks:
                    fm = new AddSocksServerForm();
                    break;
                case (int)EConfigType.VLESS:
                    fm = new AddVLESSServerForm();
                    break;
                case (int)EConfigType.Trojan:
                    fm = new AddTrojanServerForm();
                    break;
                case (int)EConfigType.Hysteria2:
                    fm = new AddHy2ServerForm();
                    break;
                default:
                    fm = new AddCustomServerForm();
                    break;
            }
            fm.EditIndex = index;
            if (fm.ShowDialog() == DialogResult.OK)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }

            // 如果新建节点, 选择节点列表的末尾
            if (index < 0)
            {
                index = lvServers.Items.Count - 1;
            }
            // 修改节点后, 让节点选中, 并显示在可见区域中
            lvServers.Items[index].Selected = true;
            lvServers.EnsureVisible(index);            
        }


        private void lvServers_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.A:
                        menuSelectAll_Click(null, null);
                        break;
                    case Keys.C:
                        menuExport2ShareUrl_Click(null, null);
                        break;
                    case Keys.V:
                        menuAddServers_Click(null, null);
                        break;
                    case Keys.P:
                        menuPingServer_Click(null, null);
                        break;
                    case Keys.O:
                        menuTcpingServer_Click(null, null);
                        break;
                    case Keys.R:
                        menuRealPingServer_Click(null, null);
                        break;
                    case Keys.S:
                        menuScanScreen_Click(null, null);
                        break;
                    case Keys.T:
                        menuSpeedServer_Click(null, null);
                        break;
                }
            }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        menuSetDefaultServer_Click(null, null);
                        break;
                    case Keys.Delete:
                        menuRemoveServer_Click(null, null);
                        break;
                    case Keys.T:
                        menuMoveTop_Click(null, null);
                        break;
                    case Keys.B:
                        menuMoveBottom_Click(null, null);
                        break;
                    case Keys.U:
                        menuMoveUp_Click(null, null);
                        break;
                    case Keys.D:
                        menuMoveDown_Click(null, null);
                        break;
                }
            }
        }

        private void menuAddVmessServer_Click(object sender, EventArgs e)
        {
            ShowServerForm((int)EConfigType.Vmess, -1);
        }

        private void menuAddVlessServer_Click(object sender, EventArgs e)
        {
            ShowServerForm((int)EConfigType.VLESS, -1);            
        }

        private void menuRemoveServer_Click(object sender, EventArgs e)
        {

            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }
            if (UI.ShowYesNo(UIRes.I18N("RemoveServer")) == DialogResult.No)
            {
                return;
            }
            for (int k = lvSelecteds.Count - 1; k >= 0; k--)
            {
                AppConfigHandler.RemoveServer(ref appConfig, lvSelecteds[k]);
            }
            //刷新
            RefreshServers();
            LoadV2ray();

        }

        private void menuRemoveDuplicateServer_Click(object sender, EventArgs e)
        {
            Utils.DedupServerList(appConfig.outbound, out List<NodeItem> servers, appConfig.keepOlderDedupl);
            int oldCount = appConfig.outbound.Count;
            int newCount = servers.Count;
            if (servers != null)
            {
                appConfig.outbound = servers;
            }
            //刷新
            RefreshServers();
            LoadV2ray();
            UI.Show(string.Format(UIRes.I18N("RemoveDuplicateServerResult"), oldCount, newCount));
        }

        // 克隆服务器
        private void menuCopyServer_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }
            if ( ( index = AppConfigHandler.CopyServer(ref appConfig, index)) >= 0)
            {
                //刷新
                RefreshServers();

                // 让移动后的列表项被选中
                lvServers.Items[index].Selected = true;
                lvServers.Items[index].Focused = true;
                lvServers.EnsureVisible(index);
            }
        }

        private void menuSetDefaultServer_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }
            SetDefaultServer(index);
        }


        private void menuPingServer_Click(object sender, EventArgs e)
        {
            Speedtest("ping");
        }
        private void menuTcpingServer_Click(object sender, EventArgs e)
        {
            Speedtest("tcping");
        }

        private void menuRealPingServer_Click(object sender, EventArgs e)
        {
            //if (!appConfig.sysAgentEnabled)
            //{
            //    UI.Show(UIRes.I18N("NeedHttpGlobalProxy"));
            //    return;
            //}

            //UI.Show(UIRes.I18N("SpeedServerTips"));

            Speedtest("realping");
        }

        private void menuSpeedServer_Click(object sender, EventArgs e)
        {
            //if (!appConfig.sysAgentEnabled)
            //{
            //    UI.Show(UIRes.I18N("NeedHttpGlobalProxy"));
            //    return;
            //}

            //UI.Show(UIRes.I18N("SpeedServerTips"));

            Speedtest("speedtest");
        }
        private void Speedtest(string actionType)
        {
            if (GetLvSelectedIndex() < 0) return;
            ClearTestResult();
            SpeedtestHandler statistics = new SpeedtestHandler(ref appConfig, ref v2rayHandler, lvSelecteds, actionType, UpdateSpeedtestHandler);
        }

        private void tsbTestMe_Click(object sender, EventArgs e)
        {
            string result = httpProxyTest() + "ms";
            AppendText(false, string.Format(UIRes.I18N("TestMeOutput"), result));
        }
        private int httpProxyTest()
        {
            SpeedtestHandler statistics = new SpeedtestHandler(ref appConfig, ref v2rayHandler, lvSelecteds, "", UpdateSpeedtestHandler);
            return statistics.RunAvailabilityCheck();
        }

        private void menuExport2ClientConfig_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            MainFormHandler.Instance.Export2ClientConfig(index, appConfig);
        }

        private void menuExport2ServerConfig_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            MainFormHandler.Instance.Export2ServerConfig(index, appConfig);
        }

        private void menuExport2ShareUrl_Click(object sender, EventArgs e)
        {
            GetLvSelectedIndex();

            StringBuilder sb = new StringBuilder();
            foreach (int v in lvSelecteds)
            {
                string url = AppConfigHandler.GetVmessQRCode(appConfig, v);
                if (Utils.IsNullOrEmpty(url))
                {
                    continue;
                }
                sb.Append(url);
                sb.AppendLine();
            }
            if (sb.Length > 0)
            {
                Utils.SetClipboardData(sb.ToString());
                AppendText(false, UIRes.I18N("BatchExportURLSuccessfully"));
                //UI.Show(UIRes.I18N("BatchExportURLSuccessfully"));
            }
        }

        private void menuExport2SubContent_Click(object sender, EventArgs e)
        {
            GetLvSelectedIndex();

            StringBuilder sb = new StringBuilder();
            foreach (int v in lvSelecteds)
            {
                string url = AppConfigHandler.GetVmessQRCode(appConfig, v);
                if (Utils.IsNullOrEmpty(url))
                {
                    continue;
                }
                sb.Append(url);
                sb.AppendLine();
            }
            if (sb.Length > 0)
            {
                Utils.SetClipboardData(Utils.Base64Encode(sb.ToString()));
                UI.Show(UIRes.I18N("BatchExportSubscriptionSuccessfully"));
            }
        }

        private void tsbOptionSetting_Click(object sender, EventArgs e)
        {
            OptionSettingForm fm = new OptionSettingForm();
            if (fm.ShowDialog() == DialogResult.OK)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
                HttpProxyHandle.RestartHttpAgent(appConfig, true);
            }
        }

        private void tsbReload_Click(object sender, EventArgs e)
        {
            Global.reloadV2ray = true;
            LoadV2ray();
        }

        private void tsbClose_Click(object sender, EventArgs e)
        {
            HideForm();
            //this.WindowState = FormWindowState.Minimized;
        }

        /// <summary>
        /// 设置活动服务器
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private int SetDefaultServer(int index)
        {
            if (index < 0)
            {
                UI.Show(UIRes.I18N("PleaseSelectServer"));
                return -1;
            }
            if (AppConfigHandler.SetDefaultServer(ref appConfig, index) == 0)
            {
                //刷新
                RefreshServers();

                lvServers.Items[index].Selected = true;
                lvServers.Items[index].Focused = true;
                lvServers.Items[index].EnsureVisible();

                LoadV2ray();
            }
            return 0;
        }

        /// <summary>
        /// 取得ListView选中的行
        /// </summary>
        /// <returns></returns>
        private int GetLvSelectedIndex()
        {
            int index = -1;
            lvSelecteds.Clear();
            try
            {
                if (lvServers.SelectedIndices.Count <= 0)
                {
                    UI.Show(UIRes.I18N("PleaseSelectServer"));
                    return index;
                }

                index = lvServers.SelectedIndices[0];
                foreach (int i in lvServers.SelectedIndices)
                {
                    lvSelecteds.Add(i);
                }
                return index;
            }
            catch
            {
                return index;
            }
        }

        private void menuAddCustomServer_Click(object sender, EventArgs e)
        {
            UI.Show(UIRes.I18N("CustomServerTips"));

            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "Config|*.json|All|*.*"
            };
            if (fileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string fileName = fileDialog.FileName;
            if (Utils.IsNullOrEmpty(fileName))
            {
                return;
            }

            if (AppConfigHandler.AddCustomServer(ref appConfig, fileName) == 0)
            {
                //刷新
                RefreshServers();
                //LoadV2ray();
                UI.Show(UIRes.I18N("SuccessfullyImportedCustomServer"));
            }
            else
            {
                UI.ShowWarning(UIRes.I18N("FailedImportedCustomServer"));
            }
        }

        private void menuAddShadowsocksServer_Click(object sender, EventArgs e)
        {
            ShowServerForm((int)EConfigType.Shadowsocks, -1);             
            ShowForm();
        }

        private void menuAddSocksServer_Click(object sender, EventArgs e)
        {
            ShowServerForm((int)EConfigType.Socks, -1);      
            ShowForm();
        }

        private void menuAddTrojanServer_Click(object sender, EventArgs e)
        {
            ShowServerForm((int)EConfigType.Trojan, -1);
            ShowForm();
        }

        private void menuAddServers_Click(object sender, EventArgs e)
        {
            string clipboardData = Utils.GetClipboardData();
            int result = AddBatchServers(clipboardData);
            if (result > 0)
            {
                UI.Show(string.Format(UIRes.I18N("SuccessfullyImportedServerViaClipboard"), result));
            }
            int endOfList = lvServers.Items.Count - 1;
            lvServers.Items[endOfList].Selected = true;
            lvServers.EnsureVisible(endOfList);
        }

        private void menuScanScreen_Click(object sender, EventArgs e)
        {
            HideForm();
            bgwScan.RunWorkerAsync();
        }

        private int AddBatchServers(string clipboardData, string subid = "", bool allowInsecure = false)
        {
            int counter;
            int _Add()
            {
                return AppConfigHandler.AddBatchServers(ref appConfig, clipboardData, subid, allowInsecure);
            }
            counter = _Add();
            // 如果添加节点失败, 那么就base64解码了再尝试添加节点
            if (counter < 1)
            {
                clipboardData = Utils.Base64Decode(clipboardData);
                counter = _Add();
            }
            RefreshServers();
            return counter;
        }

        private void menuUpdateSubscriptions_Click(object sender, EventArgs e)
        {
            UpdateSubscriptionProcess();
        }

        #endregion


        #region 提示信息

        /// <summary>
        /// 消息委托
        /// </summary>
        /// <param name="notify"></param>
        /// <param name="msg"></param>
        void v2rayHandler_ProcessEvent(bool notify, string msg)
        {
            AppendText(notify, msg);
        }

        delegate void AppendTextDelegate(string text);
        void AppendText(bool notify, string msg)
        {
            try
            {
                AppendText(msg);
                if (notify)
                {
                    notifyMsg(msg);
                }
            }
            catch
            {
            }
        }

        void AppendText(string text)
        {
            if (this.txtMsgBox.InvokeRequired)
            {
                Invoke(new AppendTextDelegate(AppendText), new object[] { text });
            }
            else
            {
                //this.txtMsgBox.AppendText(text);
                ShowMsg(text);
            }
        }

        /// <summary>
        /// 提示信息
        /// </summary>
        /// <param name="msg"></param>
        private void ShowMsg(string msg)
        {
            if (txtMsgBox.Lines.Length > 999)
            {
                ClearMsg();
            }
            this.txtMsgBox.AppendText(msg);
            if (!msg.EndsWith(Environment.NewLine))
            {
                this.txtMsgBox.AppendText(Environment.NewLine);
            }
        }

        /// <summary>
        /// 清除信息
        /// </summary>
        private void ClearMsg()
        {
            this.txtMsgBox.Clear();
        }

        /// <summary>
        /// 托盘信息
        /// </summary>
        /// <param name="msg"></param>
        private void notifyMsg(string msg)
        {
            notifyMain.Text = msg;
        }

        #endregion


        #region 托盘事件

        private void notifyMain_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowForm();
            }
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            this.Close();

            Application.Exit();
        }


        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            this.ShowInTaskbar = true;
            //this.notifyIcon1.Visible = false;
            this.txtMsgBox.ScrollToCaret();
            if (appConfig.index >= 0 && appConfig.index < lvServers.Items.Count)
            {
                lvServers.EnsureVisible(appConfig.index); // workaround
            }

            SetVisibleCore(true);
        }

        private void HideForm()
        {
            //this.WindowState = FormWindowState.Minimized;
            this.Hide();
            //this.notifyMain.Icon = this.Icon;
            this.notifyMain.Visible = true;
            this.ShowInTaskbar = false;

            SetVisibleCore(false);
        }

        #endregion

        #region 后台测速

        private void SetTestResult(int k, string txt)
        {
            if (k < lvServers.Items.Count)
            {
                appConfig.outbound[k].testResult = txt;
                lvServers.Items[k].SubItems["testResult"].Text = txt;
            }
        }
        private void ClearTestResult()
        {
            foreach (int s in lvSelecteds)
            {
                SetTestResult(s, "");
            }
        }
        private void UpdateSpeedtestHandler(int index, string msg)
        {
            lvServers.Invoke((MethodInvoker)delegate
            {
                SetTestResult(index, msg);
            });
        }

        private void UpdateStatisticsHandler(ulong up, ulong down, List<ServerStatItem> statistics)
        {
            try
            {
                up /= (ulong)(appConfig.statisticsFreshRate / 1000f);
                down /= (ulong)(appConfig.statisticsFreshRate / 1000f);
                toolSslServerSpeed.Text = string.Format("{0}/s↑ | {1}/s↓", Utils.HumanFy(up), Utils.HumanFy(down));

                List<string[]> datas = new List<string[]>();
                for (int i = 0; i < appConfig.outbound.Count; i++)
                {
                    int index = statistics.FindIndex(item_ => item_.itemId == appConfig.outbound[i].getItemId());
                    if (index != -1)
                    {
                        lvServers.Invoke((MethodInvoker)delegate
                        {
                            lvServers.BeginUpdate();

                            lvServers.Items[i].SubItems["todayDown"].Text = Utils.HumanFy(statistics[index].todayDown);
                            lvServers.Items[i].SubItems["todayUp"].Text = Utils.HumanFy(statistics[index].todayUp);
                            lvServers.Items[i].SubItems["totalDown"].Text = Utils.HumanFy(statistics[index].totalDown);
                            lvServers.Items[i].SubItems["totalUp"].Text = Utils.HumanFy(statistics[index].totalUp);

                            lvServers.EndUpdate();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.SaveLog(ex.Message, ex);
            }
        }

        #endregion

        #region 移动服务器

        private void menuMoveTop_Click(object sender, EventArgs e)
        {
            MoveServer(EMove.Top);
        }

        private void menuMoveUp_Click(object sender, EventArgs e)
        {
            MoveServer(EMove.Up);
        }

        private void menuMoveDown_Click(object sender, EventArgs e)
        {
            MoveServer(EMove.Down);
        }

        private void menuMoveBottom_Click(object sender, EventArgs e)
        {
            MoveServer(EMove.Bottom);
        }

        private void MoveServer(EMove eMove)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                UI.Show(UIRes.I18N("PleaseSelectServer"));
                return;
            }

            // AppConfigHandler.MoveServer 函数返回移动后的列表项的位置
            if ( ( index = AppConfigHandler.MoveServer(ref appConfig, index, eMove) ) >= 0)
            {
                //TODO: reload is not good.
                RefreshServers();
                //LoadV2ray();

                // 让移动后的列表项被选中
                lvServers.Items[index].Selected = true;
                lvServers.Items[index].Focused = true;
                lvServers.EnsureVisible(index);
            }
        }
        private void menuSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvServers.Items)
            {
                item.Selected = true;
            }
        }

        #endregion

        #region 系统代理相关

        private void menuCopyPACUrl_Click(object sender, EventArgs e)
        {
            Utils.SetClipboardData(HttpProxyHandle.GetPacUrl());
        }

        private void menuNotEnabledHttp_Click(object sender, EventArgs e)
        {
            SetListenerType(ListenerType.noHttpProxy);
        }
        private void menuGlobal_Click(object sender, EventArgs e)
        {
            SetListenerType(ListenerType.GlobalHttp);
        }
        private void menuGlobalPAC_Click(object sender, EventArgs e)
        {
            SetListenerType(ListenerType.GlobalPac);
        }
        private void menuKeep_Click(object sender, EventArgs e)
        {
            SetListenerType(ListenerType.HttpOpenAndClear);
        }
        private void menuKeepPAC_Click(object sender, EventArgs e)
        {
            SetListenerType(ListenerType.PacOpenAndClear);
        }
        private void menuKeepNothing_Click(object sender, EventArgs e)
        {
            SetListenerType(ListenerType.HttpOpenOnly);
        }
        private void menuKeepPACNothing_Click(object sender, EventArgs e)
        {
            SetListenerType(ListenerType.PacOpenOnly);
        }
        private void SetListenerType(ListenerType type)
        {
            appConfig.listenerType = type;
            ChangePACButtonStatus(type);
        }

        private void ChangePACButtonStatus(ListenerType type)
        {
            if (type != ListenerType.noHttpProxy)
            {
                HttpProxyHandle.RestartHttpAgent(appConfig, false);
            }
            else
            {
                HttpProxyHandle.CloseHttpAgent(appConfig);
            }

            for (int k = 0; k < menuSysAgentMode.DropDownItems.Count; k++)
            {
                ToolStripMenuItem item = ((ToolStripMenuItem)menuSysAgentMode.DropDownItems[k]);
                item.Checked = ((int)type == k);
            }

            AppConfigHandler.SaveConfig(ref appConfig, false);
            DisplayToolStatus();
        }

        #endregion


        #region CheckUpdate

        private void askToDownload(DownloadHandle downloadHandle, string url)
        {
            if (UI.ShowYesNo(string.Format(UIRes.I18N("DownloadYesNo"), url)) == DialogResult.Yes)
            {
                if (httpProxyTest() > 0)
                {
                    int httpPort = appConfig.GetLocalPort(Global.InboundHttp);
                    WebProxy webProxy = new WebProxy(Global.Loopback, httpPort);
                    downloadHandle.DownloadFileAsync(url, webProxy, 600);
                }
                else
                {
                    downloadHandle.DownloadFileAsync(url, null, 600);
                }
            }
        }
        private void tsbCheckUpdateN_Click(object sender, EventArgs e)
        {
            Process.Start(Global.UpdateUrl);

            return;

            //System.Diagnostics.Process.Start(Global.UpdateUrl);
            DownloadHandle downloadHandle = null;
            if (downloadHandle == null)
            {
                downloadHandle = new DownloadHandle();
                downloadHandle.AbsoluteCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        AppendText(false, string.Format(UIRes.I18N("MsgParsingSuccessfully"), "v2rayN"));

                        string url = args.Msg;
                        this.Invoke((MethodInvoker)(delegate
                        {
                            askToDownload(downloadHandle, url);
                        }));
                    }
                    else
                    {
                        AppendText(false, args.Msg);
                    }
                };
                downloadHandle.UpdateCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        AppendText(false, UIRes.I18N("MsgDownloadV2rayCoreSuccessfully"));

                        try
                        {
                            string fileName = Utils.GetPath(downloadHandle.DownloadFileName);
                            Process process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = "v2rayUpgrade.exe",
                                    Arguments = "\"" + fileName + "\"",
                                    WorkingDirectory = Utils.StartupPath()
                                }
                            };
                            process.Start();
                            if (process.Id > 0)
                            {
                                menuExit_Click(null, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            AppendText(false, ex.Message);
                        }
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
            }

            AppendText(false, string.Format(UIRes.I18N("MsgStartUpdating"), "v2rayN"));
            downloadHandle.CheckUpdateAsync("v2rayN");
        }

        private void tsbCheckUpdateCore_Click(object sender, EventArgs e)
        {
            Process.Start(Global.XrayCore_Url);

            return;

            DownloadHandle downloadHandle = null;
            if (downloadHandle == null)
            {
                downloadHandle = new DownloadHandle();
                downloadHandle.AbsoluteCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        AppendText(false, string.Format(UIRes.I18N("MsgParsingSuccessfully"), "v2rayCore"));

                        string url = args.Msg;
                        this.Invoke((MethodInvoker)(delegate
                        {
                            askToDownload(downloadHandle, url);
                        }));
                    }
                    else
                    {
                        AppendText(false, args.Msg);
                    }
                };
                downloadHandle.UpdateCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        AppendText(false, UIRes.I18N("MsgDownloadV2rayCoreSuccessfully"));
                        AppendText(false, UIRes.I18N("MsgUnpacking"));

                        try
                        {
                            CloseV2ray();

                            string fileName = downloadHandle.DownloadFileName;
                            fileName = Utils.GetPath(fileName);
                            FileManager.ZipExtractToFile(fileName);

                            AppendText(false, UIRes.I18N("MsgUpdateV2rayCoreSuccessfullyMore"));

                            Global.reloadV2ray = true;
                            LoadV2ray();

                            AppendText(false, UIRes.I18N("MsgUpdateV2rayCoreSuccessfully"));
                        }
                        catch (Exception ex)
                        {
                            AppendText(false, ex.Message);
                        }
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
            }

            AppendText(false, string.Format(UIRes.I18N("MsgStartUpdating"), "v2rayCore"));
            downloadHandle.CheckUpdateAsync("Core");
        }

        private void tsbCheckUpdatePACList_Click(object sender, EventArgs e)
        {
            DownloadHandle pacListHandle = null;
            if (pacListHandle == null)
            {
                pacListHandle = new DownloadHandle();
                pacListHandle.UpdateCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        string result = args.Msg;
                        if (Utils.IsNullOrEmpty(result))
                        {
                            return;
                        }
                        pacListHandle.GenPacFile(result);

                        AppendText(false, UIRes.I18N("MsgPACUpdateSuccessfully"));
                    }
                    else
                    {
                        AppendText(false, UIRes.I18N("MsgPACUpdateFailed"));
                    }
                };
                pacListHandle.Error += (sender2, args) =>
                {
                    AppendText(true, args.GetException().Message);
                };
            }
            AppendText(false, UIRes.I18N("MsgStartUpdatingPAC"));
            pacListHandle.WebDownloadString(appConfig.urlGFWList);
        }

        private void tsbCheckClearPACList_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(Utils.GetPath(Global.pacFILE), Utils.GetEmbedText(Global.BlankPacFileName), Encoding.UTF8);
                AppendText(false, UIRes.I18N("MsgSimplifyPAC"));
            }
            catch (Exception ex)
            {
                Utils.SaveLog(ex.Message, ex);
            }
        }
        #endregion

        #region Help


        private void tsbAbout_Click(object sender, EventArgs e)
        {
            Process.Start(Global.IssueUrl); // 访问Issue
        }

        private void tsbV2rayWebsite_Click(object sender, EventArgs e)
        {
            Process.Start(Global.V2RayWebsiteUrl);
        }

        private void tsbPromotion_Click(object sender, EventArgs e)
        {
            Process.Start(Global.BloggerUrl);
        }
        #endregion

        #region ScanScreen


        private void bgwScan_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            string ret = Utils.ScanScreen();
            bgwScan.ReportProgress(0, ret);
        }

        private void bgwScan_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            ShowForm();

            string result = Convert.ToString(e.UserState);
            if (Utils.IsNullOrEmpty(result))
            {
                UI.ShowWarning(UIRes.I18N("NoValidQRcodeFound"));
            }
            else
            {
                if (AddBatchServers(result) > 0)
                {
                    UI.Show(UIRes.I18N("SuccessfullyImportedServerViaScan"));
                }
            }
        }

        #endregion

        #region 订阅
        private void tsbSubSetting_Click(object sender, EventArgs e)
        {
            SubSettingForm fm = new SubSettingForm();
            if (fm.ShowDialog() == DialogResult.OK)
            {
                RefreshServers();
            }
        }

        private void tsbSubUpdate_Click(object sender, EventArgs e)
        {
            UpdateSubscriptionProcess();
        }

        /// <summary>
        /// the subscription update process
        /// </summary>
        private void UpdateSubscriptionProcess()
        {
            AppendText(false, UIRes.I18N("MsgUpdateSubscriptionStart"));

            if (appConfig.subItem == null || appConfig.subItem.Count <= 0)
            {
                AppendText(false, UIRes.I18N("MsgNoValidSubscription"));
                return;
            }

            for (int k = 1; k <= appConfig.subItem.Count; k++)
            {
                if (appConfig.subItem[k - 1].enabled == false)
                {
                    continue;
                }

                string id = appConfig.subItem[k - 1].id.TrimEx();
                string url = appConfig.subItem[k - 1].url.TrimEx();
                bool allowInsecure = appConfig.subItem[k - 1].allowInsecure;
                bool bBase64Decode = appConfig.subItem[k - 1].bBase64Decode;
                string hashCode = $"{k}->";

                if (Utils.IsNullOrEmpty(id) || Utils.IsNullOrEmpty(url))
                {
                    AppendText(false, $"{hashCode}{UIRes.I18N("MsgNoValidSubscription")}");
                    continue;
                }

                DownloadHandle downloadHandle3 = new DownloadHandle();
                downloadHandle3.UpdateCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        AppendText(false, $"{hashCode}{UIRes.I18N("MsgGetSubscriptionSuccessfully")}");
                        // 获取返回的订阅数据
                        string result = args.Msg;
                        // 如果需要base64解码
                        if (bBase64Decode)
                        {
                            // Base64解码
                            result = Utils.Base64Decode(result);
                            if (Utils.IsNullOrEmpty(result))
                            {
                                // 解码失败 报错 中止处理
                                AppendText(false, $"{hashCode}{UIRes.I18N("MsgSubscriptionDecodingFailed")}");
                                return;
                            }
                        }

                        AppConfigHandler.RemoveServerViaSubid(ref appConfig, id);
                        AppendText(false, $"{hashCode}{UIRes.I18N("MsgClearSubscription")}");
                        RefreshServers();
                        if (AddBatchServers(result, id, allowInsecure) > 0)
                        {
                            // 显示出添加到末尾的订阅节点
                            lvServers.EnsureVisible(lvServers.Items.Count - 1);
                        }
                        else
                        {
                            AppendText(false, $"{hashCode}{UIRes.I18N("MsgFailedImportSubscription")}");
                        }
                        AppendText(false, $"{hashCode}{UIRes.I18N("MsgUpdateSubscriptionEnd")}");
                    }
                    else
                    {
                        AppendText(false, args.Msg);
                    }
                };
                downloadHandle3.Error += (sender2, args) =>
                {
                    AppendText(true, args.GetException().Message);
                };

                downloadHandle3.WebDownloadString(url);
                AppendText(false, $"{hashCode}{UIRes.I18N("MsgStartGettingSubscriptions")}");
            }
        }

        private void tsbQRCodeSwitch_CheckedChanged(object sender, EventArgs e)
        {
            bool bShow = tsbQRCodeSwitch.Checked;
            scMain.Panel2Collapsed = !bShow;
        }
        #endregion

        #region Language

        private void tsbLanguageDef_Click(object sender, EventArgs e)
        {
            SetCurrentLanguage("en");
        }

        private void tsbLanguageZhHans_Click(object sender, EventArgs e)
        {
            SetCurrentLanguage("zh-Hans");
        }
        private void SetCurrentLanguage(string value)
        {
            Utils.RegWriteValue(Global.MyRegPath, Global.MyRegKeyLanguage, value);
            //Application.Restart();
        }





        #endregion

        private void v2rayCoreV4321ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(Global.V2RayCore4_32_1Url);

            return;
        }

        private void menuClearTestResult_Click(object sender, EventArgs e)
        {
            Speedtest("clear");
        }

        private void addHy2ServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowServerForm((int)EConfigType.Hysteria2, -1);
            ShowForm();
        }

        private void tsbXrayWebsite_Click(object sender, EventArgs e)
        {
            Process.Start(Global.XrayWebsiteUrl);
        }

        private void tsbCheckUpdateV2rayCore_Click(object sender, EventArgs e)
        {
            Process.Start(Global.V2RayCore_Url);
        }

        private void menuAdjustListColumn_Click(object sender, EventArgs e)
        {
            //更新 list view
            lvServers.BeginUpdate();

            lvServers.AutoResizeColumns();

            lvServers.EndUpdate();
        }
    }
}
