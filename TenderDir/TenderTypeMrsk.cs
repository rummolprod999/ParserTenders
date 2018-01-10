using System;
using System.Data;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;

namespace ParserTenders.TenderDir
{
    public class TenderTypeMrsk
    {
        private TypeMrsk _tend;
        private const int TypeFz = 9;

        public TenderTypeMrsk(TypeMrsk t)
        {
            _tend = t;
        }

        public void Parsing()
        {
            using (MySqlConnection connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                DateTime datePub = UtilsFromParsing.ParseDateMrsk(_tend.DatePub);
                DateTime dateUpd = UtilsFromParsing.ParseDateMrsk(_tend.DateUpd);
                if (dateUpd == DateTime.MinValue)
                {
                    dateUpd = datePub;
                }

                string selectTend =
                    $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND date_version = @date_version AND type_fz = @type_fz";
                MySqlCommand cmd = new MySqlCommand(selectTend, connect);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@purchase_number", _tend.IdTender);
                cmd.Parameters.AddWithValue("@date_version", dateUpd);
                cmd.Parameters.AddWithValue("@type_fz", TypeFz);
                DataTable dt = new DataTable();
                MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd};
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    Log.Logger("This tender is exist in base", _tend.IdTender);
                    return;
                }

                string s = DownloadString.DownL(_tend.Href);
                if (String.IsNullOrEmpty(s))
                {
                    Log.Logger("Empty string in Parsing()", _tend.Href);
                    return;
                }

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(s);
                int cancelStatus = 0;
                string selectDateT =
                    $"SELECT id_tender, date_version, cancel FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND type_fz = @type_fz";
                MySqlCommand cmd2 = new MySqlCommand(selectDateT, connect);
                cmd2.Prepare();
                cmd2.Parameters.AddWithValue("@purchase_number", _tend.IdTender);
                cmd2.Parameters.AddWithValue("@type_fz", TypeFz);
                MySqlDataAdapter adapter2 = new MySqlDataAdapter {SelectCommand = cmd2};
                DataTable dt2 = new DataTable();
                adapter2.Fill(dt2);
                //Console.WriteLine(dt2.Rows.Count);
                foreach (DataRow row in dt2.Rows)
                {
                    //DateTime dateNew = DateTime.Parse(pr.DatePublished);

                    if (dateUpd >= (DateTime) row["date_version"])
                    {
                        row["cancel"] = 1;
                        //row.AcceptChanges();
                        //row.SetModified();
                    }
                    else
                    {
                        cancelStatus = 1;
                    }
                }

                MySqlCommandBuilder commandBuilder =
                    new MySqlCommandBuilder(adapter2) {ConflictOption = ConflictOption.OverwriteChanges};
                //Console.WriteLine(commandBuilder.GetUpdateCommand().CommandText);
                adapter2.Update(dt2);
                string noticeVersion = "";
                string printForm = _tend.Href;
                int customerId = 0;
                int organiserId = 0;
                var navT = (HtmlNodeNavigator) htmlDoc.CreateNavigator();
            }
        }
    }
}