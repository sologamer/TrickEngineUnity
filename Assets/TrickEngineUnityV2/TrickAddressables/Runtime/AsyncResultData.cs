﻿namespace TrickCore
{
    public class AsyncResultData
    {
        public bool? Result;
        public object Data;

        public bool GetValueOrDefault()
        {
            return Result.GetValueOrDefault();
        }
    }
}