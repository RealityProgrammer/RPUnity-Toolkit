using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealityProgrammer.CSStandard.Interpreter.Expressions {
    public class UnaryExpression : BaseExpression {
        public BaseExpression Expression { get; protected set; }
        public LexerToken Operator { get; protected set; }

        public UnaryExpression(LexerToken @operator, BaseExpression expr) {
            Expression = expr;
            Operator = @operator;
        }

        public override object Evaluate(IInterpreter interpreter) {
            return interpreter.EvaluateUnaryExpression(this);
        }
    }
}
