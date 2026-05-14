using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace final_project
{
    public static class UIHelper
    {
        
        //輸入框的 Placeholder
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        public static void SetPlaceholder(TextBox textBox, string placeholderText)
        {
            SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholderText);
        }

      
        //通用按鈕樣式
      
        public static Button CreateStyledButton(string text, Color bgColor, int width = 120, int height = 40)
        {
            Button btn = new Button
            {
                Text = text,
                BackColor = bgColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(width, height),
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft JhengHei UI", 10, FontStyle.Regular)
            };
            btn.FlatAppearance.BorderSize = 0; // 去除邊框
            return btn;
        }    
        //  通用 DataGridView 表格樣式
        public static void SetGridStyle(DataGridView dgv)
        {
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AllowUserToAddRows = false;
            dgv.RowHeadersVisible = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // 標題列美化
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersHeight = 45;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft JhengHei UI", 10, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            
            // 資料列美化
            dgv.DefaultCellStyle.Font = new Font("Microsoft JhengHei UI", 10, FontStyle.Regular);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 231, 255); // 柔和的選取藍色
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 41, 59);
            dgv.RowTemplate.Height = 40; // 增加列高讓畫面不擁擠
            dgv.ReadOnly = true;
            
        }
    }
}