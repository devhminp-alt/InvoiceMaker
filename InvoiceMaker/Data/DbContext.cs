using System;
using System.Data.SQLite;
using System.IO;

namespace InvoiceMaker.Data
{
    public static class DbContext
    {
        private static readonly string DbPath;
        private static readonly string ConnectionString;

        static DbContext()
        {
            // ✅ 1순위: 시스템 환경 변수
            DbPath = Environment.GetEnvironmentVariable("DBPath_Sqlite");

            // ✅ 안전장치 (없으면 바로 죽이자)
            if (string.IsNullOrWhiteSpace(DbPath))
            {
                throw new InvalidOperationException(
                    "시스템 변수 'DBPath_Sqlite'가 설정되어 있지 않습니다."
                );
            }

            if (!File.Exists(DbPath))
            {
                throw new FileNotFoundException(
                    $"SQLite DB 파일을 찾을 수 없습니다: {DbPath}"
                );
            }

            ConnectionString = $"Data Source={DbPath};Version=3;";
        }

        public static SQLiteConnection GetConnection()
        {
            var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            return conn;
        }
    }
}
