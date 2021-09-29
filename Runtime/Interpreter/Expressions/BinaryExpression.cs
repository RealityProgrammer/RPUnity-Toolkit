using System;

namespace RealityProgrammer.CSStandard.Interpreter.Expressions {
    public class BinaryExpression : BaseExpression {
        public BaseExpression Left { get; protected set; }
        public BaseExpression Right { get; protected set; }
        public LexerToken Operator { get; protected set; }

        public BinaryExpression(BaseExpression left, LexerToken @operator, BaseExpression right) {
            Left = left;
            Operator = @operator;
            Right = right;
        }

        public override object Evaluate(IInterpreter interpreter) {
            return interpreter.EvaluateBinaryExpresison(this);
        }
    }
}
