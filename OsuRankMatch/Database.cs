using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace RegistrationWebsite.Helper
{
    public class Database
    {
        const string sqlconnectstr = "";
        internal static MySqlConnection GetConnection()
            {
                return new MySqlConnection(sqlconnectstr);
            }


            internal static MySqlDataReader RunQuery(string sqlString, params MySqlParameter[] parameters)
            {
                // using (var m = GetConnection())
                //{
                var m = GetConnection();
                m.Open();
                MySqlCommand c = m.CreateCommand();
                if (parameters != null)
                    c.Parameters.AddRange(parameters);
                c.CommandText = sqlString;
                c.CommandTimeout = 5;
                var reader = c.ExecuteReader(CommandBehavior.CloseConnection);
                return reader;
                // }
            }



            public static object RunQueryOne(string sqlString, params MySqlParameter[] parameters)
            {

                using (MySqlConnection m = GetConnection())
                {
                    m.Open();
                    using (MySqlCommand c = m.CreateCommand())
                    {
                        c.Parameters.AddRange(parameters);
                        c.CommandText = sqlString;
                        c.CommandTimeout = 5;
                        
                        return c.ExecuteScalar();
                    }
                    
                }
            }

            internal static int Exec(string sqlString, params MySqlParameter[] parameters)
            {

                using (MySqlConnection m = GetConnection())
                {
                    m.Open();
                    using (MySqlCommand c = m.CreateCommand())
                    {
                        c.Parameters.AddRange(parameters);
                        c.CommandText = sqlString;
                        c.CommandTimeout = 5;
                        return c.ExecuteNonQuery();
                    }
                }
            }

            internal static DataSet RunDataset(string sqlString, params MySqlParameter[] parameters)
            {

                using (MySqlConnection m = GetConnection())
                {
                    m.Open();
                    return MySqlHelper.ExecuteDataset(m, sqlString, parameters);
                }
            }
    }
}
