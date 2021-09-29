using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealityProgrammer.CSStandard.Interpreter.Exceptions {
    public class BinaryOperatorNotExistsException : Exception {
        public BinaryOperatorNotExistsException(string msg, object left, string @operator, object right) : base(msg + ". " + (left == null ? "Null" : left.GetType().FullName) + " " + @operator + " " + (right == null ? "Null" : right.GetType().FullName) + ".") {
        }
    }
}
