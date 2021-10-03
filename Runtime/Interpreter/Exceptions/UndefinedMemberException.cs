using System;

namespace RealityProgrammer.CSStandard.Interpreter.Exceptions {
    public class UndefinedMemberException : Exception {
        public UndefinedMemberException(string msg) : base(msg) { }
        public UndefinedMemberException(LexerToken method) : base("Member name \"" + method.Lexeme + "\" cannot be found or not defined yet.") { }
    }
}