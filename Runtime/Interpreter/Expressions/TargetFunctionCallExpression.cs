using System.Collections.Generic;

namespace RealityProgrammer.CSStandard.Interpreter.Expressions {
    public class TargetFunctionCallExpression : BaseExpression {
        public BaseExpression Target { get; protected set; }
        public IdentifierExpression MethodName { get; protected set; }
        public List<BaseExpression> Parameters { get; protected set; }

        public TargetFunctionCallExpression(BaseExpression target, IdentifierExpression method, List<BaseExpression> parameters) {
            Target = target;
            MethodName = method;
            Parameters = parameters;
        }

        public override object Evaluate(IInterpreter interpreter) {
            return interpreter.EvaluateTargetCallExpression(this);
        }
    }
}
