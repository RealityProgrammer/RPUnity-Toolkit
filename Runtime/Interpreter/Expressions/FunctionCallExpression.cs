using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealityProgrammer.CSStandard.Interpreter.Expressions {
    public class FunctionCallExpression : BaseExpression {
        public BaseExpression Target { get; protected set; }
        public IdentifierExpression MethodName { get; protected set; }
        public List<BaseExpression> Parameters { get; protected set; }

        public FunctionCallExpression(BaseExpression target, IdentifierExpression method, List<BaseExpression> parameters) {
            Target = target;
            MethodName = method;
            Parameters = parameters;
        }

        public override object Evaluate(IInterpreter interpreter) {
            return interpreter.EvaluateCallExpression(this);
        }
    }
}
