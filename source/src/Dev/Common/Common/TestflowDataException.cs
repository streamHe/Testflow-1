﻿using System;

namespace Testflow.Common
{
    /// <summary>
    /// Testflow的数据异常
    /// </summary>
    [Serializable]
    public class TestflowDataException : TestflowException
    {
        /// <summary>
        /// 创建TestflowDataException的实例
        /// </summary>
        /// <param name="errorCode">异常码</param>
        /// <param name="message">错误信息</param>
        public TestflowDataException(int errorCode, string message) : base(errorCode, message)
        {
        }

        /// <summary>
        /// 创建TestflowDataException的实例
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <param name="message">异常信息</param>
        /// <param name="innerException">内部异常</param>
        public TestflowDataException(int errorCode, string message, Exception innerException) : base(errorCode, message, innerException)
        {
        }
    }
}