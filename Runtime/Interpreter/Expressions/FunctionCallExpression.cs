using System.Collections.Generic;

namespace RealityProgrammer.CSStandard.Interpreter.Expressions {
    public class FunctionCallExpression : BaseExpression {
        public IdentifierExpression MethodName { get; protected set; }
        public List<BaseExpression> Parameters { get; protected set; }

        public FunctionCallExpression(IdentifierExpression method, List<BaseExpression> parameters) {
            MethodName = method;
            Parameters = parameters;
        }

        public override object Evaluate(IInterpreter interpreter) {
            return interpreter.EvaluateCallExpression(this);
        }
    }
}