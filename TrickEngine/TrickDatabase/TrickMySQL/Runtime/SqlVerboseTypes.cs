#if UNITY_EDITOR || USE_TRICK_MYSQL
using System;

namespace TrickCore
{
    [Flags]
    public enum SqlVerboseTypes
    {
        /// <summary>
        /// Nothing will be logged
        /// </summary>
        Nothing = 1 << 0,

        /// <summary>
        /// Logs errors
        /// </summary>
        LogErrors = 1 << 1,

        /// <summary>
        /// Log all queries
        /// </summary>
        LogQueries = 1 << 2,

        /// <summary>
        /// Log all failed queries
        /// </summary>
        LogFailedQueries = 1 << 3,
    }
}
#endif