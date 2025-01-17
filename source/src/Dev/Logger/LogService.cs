﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using log4net;
using Testflow.Common;
using Testflow.Log;
using Testflow.Modules;
using Testflow.Runtime;
using Testflow.Utility.I18nUtil;
using Testflow.Utility.MessageUtil;

namespace Testflow.Logger
{
    /// <summary>
    /// 日志服务
    /// </summary>
    public class LogService : ILogService
    {
        private readonly Dictionary<int, LocalLogSession> _runtimeLogSessions;
        private PlatformLogSession _platformLogSession;
        private readonly I18N _i18N;
        private Messenger _messenger;
        private readonly TestflowContext _context;
        private readonly TestflowRunner _testflowInst;

        private static LogService _inst = null;
        private static readonly object _instLock = new object();

        /// <summary>
        /// 创建日志服务实例
        /// </summary>
        public LogService()
        {
            if (null != _inst)
            {
                I18N i18N = I18N.GetInstance(Constants.I18NName);
                throw new TestflowRuntimeException(CommonErrorCode.InternalError, i18N.GetStr("InstAlreadyExist"));
            }
            lock (_instLock)
            {
                Thread.MemoryBarrier();
                if (null != _inst)
                {
                    I18N i18N = I18N.GetInstance(Constants.I18NName);
                    throw new TestflowRuntimeException(CommonErrorCode.InternalError, i18N.GetStr("InstAlreadyExist"));
                }
                _runtimeLogSessions = new Dictionary<int, LocalLogSession>(Constants.DefaultLogStreamSize);
                I18NOption i18NOption = new I18NOption(this.GetType().Assembly, "i18n_logger_zh", "i18n_logger_en")
                {
                    Name = Constants.I18NName
                };
                _i18N = I18N.GetInstance(i18NOption);
                _testflowInst = TestflowRunner.GetInstance();
                _context = _testflowInst.Context;
                _inst = this;
            }
        }

        IModuleConfigData IController.ConfigData { get; set; }

        void IController.RuntimeInitialize()
        {
            this._platformLogSession = new PlatformLogSession(-1);
            MessengerOption option = new MessengerOption(Constants.LogQueueName, typeof(LogMessage));
            _messenger = Messenger.GetMessenger(option);
            InitializeRuntimeSession();
            _messenger.RegisterConsumer(_runtimeLogSessions.Values.ToArray());
            foreach (LocalLogSession logSession in _runtimeLogSessions.Values)
            {
                logSession.Dispose();
            }
            _runtimeLogSessions.Clear();
        }

        private void InitializeRuntimeSession()
        {
            _runtimeLogSessions.Clear();
            foreach (IRuntimeSession runtimeSession in _testflowInst.RuntimeService.Sessions)
            {
                _runtimeLogSessions.Add(runtimeSession.ID, new LocalLogSession(runtimeSession.ID));
            }
        }

        void IController.DesigntimeInitialize()
        {
            _platformLogSession = new PlatformLogSession(CommonConst.PlatformLogSession);
        }

        void IController.ApplyConfig(IModuleConfigData configData)
        {
            // TODO to implement
        }

        public LogLevel LogLevel { get; set; }

        void ILogService.Print(LogLevel logLevel, int sequenceIndex, string message)
        {
            _platformLogSession.Print(logLevel, sequenceIndex, message);
        }

        void ILogService.Print(LogLevel logLevel, int sequenceIndex, Exception exception, string message)
        {
            _platformLogSession.Print(logLevel, sequenceIndex, exception, message);
        }

        LogLevel ILogService.RuntimeLogLevel { get; set; }

        ILogSession ILogService.GetLogSession(int sessionId)
        {
            return _runtimeLogSessions.ContainsKey(sessionId) ?_runtimeLogSessions[sessionId] : null;
        }

        public void Print(LogLevel logLevel, int sessionId, int sequenceIndex, string message)
        {
            _platformLogSession.Print(logLevel, Constants.DesigntimeSessionId, message);
        }

        public void Print(LogLevel logLevel, int sessionId, int sequenceIndex, Exception exception, string message = "")
        {
            _platformLogSession.Print(logLevel, Constants.DesigntimeSessionId, exception, message);
        }

        public void DestroyLogStream(int sessionId)
        {
            if (_runtimeLogSessions.ContainsKey(sessionId))
            {
                _runtimeLogSessions[sessionId].Dispose();
                _runtimeLogSessions.Remove(sessionId);
            }
        }

        public void DestroyLogStream()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 停止运行时日志
        /// </summary>
        public void StopRuntimeLogging()
        {
            _messenger.Dispose();
            _messenger = null;
        }

        void IDisposable.Dispose()
        {
            _platformLogSession?.Dispose();
            foreach (LocalLogSession logSession in _runtimeLogSessions.Values)
            {
                logSession.Dispose();
            }
            _runtimeLogSessions.Clear();
            _messenger.Clear();
            Messenger.DestroyMessenger(Constants.LogQueueName);
            LogManager.Shutdown();
        }
    }
}