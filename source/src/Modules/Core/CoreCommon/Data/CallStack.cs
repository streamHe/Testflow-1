﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Testflow.CoreCommon.Common;
using Testflow.Data.Sequence;
using Testflow.Runtime;

namespace Testflow.CoreCommon.Data
{
    public class CallStack : ICallStack, ISerializable
    {
        private const string StackDelim = "_";

        public CallStack()
        {
            SessionIndex = CoreConstants.UnverifiedSequenceIndex;
            Session = CoreConstants.UnverifiedSequenceIndex;
            this.StepStack = new List<int>(CoreConstants.DefaultRuntimeSize);
        }

        public int SessionIndex { get; internal set; }
        public int Session { get; internal set; }

        public IList<int> StepStack { get; internal set; }

        public static CallStack GetStack(ISequenceStep step)
        {
            CallStack callStack = new CallStack();

            ISequence sequence = null;
            ISequenceGroup sequenceGroup = null;
            while (null != step)
            {
                callStack.StepStack.Add(step.Index);
                ISequenceFlowContainer parent = step.Parent;
                if (parent is ISequence)
                {
                    sequence = parent as ISequence;
                }
                step = parent as ISequenceStep;
            }
            if (null == sequence)
            {
                return callStack;
            }
            callStack.Session = sequence.Index;
            sequenceGroup = sequence.Parent as ISequenceGroup;
            if (!(sequenceGroup?.Parent is ITestProject))
            {
                return callStack;
            }
            ITestProject testProject = (ITestProject) sequenceGroup.Parent;
            callStack.SessionIndex = testProject.SequenceGroups.IndexOf(sequenceGroup);
            return callStack;
        }

        public static CallStack GetStack(string stackStr)
        {
            CallStack callStack = new CallStack();
            string[] stackElement = stackStr.Split(StackDelim.ToCharArray());
            callStack.SessionIndex = int.Parse(stackElement[0]);
            callStack.Session = int.Parse(stackElement[1]);
            for (int i = 2; i < stackElement.Length; i++)
            {
                callStack.StepStack.Add(int.Parse(stackElement[i]));
            }
            return callStack;
        }

        public CallStack(SerializationInfo info, StreamingContext context)
        {
            this.SessionIndex = (int) info.GetValue("SequenceGroupIndex", typeof(int));
            this.Session = (int) info.GetValue("SequenceIndex", typeof(int));
            this.StepStack = info.GetValue("StepStack", typeof(List<int>)) as List<int>;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("SequenceGroupIndex", SessionIndex);
            info.AddValue("SequenceIndex", Session);
            info.AddValue("StepStack", StepStack, typeof(List<int>));
        }

        public override bool Equals(object obj)
        {
            CallStack callStack = obj as CallStack;
            if (null == callStack || SessionIndex != callStack.SessionIndex ||
                Session != callStack.Session || callStack.StepStack.Count != StepStack.Count)
            {
                return false;
            }
            for (int i = 0; i < StepStack.Count; i++)
            {
                if (StepStack[i] != callStack.StepStack[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = SessionIndex;
                hashCode = (hashCode * 397) ^ Session;
                hashCode = (hashCode * 397) ^ (StepStack != null ? StepStack.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            StringBuilder callStackStr = new StringBuilder(100);
            callStackStr.Append(SessionIndex)
                .Append(StackDelim)
                .Append(Session)
                .Append(StackDelim)
                .Append(string.Join(StackDelim, StepStack));
            return callStackStr.ToString();
        }
    }
}