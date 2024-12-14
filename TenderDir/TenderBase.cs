#region

using System.Data;
using MySql.Data.MySqlClient;

#endregion

namespace ParserTenders.TenderDir
{
    public class TenderBase
    {
        protected string PlacingWay;

        protected void AddVerNumber(MySqlConnection connect, string purchaseNumber, int typeFz)
        {
            var verNum = 1;
            var selectTenders =
                $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchaseNumber AND type_fz = @typeFz ORDER BY id_tender ASC";
            var cmd1 = new MySqlCommand(selectTenders, connect);
            cmd1.Prepare();
            cmd1.Parameters.AddWithValue("@purchaseNumber", purchaseNumber);
            cmd1.Parameters.AddWithValue("@typeFz", typeFz);
            var dt1 = new DataTable();
            var adapter1 = new MySqlDataAdapter { SelectCommand = cmd1 };
            adapter1.Fill(dt1);
            if (dt1.Rows.Count > 0)
            {
                var updateTender =
                    $"UPDATE {Program.Prefix}tender SET num_version = @num_version WHERE id_tender = @id_tender";
                foreach (DataRow ten in dt1.Rows)
                {
                    var idTender = (int)ten["id_tender"];
                    var cmd2 = new MySqlCommand(updateTender, connect);
                    cmd2.Prepare();
                    cmd2.Parameters.AddWithValue("@id_tender", idTender);
                    cmd2.Parameters.AddWithValue("@num_version", verNum);
                    cmd2.ExecuteNonQuery();
                    verNum++;
                }
            }
        }

        protected void GetEtp(MySqlConnection connect, out int idEtp, string etpName, string etpUrl)
        {
            var selectEtp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE name = @name AND url = @url";
            var cmd7 = new MySqlCommand(selectEtp, connect);
            cmd7.Prepare();
            cmd7.Parameters.AddWithValue("@name", etpName);
            cmd7.Parameters.AddWithValue("@url", etpUrl);
            var dt5 = new DataTable();
            var adapter5 = new MySqlDataAdapter { SelectCommand = cmd7 };
            adapter5.Fill(dt5);
            if (dt5.Rows.Count > 0)
            {
                idEtp = (int)dt5.Rows[0].ItemArray[0];
            }
            else
            {
                var insertEtp =
                    $"INSERT INTO {Program.Prefix}etp SET name = @name, url = @url, conf=0";
                var cmd8 = new MySqlCommand(insertEtp, connect);
                cmd8.Prepare();
                cmd8.Parameters.AddWithValue("@name", etpName);
                cmd8.Parameters.AddWithValue("@url", etpUrl);
                cmd8.ExecuteNonQuery();
                idEtp = (int)cmd8.LastInsertedId;
            }
        }

        protected void GetPlacingWay(MySqlConnection connect, out int idPlacingWay)
        {
            if (!string.IsNullOrEmpty(PlacingWay))
            {
                var selectPlacingWay =
                    $"SELECT id_placing_way FROM {Program.Prefix}placing_way WHERE name = @name";
                var cmd5 = new MySqlCommand(selectPlacingWay, connect);
                cmd5.Prepare();
                cmd5.Parameters.AddWithValue("@name", PlacingWay);
                var dt4 = new DataTable();
                var adapter4 = new MySqlDataAdapter { SelectCommand = cmd5 };
                adapter4.Fill(dt4);
                if (dt4.Rows.Count > 0)
                {
                    idPlacingWay = (int)dt4.Rows[0].ItemArray[0];
                }
                else
                {
                    var insertPlacingWay =
                        $"INSERT INTO {Program.Prefix}placing_way SET name= @name, conformity = @conformity";
                    var cmd6 = new MySqlCommand(insertPlacingWay, connect);
                    cmd6.Prepare();
                    var conformity = UtilsFromParsing.GetConformity(PlacingWay);
                    cmd6.Parameters.AddWithValue("@name", PlacingWay);
                    cmd6.Parameters.AddWithValue("@conformity", conformity);
                    cmd6.ExecuteNonQuery();
                    idPlacingWay = (int)cmd6.LastInsertedId;
                }
            }
            else
            {
                idPlacingWay = 0;
            }
        }
    }
}