#if UNITY_EDITOR || USE_TRICK_MYSQL
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using TrickCore;

namespace TrickCore
{
    public class MySqlResultSingle<T> : MySqlResult<T>
    {
        public T Result { get; private set; }

        public MySqlResultSingle(IMySqlObject sql) : base(sql)
        {
        }

        public void SetResult(T result)
        {
            Result = result;
        }

        public override string ToString()
        {
            return Succeed ? $"{Result}" : $"{LastError}";
        }
    }

    public class MySqlResultMultiple<T> : MySqlResult<T>
    {
        public List<T> Result { get; private set; }

        public MySqlResultMultiple(IMySqlObject sql) : base(sql)
        {
        }

        public void SetResult(List<T> result)
        {
            Result = result;
        }

        public override string ToString()
        {
            return Succeed ? $"{typeof(T)}[{Result?.Count ?? 0}]" : $"{LastError}";
        }
    }

    public abstract class MySqlResult<T> : IMySqlResult
    {
        protected readonly IMySqlObject Sql;

        public MySqlResult(IMySqlObject sql)
        {
            Sql = sql;
        }

        public bool Completed { get; set; }
        public bool Succeed { get; set; }
        public Exception LastError { get; set; }
        public MySqlCommand LastCommand { get; set; }

        
        public void SetSucceed()
        {
            Completed = true;
            Succeed = true;
        }

        public void SetFail(MySqlCommand lastCommand, Exception lastError)
        {
            LastCommand = lastCommand;
            LastError = lastError;
            Completed = true;
            Succeed = false;

            if (Sql.Verbosity.HasFlag(SqlVerboseTypes.LogFailedQueries))
            {
                if (LastCommand != null) Logger.Sql.LogWarning("LogFailedQueries:" + LastCommand.CommandText);
                if (LastError != null) Logger.Sql.LogException(LastError);
            }
        }
    }
}
#endif