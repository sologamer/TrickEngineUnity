#if UNITY_EDITOR || USE_TRICK_MYSQL
using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using TrickCore;

namespace TrickCore
{
    public class MySqlTransactionHelper : IDisposable
    {
        private class QueryAction
        {
            public IMySqlResult Query;
            public Action Action;
            public bool TargetResult;

            public QueryAction(IMySqlResult query, Action action, bool targetResult)
            {
                Query = query;
                Action = action;
                TargetResult = targetResult;
            }
        }

        public MySqlTransactionHelper(MySqlTransaction transaction)
        {
            ActiveTransaction = transaction;
        }

        public MySqlTransaction ActiveTransaction { get; }
        public bool HasError { private get; set; }

        private readonly List<QueryAction> _queryActions = new List<QueryAction>();
 
        public MySqlConnection Connection => ActiveTransaction.Connection;

        public bool IsDisposed { get; private set; }

        public void Commit()
        {
            if (IsDisposed) return;

            try
            {
                ActiveTransaction?.Commit();
                ActiveTransaction?.Connection?.Close();
                foreach (QueryAction queryAction in _queryActions.Where(action => action.TargetResult))
                {
                    if (queryAction.Query == null || queryAction.Query.Succeed)
                    {
                        queryAction.Action?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Core.LogException(ex);
            }
            IsDisposed = true;
        }

        public void Rollback()
        {
            if (IsDisposed) return;

            try
            {
                ActiveTransaction?.Rollback();
                ActiveTransaction?.Connection?.Close();
                foreach (QueryAction queryAction in _queryActions.Where(action => !action.TargetResult))
                {
                    if (queryAction.Query == null || !queryAction.Query.Succeed)
                    {
                        queryAction.Action?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Core.LogException(ex);
            }
            IsDisposed = true;

        }

        public void Dispose()
        {
            if (IsDisposed) return;

            if (HasError)
            {
                Rollback();
            }
            else
            {
                Commit();
            }

            IsDisposed = true;
        }

        public void SucceedAction(IMySqlResult query, Action action)
        {
            _queryActions.Add(new QueryAction(query, action, true));
        }

        public void FailAction(IMySqlResult query, Action action)
        {
            _queryActions.Add(new QueryAction(query, action, false));
        }

        public void SucceedAction(Action action)
        {
            _queryActions.Add(new QueryAction(null, action, true));
        }

        public void FailAction(Action action)
        {
            _queryActions.Add(new QueryAction(null, action, false));
        }
    }
}
#endif