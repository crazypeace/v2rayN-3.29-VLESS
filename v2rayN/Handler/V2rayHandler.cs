﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using v2rayN.Mode;

namespace v2rayN.Handler
{

    /// <summary>
    /// 消息委托
    /// </summary>
    /// <param name="notify">是否显示在托盘区</param>
    /// <param name="msg">内容</param>
    public delegate void ProcessDelegate(bool notify, string msg);

    /// <summary>
    /// v2ray进程处理类
    /// </summary>
    class V2rayHandler
    {
        private static string v2rayConfigRes = Global.v2rayConfigFileName;
        private static string coreExe = "";   // 指定内核的程序名称
        private List<string> lstV2ray;
        public event ProcessDelegate ProcessEvent;
        //private int processId = 0;
        private Process _process;

        public V2rayHandler()
        {
            lstV2ray = new List<string>
            {
                "xray",
                "wv2ray",
                "v2ray"
            };
        }

        /// <summary>
        /// 载入V2ray
        /// </summary>
        public void LoadV2ray(V2rayNappConfig appConfig)
        {
            if (Global.reloadV2ray)
            {
                string fileName = Utils.GetPath(v2rayConfigRes);
                if (V2rayConfigHandler.GenerateClientConfig(appConfig, fileName, false, out string msg) != 0)
                {
                    ShowMsg(false, msg);
                }
                else
                {
                    ShowMsg(true, msg);

                    // 根据协议不同指定不同的内核
                    SetCoreExeByConfig( appConfig.outbound[ appConfig.index] );
                    V2rayRestart();
                }
            }
        }

        // 根据协议不同指定不同的内核
        public void SetCoreExeByConfig(NodeItem outbound)
        {
            // 如果是 reality 要使用 xray
            if (outbound.streamSecurity == Global.StreamSecurityReality)
            {
                coreExe = "xray";
            }
            // 如果是 hy2 要使用 v2ray
            else if (outbound.configType == (int)EConfigType.Hysteria2)
            {
                coreExe = "v2ray";
            }
            // 如果是V vmess 要使用 v2ray
            else if (outbound.configType == (int)EConfigType.Vmess)
            {
                coreExe = "v2ray";
            }
            // 其它情况不指定
            else
            {
                coreExe = "";
            }
        }

        /// <summary>
        /// 新建进程，载入V2ray配置文件字符串
        /// 主要是在测速的时候用到
        /// 返回新进程pid。
        /// </summary>
        public int LoadV2rayConfigString(V2rayNappConfig appConfig, List<int> _selecteds)
        {
            int pid = -1;
            string configStr = V2rayConfigHandler.GenerateClientSpeedtestConfigString(appConfig, _selecteds, out string msg);
            if (configStr == "")
            {
                ShowMsg(false, msg);
            }
            else
            {
                ShowMsg(false, msg);

                // 根据协议不同指定不同的内核
                SetCoreExeByConfig( appConfig.outbound[ _selecteds[0] ]);
                // 启动测试用的内核
                pid = V2rayStartNew(configStr);
                //V2rayRestart();
                // start with -appConfig
            }
            return pid;
        }

        /// <summary>
        /// V2ray重启
        /// </summary>
        private void V2rayRestart()
        {
            V2rayStop();
            V2rayStart();
        }

        /// <summary>
        /// V2ray停止
        /// </summary>
        public void V2rayStop()
        {
            try
            {
                if (_process != null)
                {
                    KillProcess(_process);
                    _process.Dispose();
                    _process = null;
                }
                else
                {
                    foreach (string vName in lstV2ray)
                    {
                        Process[] existing = Process.GetProcessesByName(vName);
                        foreach (Process p in existing)
                        {
                            string path = p.MainModule.FileName;
                            if (path == $"{Utils.GetPath(vName)}.exe")
                            {
                                KillProcess(p);
                            }
                        }
                    }
                }

                //bool blExist = true;
                //if (processId > 0)
                //{
                //    Process p1 = Process.GetProcessById(processId);
                //    if (p1 != null)
                //    {
                //        p1.Kill();
                //        blExist = false;
                //    }
                //}
                //if (blExist)
                //{
                //    foreach (string vName in lstV2ray)
                //    {
                //        Process[] killPro = Process.GetProcessesByName(vName);
                //        foreach (Process p in killPro)
                //        {
                //            p.Kill();
                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                Utils.SaveLog(ex.Message, ex);
            }
        }
        /// <summary>
        /// V2ray停止
        /// </summary>
        public void V2rayStopPid(int pid)
        {
            try
            {
                Process _p = Process.GetProcessById(pid);
                KillProcess(_p);
            }
            catch (Exception ex)
            {
                Utils.SaveLog(ex.Message, ex);
            }
        }

