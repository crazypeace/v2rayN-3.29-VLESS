using System;
using System.Windows.Forms;
using v2rayN.Handler;
using v2rayN.Mode;

namespace v2rayN.Forms
{
    public partial class AddHy2ServerForm : BaseServerForm
    { 

        public AddHy2ServerForm()
        {
            InitializeComponent();
        }

        private void AddHy2ServerForm_Load(object sender, EventArgs e)
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
            cmbSecurity.Text = nodeItem.security;
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
            cmbSecurity.Text = Global.DefaultSecurity;
            txtRemarks.Text = "";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string address = txtAddress.Text;
            string port = txtPort.Text;
            string id = txtId.Text;
            string security = cmbSecurity.Text;
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
            if (Utils.IsNullOrEmpty(id))
            {
                UI.Show(UIRes.I18N("FillPassword"));
                return;
            }
            if (Utils.IsNullOrEmpty(security))
            {
                UI.Show(UIRes.I18N("PleaseSelectEncryption"));
                return;
            }

            nodeItem.address = address;
            nodeItem.port = Utils.ToInt(port);
            nodeItem.id = id;
            nodeItem.security = security;
            nodeItem.remarks = remarks;

            if (AppConfigHandler.AddShadowsocksServer(ref appConfig, nodeItem, EditIndex) == 0)
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

            NodeItem ssItem = V2rayConfigHandler.ImportFromClipboardConfig(Utils.GetClipboardData(), out string msg);
            if (ssItem == null)
            {
                UI.ShowWarning(msg);
                return;
            }

            txtAddress.Text = ssItem.address;
            txtPort.Text = ssItem.port.ToString();
            cmbSecurity.Text = ssItem.security;
            txtId.Text = ssItem.id;
            txtRemarks.Text = ssItem.remarks;
        }
         
        #endregion
         

    }
}
