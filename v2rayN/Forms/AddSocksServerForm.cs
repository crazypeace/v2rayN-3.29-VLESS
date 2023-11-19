using System;
using System.Windows.Forms;
using v2rayN.Handler;
using v2rayN.Mode;

namespace v2rayN.Forms
{
    public partial class AddSocksServerForm : BaseServerForm
    { 

        public AddSocksServerForm()
        {
            InitializeComponent();
        }

        private void AddSocksServerForm_Load(object sender, EventArgs e)
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

            if (EditIndex >= 0)
            {
                nodeItem = appConfig.outbound[EditIndex];
                BindingServer();
            }
            else
            {
                nodeItem = new NodeItem();
                ClearServer();
            }
        }

        /// <summary>
        /// 绑定数据
        /// </summary>
        private void BindingServer()
        {
            txtAddress.Text = nodeItem.address;
            txtPort.Text = nodeItem.port.ToString();
            txtId.Text = nodeItem.id;
            txtSecurity.Text = nodeItem.security;
            txtRemarks.Text = nodeItem.remarks;
        }


        /// <summary>
        /// 清除设置
        /// </summary>
        private void ClearServer()
        {
            txtAddress.Text = "";
            txtPort.Text = "";
            txtId.Text = "";
            txtSecurity.Text = "";
            txtRemarks.Text = "";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string address = txtAddress.Text;
            string port = txtPort.Text;
            string id = txtId.Text;
            string security = txtSecurity.Text;
            string remarks = txtRemarks.Text;

            if (Utils.IsNullOrEmpty(address))
            {
                UI.Show(UIRes.I18N("FillServerAddress"));
                return;
            }
            if (Utils.IsNullOrEmpty(port) || !Utils.IsNumberic(port))
            {
                UI.Show(UIRes.I18N("FillCorrectServerPort"));
                return;
            }

            nodeItem.address = address;
            nodeItem.port = Utils.ToInt(port);
            nodeItem.id = id;
            nodeItem.security = security;
            nodeItem.remarks = remarks;

            if (AppConfigHandler.AddSocksServer(ref appConfig, nodeItem, EditIndex) == 0)
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                UI.ShowWarning(UIRes.I18N("OperationFailed"));
            }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }


        #region 导入配置
         
        /// <summary>
        /// 从剪贴板导入URL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItemImportClipboard_Click(object sender, EventArgs e)
        {
            ImportConfig();
        }

        private void ImportConfig()
        {
            ClearServer();

            NodeItem socksItem = V2rayConfigHandler.ImportFromClipboardConfig(Utils.GetClipboardData(), out string msg);
            if (socksItem == null)
            {
                UI.ShowWarning(msg);
                return;
            }

            txtAddress.Text = socksItem.address;
            txtPort.Text = socksItem.port.ToString();
            txtSecurity.Text = socksItem.security;
            txtId.Text = socksItem.id;
            txtRemarks.Text = socksItem.remarks;
        }        

        #endregion
         

    }
}
