﻿using Testflow.Common;

namespace Testflow.Modules
{
    /// <summary>
    /// 配置管理模块
    /// </summary>
    public interface IConfigurationManager : IController
    {
        /// <summary>
        /// 全局信息
        /// </summary>
        IPropertyExtendable GlobalInfo { get; set; }

        /// <summary>
        /// 加载模块配置数据
        /// </summary>
        void LoadConfigurationData();

        /// <summary>
        /// 为模块写入配置数据
        /// </summary>
        /// <param name="controller"></param>
        void ApplyConfigData(IController controller);
    }
}