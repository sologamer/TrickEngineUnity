#if UNITY_EDITOR || USE_TRICK_MYSQL
using System;
using MySql.Data.MySqlClient;
using TrickCore;

namespace TrickCore
{
    public class MySqlQueryResult : IMySqlResult
    {
        private readonly IMySqlObject _sql;

        public MySqlQueryResult(IMySqlObject sql)
        {
            _sql = sql;
        }

        public bool Completed { get; set; }
        public bool Succeed { get; set; }
        public Exception LastError { get; set; }
        public MySqlCommand LastCommand { get; set; }

        public void SetSucceed()
        {
            Succeed = true;
            Completed = true;
        }

        public void SetFail(MySqlCommand lastCommand, Exception lastError)
        {
            Succeed = false;
            Completed = true;
            LastCommand = lastCommand;
            LastError = lastError;

            if (_sql.Verbosity.HasFlag(SqlVerboseTypes.LogFailedQueries))
            {
                if (LastCommand != null) Logger.Sql.LogWarning("LogFailedQueries:" + LastCommand.CommandText);
                if (LastError != null) Logger.Sql.LogException(LastError);
            }
        }
        
        public static implicit operator bool(MySqlQueryResult inResult)
        {
            return inResult.Completed;
        }
    }
}
#endif