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
            txtId.Text = nodeItem.id; // 密码放在 ID 属性中
            cmbStreamSecurity.Text = nodeItem.streamSecurity;
            cmbAllowInsecure.Text = nodeItem.allowInsecure;
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
            cmbStreamSecurity.Text = "";
            cmbAllowInsecure.Text = "";
            txtRemarks.Text = "";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string address = txtAddress.Text;
            string port = txtPort.Text;
            string id = txtId.Text;
            string streamSecurity = cmbStreamSecurity.Text;
            string allowInsecure = cmbAllowInsecure.Text;
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

            nodeItem.address = address;
            nodeItem.port = Utils.ToInt(port);
            nodeItem.id = id; // 密码放在 ID 属性中
            nodeItem.streamSecurity = streamSecurity;
            nodeItem.allowInsecure = allowInsecure;
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

            NodeItem hy2Item = V2rayConfigHandler.ImportFromClipboardConfig(Utils.GetClipboardData(), out string msg);
            if (hy2Item == null)
            {
                UI.ShowWarning(msg);
                return;
            }

            txtAddress.Text = hy2Item.address;
            txtPort.Text = hy2Item.port.ToString();
            cmbStreamSecurity.Text = hy2Item.streamSecurity;
            cmbAllowInsecure.Text = hy2Item.allowInsecure;
            txtId.Text = hy2Item.id;
            txtRemarks.Text = hy2Item.remarks;
        }
         
        #endregion        

    }
}
