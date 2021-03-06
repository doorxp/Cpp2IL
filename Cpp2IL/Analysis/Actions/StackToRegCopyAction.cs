﻿using Cpp2IL.Analysis.ResultModels;
using Iced.Intel;

namespace Cpp2IL.Analysis.Actions
{
    public class StackToRegCopyAction : BaseAction
    {
        private IAnalysedOperand beingMoved;
        private ulong stackOffset;
        private string destReg;
        
        public StackToRegCopyAction(MethodAnalysis context, Instruction instruction) : base(context, instruction)
        {
        }

        public override Mono.Cecil.Cil.Instruction[] ToILInstructions()
        {
            //No-op
            return new Mono.Cecil.Cil.Instruction[0];
        }

        public override string? ToPsuedoCode()
        {
            return null;
        }

        public override string ToTextSummary()
        {
            return $"Copies {beingMoved} from stack offset 0x{stackOffset:X} into {destReg}";
        }
    }
}