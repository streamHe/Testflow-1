﻿using System;
using System.Xml;
using System.Xml.Serialization;
using Testflow.Common;

namespace Testflow.Data.Sequence
{
    /// <summary>
    /// 保存测试序列组相关信息的接口
    /// </summary>
    public interface ISequenceGroupInfo : ICloneableClass<ISequenceGroupInfo>, ISequenceElement
    {
        /// <summary>
        /// 测试序列组的格式版本
        /// </summary>
        string Version { get; set; }

        /// <summary>
        /// 测试序列组的哈希值，用以唯一确定一个测试序列组，一旦创建不会再更改
        /// </summary>
        string Hash { get; set; }

        /// <summary>
        /// 测试序列组的创建时间
        /// </summary>
        DateTime CreationTime { get; set; }

        /// <summary>
        /// 测试序列组的最新更新时间
        /// </summary>
        DateTime ModifiedTime { get; set; }

        /// <summary>
        /// 测试序列组文件的路径
        /// </summary>
        [XmlIgnore]
        string SequenceGroupFile { get; set; }

        /// <summary>
        /// 测试序列组参数配置文件的路径
        /// </summary>
        string SequenceParamFile { get; set; }

        /// <summary>
        /// 测试序列组是否被修改的标识位
        /// </summary>
        [XmlIgnore]
        bool Modified { get; set; }
    }
}