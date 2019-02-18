﻿using System;
using Testflow.Data;
using Testflow.Data.Sequence;
using Testflow.SequenceManager.Common;

namespace Testflow.SequenceManager.SequenceElements
{
    [Serializable]
    public class Argument : IArgument
    {
        public Argument()
        {
            this.Name = "";
            this.Type = null;
            this.TypeIndex = Constants.UnverifiedTypeIndex;
            this.Modifier = ArgumentModifier.None;
            this.VariableType = VariableType.Undefined;
        }

        public string Name { get; set; }

        public ITypeData Type { get; set; }

        public int TypeIndex { get; set; }

        public ArgumentModifier Modifier { get; set; }

        public VariableType VariableType { get; set; }

        public IArgument Clone()
        {
            Argument argument = new Argument()
            {
                Name = this.Name,
                Type = this.Type,
                TypeIndex = this.TypeIndex,
                Modifier = this.Modifier,
                VariableType = this.VariableType
            };
            return argument;
        }
    }
}