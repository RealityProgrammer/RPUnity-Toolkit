using System;

namespace RealityProgrammer.CSStandard.Interpreter.Exceptions {
    public class UnclosedParenthesesException : Exception {
        public UnclosedParenthesesException(string message) : base(message) { }
    }
}
