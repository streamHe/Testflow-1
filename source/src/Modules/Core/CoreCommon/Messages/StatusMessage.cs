﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using Testflow.CoreCommon.Common;
using Testflow.CoreCommon.Data;
using Testflow.Runtime;
using Testflow.Runtime.Data;

namespace Testflow.CoreCommon.Messages
{
    /// <summary>
    /// 状态报告消息
    /// </summary>
    public class StatusMessage : MessageBase
    {
        public List<CallStack> Stacks { get; set; }

        public RuntimeState State { get; set; }

        public List<RuntimeState> SequenceStates { get; set; }

        public List<StepResult> Results { get; set; }

        public PerformanceData Performance { get; set; }

        public Dictionary<int, string> FailedInfo { get; set; }

        public List<int> InterestedSequence { get; set; }

        public Dictionary<string, string> WatchData { get; }

        public SequenceFailedInfo ExceptionInfo { get; set; }

        public StatusMessage(string name, RuntimeState state, int id) : base(name, id, MessageType.Status)
        {
            this.State = state;
            this.WatchData = new Dictionary<string, string>(CoreConstants.DefaultRuntimeSize);
            this.InterestedSequence = new List<int>(CoreConstants.DefaultRuntimeSize);
        }

        public StatusMessage(SerializationInfo info, StreamingContext context) : base(info, context)
        {
//            this.Stack = info.GetValue("Stack", typeof(CallStack)) as CallStack;
//            this.State = (RuntimeState)info.GetValue("State", typeof(RuntimeState));
//            this.Performance = info.GetValue("Performance", typeof(PerformanceData)) as PerformanceData;
//            this.WatchData =
//                info.GetValue("WatchData", typeof(Dictionary<string, string>)) as Dictionary<string, string>;
            CoreUtils.SetMessageValue(info, this, this.GetType());
            if (null == FailedInfo)
            {
                FailedInfo = new Dictionary<int, string>(1);
            }
            if (null == InterestedSequence)
            {
                InterestedSequence = new List<int>(1);
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Stacks", Stacks, typeof(List<CallStack>));
            info.AddValue("SequenceStates", SequenceStates, typeof(List<RuntimeState>));
            info.AddValue("State", State, typeof(RuntimeState));
            if (null != Performance)
            {
                info.AddValue("Performance", Performance, typeof(PerformanceData));
            }
            if (null != WatchData && WatchData.Count > 0)
            {
                info.AddValue("WatchData", WatchData, typeof(Dictionary<string, string>));
            }
            if (null != ExceptionInfo)
            {
                info.AddValue("ExceptionInfo", ExceptionInfo, typeof(SequenceFailedInfo));
            }
            info.AddValue("Results", Results, typeof(List<StepResult>));
            if (null != FailedInfo && FailedInfo.Count > 0)
            {
                info.AddValue("FailedInfo", FailedInfo, typeof(Dictionary<int, string>));
            }
            if (null != InterestedSequence && InterestedSequence.Count > 0)
            {
                info.AddValue("InterestedSequence", InterestedSequence, typeof(List<int>));
            }
        }
    }
}