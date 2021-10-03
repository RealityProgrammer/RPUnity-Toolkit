using System;

namespace RealityProgrammer.CSStandard.Interpreter.Exceptions {
    public class InvalidNumericLiteralException : Exception {
        public InvalidNumericLiteralException(string msg) : base(msg) { }
    }
}
