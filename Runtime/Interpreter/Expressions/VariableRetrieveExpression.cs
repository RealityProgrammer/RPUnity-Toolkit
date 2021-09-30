using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealityProgrammer.CSStandard.Interpreter.Expressions {
    public class VariableRetrieveExpression : BaseExpression {
        public LexerToken Name { get; protected set; }

        public VariableRetrieveExpression(LexerToken name) {
            Name = name;
        }

        public override object Evaluate(IInterpreter interpreter) {
            return interpreter.EvaluateVariableRetrieveExpression(this);
        }
    }
}