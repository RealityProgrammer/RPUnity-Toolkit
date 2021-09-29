using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
