using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace final_project
{
    public static class DBHelper
    {
    
        private static string connString = @"Data Source =.\sqlexpress;Initial Catalog = InventorySystem; Integrated Security = True; Encrypt=False";

        // 核心功能：讀取資料並回傳 DataTable (給 DataGridView 使用)
        public static DataTable GetDataTable(string sql)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                    adapter.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("資料庫讀取失敗：\n" + ex.Message, "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return dt;
        }
        // 核心功能：執行 新增/修改/刪除 指令
        public static int ExecuteNonQuery(string sql, SqlParameter[] parameters = null)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("執行錯誤：" + ex.Message);
                return -1;
            }
        }
    }
}