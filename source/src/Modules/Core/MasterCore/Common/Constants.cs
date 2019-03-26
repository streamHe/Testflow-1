﻿using System;

namespace Testflow.MasterCore.Common
{
    internal static class Constants
    {
        public const string LocalHostAddr = "127.0.0.1";
        public const int DefaultSequenceCapacaity = 10;
        public const int DefaultRuntimeSize = 10;
        public const string I18nName = "engineCore";
        public const int DefaultEventsQueueSize = 512;
        public const int MaxEventsQueueSize = 12800;

        public const int UnverifiedSequenceIndex = -1;

        public const int TestProjectSessionId = -1;
        public const int EventQueueTimeOut = 500;

        public const int OperationTimeout = 1000;

        #region Block事件状态值定义

        public const int RmtGenState = 0;

        #endregion


        #region 事件名称

        public const string TestGenerationStart = "TestGenerationStart";
        public const string TestGenerationReport = "TestGenerationReport";
        public const string TestGenerationEnd = "TestGenerationEnd";
        public const string SequenceStarted = "SequenceStarted";
        public const string StatusReceived = "StatusReceived";
        public const string SequenceOver = "SequenceOver";
        public const string TestOver = "TestOver";
        public const string BreakPointHitted = "BreakPointHitted";

        #endregion

    }
}
