using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace SOMIOD.App.Helpers
{
    public class SqlDataHelper
    {
        // Vai buscar a Connection String que definimos no Web.config
        private static string connectionString = ConfigurationManager.ConnectionStrings["SomiodConnStr"].ConnectionString;

        // Método para Ler dados (SELECT)
        public static DataTable ExecuteQuery(string query, List<SqlParameter> parameters = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                    }

                    DataTable dt = new DataTable();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);

                    conn.Open();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        // Método para Gravar/Apagar dados (INSERT, UPDATE, DELETE)
        public static int ExecuteNonQuery(string query, List<SqlParameter> parameters = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                    }

                    conn.Open();
                    return cmd.ExecuteNonQuery(); // Devolve o número de linhas afetadas
                }
            }
        }
    }
}