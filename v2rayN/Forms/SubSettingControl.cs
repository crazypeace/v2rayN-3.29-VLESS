using System;
using System.Windows.Forms;
using v2rayN.Base;
using v2rayN.Mode;

namespace v2rayN.Forms
{
    public delegate void ChangeEventHandler(object sender, EventArgs e);
    public partial class SubSettingControl : UserControl
    {
        public event ChangeEventHandler OnButtonClicked;


        public SubItem subItem { get; set; }

        public SubSettingControl()
        {
            InitializeComponent();
        }

        private void SubSettingControl_Load(object sender, EventArgs e)
        {
            BindingSub();
        }

        private void BindingSub()
        {
            if (subItem != null)
            {
                txtRemarks.Text = subItem.remarks.ToString();
                txtUrl.Text = subItem.url.ToString();
                chkEnabled.Checked = subItem.enabled;
                chkAllowInsecureTrue.Checked = subItem.allowInsecure;
                chkBase64Decode.Checked = subItem.bBase64Decode;
            }
        }
        private void EndBindingSub()
        {
            if (subItem != null)
            {
                subItem.remarks = txtRemarks.Text.TrimEx();
                subItem.url = txtUrl.Text.TrimEx();
                subItem.enabled = chkEnabled.Checked;
                subItem.allowInsecure = chkAllowInsecureTrue.Checked;
                subItem.bBase64Decode = chkBase64Decode.Checked;
            }
        }
        private void txtRemarks_Leave(object sender, EventArgs e)
        {
            EndBindingSub();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (subItem != null)
            {
                subItem.remarks = string.Empty;
                subItem.url = string.Empty;
            }

            OnButtonClicked?.Invoke(sender, e);
        }

        private void chkEnabled_Leave(object sender, EventArgs e)
        {
            EndBindingSub();
        }

        private void txtUrl_Leave(object sender, EventArgs e)
        {
            EndBindingSub();
        }

        private void chkAllowInsecureTrue_Leave(object sender, EventArgs e)
        {
            EndBindingSub();
        }

        private void chkBase64Decode_Leave(object sender, EventArgs e)
        {
            EndBindingSub();
        }
    }
}
