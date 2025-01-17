﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Testflow.Common;
using Testflow.CoreCommon.Common;
using Testflow.CoreCommon.Data.EventInfos;
using Testflow.CoreCommon.Messages;
using Testflow.Data.Sequence;
using Testflow.MasterCore.Common;
using Testflow.MasterCore.EventData;
using Testflow.Runtime;
using Testflow.Runtime.Data;
using PerformanceData = Testflow.CoreCommon.Data.PerformanceData;

namespace Testflow.MasterCore.StatusManage
{
    internal class SessionStateHandle
    {
        private EventDispatcher _eventDispatcher;
        private readonly StateManageContext _stateManageContext;
        private ISequenceFlowContainer _sequenceData; 
        private readonly Dictionary<int, SequenceStateHandle> _sequenceHandles;
        private long _heatBeatIndex = -1;

        public SessionStateHandle(ITestProject testProject, StateManageContext stateManageContext)
        {
            this._stateManageContext = stateManageContext;
            InitializeBasicInfo(CommonConst.TestGroupSession, testProject);

            this._sequenceHandles = new Dictionary<int, SequenceStateHandle>(Constants.DefaultRuntimeSize);
            _sequenceHandles.Add(CommonConst.SetupIndex, new SequenceStateHandle(Session,
                testProject.SetUp, _stateManageContext));
            _sequenceHandles.Add(CommonConst.TeardownIndex, new SequenceStateHandle(Session,
                testProject.TearDown, _stateManageContext));
        }

        public SessionStateHandle(int session, ISequenceGroup sequenceGroup, StateManageContext stateManageContext)
        {
            this._stateManageContext = stateManageContext;
            InitializeBasicInfo(session, sequenceGroup);

            // 初始化SequenceHandles
            this._sequenceHandles = new Dictionary<int, SequenceStateHandle>(Constants.DefaultRuntimeSize);
            _sequenceHandles.Add(CommonConst.SetupIndex, new SequenceStateHandle(Session, 
                sequenceGroup.SetUp, _stateManageContext));
            _sequenceHandles.Add(CommonConst.TeardownIndex, new SequenceStateHandle(Session, 
                sequenceGroup.TearDown, _stateManageContext));
            for (int i = 0; i < sequenceGroup.Sequences.Count; i++)
            {
                _sequenceHandles.Add(i, new SequenceStateHandle(Session, sequenceGroup.Sequences[i], _stateManageContext));
            }
        }

        private void InitializeBasicInfo(int session, ISequenceFlowContainer sequenceData)
        {
            // 配置基本信息
            this._sequenceData = sequenceData;
            this.Session = session;
            this.RuntimeHash = _stateManageContext.GlobalInfo.RuntimeHash;
            this.State = RuntimeState.NotAvailable;
            this._eventDispatcher = _stateManageContext.EventDispatcher;

            this.StartGenTime = DateTime.MaxValue;
            this.EndGenTime = DateTime.MaxValue;
            this.StartTime = DateTime.MaxValue;
            this.EndTime = DateTime.MaxValue;
            this.CurrentTime = DateTime.MaxValue;
            this.ElapsedTime = TimeSpan.Zero;

            // 获取测试结果对象和生成信息对象
            _testResults = _stateManageContext.GetSessionResults(Session);
            _generationInfo = _stateManageContext.GetGenerationInfo(Session);
            _sessionResults = new SessionResultData()
            {
                Name = SequenceData.Name,
                Description = SequenceData.Description,
                RuntimeHash = _stateManageContext.RuntimeHash,
                Session = this.Session,
                SequenceHash = (sequenceData is ISequenceGroup) ? ((ISequenceGroup)sequenceData).Info.Hash : string.Empty,
                State = RuntimeState.NotAvailable,
                FailedInfo = string.Empty
            };
            _performanceStatus = new PerformanceStatus()
            {
                RuntimeHash = _stateManageContext.RuntimeHash,
                Session = this.Session,
            };

            _stateManageContext.DatabaseProxy.WriteData(_sessionResults);
        }

        public int Session { get; private set; }

        public string RuntimeHash { get; private set; }

        public ISequenceFlowContainer SequenceData => _sequenceData;

        private int _state;
        private ITestResultCollection _testResults;
        private SessionResultData _sessionResults;
        private PerformanceStatus _performanceStatus;
        private ISessionGenerationInfo _generationInfo;

        public long HeartBeatIndex => Thread.VolatileRead(ref _heatBeatIndex);

        public RuntimeState State
        {
            get { return (RuntimeState) _state; }
            set
            {
                if (_state == (int) value)
                {
                    return;
                }
                Thread.VolatileWrite(ref _state, (int)value);
            }
        }

