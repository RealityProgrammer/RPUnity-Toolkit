using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealityProgrammer.CSStandard.Interpreter.Exceptions {
    public class InterpreterErrorCodeException : Exception {
        public InterpreterErrorCodeException(InterpreterRuntimeErrorCode code) : base(_messages.ContainsKey(code) ? _messages[code] : _messages[InterpreterRuntimeErrorCode.UndefinedErrorCode]) {
        }

        private static readonly Dictionary<InterpreterRuntimeErrorCode, string> _messages = new Dictionary<InterpreterRuntimeErrorCode, string>() {
            { InterpreterRuntimeErrorCode.NoError, "No Error. Report if you see this message." },
            { InterpreterRuntimeErrorCode.UndefinedErrorCode, "Undefined Error Code." },
            { InterpreterRuntimeErrorCode.OperandNotANumber, "Operand is not a integer numbers or floating points numbers." },
            { InterpreterRuntimeErrorCode.InvalidMinusUnaryOperator, "Unary operator cannot be applied to Operand." },
            { InterpreterRuntimeErrorCode.InvalidLogicalNotOperator, "Logical not (!) operator cannot be applied to Operand." },
        };
    }
}
