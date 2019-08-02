﻿using System;
using System.IO;
using System.Xml;
using Testflow.Modules;
using Testflow.Runtime;
using Testflow.Usr;
using Testflow.Utility.I18nUtil;

namespace Testflow.ConfigurationManager
{
    public class PlatformConfigManager : IConfigurationManager
    {
        public PlatformConfigManager()
        {
            I18NOption i18NOption = new I18NOption(this.GetType().Assembly, "i18n_config_zh", "i18n_config_en")
            {
                Name = Constants.I18nName
            };
            I18N.InitInstance(i18NOption);
            I18N i18N = I18N.GetInstance(Constants.I18nName);

            string platformDir = Environment.GetEnvironmentVariable(CommonConst.EnvironmentVariable);
            if (string.IsNullOrWhiteSpace(platformDir) || Directory.Exists(platformDir))
            {
                TestflowRunner.GetInstance().LogService.Print(LogLevel.Fatal, CommonConst.PlatformLogSession, 
                    $"Invalid environment variable:{CommonConst.EnvironmentVariable}");
                throw new TestflowRuntimeException(ModuleErrorCode.InvalidTestHome, i18N.GetStr("InvalidHomeVariable"));
            }
            this.ConfigData = new ModuleConfigData();
            string configFilePath = $"{platformDir}{Path.DirectorySeparatorChar}{Constants.ConfigFileDir}{Path.DirectorySeparatorChar}{Constants.ConfigFileName}";
            this.ConfigData.SetProperty(Constants.ConfigFile, configFilePath);
            this.GlobalInfo = ConfigData;
        }

        public IModuleConfigData ConfigData { get; set; }
        public void RuntimeInitialize()
        {
            GlobalConfigData globalConfigData = Initialize();
            TestflowRunner testflowRunner = TestflowRunner.GetInstance();

            testflowRunner.DataMaintainer.ApplyConfig(globalConfigData.GetModuleConfigData(Constants.DataMaintain));
            testflowRunner.EngineController.ApplyConfig(globalConfigData.GetModuleConfigData(Constants.EngineConfig));
            testflowRunner.SequenceManager.ApplyConfig(globalConfigData.GetModuleConfigData(Constants.SequenceManage));
            testflowRunner.ResultManager.ApplyConfig(globalConfigData.GetModuleConfigData(Constants.ResultManage));

            globalConfigData.Dispose();
        }

        public void DesigntimeInitialize()
        {
            GlobalConfigData globalConfigData = Initialize();
            TestflowRunner testflowRunner = TestflowRunner.GetInstance();

            testflowRunner.ComInterfaceManager.ApplyConfig(globalConfigData.GetModuleConfigData(Constants.InterfaceLoad));
            testflowRunner.DataMaintainer.ApplyConfig(globalConfigData.GetModuleConfigData(Constants.DataMaintain));
            testflowRunner.EngineController.ApplyConfig(globalConfigData.GetModuleConfigData(Constants.EngineConfig));
            testflowRunner.SequenceManager.ApplyConfig(globalConfigData.GetModuleConfigData(Constants.SequenceManage));
            testflowRunner.ResultManager.ApplyConfig(globalConfigData.GetModuleConfigData(Constants.ResultManage));
            testflowRunner.ParameterChecker.ApplyConfig(globalConfigData.GetModuleConfigData(Constants.ParamCheck));

            this.ConfigData.Properties.Add("TestName", "");
            this.ConfigData.Properties.Add("TestDescription", "");
            this.ConfigData.Properties.Add("RuntimeHash", "");
            this.ConfigData.Properties.Add("RuntimeType", RuntimeType.Run);


            globalConfigData.Dispose();
        }

        private GlobalConfigData Initialize()
        {
            GlobalConfigData globalConfigData;
            using (ConfigDataLoader dataLoader = new ConfigDataLoader())
            {
                globalConfigData = dataLoader.Load(ConfigData.GetProperty<string>(Constants.ConfigFile));
            }
            return globalConfigData;
        }

        public void ApplyConfig(IModuleConfigData configData)
        {
            this.ConfigData = configData;
        }

        public IPropertyExtendable GlobalInfo { get; set; }
        public void LoadConfigurationData()
        {
            // ignore
        }

        public void ApplyConfigData(IController controller)
        {
            // ignore
        }

        public void Dispose()
        {
            I18N.RemoveInstance(Constants.I18nName);
        }

    }
}