﻿using System.Collections.Generic;
using Testflow.Common;
using Testflow.Data.Sequence;
using Testflow.Runtime;
using Testflow.Runtime.Data;

namespace Testflow.Modules
{
    /// <summary>
    /// 数据持久化模块
    /// </summary>
    public interface IDataMaintainer : IController
    {
        #region Result maintain

        /// <summary>
        /// 返回所有符合过滤字符串的TestInstance的条目数
        /// </summary>
        int GetTestInstanceCount(string fileterString);

        /// <summary>
        /// 获取指定运行时Hash的TestInstance数据
        /// </summary>
        TestInstanceData GetTestInstanceData(string runtimeHash);

        /// <summary>
        /// 返回所有符合过滤字符串的TestInstance条目
        /// </summary>
        IList<TestInstanceData> GetTestInstanceDatas(string filterString);

        /// <summary>
        /// 记录TestInstanceData
        /// </summary>
        void AddData(TestInstanceData testInstance);

        /// <summary>
        /// 更新TestInstanceData
        /// </summary>
        void UpdateData(TestInstanceData testInstance);

        /// <summary>
        /// 使用指定的过滤语句删除TestInstance
        /// </summary>
        void DeleteTestInstance(string fileterString);

        /// <summary>
        /// 获取某个运行实例的所有会话结果
        /// </summary>
        IList<SessionResultData> GetSessionResults(string runtimeHash);

        /// <summary>
        /// 获取某个运行实例的某个会话结果
        /// </summary>
        SessionResultData GetSessionResult(string runtimeHash, int sessionId);

        /// <summary>
        /// 记录SessionResultData
        /// </summary>
        void AddData(SessionResultData sessionResult);

        /// <summary>
        /// 更新SessionResultData
        /// </summary>
        void UpdateData(SessionResultData sessionResult);

        /// <summary>
        /// 获取某个运行实例的某个会话的所有序列执行结果
        /// </summary>
        IList<SequenceResultData> GetSequenceResultDatas(string runtimeHash, int sessionId);

        /// <summary>
        /// 获取某个运行实例的某个会话的某个序列的执行结果
        /// </summary>
        SequenceResultData GetSequenceResultData(string runtimeHash, int sessionId, int sequenceIndex);

        /// <summary>
        /// 记录SequenceResultData
        /// </summary>
        void AddData(SequenceResultData sequenceResult);

        /// <summary>
        /// 更新SequenceResultData
        /// </summary>
        void UpdateData(SequenceResultData sequenceResult);

        #endregion

        #region Middle status maintain

        /// <summary>
        /// 记录PerformanceStatus
        /// </summary>
        void AddData(PerformanceStatus performanceStatus);

        /// <summary>
        /// 记录RuntimeStatusData
        /// </summary>
        void AddData(RuntimeStatusData runtimeStatus);

        #endregion
    }
}