using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DataAccess;

namespace MedicalRecordsBusiness
{
    public class mrBO
    {
        private string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=diabetis;Trusted_Connection=True;"; //get from configuration
        private DataAccess.DataAccess da = new DataAccess.DataAccess();
        private string cs = @"Server=(localdb)\MSSQLLocalDB;Database=Expenses;Trusted_Connection=True;";

        Dictionary<string, List<string>> recs = new Dictionary<string, List<string>>();
            

        private DateTime getDate(string fecha, string hora)
        {
            DateTime dt = new DateTime();
            try
            {
                var dp = fecha.Split('-');
                var tp = hora.Split(':');
                var y = Convert.ToInt32(dp[0]);
                var m = Convert.ToInt32(dp[1]);
                var d = Convert.ToInt32(dp[2]);
                var h = Convert.ToInt32(tp[0]);
                var mi = Convert.ToInt32(tp[1]);
                dt = new DateTime(y, m, d, h, m,0);
            }
            catch(Exception ex)
            {
                dt = new DateTime(1900, 1, 1);
            }
            return dt;
        }

        private DateTime getDate2(string fecha, string hora)
        {
            DateTime dt = new DateTime();
            try
            {
                var tp = hora.Split(':');
                var y = Convert.ToInt32(fecha.Substring(0, 4));
                var m = Convert.ToInt32(fecha.Substring(4, 2));
                var d = Convert.ToInt32(fecha.Substring(6,2));
                var h = Convert.ToInt32(tp[0]);
                var mi = Convert.ToInt32(tp[1]);
                dt = new DateTime(y, m, d, h, m, 0);
            }
            catch (Exception ex)
            {
                dt = new DateTime(1900, 1, 1);
            }
            return dt;
        }



        public void LoadData(string[] lines )
        {
            var sql = "";
            foreach (var l in lines)
            {
                var cols = l.Split('\t');
                if (!string.IsNullOrEmpty(cols[0]))
                {
                    if (cols.Length > 3)
                    {
                        try
                        {
                            //INSERT INTO RAWDATA VALUES(1 pan con queso crema,'','','1/1/1900 12:00:00 AM','','','','','','','')
                            var dt = getDate(cols[2], cols[3]);
                            sql = $"INSERT INTO RAWDATA VALUES({cols[0]},'{cols[2]}','{cols[3]}','{dt}','{cols[1]}',";
                            for (int i = 4; i < 10; i++)
                            {
                                //if ( !string.IsNullOrEmpty(cols[i]))
                                sql += $"'{cols[i]}',";
                            }
                            if (sql.EndsWith(","))
                                sql = sql.Substring(0, sql.Length - 1);
                            sql += ")";
                            try
                            {
                                da.ExecuteNonQuery(connectionString, sql, "sqltext");
                            }
                            catch (Exception ex)
                            {
                                string s = ex.Message;
                            }

                        }
                        catch (Exception ex)
                        {
                            string s = "";
                        }
                    }
                }


            }

        }

        public void CleanMiDinero(string[] files)
        {
            foreach(string f in files)
            {
                if ( File.Exists(f))
                {
                    string s = File.ReadAllText(f);
                    s = s.Replace("\r\nRecarga", "Recarga");
                    s = s.Replace("\r\n", "");
                    s = s.Replace("", "");
                    s = s.Replace("\r\n\r\n", "\r\n");
                    s = s.Replace("\r\n-", " -");
                   s = s.Replace("Recarga -", "Recarga ");
                    s = s.Replace("Recarga", "Recarga ");
                    s = s.Replace("Recarga  ", "Recarga ");
                    s = s.Replace("MONEDA \r\n", "MONEDA ");
                    s = s.Replace("MONEDA", "MONEDA ");
                    s = s.Replace("MONEDA  ", "MONEDA ");
                    s = s.Replace("/2019 \r\n", "/2019 ");
                    s = s.Replace("/2018 \r\n", "/2018 ");
                    s = s.Replace("/2017 \r\n", "/2017 ");
                    //s = s.Replace(".", "");
                    //s = s.Replace(",", ".");
                    s = s.Replace("'", "");
                    File.WriteAllText(f, s);
                }
            }
        }
        public void cleanAnda(string[] files)
        {
            foreach(string f in files)
            {
                if ( File.Exists(f))
                {
                    string s = File.ReadAllText(f);
                    s = s.Replace("TOTAL DE CARGOS DEL MES	\r\n", "TOTAL DE CARGOS DEL MES	");
                    s = s.Replace("SALDO A FIN DE ESTE PERIODO	\r\n", "SALDO A FIN DE ESTE PERIODO	");
                    s = s.Replace("|", "\r\n");
                    File.WriteAllText(f, s);
                }
            }
        }

