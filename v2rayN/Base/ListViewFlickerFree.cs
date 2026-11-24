using System.Drawing;
using System.Windows.Forms;

namespace v2rayN.Base
{
    class ListViewFlickerFree : ListView
    {
        public ListViewFlickerFree()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.AllPaintingInWmPaint
                    , true);
            UpdateStyles();
        }


        public void AutoResizeColumns()
        {
            try
            {
                this.SuspendLayout();
                Graphics graphics = this.CreateGraphics();

                // 原生 ColumnHeaderAutoResizeStyle.ColumnContent 将忽略列头宽度
                this.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

                for (int i = 0; i < this.Columns.Count; i++)
                {
                    ColumnHeader c = this.Columns[i];
                    int cWidth = c.Width;
                    string MaxStr = "";
                    Font font = this.Items[0].SubItems[0].Font;

                    foreach (ListViewItem item in this.Items)
                    {
                        // 整行视作相同字形，不单独计算每个单元格
                        //font = item.SubItems[i].Font;
                        string str = item.SubItems[i].Text;
                        if (str.Length > MaxStr.Length) // 未考虑非等宽问题
                            MaxStr = str;
                    }

                    // 避免选中项字体变粗, 宽度不够.  追加 字符数*2 的宽度
                    int strWidth = (int)graphics.MeasureString(MaxStr, font).Width + MaxStr.Length * 2;
                    c.Width = System.Math.Max(cWidth, strWidth);
                }

                // 只收缩最后一列, 补偿前面各列累加后超出窗口宽度的部分, 避免水平滚动条。
                // 其它列的宽度计算保持不变; 溢出太大时最后一列压到下限为止。
                if (this.Columns.Count > 1)
                {
                    int available = this.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2;
                    int total = 0;
                    for (int i = 0; i < this.Columns.Count; i++)
                    {
                        total += this.Columns[i].Width;
                    }
                    int overflow = total - available;
                    if (overflow > 0)
                    {
                        ColumnHeader lastColumn = this.Columns[this.Columns.Count - 1];
                        int minWidth = System.Math.Max(
                            (int)graphics.MeasureString(lastColumn.Text, this.Font).Width + 8, 60);
                        if (lastColumn.Width > minWidth)
                        {
                            lastColumn.Width = System.Math.Max(minWidth, lastColumn.Width - overflow);
                        }
                    }
                }
                this.ResumeLayout();
            }
            catch { }
        }
    }
}