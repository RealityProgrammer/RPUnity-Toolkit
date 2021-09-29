using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealityProgrammer.CSStandard.Interpreter.Expressions {
    public class GroupingExpression : BaseExpression {
        public BaseExpression Expression { get; protected set; }

        public GroupingExpression(BaseExpression input) {
            Expression = input;
        }

        public override object Evaluate(IInterpreter interpreter) {
            return interpreter.EvaluateGroupingExpression(this);
        }
    }
}