        public void Anda(string[] files, bool clean = true)
        {
            if (clean) cleanAnda(files);
            string defaultDate = "";
            string date = "";
            string sql = "";
            string valor = "";
            int index = 0;
            string detalle = "";
            string referencia = "";
            foreach (string f in files)
            {
                if (File.Exists(f))
                {
                    defaultDate = Path.GetFileNameWithoutExtension(f);
                    defaultDate = defaultDate.Substring(0, 4) + "/" + defaultDate.Substring(4, 2) + "/01";
                    string[] lines = File.ReadAllLines(f);
                    foreach(string l in lines)
                    {
                        if (l.Contains("--------")||
                            l.Contains(".                PARA SU INFORMACION")||
                            l.Contains("INTERESES GENERADOS EN EL MES	")||
                            l.Contains("OTRA INFORMACION DE INTERES"))
                                continue;

                        string[] cols = l.Trim().Split('\t');
                        valor = cols[cols.Length - 1].Trim();
                        index = valor.LastIndexOf(" ");
                        if (index > 0)
                        {
                            detalle = valor.Substring(0, index).Trim();
                            valor = valor.Substring(index + 1).Trim().Replace(".", "").Replace(",", ".");
                            decimal vr = 0;
                            try
                            {
                                vr = Convert.ToDecimal(valor);
                            }
                            catch(Exception ex )
                            {
                                vr = 0;
                            }
                            if (vr > 0)
                            {

                                if (l.Contains("-  CARGO") || 
                                    l.Contains("-  IVA CARGO*") ||
                                    cols.Length == 1)
                                {
                                    date = defaultDate;
                                    referencia = "";
                                }
                                else
                                {
                                    string[] dt = cols[0].Split('.');
                                    if (dt.Length > 2)
                                    {
                                        date = $"{dt[2]}/{dt[1]}/{dt[0]}";
                                    }
                                    else
                                        date = defaultDate;
                                    if (cols.Length > 1)
                                        referencia = cols[1];
                                    else referencia = "";
                                }
                                sql = $"'ANDA','{date}','{referencia}','{detalle}',{valor},0";
                                sql = $"insert into registrostarjetas(cardholder, fecha, referencia,detalle, debito, moneda)values({sql})";
                                try
                                {
                                    da.ExecuteNonQuery(cs, sql, "sqltext");
                                }
                                catch (Exception ex)
                                {
                                    string s = ex.Message;
                                }
                            }
                        }

                    }
                }
            }
        }

        public void MiDinero(string card, string[] files, bool clean = true  )
        {
            if ( clean ) CleanMiDinero(files);
            int start = 0;
            int startcol = 0;
            string sql = "";

            string defaultDate = "";
            foreach(string f in files )
            {
                if ( File.Exists(f))
                {
                    defaultDate =  Path.GetFileNameWithoutExtension(f);
                    defaultDate = defaultDate.Substring(0, 4) + "/" + defaultDate.Substring(4, 2) + "/01";
                    string date = "";
                    string[] lines = File.ReadAllLines(f);
                    for(int i=0; i< lines.Length; i++)
                    {
                        if ( lines[i].Contains("Saldo Anterior"))
                        {
                            start = i+1;
                            break;
                        }
                        if (lines[i].Contains("Saldo Actual")) break;
                    }

                    for( int  i = start; i<lines.Length; i++)
                    {
                        string l = lines[i].Trim();
                        if (l.Contains("Saldo Anterior"))
                            continue;
                        string[] cols = l.Split(' ');
                        string[] dd = cols[0].Split('/');
                        if ( dd.Length == 3 )
                        {
                            date = $"{dd[2]}/{dd[1]}/{dd[0]}";
                            startcol = 1;
                        }
                        else
                        {
                            date = defaultDate;
                            startcol = 0;
                        }

                        sql = "";
                        for(int j=startcol; j<cols.Length-1; j++)
                        {
                            sql += cols[j] + " ";
                        }
                        string referencia = "";
                        int index = sql.IndexOf(" ");
                        if (index > 0)
                        {
                            referencia = sql.Substring(0, index).Trim();
                            int iref = 0;
                            int.TryParse(referencia, out iref);

                            if ( iref > 0 )
                            {
                                sql = sql.Substring(index + 1);
                            }
                            else
                            {
                                referencia = "";
                            }

                            sql = $"'{sql.Trim()}',";
                            string last = cols[cols.Length - 1].Trim();
                            if ( last.Contains(",") || last.Contains("."))
                            {
                                if ( last[last.Length - 3] == ',')
                                {
                                    last = last.Replace(".", "");
                                    last = last.Replace(",", ".");
                                }
                                else if (last[last.Length - 3] == '.')
                                {
                                    last = last.Substring(0, last.Length - 3).Replace(".","") +
                                        "." + last.Substring(last.Length-2);
                                }
                            }
                            if (last.Contains("-"))
                            {
                                last = last.Replace("-", "");
                                sql = $"{sql}{last},0,0";
                            }
                            else
                            {
                                sql = $"{sql}0,{last},0";
                            }
                            sql = $"insert into registrostarjetas(cardholder, fecha, referencia,detalle, debito, credito, moneda)values('{card}', '{date}', '{referencia}',{sql})";
                            try
                            {
                                da.ExecuteNonQuery(cs, sql, "sqltext");
                            }
                            catch (Exception ex)
                            {
                                string s = ex.Message;
                                break;
                            }
                        }


                    }

                }
            }

        }

