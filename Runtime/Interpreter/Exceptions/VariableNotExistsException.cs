using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealityProgrammer.CSStandard.Interpreter.Exceptions {
    public class VariableNotExistsException : Exception {
        public VariableNotExistsException(LexerToken variable) : base("Variable named \"" + variable.Lexeme + "\" is not exists in the current interpreter. You should consider define the variable first before retrieving it.") { }
    }
}