        public DateTime StartGenTime { get; set; }

        public DateTime EndGenTime { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public DateTime CurrentTime { get; set; }

        public TimeSpan ElapsedTime { get; set; }

        public PerformanceData Performance { get; set; }

        public void Start()
        {
            State = RuntimeState.Idle;
        }

        public SequenceStateHandle this[int sequenceIndex] => _sequenceHandles[sequenceIndex];

        public int SequenceCount => _sequenceHandles.Count;

        // 事件处理和消息处理需要完成：Handle状态更新、外部事件触发、数据库写入三个功能
        // SeesionHandle主要实现会话级别的管理(SequenceGroup)，例如TestGen、全局状态维护、会话时间统计、性能统计、全局结束和全局终止。
        // SequenceStateHandle实现序列级别的管理，例如当前栈维护、序列级别的时间统计、序列的时间维护。
        // 写入数据库的状态数据包含两部分，分别是以Sequence为单位和Session为单位执行统计
        // 该类的处理方法完成的工作有：
        // 更新SessionStateHandle的状态、生成PerformanceData并持久化、序列执行结束后生成SessionResultData并持久化、更新TestResult、整体执行结束后触发结束事件
        #region 消息处理和内部事件处理

        public void TestGenEventProcess(TestGenEventInfo eventInfo)
        {
            // TODO 暂时不更新所有Sequence的状态，按照SequenceGroup为单位进行报告
            ISessionGenerationInfo generationInfo;
            switch (eventInfo.GenState)
            {
                case TestGenState.StartGeneration:
                    this.State = RuntimeState.TestGen;
                    this.StartGenTime = eventInfo.TimeStamp;
                    RefreshTime(eventInfo);

                    SetGenerationInfo(eventInfo, GenerationStatus.InProgress);
                    break;
                case TestGenState.GenerationOver:
                    this.State = RuntimeState.StartIdle;
                    this.EndGenTime = eventInfo.TimeStamp;
                    RefreshTime(eventInfo);

                    SetGenerationInfo(eventInfo, GenerationStatus.Success);
                    break;
                case TestGenState.Error:
                    // 更新Handle状态
                    this.State = RuntimeState.Error;
                    RefreshTime(eventInfo);

                    // 停止所有Handle，写入错误数据
                    foreach (SequenceStateHandle sequenceStateHandle in _sequenceHandles.Values)
                    {
                        sequenceStateHandle.StopStateHandle(eventInfo.TimeStamp, State, eventInfo.ErrorInfo);
                    }
                    // 持久化会话失败信息
                    UpdateSessionResultData(eventInfo.ErrorInfo);
                    // 更新TestResults信息
                    SetTestResultStatistics(null);
                    // 触发生成失败的事件
                    SetGenerationInfo(eventInfo, GenerationStatus.Failed);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void AbortEventProcess(AbortEventInfo eventInfo)
        {
            foreach (SequenceStateHandle sequenceStateHandle in _sequenceHandles.Values)
            {
                sequenceStateHandle.AbortEventProcess(eventInfo);
            }
            if (!eventInfo.IsRequest)
            {
                this.State = RuntimeState.AbortRequested;
            }
            else
            {
                foreach (SequenceStateHandle sequenceStateHandle in _sequenceHandles.Values)
                {
                    sequenceStateHandle.AbortEventProcess(eventInfo);
                }
                this.State = RuntimeState.Abort;
                RefreshTime(eventInfo);

                SetTestResultStatistics(null);
                _stateManageContext.EventDispatcher.RaiseEvent(Constants.SessionOver, Session, _testResults);

                UpdateSessionResultData(string.Empty);
            }
        }

        public void DebugEventProcess(DebugEventInfo eventInfo)
        {
            _sequenceHandles[eventInfo.BreakPoint.Session].DebugEventProcess(eventInfo, this.SequenceData);
        }

        public void ExceptionEventProcess(ExceptionEventInfo eventInfo)
        {
        }

        public void SyncEventProcess(SyncEventInfo eventInfo)
        {
            // TODO
        }

        public bool HandleTestGenMessage(TestGenMessage message)
        {
            GenerationStatus generationState = message.State;
            _generationInfo.Status = generationState;
            foreach (int sequenceIndex in _generationInfo.SequenceStatus.Keys)
            {
                _generationInfo.SequenceStatus[sequenceIndex] = generationState;
            }

            FlipHeatBeatIndex();
            return true;
        }

        public bool HandleStatusMessage(StatusMessage message)
        {
            this.State = message.State;
            this.Performance = message.Performance;
            IRuntimeStatusInfo runtimeStatusInfo;
            switch (message.Name)
            {
                case MessageNames.StartStatusName:
                    this.StartTime = message.Time;
                    RefreshTime(message);

                    UpdateSessionResultData(string.Empty);

                    SetTestResultStatistics(message.WatchData);
                    _testResults.Performance = ModuleUtils.GetPerformanceResult(_stateManageContext.DatabaseProxy,
                        RuntimeHash, Session);
                    _stateManageContext.EventDispatcher.RaiseEvent(Constants.SessionStart,
                        Session, _testResults);
                    // 写入性能记录条目
                    WritePerformanceStatus();
                    for (int i = 0; i < message.Stacks.Count; i++)
                    {
                        if (message.SequenceStates[i] == RuntimeState.Running)
                        {
                            _sequenceHandles[message.Stacks[i].Session].HandleStatusMessage(message, i);
                        }
                    }
                    break;
                case MessageNames.ReportStatusName:
                    RefreshTime(message);

                    for (int i = 0; i < message.Stacks.Count; i++)
                    {
                        RuntimeState sequenceState = message.SequenceStates[i];
                        if (sequenceState == RuntimeState.Running || RuntimeState.Blocked == sequenceState ||
                            RuntimeState.DebugBlocked == sequenceState)
                        {
                            _sequenceHandles[message.Stacks[i].Session].HandleStatusMessage(message, i);
                        }
                    }

                    runtimeStatusInfo = CreateRuntimeStatusInfo(message);
                    _stateManageContext.EventDispatcher.RaiseEvent(Constants.StatusReceived,
                        Session, runtimeStatusInfo);

                    // 写入性能记录条目
                    WritePerformanceStatus();
                    break;
                case MessageNames.ResultStatusName:
                    this.EndTime = message.Time;
                    RefreshTime(message);

                    SetTestResultStatistics(message.WatchData);
                    _testResults.Performance = ModuleUtils.GetPerformanceResult(_stateManageContext.DatabaseProxy,
                        RuntimeHash, Session);
                    _stateManageContext.EventDispatcher.RaiseEvent(Constants.SessionOver,
                        Session, _testResults);

                    UpdateSessionResultData(string.Empty);
                    // 写入性能记录条目
                    WritePerformanceStatus();
                    break;
                case MessageNames.ErrorStatusName:
                    this.EndTime = message.Time;
                    RefreshTime(message);

                    foreach (SequenceStateHandle sequenceStateHandle in _sequenceHandles.Values)
                    {
                        RuntimeState runtimeState = sequenceStateHandle.State;
                        if (runtimeState == RuntimeState.Running || runtimeState == RuntimeState.Blocked ||
                            runtimeState == RuntimeState.DebugBlocked)
                        {
                            sequenceStateHandle.HandleStatusMessage(message, CommonConst.BroadcastSession);
                        }
                    }

                    UpdateSessionResultData(message.ExceptionInfo.ToString());

                    SetTestResultStatistics(message.WatchData);
                    _testResults.Performance = ModuleUtils.GetPerformanceResult(_stateManageContext.DatabaseProxy,
                        RuntimeHash, Session);
                    _stateManageContext.EventDispatcher.RaiseEvent(Constants.SessionOver,
                        Session, _testResults);
                    // 写入性能记录条目
                    WritePerformanceStatus();
                    break;
                case MessageNames.HearBeatStatusName:
                    RefreshTime(message);

                    for (int i = 0; i < message.Stacks.Count; i++)
                    {
                        if (message.SequenceStates[i] == RuntimeState.Running)
                        {
                            _sequenceHandles[message.Stacks[i].Session].HandleStatusMessage(message, i);
                        }
                    }

                    runtimeStatusInfo = CreateRuntimeStatusInfo(message);
                    _stateManageContext.EventDispatcher.RaiseEvent(Constants.StatusReceived,
                        Session, runtimeStatusInfo);
                    break;
                default:
                    throw new InvalidProgramException();
                    break;
            }

            FlipHeatBeatIndex();
            return true;
        }

        #endregion

        private void RefreshTime(MessageBase message)
        {
            this.CurrentTime = message.Time;
            if (this.StartTime != DateTime.MaxValue)
            {
                this.ElapsedTime = message.Time - this.StartTime;
            }
            if (this.State > RuntimeState.AbortRequested)
            {
                this.EndTime = message.Time;
            }
        }

        private void RefreshTime(EventInfoBase eventInfo)
        {
            this.CurrentTime = eventInfo.TimeStamp;
            if (this.StartTime != DateTime.MaxValue)
            {
                this.ElapsedTime = eventInfo.TimeStamp - this.StartTime;
            }
            if (this.State > RuntimeState.AbortRequested)
            {
                this.EndTime = eventInfo.TimeStamp;
            }
        }



        #region 更新各个数据结构的值

        private ISessionGenerationInfo SetGenerationInfo(TestGenEventInfo eventInfo, GenerationStatus status)
        {
            _generationInfo.Status = status;
            foreach (int sequenceIndex in _generationInfo.SequenceStatus.Keys)
            {
                _generationInfo.SequenceStatus[sequenceIndex] = status;
            }
            return _generationInfo;
        }

        private void UpdateSessionResultData(string failedInfo)
        {
            _sessionResults.StartTime = StartTime;
            _sessionResults.EndTime = EndTime;
            _sessionResults.ElapsedTime = ElapsedTime.TotalMilliseconds;
            _sessionResults.State = State;
            if (State == RuntimeState.Error)
            {
                _sessionResults.FailedInfo = failedInfo;
            }
            _stateManageContext.DatabaseProxy.UpdateData(_sessionResults);
        }

        private void WritePerformanceStatus()
        {
            if (null == this.Performance)
            {
                return;
            }
            this._performanceStatus.Index = _stateManageContext.PerfStatusIndex;
            this._performanceStatus.TimeStamp = this.CurrentTime;
            this._performanceStatus.MemoryAllocated = this.Performance.MemoryAllocated;
            this._performanceStatus.MemoryUsed = this.Performance.MemoryUsed;
            this._performanceStatus.ProcessorTime = this.Performance.ProcessorTime;

            this._stateManageContext.DatabaseProxy.WriteData(this._performanceStatus);
        }

        private void SetTestResultStatistics(IDictionary<string, string> watchData)
        {
            _testResults.WatchData.Clear();
            _testResults.SetUpSuccess = _testResults[CommonConst.SetupIndex].ResultState == RuntimeState.Success;
            _testResults.SuccessCount = (from result in _testResults.Values where (result.SequenceIndex >= 0 && result.ResultState == RuntimeState.Success) select result).Count();
            _testResults.FailedCount = (from result in _testResults.Values where (result.SequenceIndex >= 0 && result.ResultState == RuntimeState.Failed || result.ResultState == RuntimeState.Error) select result).Count();
            _testResults.TimeOutCount = (from result in _testResults.Values where (result.SequenceIndex >= 0 && result.ResultState == RuntimeState.Timeout) select result).Count();
            _testResults.TearDownSuccess = _testResults[CommonConst.TeardownIndex].ResultState == RuntimeState.Success;
            _testResults.AbortCount = (from result in _testResults.Values where (result.SequenceIndex >= 0 && result.ResultState == RuntimeState.Abort) select result).Count();
            _testResults.TestOver = _testResults.Values.All(item => item.ResultState > RuntimeState.AbortRequested);
            if (null != watchData)
            {
                Regex varNameRegex = new Regex(CoreUtils.GetVariableNameRegex(_sequenceData, Session));
                foreach (KeyValuePair<string, string> varToValue in watchData)
                {
                    if (varNameRegex.IsMatch(varToValue.Key))
                    {
                        IVariable variable = CoreUtils.GetVariable(_sequenceData, varToValue.Key);
                        _testResults.WatchData.Add(variable, varToValue.Value);
                    }
                }
            }
        }

        private IRuntimeStatusInfo CreateRuntimeStatusInfo(StatusMessage message)
        {
            Dictionary<string, string> watchData = message.WatchData;
            Dictionary<IVariable, string> varValues;
            if (null != watchData)
            {
                varValues = new Dictionary<IVariable, string>(watchData.Count);
                Regex varNameRegex = new Regex(CoreUtils.GetVariableNameRegex(_sequenceData, Session));
                foreach (KeyValuePair<string, string> varToValue in watchData)
                {
                    if (varNameRegex.IsMatch(varToValue.Key))
                    {
                        IVariable variable = CoreUtils.GetVariable(_sequenceData, varToValue.Key);
                        varValues.Add(variable, varToValue.Value);
                    }
                }
            }
            else
            {
                varValues = new Dictionary<IVariable, string>(1);
            }
            ulong dataStatusIndex = (ulong) _stateManageContext.DataStatusIndex;
            return new RuntimeStatusInfo(this, dataStatusIndex, null, varValues);
        }

        #endregion

        private void FlipHeatBeatIndex()
        {
            Interlocked.Increment(ref _heatBeatIndex);
        }

//        private void SetErrorTestResults()

//        {

//            _testResults.SetUpSuccess = false;

//            _testResults.SuccessCount = 0;

//            _testResults.FailedCount = _testResults.Count;

//            _testResults.TimeOutCount = 0;

//            _testResults.TearDownSuccess = false;

//            _testResults.TestOver = true;

//            _testResults.AbortCount = 0;

//        }
    }
}