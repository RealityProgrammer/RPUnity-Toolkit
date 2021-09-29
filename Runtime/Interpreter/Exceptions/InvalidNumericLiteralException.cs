using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealityProgrammer.CSStandard.Interpreter.Exceptions {
    public class InvalidNumericLiteralException : Exception {
        public InvalidNumericLiteralException(string msg) : base(msg) { }
    }
}
