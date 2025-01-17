﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Testflow.Common;
using Testflow.Data;
using Testflow.Data.Sequence;
using Testflow.SequenceManager.Common;
using Testflow.SequenceManager.Serializer;

namespace Testflow.SequenceManager.SequenceElements
{
    [Serializable]
    [JsonConverter(typeof(SequenceJsonConvertor))]
    public class TestProject : ITestProject
    {
        public TestProject()
        {
            this.Name = string.Empty;
            this.Description = string.Empty;
            this.TypeDatas = new TypeDataCollection();
            this.Assemblies = new AssemblyInfoCollection();
            this.ExecutionModel = ExecutionModel.SequentialExecution;
            this.Variables = new VariableCollection();
            this.VariableValues = new VariableInitValueCollection();
            this.SetUp = new Sequence()
            {
                Name = "SetUp",
                Index = CommonConst.SetupIndex
            };
            this.SetUpParameters = new SequenceParameter();
            this.SequenceGroups = new SequenceGroupCollection();
            this.SequenceGroupParameters = new ParameterDataCollections();
            this.SequenceGroupLocations = null;
            this.TearDown = new Sequence()
            {
                Name = "TearDown",
                Index = CommonConst.TeardownIndex
            };
            this.TearDownParameters = new SequenceParameter();
            this.ModelVersion = string.Empty;
        }

        [RuntimeSerializeIgnore]
        public string Name { get; set; }
        [RuntimeSerializeIgnore]
        public string Description { get; set; }

        [XmlIgnore]
        [SerializationIgnore]
        [RuntimeSerializeIgnore]
        public ISequenceFlowContainer Parent { get; set; }

        [RuntimeType(typeof(TypeDataCollection))]
        public ITypeDataCollection TypeDatas { get; set; }

        [RuntimeSerializeIgnore]
        public string ModelVersion { get; set; }

        [RuntimeType(typeof(AssemblyInfoCollection))]
        public IAssemblyInfoCollection Assemblies { get; set; }

        public ExecutionModel ExecutionModel { get; set; }

        [RuntimeType(typeof(VariableCollection))]
        public IVariableCollection Variables { get; set; }

        [RuntimeType(typeof(VariableInitValueCollection))]
        public IList<IVariableInitValue> VariableValues { get; set; }

        [RuntimeType(typeof(Sequence))]
        public ISequence SetUp { get; set; }

        [RuntimeType(typeof(SequenceParameter))]
        public ISequenceParameter SetUpParameters { get; set; }

        [XmlIgnore]
        [SerializationIgnore]
        [RuntimeSerializeIgnore]
        public ISequenceGroupCollection SequenceGroups { get; set; }

        [RuntimeSerializeIgnore]
        public SequenceGroupLocationInfoCollection SequenceGroupLocations { get; set; }

        [RuntimeType(typeof(ParameterDataCollections))]
        public IList<IParameterDataCollection> SequenceGroupParameters { get; set; }

        [RuntimeType(typeof(Sequence))]
        public ISequence TearDown { get; set; }

        [RuntimeType(typeof(SequenceParameter))]
        public ISequenceParameter TearDownParameters { get; set; }

        public void Initialize(ISequenceFlowContainer parent)
        {
            if (!ModuleUtils.IsValidName(this.Name))
            {
                this.Name = string.Format(Constants.TestProjectNameFormat, 1);
            }
            this.SequenceGroupLocations = new SequenceGroupLocationInfoCollection();
        }

        public ISequenceFlowContainer Clone()
        {
            throw new InvalidOperationException();
        }

        #region 序列化声明及反序列化构造

        public TestProject(SerializationInfo info, StreamingContext context)
        {
            ModuleUtils.FillDeserializationInfo(info, this, this.GetType());
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ModuleUtils.FillSerializationInfo(info, this);
        }

        #endregion

    }
}