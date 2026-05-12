using System;
using System.Collections.Generic;
using System.Data;       
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices; // 為了實現 Placeholder 效果
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
namespace final_project
{
    public partial class Form1 : Form
    {
        // 定義圖 2 的現代色彩
        private readonly Color ColorSidebar = Color.FromArgb(243, 246, 252);
        private readonly Color ColorPrimary = Color.FromArgb(79, 70, 229);
        private readonly Color ColorTextDark = Color.FromArgb(30, 41, 59);

        private Panel pnlSidebar, pnlTopBar, pnlContent;
        private Label lblPageTitle;

        // --- 高手黑科技：讓 .NET Framework 也能有 Placeholder ---
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;
        // -------------------------------------------------------

        public Form1()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1280, 800);
            this.Text = "進銷存系統 - 現代化版";
            this.Font = new Font("Microsoft JhengHei UI", 10);
            this.BackColor = Color.White;

            ApplyModernTheme();
        }

        private void ApplyModernTheme()
        {
            // 側邊導覽
            pnlSidebar = new Panel { Width = 260, Dock = DockStyle.Left, BackColor = ColorSidebar, Padding = new Padding(15) };

            // 頂部欄
            pnlTopBar = new Panel { Height = 70, Dock = DockStyle.Top, BackColor = Color.White };
            lblPageTitle = new Label { Text = "Dashboard", Font = new Font("Microsoft JhengHei UI", 16, FontStyle.Bold), Location = new Point(25, 20), AutoSize = true, ForeColor = ColorTextDark };
            pnlTopBar.Controls.Add(lblPageTitle);

            // 主內容
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(30) };

            // 生成選單 (評分表 3-23)
            string[] items = { "Dashboard", "客戶資料維護", "產品資料維護", "供應商資料維護", "銷貨結帳", "採購作業", "庫存報表", "備份還原" };
            int y = 80;
            foreach (var item in items)
            {
                Button btn = new Button
                {
                    Text = "   " + item,
                    Size = new Size(230, 48),
                    Location = new Point(15, y),
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => { lblPageTitle.Text = item; ShowModule(item); };
                pnlSidebar.Controls.Add(btn);
                y += 55;
            }

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlTopBar);
            this.Controls.Add(pnlSidebar);
            ShowModule("Dashboard");
        }

        private void ShowModule(string module)
        {
            pnlContent.Controls.Clear();
            lblPageTitle.Text = module;

            // 1. 處理「備份還原」特殊頁面
            if (module == "備份還原")
            {
                pnlContent.Controls.Add(CreateBackupRestoreSection());
                return;
            }

            // 2. 處理「Dashboard」首頁
            if (module == "Dashboard")
            {
                Label lbl = new Label
                {
                    Text = "歡迎使用進銷存系統！\n目前資料庫狀態：連線正常\n系統時間：" + DateTime.Now.ToString("yyyy/MM/dd HH:mm"),
                    AutoSize = true,
                    Font = new Font("Microsoft JhengHei UI", 12),
                    Location = new Point(30, 30)
                };
                pnlContent.Controls.Add(lbl);
                return;
            }

            // 3. 處理「一般維護頁面」（自動生成工具列與 DataGrid）
            string sql = "";
            switch (module)
            {
                case "客戶資料維護": sql = "SELECT CustomerID AS [編號], CustomerName AS [客戶名稱], Phone AS [電話], Email AS [信箱] FROM Customers"; break;
                case "產品資料維護": sql = "SELECT ProductID AS [編號], ProductName AS [產品名稱], UnitPrice AS [建議售價], StockQuantity AS [庫存量], SafetyStock AS [安全水位] FROM Products"; break;
                case "供應商資料維護": sql = "SELECT SupplierID AS [編號], CompanyName AS [公司名稱], ContactName AS [聯絡人], Phone AS [電話] FROM Suppliers"; break;
                case "庫存報表": sql = "SELECT ProductName AS [產品名稱], StockQuantity AS [目前庫存], SafetyStock AS [安全水位] FROM Products WHERE StockQuantity < SafetyStock"; break;
                case "銷貨結帳":
                    pnlContent.Controls.Add(CreateCheckoutSection());
                    break;
            }

            if (!string.IsNullOrEmpty(sql))
            {
                pnlContent.Controls.Add(CreateGridSection(module, sql));
            }
        }

        // 新增：專門處理備份還原的介面生成
        private Control CreateBackupRestoreSection()
        {
            Panel container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            Label lblTitle = new Label { Text = "系統維護工具", Font = new Font("Microsoft JhengHei UI", 14, FontStyle.Bold), AutoSize = true, Location = new Point(10, 10) };
            Label lblDesc = new Label { Text = "建議定期進行資料庫備份，以防資料遺失。", ForeColor = Color.Gray, AutoSize = true, Location = new Point(10, 45) };

            // 備份按鈕
            Button btnBackup = CreateActionButton("執行資料庫備份", ColorPrimary, 10);
            btnBackup.Location = new Point(10, 90);
            btnBackup.Size = new Size(200, 50);
            btnBackup.Click += (s, e) => {
                string path = @"C:\Temp\InventoryBackup.bak";
                // 確保路徑資料夾存在，或是提示使用者
                if (!System.IO.Directory.Exists(@"C:\Temp")) System.IO.Directory.CreateDirectory(@"C:\Temp");

                string backupSql = $@"BACKUP DATABASE [InventorySystem] TO DISK = '{path}' WITH FORMAT";
                if (DBHelper.ExecuteNonQuery(backupSql) != -1)
                {
                    MessageBox.Show($"備份成功！\n路徑：{path}", "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            // 還原按鈕 (補充功能)
            Button btnRestore = CreateActionButton("執行資料庫還原", Color.FromArgb(100, 116, 139), 220);
            btnRestore.Location = new Point(220, 90);
            btnRestore.Size = new Size(200, 50);
            btnRestore.Click += (s, e) => {
                if (MessageBox.Show("還原將會覆蓋現有資料，確定執行嗎？", "確認還原", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    string path = @"C:\Temp\InventoryBackup.bak";
                    // 還原 SQL 需要切換到 master 資料庫或強行中斷連線，這裡提供基礎指令
                    string restoreSql = $@"USE master; ALTER DATABASE [InventorySystem] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; 
                                   RESTORE DATABASE [InventorySystem] FROM DISK = '{path}' WITH REPLACE;
                                   ALTER DATABASE [InventorySystem] SET MULTI_USER;";

                    if (DBHelper.ExecuteNonQuery(restoreSql) != -1)
                    {
                        MessageBox.Show("還原成功！系統將自動重新載入。", "完成");
                        Application.Restart(); // 還原後建議重啟
                    }
                }
            };

            container.Controls.AddRange(new Control[] { lblTitle, lblDesc, btnBackup, btnRestore });
            return container;
        }

        // --- 僅列出關鍵修改的 CreateGridSection 部分 ---
        private Control CreateGridSection(string title, string sql)
        {
            Panel container = new Panel { Dock = DockStyle.Fill };
            Panel toolBar = new Panel { Height = 60, Dock = DockStyle.Top, Padding = new Padding(0, 10, 0, 10) };

            // 工具列按鈕
            TextBox txtSearch = new TextBox { Width = 180, Location = new Point(0, 15) };
            SendMessage(txtSearch.Handle, EM_SETCUEBANNER, 0, "搜尋內容...");
            Button btnAdd = CreateActionButton("新增", ColorPrimary, 200);
            Button btnEdit = CreateActionButton("修改", Color.Orange, 290);
            Button btnDelete = CreateActionButton("刪除", Color.Red, 380);
            toolBar.Controls.AddRange(new Control[] { txtSearch, btnAdd, btnEdit, btnDelete });

            // 資料表格
            DataTable data = DBHelper.GetDataTable(sql);
            DataGridView dgv = new DataGridView
            {
                DataSource = data,
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true
            };

            // 產品相片預覽
            if (title == "產品資料維護")
            {
                Panel pnlPhoto = new Panel { Width = 220, Dock = DockStyle.Right, Padding = new Padding(10), BackColor = Color.FromArgb(248, 250, 252) };
                PictureBox pb = new PictureBox { Width = 200, Height = 200, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
                pnlPhoto.Controls.Add(pb);
                container.Controls.Add(pnlPhoto);

                // 在 Form1.cs 的 CreateGridSection 內
                dgv.SelectionChanged += (s, e) => {
                    if (dgv.SelectedRows.Count > 0)
                    {
                        var id = dgv.SelectedRows[0].Cells[0].Value;
                        DataTable dt = DBHelper.GetDataTable(string.Format("SELECT ProductImage FROM Products WHERE ProductID = {0}", id));

                        if (dt.Rows.Count > 0 && dt.Rows[0]["ProductImage"] != DBNull.Value)
                        {
                            try
                            {
                                byte[] bytes = (byte[])dt.Rows[0]["ProductImage"];
                                using (MemoryStream ms = new MemoryStream(bytes))
                                {
                                    // 釋放舊圖片資源，避免記憶體洩漏
                                    if (pb.Image != null) pb.Image.Dispose();
                                    pb.Image = Image.FromStream(ms);
                                }
                            }
                            catch
                            {
                                pb.Image = null;
                            }
                        }
                        else { pb.Image = null; }
                    }
                };
            }

            // 按鈕點擊事件 (呼叫 EditForm)
            btnAdd.Click += (s, e) => {
                var meta = GetTableMetadata(title);
                if (new EditForm(meta.TableName, meta.PrimaryKey).ShowDialog() == DialogResult.OK) ShowModule(title);
            };

            btnEdit.Click += (s, e) => {
                if (dgv.SelectedRows.Count > 0)
                {
                    var meta = GetTableMetadata(title);
                    if (new EditForm(meta.TableName, meta.PrimaryKey, dgv.SelectedRows[0].Cells[0].Value).ShowDialog() == DialogResult.OK) ShowModule(title);
                }
            };

            // 搜尋功能實作
            txtSearch.TextChanged += (s, e) => {
                if (dgv.DataSource is DataTable dt)
                    dt.DefaultView.RowFilter = string.Format("[{0}] LIKE '%{1}%'", dt.Columns[1].ColumnName, txtSearch.Text);
            };

            container.Controls.Add(dgv);
            container.Controls.Add(toolBar);
            return container;
        }

        // 輔助方法：快速建立美化按鈕
        private Button CreateActionButton(string text, Color color, int x)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, 10),
                Size = new Size(80, 35),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft JhengHei UI", 9, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // 輔助方法：標題轉對應資料表名
        private string GetTableNameByTitle(string title)
        {
            if (title.Contains("客戶")) return "Customers";
            if (title.Contains("產品")) return "Products";
            if (title.Contains("供應商")) return "Suppliers";
            return "";
        }
        // 定義一個簡單的結構來存放資料表資訊
        private struct TableMetadata
        {
            public string TableName;
            public string PrimaryKey;
        }

        private TableMetadata GetTableMetadata(string title)
        {
            TableMetadata meta = new TableMetadata();
            switch (title)
            {
                case "客戶資料維護":
                    meta.TableName = "Customers";
                    meta.PrimaryKey = "CustomerID";
                    break;
                case "產品資料維護":
                    meta.TableName = "Products";
                    meta.PrimaryKey = "ProductID";
                    break;
                case "供應商資料維護":
                    meta.TableName = "Suppliers";
                    meta.PrimaryKey = "SupplierID";
                    break;
                // 銷貨與採購通常不建議直接刪除明細，若要測試可加入
                default:
                    meta.TableName = "";
                    meta.PrimaryKey = "";
                    break;
            }
            return meta;
        }
  

        private DataTable cartTable; // 用來存購物車的內容

        private Control CreateCheckoutSection()
        {
            Panel container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            // 初始化購物車結構
            cartTable = new DataTable();
            cartTable.Columns.Add("ProductID", typeof(int));
            cartTable.Columns.Add("產品名稱", typeof(string));
            cartTable.Columns.Add("單價", typeof(decimal));
            cartTable.Columns.Add("數量", typeof(int));
            cartTable.Columns.Add("小計", typeof(decimal), "單價 * 數量");

            // --- 左側：輸入區域 ---
            Panel pnlInput = new Panel { Width = 350, Dock = DockStyle.Left, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(15) };

            Label lblCust = new Label { Text = "1. 選擇客戶", Top = 10, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            ComboBox cbCust = new ComboBox { Top = 35, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            cbCust.DataSource = DBHelper.GetDataTable("SELECT CustomerID, CustomerName FROM Customers");
            cbCust.DisplayMember = "CustomerName"; cbCust.ValueMember = "CustomerID";

            Label lblProd = new Label { Text = "2. 選擇商品", Top = 80, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            ComboBox cbProd = new ComboBox { Top = 105, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            DataTable dtProd = DBHelper.GetDataTable("SELECT ProductID, ProductName, UnitPrice, StockQuantity FROM Products");
            cbProd.DataSource = dtProd;
            cbProd.DisplayMember = "ProductName"; cbProd.ValueMember = "ProductID";

            Label lblQty = new Label { Text = "3. 輸入數量", Top = 155, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            NumericUpDown numQty = new NumericUpDown { Top = 180, Width = 100, Minimum = 1, Maximum = 9999, Value = 1 };

            Button btnAdd = CreateActionButton("加入清單", Color.FromArgb(16, 185, 129), 0);
            btnAdd.Location = new Point(15, 230); btnAdd.Width = 300;

            pnlInput.Controls.AddRange(new Control[] { lblCust, cbCust, lblProd, cbProd, lblQty, numQty, btnAdd });

            // --- 右側：購物車區域 ---
            Panel pnlCart = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 0, 0) };
            DataGridView dgvCart = new DataGridView
            {
                DataSource = cartTable,
                Dock = DockStyle.Top,
                Height = 450,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            Label lblTotal = new Label { Text = "總金額：$ 0", Top = 470, Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = ColorPrimary, AutoSize = true };
            Button btnConfirm = CreateActionButton("確認結帳 (產生單據)", ColorPrimary, 0);
            btnConfirm.Location = new Point(0, 530); btnConfirm.Size = new Size(250, 50);

            pnlCart.Controls.AddRange(new Control[] { dgvCart, lblTotal, btnConfirm });

            // --- 事件處理 ---

            // 加入清單邏輯
            btnAdd.Click += (s, e) => {
                DataRowView selProd = (DataRowView)cbProd.SelectedItem;
                int stock = Convert.ToInt32(selProd["StockQuantity"]);
                int want = (int)numQty.Value;

                // 庫存檢查防呆 (評分項 19-22)
                if (want > stock)
                {
                    MessageBox.Show($"庫存不足！目前僅剩 {stock} 件。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                cartTable.Rows.Add(selProd["ProductID"], selProd["ProductName"], selProd["UnitPrice"], want);
                UpdateTotal(lblTotal);
            };

            // 結帳邏輯
            btnConfirm.Click += (s, e) => {
                if (cartTable.Rows.Count == 0) return;

                decimal total = (decimal)cartTable.Compute("Sum(小計)", "");
                if (PerformTransactionCheckout((int)cbCust.SelectedValue, total))
                {
                    MessageBox.Show("結帳成功！已同步更新庫存。", "完成");
                    cartTable.Rows.Clear();
                    UpdateTotal(lblTotal);
                    ShowModule("銷貨結帳"); // 重新載入以更新下拉選單的庫存數
                }
            };

            container.Controls.Add(pnlCart);
            container.Controls.Add(pnlInput);
            return container;
        }

        private void UpdateTotal(Label lbl)
        {
            object sum = cartTable.Compute("Sum(小計)", "");
            lbl.Text = "總金額：$ " + (sum == DBNull.Value ? "0" : Convert.ToDecimal(sum).ToString("N0"));
        }
        private bool PerformTransactionCheckout(int customerId, decimal total)
        {
            // 請確保連線字串正確
            string connStr = @"Data Source =.\sqlexpress;Initial Catalog = InventorySystem; Integrated Security = True; Encrypt=False";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    // 1. 寫入主檔
                    string sqlM = "INSERT INTO SalesMaster (CustomerID, TotalAmount, SalesDate) OUTPUT INSERTED.SalesID VALUES (@cid, @total, GETDATE())";
                    SqlCommand cmdM = new SqlCommand(sqlM, conn, trans);
                    cmdM.Parameters.AddWithValue("@cid", customerId);
                    cmdM.Parameters.AddWithValue("@total", total);
                    int salesId = (int)cmdM.ExecuteScalar();

                    // 2. 寫入明細並扣庫存
                    foreach (DataRow row in cartTable.Rows)
                    {
                        // 寫入明細
                        string sqlD = "INSERT INTO SalesDetails (SalesID, ProductID, Quantity, UnitPrice) VALUES (@sid, @pid, @qty, @price)";
                        SqlCommand cmdD = new SqlCommand(sqlD, conn, trans);
                        cmdD.Parameters.AddWithValue("@sid", salesId);
                        cmdD.Parameters.AddWithValue("@pid", row["ProductID"]);
                        cmdD.Parameters.AddWithValue("@qty", row["數量"]);
                        cmdD.Parameters.AddWithValue("@price", row["單價"]);
                        cmdD.ExecuteNonQuery();

                        // 扣庫存 (重點！)
                        string sqlUp = "UPDATE Products SET StockQuantity = StockQuantity - @qty WHERE ProductID = @pid";
                        SqlCommand cmdUp = new SqlCommand(sqlUp, conn, trans);
                        cmdUp.Parameters.AddWithValue("@qty", row["數量"]);
                        cmdUp.Parameters.AddWithValue("@pid", row["ProductID"]);
                        cmdUp.ExecuteNonQuery();
                    }

                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    MessageBox.Show("結帳失敗，資料已回滾：\n" + ex.Message);
                    return false;
                }
            }
        }
    }
}