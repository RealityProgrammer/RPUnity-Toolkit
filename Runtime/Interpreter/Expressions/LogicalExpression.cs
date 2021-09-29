using System;

namespace RealityProgrammer.CSStandard.Interpreter.Expressions {
    public class LogicalExpression: BaseExpression {
        public BaseExpression Left { get; protected set; }
        public BaseExpression Right { get; protected set; }
        public LexerToken Operator { get; protected set; }

        public LogicalExpression(BaseExpression left, LexerToken @operator, BaseExpression right) {
            Left = left;
            Operator = @operator;
            Right = right;
        }

        public override object Evaluate(IInterpreter interpreter) {
            return interpreter.EvaluateLogicalExpression(this);
        }
    }
}