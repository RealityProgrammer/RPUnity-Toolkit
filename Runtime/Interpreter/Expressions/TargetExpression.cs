using System;

namespace RealityProgrammer.CSStandard.Interpreter.Expressions {
    public class TargetExpression : BaseExpression {
        public LexerToken Token { get; protected set; }

        public TargetExpression(LexerToken token) {
            Token = token;
        }

        public override object Evaluate(IInterpreter interpreter) {
            throw new NotImplementedException();
        }
    }
}
