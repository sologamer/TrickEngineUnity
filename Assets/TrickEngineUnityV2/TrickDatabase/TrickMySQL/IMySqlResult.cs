#if UNITY_EDITOR || USE_TRICK_MYSQL
using System;
using MySql.Data.MySqlClient;

namespace TrickCore
{
    public interface IMySqlResult
    {
        bool Completed { get; set; }
        bool Succeed { get; set; }
        Exception LastError { get; set; }
        MySqlCommand LastCommand { get; set; }

        void SetSucceed();
        void SetFail(MySqlCommand lastCommand, Exception lastError);
    }
}
#endif