﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Testflow.Common;
using Testflow.Data;
using Testflow.Data.Sequence;
using Testflow.Logger;
using Testflow.Modules;
using Testflow.SequenceManager.Common;
using Testflow.SequenceManager.SequenceElements;

namespace Testflow.SequenceManager.Serializer
{
    internal static class SequenceSerializer
    {
        #region 序列化

        public static void Serialize(string filePath, TestProject testProject)
        {
            filePath = ModuleUtils.GetAbsolutePath(filePath, Directory.GetCurrentDirectory());

            VerifySequenceData(testProject);
            List<string> serialziedFileList = new List<string>(10);
            try
            {
                // 初始化各个SequenceGroup的文件位置信息
                InitSequenceGroupLocations(testProject, filePath);
                serialziedFileList.Add(filePath);
                XmlWriterHelper.Write(testProject, filePath);
                // 将testProject当前配置的数据信息写入ParameterData中
                FillParameterDataToSequenceData(testProject);
                for (int i = 0; i < testProject.SequenceGroups.Count; i++)
                {
                    SequenceGroup sequenceGroup = testProject.SequenceGroups[i] as SequenceGroup;
                    string sequenceGroupPath = ModuleUtils.GetAbsolutePath(sequenceGroup.Info.SequenceGroupFile, filePath);
                    string parameterFilePath = ModuleUtils.GetAbsolutePath(sequenceGroup.Info.SequenceParamFile, sequenceGroupPath); ;
                    if (!ModuleUtils.IsValidFilePath(sequenceGroupPath))
                    {
                        sequenceGroupPath = ModuleUtils.GetSequenceGroupPath(filePath, i);
                        sequenceGroup.Info.SequenceGroupFile = ModuleUtils.GetRelativePath(sequenceGroupPath, filePath);
                        parameterFilePath = ModuleUtils.GetParameterFilePath(sequenceGroupPath);
                        sequenceGroup.Info.SequenceParamFile = ModuleUtils.GetRelativePath(parameterFilePath, sequenceGroupPath);
                    }
                    else if (!ModuleUtils.IsValidFilePath(parameterFilePath))
                    {
                        parameterFilePath = ModuleUtils.GetParameterFilePath(sequenceGroupPath);
                        sequenceGroup.Info.SequenceParamFile = ModuleUtils.GetRelativePath(parameterFilePath, sequenceGroupPath);
                    }
                    SequenceGroupParameter parameter = new SequenceGroupParameter();
                    parameter.Initialize(sequenceGroup);
                    // 将SequeneGroup配置的参数写入ParameterData中，用以序列化
                    FillParameterDataToSequenceData(sequenceGroup, parameter);
                    sequenceGroup.RefreshSignature();
                    parameter.RefreshSignature(sequenceGroup);
                    // 创建sequenceGroupd的文件夹
                    string directory = ModuleUtils.GetSequenceGroupDirectory(sequenceGroupPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    serialziedFileList.Add(sequenceGroupPath);
                    XmlWriterHelper.Write(sequenceGroup, sequenceGroupPath);

                    serialziedFileList.Add(parameterFilePath);
                    XmlWriterHelper.Write(parameter, parameterFilePath);
                }
            }
            catch (IOException ex)
            {
                RollBackFilesIfFailed(serialziedFileList);
                throw new TestflowRuntimeException(ModuleErrorCode.SerializeFailed, ex.Message, ex);
            }
            catch (ApplicationException)
            {
                RollBackFilesIfFailed(serialziedFileList);
                throw;
            }
        }

        public static void Serialize(string filePath, SequenceGroup sequenceGroup)
        {
            filePath = ModuleUtils.GetAbsolutePath(filePath, Directory.GetCurrentDirectory());

            VerifySequenceData(sequenceGroup);
            sequenceGroup.Info.SequenceGroupFile = filePath;
            sequenceGroup.Info.SequenceParamFile = ModuleUtils.GetParameterFilePath(filePath);

            SequenceGroupParameter parameter = new SequenceGroupParameter();
            parameter.Initialize(sequenceGroup);
            // 将SequeneGroup配置的参数写入ParameterData中，用以序列化
            FillParameterDataToSequenceData(sequenceGroup, parameter);

            List<string> serialziedFileList = new List<string>(20);
            try
            {
                serialziedFileList.Add(filePath);
                XmlWriterHelper.Write(sequenceGroup, sequenceGroup.Info.SequenceGroupFile);

                serialziedFileList.Add(sequenceGroup.Info.SequenceParamFile);
                XmlWriterHelper.Write(parameter, sequenceGroup.Info.SequenceParamFile);
            }
            catch (IOException ex)
            {
                RollBackFilesIfFailed(serialziedFileList);
                throw new TestflowRuntimeException(ModuleErrorCode.SerializeFailed, ex.Message, ex);
            }
            catch (ApplicationException)
            {
                RollBackFilesIfFailed(serialziedFileList);
                throw;
            }
        }

        public static string ToJson(TestProject testProject)
        {
            // 目前该方法还会同时核查下级SequenceGroup的数据，后续优化
            VerifySequenceData(testProject);
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(testProject, settings);
        }

        public static string ToJson(SequenceGroup sequenceGroup)
        {
            VerifySequenceData(sequenceGroup);
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(sequenceGroup, settings);
        }

        private static void InitSequenceGroupLocations(TestProject testProject, string testProjectPath)
        {
            testProject.SequenceGroupLocations.Clear();
            ISequenceGroupCollection sequenceGroups = testProject.SequenceGroups;
            for (int i = 0; i < sequenceGroups.Count; i++)
            {
                ISequenceGroup sequenceGroup = sequenceGroups[i];
                string sequenceGroupPath = ModuleUtils.GetAbsolutePath(sequenceGroup.Info.SequenceGroupFile,
                    testProjectPath);
                string parameterPath = ModuleUtils.GetAbsolutePath(sequenceGroup.Info.SequenceParamFile,
                    testProjectPath);
                if (!ModuleUtils.IsValidFilePath(sequenceGroupPath))
                {
                    sequenceGroupPath = ModuleUtils.GetSequenceGroupPath(testProjectPath, i);
                    parameterPath = ModuleUtils.GetParameterFilePath(sequenceGroupPath);
                    sequenceGroup.Info.SequenceGroupFile = ModuleUtils.GetRelativePath(sequenceGroupPath, testProjectPath);
                    sequenceGroup.Info.SequenceParamFile = ModuleUtils.GetRelativePath(parameterPath, sequenceGroupPath);
                }
                else if (!ModuleUtils.IsValidFilePath(sequenceGroup.Info.SequenceParamFile))
                {
                    parameterPath = ModuleUtils.GetParameterFilePath(sequenceGroupPath);
                    sequenceGroup.Info.SequenceParamFile = ModuleUtils.GetRelativePath(parameterPath, sequenceGroupPath);
                }
                SequenceGroupLocationInfo locationInfo = new SequenceGroupLocationInfo()
                {
                    Name = sequenceGroup.Name,
                    SequenceFilePath = sequenceGroup.Info.SequenceGroupFile,
                    ParameterFilePath = sequenceGroup.Info.SequenceParamFile
                };
                testProject.SequenceGroupLocations.Add(locationInfo);
            }
        }

        private static void FillParameterDataToSequenceData(ITestProject testProject)
        {
            FillParameterDataToSequenceData(testProject.SetUp, testProject.SetUpParameters);
            for (int i = 0; i < testProject.Variables.Count; i++)
            {
                testProject.VariableValues[i].Value = testProject.Variables[i].Value;
            }
            FillParameterDataToSequenceData(testProject.TearDown, testProject.TearDownParameters);
        }

        private static void FillParameterDataToSequenceData(ISequenceGroup sequenceGroup, ISequenceGroupParameter parameter)
        {
            for (int i = 0; i < sequenceGroup.Variables.Count; i++)
            {
                parameter.VariableValues[i].Value = sequenceGroup.Variables[i].Value;
            }
            FillParameterDataToSequenceData(sequenceGroup.SetUp, parameter.SetUpParameters);
            for (int i = 0; i < sequenceGroup.Sequences.Count; i++)
            {
                FillParameterDataToSequenceData(sequenceGroup.Sequences[i], parameter.SequenceParameters[i]);
            }
            FillParameterDataToSequenceData(sequenceGroup.TearDown, parameter.TearDownParameters);
        }

        private static void FillParameterDataToSequenceData(ISequence sequence, ISequenceParameter parameter)
        {
            for (int i = 0; i < sequence.Variables.Count; i++)
            {
                parameter.VariableValues[i].Value = sequence.Variables[i].Value;
            }

            for (int i = 0; i < sequence.Steps.Count; i++)
            {
                FillParameterDataToSequenceData(sequence.Steps[i], parameter.StepParameters[i]);
            }
        }

        private static void FillParameterDataToSequenceData(ISequenceStep sequenceStep, ISequenceStepParameter parameter)
        {
            if (sequenceStep.HasSubSteps)
            {
                for (int i = 0; i < sequenceStep.SubSteps.Count; i++)
                {
                    FillParameterDataToSequenceData(sequenceStep.SubSteps[i], parameter.SubStepParameters[i]);
                }
            }
            else if (null != sequenceStep.Function)
            {
                parameter.Instance = sequenceStep.Function.Instance;
                parameter.Return = sequenceStep.Function.Return;
                IParameterDataCollection parameterValues = sequenceStep.Function.Parameters;
                for (int i = 0; i < parameterValues.Count; i++)
                {
                    parameter.Parameters[i].ParameterType = parameterValues[i].ParameterType;
                    parameter.Parameters[i].Value = parameterValues[i].Value;
                }
            }
        }

        /// <summary>
        /// 序列化前预处理，包括更新TypeData和TypeIndex属性
        /// </summary>
        private static void VerifySequenceData(TestProject testProject)
        {
            testProject.TypeDatas.Clear();
            foreach (IVariable variable in testProject.Variables)
            {
                VerifyTypeIndexes(testProject.TypeDatas, variable as Variable);
            }
            foreach (ISequenceGroup sequenceGroup in testProject.SequenceGroups)
            {
                foreach (IArgument argument in sequenceGroup.Arguments)
                {
                    VerifyTypeIndexes(testProject.TypeDatas, argument as Argument);
                }
            }

            foreach (ISequenceGroup sequenceGroup in testProject.SequenceGroups)
            {
                VerifySequenceData(sequenceGroup as SequenceGroup);
            }
        }

        private static void VerifySequenceData(SequenceGroup sequenceGroup)
        {
            foreach (IVariable variable in sequenceGroup.Variables)
            {
                VerifyTypeIndexes(sequenceGroup.TypeDatas, variable as Variable);
            }
            foreach (IArgument argument in sequenceGroup.Arguments)
            {
                VerifyTypeIndexes(sequenceGroup.TypeDatas, argument as Argument);
            }
            foreach (ISequence sequence in sequenceGroup.Sequences)
            {
                foreach (IVariable variable in sequence.Variables)
                {
                    VerifyTypeIndexes(sequenceGroup.TypeDatas, variable as Variable);
                }
                foreach (ISequenceStep sequenceStep in sequence.Steps)
                {
                    VerifyTypeIndexes(sequenceGroup.TypeDatas, sequenceStep);
                }
            }
        }

        private static void RollBackFilesIfFailed(IList<string> fileList)
        {
            foreach (string file in fileList)
            {
                if (File.Exists(file))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException ex)
                    {
                        ILogService logService = TestflowRunner.GetInstance().LogService;
                        logService.Print(LogLevel.Warn, CommonConst.PlatformLogSession, 0, ex, "Roll back file failed.");
                    }
                }
            }
        }

