using System;

namespace RealityProgrammer.CSStandard.Interpreter.Expressions {
    public class IdentifierExpression : BaseExpression {
        public LexerToken Name { get; protected set; }

        public IdentifierExpression(LexerToken name) {
            Name = name;
        }

        public override object Evaluate(IInterpreter interpreter) {
            throw new NotImplementedException();
        }
    }
}
