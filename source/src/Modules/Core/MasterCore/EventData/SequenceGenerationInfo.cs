﻿using System.Collections.Generic;
using Testflow.Common;
using Testflow.Data.Sequence;
using Testflow.Runtime;
using Testflow.Utility.Collections;

namespace Testflow.MasterCore.EventData
{
    internal class SequenceGenerationInfo : ISequenceGenerationInfo
    {
        public int Session { get; }
        public ISerializableMap<int, GenerationStatus> SequenceStatus { get; }
        public GenerationStatus Status { get; set; }

        public SequenceGenerationInfo(ISequenceGroup sequenceGroup, int session)
        {
            this.Session = session;
            this.Status = GenerationStatus.Idle;
            this.SequenceStatus = new SerializableMap<int, GenerationStatus>(sequenceGroup.Sequences.Count + 2);
            SequenceStatus.Add(CommonConst.SetupIndex, GenerationStatus.Idle);
            SequenceStatus.Add(CommonConst.TeardownIndex, GenerationStatus.Idle);
            for (int i = 0; i < sequenceGroup.Sequences.Count; i++)
            {
                SequenceStatus.Add(i, GenerationStatus.Idle);
            }
        }

        public SequenceGenerationInfo(ITestProject testProject)
        {
            this.Session = CommonConst.TestGroupSession;
            this.Status = GenerationStatus.Idle;
            this.SequenceStatus = new SerializableMap<int, GenerationStatus>(2);
            SequenceStatus.Add(CommonConst.SetupIndex, GenerationStatus.Idle);
            SequenceStatus.Add(CommonConst.TeardownIndex, GenerationStatus.Idle);
        }

        public SequenceGenerationInfo(ISequenceGenerationInfo generationInfo)
        {
            this.Session = generationInfo.Session;
            this.Status = generationInfo.Status;
            this.SequenceStatus = new SerializableMap<int, GenerationStatus>(generationInfo.SequenceStatus.Count);
            foreach (KeyValuePair<int, GenerationStatus> infoPair in generationInfo.SequenceStatus)
            {
                this.SequenceStatus.Add(infoPair.Key, infoPair.Value);
            }
        }

        public ISequenceGenerationInfo Clone()
        {
            return new SequenceGenerationInfo(this);
        }
    }
}