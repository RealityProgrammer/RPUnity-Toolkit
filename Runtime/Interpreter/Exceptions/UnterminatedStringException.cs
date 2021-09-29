using System;

namespace RealityProgrammer.CSStandard.Interpreter.Exceptions {
    public class UnterminatedStringException : Exception {
        public UnterminatedStringException(string message) : base(message) { }
    }
}
