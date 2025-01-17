﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Testflow.Data.Sequence
{
    /// <summary>
    /// 保存测试序列中单个步骤的数据结构
    /// </summary>
    public interface ISequenceStep : ISequenceFlowContainer
    {
        /// <summary>
        /// 保存子步骤，如果不包含则为空或null
        /// </summary>
        ISequenceStepCollection SubSteps { get; set; }

        /// <summary>
        /// 步骤在当前序列的索引
        /// </summary>
        int Index { get; set; }

        /// <summary>
        /// 当前步骤的Function，如果该步骤包含子步骤，该参数为null
        /// </summary>
        IFunctionData Function { get; set; }

        /// <summary>
        /// 是否包含子步骤
        /// </summary>
        [XmlIgnore]
        bool HasSubSteps { get; }
        
        /// <summary>
        /// 当执行失败时是否停止执行
        /// </summary>
        bool BreakIfFailed { get; set; }

        /// <summary>
        /// 标记当前Step的执行状态是否会被记录
        /// </summary>
        bool RecordStatus { get; set; }

        /// <summary>
        /// 运行行为，正常运行/跳过/强制成功/强制失败
        /// </summary>
        RunBehavior Behavior { get; set; }

        /// <summary>
        /// 循环计数器
        /// </summary>
        ILoopCounter LoopCounter { get; set; }

        /// <summary>
        /// 重试计数器
        /// </summary>
        IRetryCounter RetryCounter { get; set; }
    }
}