        //Fecha		Descripción	Número Documento	Num. Dep.	Asunto		Débito	Crédito	
        /*
         id	auto,cardholder cesar,Fecha	fecha	,Descripción	detalle,Número Documento	referencia
        Num. Dep.	aux1,Asunto		 aux2,Débito,	debito,Crédito	credito,--moneda 0 
         
         */
        public void loadBrou(string userId, string[] files )
        {
            string sql = "";
            foreach (string f in files )
            {
                if( File.Exists(f))
                {
                    string[] lines = File.ReadAllLines(f);
                    foreach(string l in lines)
                    {
                        string[] cols = l.Split('\t');
                        string[] fe = cols[0].Split('/');
                        DateTime dt = new DateTime(Convert.ToInt32(fe[2]),
                            Convert.ToInt32(fe[1]), Convert.ToInt32(fe[0]));

                        if (string.IsNullOrEmpty(cols[7])) cols[7] = "0.0";
                        if (string.IsNullOrEmpty(cols[8])) cols[8] = "0.0";
                        cols[2] = cols[2].Replace("'", "");
                        cols[4] = cols[4].Replace("'", "");


                        sql = $"INSERT INTO REGISTROSTARJETAS VALUES('{userId}','{fe[2]}/{fe[1]}/{fe[0]}','{cols[3]}','{cols[2]}','{cols[4]}','{cols[5]}',{cols[7].Replace(",","")},{cols[8].Replace(",","")},0)";
                        da.ExecuteNonQuery(cs, sql, "sqltext");

                    }
                }
            }
        }


        public void Prex(string[] files )
        {
            string fecha = "";
            string detalle = "";
            string valor = "";
            int moneda = 0;
            string debito = "";
            string credito = "";
            string sql = "";
            foreach(string f in files)
            {
                if ( File.Exists(f))
                {
                    string[] lines = File.ReadAllLines(f);
                    foreach (string l in lines)
                    {
                        if (l.Contains("Movimientos en ") ||
                            l.Contains("Fecha	Descripción	Importe	Estado	 "))
                            continue;

                        string[] cols = l.Split('\t');
                        if (cols.Length > 1)
                        {
                            string[] dt = cols[0].Split('/');
                            if (dt.Length > 2)
                            {
                                fecha = $"{dt[2]}/{dt[1]}/{dt[0]}";
                            }
                            else fecha = "2017/01/01";
                            detalle = cols[1].Replace("'","");
                            valor = cols[2];
                            valor = valor.Replace(".", "").Replace(",",".");
                            if (valor.Contains("U$S"))
                            {
                                moneda = 1;
                                valor = valor.Replace("U$S", "").Trim();
                            }
                            else
                            {
                                valor = valor.Replace("$", "").Trim();
                                moneda = 0;
                            }
                            valor = valor.Replace(",", ".");
                            if (valor.Contains("-"))
                            {
                                valor = valor.Replace("-", "");
                                debito = valor;
                                credito = "0";
                            }
                            else
                            {
                                debito = "0";
                                credito = valor;
                            }
                            sql = $"'PREX','{fecha}','{detalle}',{debito},{credito},{moneda}";
                            sql = $"insert into registrostarjetas(cardholder, fecha, detalle, debito, credito, moneda)values({sql})";
                            try
                            {
                                da.ExecuteNonQuery(cs, sql, "sqltext");
                            }
                            catch (Exception ex)
                            {
                                string s = ex.Message;
                            }
                        }
                    }
                }
            }
        }

        public void loadData(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);
            LoadData(lines);
        }

        public void loadDataAugust(string fileName)
        {
            //20190806	15:37	146	75	72	walk 	0:15	
            DateTime dt = new DateTime();
            string[] lines = File.ReadAllLines(fileName);
            string sql =  "";
            foreach (string l in lines)
            {
                string[] cols = l.Split('\t');
                dt = getDate2(cols[0], cols[1]);



                sql = $"INSERT INTO RAWDATA VALUES(3000,{cols[0]},'{cols[1]}','{dt}','PRESURE',";

                for (int i = 2; i < 6; i++)
                {
                    //if ( !string.IsNullOrEmpty(cols[i]))
                    sql += $"'{cols[i]}',";
                }
                sql += "NULL,NULL)";
                try
                {
                    da.ExecuteNonQuery(connectionString, sql, "sqltext");
                    if (cols[5].ToLower().Contains("swim"))
                    {
                        sql = $"INSERT INTO RAWDATA VALUES(3000,{cols[0]},'{cols[1]}','{dt}','EXE',NULL,NULL,NULL,NULL,NULL,NULL)";
                        
                        da.ExecuteNonQuery(connectionString, sql, "sqltext");
                    }

                }
                catch (Exception ex)
                {
                    string s = ex.Message;
                }


            }


        }
    }
}