        private static void VerifyTypeIndexes(ITypeDataCollection typeDatas, ISequenceStep step)
        {
            if (step.HasSubSteps)
            {
                foreach (ISequenceStep subStep in step.SubSteps)
                {
                    VerifyTypeIndexes(typeDatas, subStep);
                }
            }
            else
            {
                VerifyTypeIndexes(typeDatas, step.Function as FunctionData);
            }
        }

        private static void VerifyTypeIndexes(ITypeDataCollection typeDatas, Variable variable)
        {
            if (null == variable.Type || VariableType.Undefined == variable.VariableType)
            {
                variable.TypeIndex = Constants.UnverifiedTypeIndex;
                return;
            }
            int index = typeDatas.IndexOf(variable.Type);
            if (-1 == index)
            {
                index = typeDatas.Count;
                typeDatas.Add(variable.Type);
            }
            variable.TypeIndex = index;
        }

        private static void VerifyTypeIndexes(ITypeDataCollection typeDatas, Argument argument)
        {
            if (null == argument.Type || VariableType.Undefined == argument.VariableType)
            {
                argument.TypeIndex = Constants.UnverifiedTypeIndex;
                return;
            }
            int index = typeDatas.IndexOf(argument.Type);
            if (-1 == index)
            {
                index = typeDatas.Count;
                typeDatas.Add(argument.Type);
            }
            argument.TypeIndex = index;
        }

        private static void VerifyTypeIndexes(ITypeDataCollection typeDatas, FunctionData functionData)
        {
            if (functionData?.ClassType == null)
            {
                if (null != functionData)
                {
                    functionData.ClassTypeIndex = Constants.UnverifiedTypeIndex;
                }
                return;
            }
            int index = typeDatas.IndexOf(functionData.ClassType);
            if (-1 == index)
            {
                index = typeDatas.Count;
                typeDatas.Add(functionData.ClassType);
            }

            functionData.ClassTypeIndex = index;
            foreach (IArgument argument in functionData.ParameterType)
            {
                VerifyTypeIndexes(typeDatas, argument as Argument);
            }

            VerifyTypeIndexes(typeDatas, functionData.ReturnType as Argument);
        }

        #endregion
    }
}