        private string V2rayFindexe() {
            //查找v2ray文件是否存在
            string fileName = string.Empty;
            //lstV2ray.Reverse();

            // 如果指定了内核, 那么只查找指定内核
            List<string> checklist = new List<string>();
            if ( ! Utils.IsNullOrEmpty( coreExe) )
            {
                checklist.Add(coreExe);
            }
            else
            {   
                // 否则查找默认内核列表
                checklist = lstV2ray;
            }

            foreach (string name in checklist)
            {
                string vName = string.Format("{0}.exe", name);
                vName = Utils.GetPath(vName);
                if (File.Exists(vName))
                {
                    fileName = vName;
                    break;
                }
            }
            if (Utils.IsNullOrEmpty(fileName))
            {
                string msg = string.Format(UIRes.I18N("NotFoundCore"), @"https://github.com/v2fly/v2ray-core/releases");
                ShowMsg(false, msg);
            }
            return fileName;
        }

        /// <summary>
        /// V2ray启动
        /// </summary>
        private void V2rayStart()
        {
            ShowMsg(false, string.Format(UIRes.I18N("StartService"), DateTime.Now.ToString()));

            try
            {
                string fileName = V2rayFindexe();
                if (fileName == "") return;

                Process p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = "run -c "+ Global.v2rayConfigFileName,
                        WorkingDirectory = Utils.StartupPath(),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8
                    }
                };
                p.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        string msg = e.Data + Environment.NewLine;
                        ShowMsg(false, msg);
                    }
                });
                p.Start();
                p.PriorityClass = ProcessPriorityClass.High;
                p.BeginOutputReadLine();
                //processId = p.Id;
                _process = p;

                if (p.WaitForExit(1000))
                {
                    throw new Exception(p.StandardError.ReadToEnd());
                }

                Global.processJob.AddProcess(p.Handle);
            }
            catch (Exception ex)
            {
                Utils.SaveLog(ex.Message, ex);
                string msg = ex.Message;
                ShowMsg(true, msg);
            }
        }
        /// <summary>
        /// V2ray启动，新建进程，传入配置字符串
        /// </summary>
        private int V2rayStartNew(string configStr)
        {
            ShowMsg(false, string.Format(UIRes.I18N("StartService"), DateTime.Now.ToString()));

            try
            {
                string fileName = V2rayFindexe();
                if (fileName == "") return -1;

                Process p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = "run -format json",
                        WorkingDirectory = Utils.StartupPath(),
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8
                    }
                };
                p.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        string msg = e.Data + Environment.NewLine;
                        ShowMsg(false, msg);
                    }
                });
                p.Start();
                p.BeginOutputReadLine();

                p.StandardInput.Write(configStr);
                p.StandardInput.Close();

                if (p.WaitForExit(1000))
                {
                    throw new Exception(p.StandardError.ReadToEnd());
                }

                Global.processJob.AddProcess(p.Handle);
                return p.Id;
            }
            catch (Exception ex)
            {
                Utils.SaveLog(ex.Message, ex);
                string msg = ex.Message;
                ShowMsg(false, msg);
                return -1;
            }
        }

        /// <summary>
        /// 消息委托
        /// </summary>
        /// <param name="updateToTrayTooltip">是否更新托盘图标的工具提示</param>
        /// <param name="msg">输出到日志框</param>
        private void ShowMsg(bool updateToTrayTooltip, string msg)
        {
            ProcessEvent?.Invoke(updateToTrayTooltip, msg);
        }

        private void KillProcess(Process p)
        {
            try
            {
                p.CloseMainWindow();
                p.WaitForExit(100);
                if (!p.HasExited)
                {
                    p.Kill();
                    p.WaitForExit(100);
                }
            }
            catch (Exception ex)
            {
                Utils.SaveLog(ex.Message, ex);
            }
        }         
    }
}
