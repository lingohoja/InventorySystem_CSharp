using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices; // 為了實現 Placeholder 效果
using System.Windows.Forms;
using System.Data;       
using System.Data.SqlClient;
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

        private Control CreateGridSection(string title, string sql)
        {
            Panel container = new Panel { Dock = DockStyle.Fill };

            // 1. 建立工具列 (Toolbar)
            Panel toolBar = new Panel { Height = 60, Dock = DockStyle.Top, Padding = new Padding(0, 10, 0, 10) };

            // --- 搜尋功能 (查詢) ---
            TextBox txtSearch = new TextBox { Width = 200, Location = new Point(0, 15) };
            SendMessage(txtSearch.Handle, EM_SETCUEBANNER, 0, "搜尋內容...");

            // --- 功能按鈕生成器 ---
            Button btnAdd = CreateActionButton("新增", ColorPrimary, 220);
            Button btnEdit = CreateActionButton("修改", Color.FromArgb(245, 158, 11), 310); // 橘色
            Button btnDelete = CreateActionButton("刪除", Color.FromArgb(239, 68, 68), 400); // 紅色
            Button btnPrint = CreateActionButton("列印", Color.FromArgb(16, 185, 129), 490);  // 綠色

            toolBar.Controls.AddRange(new Control[] { txtSearch, btnAdd, btnEdit, btnDelete, btnPrint });

            // 2. 建立 DataGridView
            DataTable data = DBHelper.GetDataTable(sql);
            DataGridView dgv = new DataGridView
            {
                DataSource = data,
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersHeight = 45;

            // --- 邏輯實作：搜尋 (即時查詢) ---
            txtSearch.TextChanged += (s, e) => {
                if (dgv.DataSource is DataTable dt)
                {
                    string filter = "";
                    foreach (DataColumn col in dt.Columns)
                    {
                        if (col.DataType == typeof(string))
                        {
                            if (filter != "") filter += " OR ";
                            filter += $"[{col.ColumnName}] LIKE '%{txtSearch.Text}%'";
                        }
                    }
                    dt.DefaultView.RowFilter = filter;
                }
            };

            // --- 邏輯實作：刪除 ---
            btnDelete.Click += (s, e) => {
                if (dgv.SelectedRows.Count > 0)
                {
                    // 1. 抓取選取列的第一個單元格數值 (ID 數值)
                    var idValue = dgv.SelectedRows[0].Cells[0].Value;

                    // 2. 取得該模組對應的資料庫資訊 (表名與主鍵名)
                    var metadata = GetTableMetadata(title);
                    string tableName = metadata.TableName;
                    string pkName = metadata.PrimaryKey;

                    if (string.IsNullOrEmpty(tableName))
                    {
                        MessageBox.Show("此頁面不支援刪除操作。");
                        return;
                    }

                    // 3. 確認刪除提示
                    if (MessageBox.Show($"確定要刪除 {title} 編號: {idValue} 嗎？", "確認刪除",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        // 使用正確的資料庫欄位名稱建構 SQL
                        string delSql = $"DELETE FROM {tableName} WHERE [{pkName}] = @id";
                        SqlParameter[] p = { new SqlParameter("@id", idValue) };

                        if (DBHelper.ExecuteNonQuery(delSql, p) > 0)
                        {
                            MessageBox.Show("資料已刪除，請透過「備份還原」測試恢復功能！", "操作成功");
                            ShowModule(title); // 重新整理頁面
                        }
                    }
                }
                else
                {
                    MessageBox.Show("請先選擇要刪除的資料列。");
                }
            };

            // --- 邏輯實作：列印 (簡單模擬) ---
            btnPrint.Click += (s, e) => {
                MessageBox.Show("正在準備報表格式並傳送到列印機...", "系統提示");
                // 實際開發可整合 Crystal Reports 或 PrintDocument
            };

            // --- 邏輯實作：新增與修改 (跳出對話框) ---
            btnAdd.Click += (s, e) => MessageBox.Show($"啟動 {title} 的新增表單");
            btnEdit.Click += (s, e) => MessageBox.Show($"啟動 {title} 的編輯表單 (對象 ID: {dgv.SelectedRows[0].Cells[0].Value})");

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
        // 定義一個暫存的購物車 DataTable
        private DataTable cartTable;

        private Control CreateCheckoutSection()
        {
            Panel container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // --- 1. 初始化暫存購物車結構 ---
            cartTable = new DataTable();
            cartTable.Columns.Add("ProductID", typeof(int));
            cartTable.Columns.Add("產品名稱", typeof(string));
            cartTable.Columns.Add("單價", typeof(decimal));
            cartTable.Columns.Add("數量", typeof(int));
            cartTable.Columns.Add("小計", typeof(decimal), "單價 * 數量");

            // --- 2. 佈局：左側輸入區 ---
            Panel pnlInput = new Panel { Width = 350, Dock = DockStyle.Left, Padding = new Padding(10) };

            Label lblCust = new Label { Text = "選擇客戶：", Top = 10, Left = 10 };
            ComboBox cbCustomer = new ComboBox { Top = 35, Left = 10, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            // 載入客戶資料
            cbCustomer.DataSource = DBHelper.GetDataTable("SELECT CustomerID, CustomerName FROM Customers");
            cbCustomer.DisplayMember = "CustomerName";
            cbCustomer.ValueMember = "CustomerID";

            Label lblProd = new Label { Text = "選擇商品：", Top = 80, Left = 10 };
            ComboBox cbProduct = new ComboBox { Top = 105, Left = 10, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            // 載入商品資料
            cbProduct.DataSource = DBHelper.GetDataTable("SELECT ProductID, ProductName, UnitPrice FROM Products");
            cbProduct.DisplayMember = "ProductName";
            cbProduct.ValueMember = "ProductID";

            Label lblQty = new Label { Text = "輸入數量：", Top = 150, Left = 10 };
            NumericUpDown numQty = new NumericUpDown { Top = 175, Left = 10, Width = 100, Minimum = 1, Value = 1 };

            Button btnAddCart = CreateActionButton("加入購物車", Color.FromArgb(16, 185, 129), 10);
            btnAddCart.Top = 220; btnAddCart.Width = 300;

            pnlInput.Controls.AddRange(new Control[] { lblCust, cbCustomer, lblProd, cbProduct, lblQty, numQty, btnAddCart });

            // --- 3. 佈局：右側購物車與結帳 ---
            Panel pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            DataGridView dgvCart = new DataGridView
            {
                DataSource = cartTable,
                Dock = DockStyle.Top,
                Height = 400,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            Label lblTotal = new Label
            {
                Text = "總計金額：$0",
                Top = 420,
                Left = 10,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = ColorPrimary,
                AutoSize = true
            };

            Button btnCheckout = CreateActionButton("確認結帳 (送出單據)", ColorPrimary, 10);
            btnCheckout.Top = 470; btnCheckout.Width = 200; btnCheckout.Height = 50;

            pnlRight.Controls.AddRange(new Control[] { dgvCart, lblTotal, btnCheckout });

            // --- 4. 邏輯實作 ---

            // 加入購物車
            btnAddCart.Click += (s, e) => {
                DataRowView prodRow = (DataRowView)cbProduct.SelectedItem;
                int pid = (int)prodRow["ProductID"];
                string pName = prodRow["ProductName"].ToString();
                decimal price = (decimal)prodRow["UnitPrice"];
                int qty = (int)numQty.Value;

                cartTable.Rows.Add(pid, pName, price, qty);

                decimal sum = 0;
                foreach (DataRow row in cartTable.Rows) sum += (decimal)row["小計"];
                lblTotal.Text = "總計金額：$" + sum.ToString("N0");
            };

            // 確認結帳 (Transaction 核心邏輯)
            btnCheckout.Click += (s, e) => {
                if (cartTable.Rows.Count == 0) { MessageBox.Show("購物車是空的！"); return; }

                int customerId = (int)cbCustomer.SelectedValue;
                decimal totalAmount = 0;
                foreach (DataRow row in cartTable.Rows) totalAmount += (decimal)row["小計"];

                if (PerformCheckout(customerId, totalAmount))
                {
                    MessageBox.Show("結帳成功！已扣除庫存並產生單據。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    cartTable.Rows.Clear();
                    lblTotal.Text = "總計金額：$0";
                    ShowModule("銷貨結帳"); // 重新整理
                }
            };

            container.Controls.Add(pnlRight);
            container.Controls.Add(pnlInput);
            return container;
        }
        private bool PerformCheckout(int customerId, decimal total)
        {
            // 這裡我們直接寫在 Form1 方便你查看，高手通常會封裝在 DBHelper
            string connString = @"Data Source =.\sqlexpress;Initial Catalog = InventorySystem; Integrated Security = True; Encrypt=False";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction(); // 開始交易

                try
                {
                    // 1. 寫入銷貨主檔 (SalesMaster)
                    string sqlM = "INSERT INTO SalesMaster (CustomerID, TotalAmount, SalesDate) OUTPUT INSERTED.SalesID VALUES (@cid, @total, GETDATE())";
                    SqlCommand cmdM = new SqlCommand(sqlM, conn, trans);
                    cmdM.Parameters.AddWithValue("@cid", customerId);
                    cmdM.Parameters.AddWithValue("@total", total);
                    int salesId = (int)cmdM.ExecuteScalar(); // 取得剛生成的單號

                    // 2. 迴圈寫入明細 (SalesDetails) 並 扣庫存 (Products)
                    foreach (DataRow row in cartTable.Rows)
                    {
                        int pid = (int)row["ProductID"];
                        int qty = (int)row["數量"];
                        decimal price = (decimal)row["單價"];

                        // 寫入明細
                        string sqlD = "INSERT INTO SalesDetails (SalesID, ProductID, Quantity, UnitPrice) VALUES (@sid, @pid, @qty, @price)";
                        SqlCommand cmdD = new SqlCommand(sqlD, conn, trans);
                        cmdD.Parameters.AddWithValue("@sid", salesId);
                        cmdD.Parameters.AddWithValue("@pid", pid);
                        cmdD.Parameters.AddWithValue("@qty", qty);
                        cmdD.Parameters.AddWithValue("@price", price);
                        cmdD.ExecuteNonQuery();

                        // 扣庫存
                        string sqlUpdate = "UPDATE Products SET StockQuantity = StockQuantity - @qty WHERE ProductID = @pid";
                        SqlCommand cmdUpdate = new SqlCommand(sqlUpdate, conn, trans);
                        cmdUpdate.Parameters.AddWithValue("@qty", qty);
                        cmdUpdate.Parameters.AddWithValue("@pid", pid);
                        cmdUpdate.ExecuteNonQuery();
                    }

                    trans.Commit(); // 全部成功，提交！
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback(); // 只要有一步失敗，全部撤回！
                    MessageBox.Show("結帳失敗，已回滾所有更動：\n" + ex.Message);
                    return false;
                }
            }
        }
    }
}