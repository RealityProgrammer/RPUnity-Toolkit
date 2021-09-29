using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

// Code collected from: http://wiki.unity3d.com/index.php?title=ExpressionParser&oldid=19546
// Adapted and optimized by RealityProgrammer

#pragma warning disable IDE0044
#pragma warning disable IDE0090

namespace RealityProgrammer.UnityToolkit.Core.Utility {
    #region Classes
    public interface IValue {
        double Value { get; }
    }

    public class Number : IValue {
        private double m_Value;
        public double Value {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public Number(double aValue) {
            m_Value = aValue;
        }

        public override string ToString() {
            return "" + m_Value + "";
        }
    }

    public class OperationSum : IValue {
        private IValue[] m_Values;

        public double Value {
            get { return m_Values.Select(v => v.Value).Sum(); }
        }

        public OperationSum(params IValue[] aValues) {
            // collapse unnecessary nested sum operations.
            List<IValue> v = new List<IValue>(aValues.Length);
            foreach (var I in aValues) {
                if (I is OperationSum sum)
                    v.AddRange(sum.m_Values);
                else
                    v.Add(I);
            }
            m_Values = v.ToArray();
        }

        public override string ToString() {
            return "( " + string.Join(" + ", m_Values.Select(v => v.ToString()).ToArray()) + " )";
        }
    }
    public class OperationProduct : IValue {
        private IValue[] m_Values;

        public double Value {
            get { return m_Values.Select(v => v.Value).Aggregate((v1, v2) => v1 * v2); }
        }

        public OperationProduct(params IValue[] aValues) {
            m_Values = aValues;
        }

        public override string ToString() {
            return "( " + string.Join(" * ", m_Values.Select(v => v.ToString()).ToArray()) + " )";
        }
    }
    public class OperationPower : IValue {
        private IValue m_Value;
        private IValue m_Power;

        public double Value {
            get { return Math.Pow(m_Value.Value, m_Power.Value); }
        }

        public OperationPower(IValue aValue, IValue aPower) {
            m_Value = aValue;
            m_Power = aPower;
        }

        public override string ToString() {
            return "( " + m_Value + "^" + m_Power + " )";
        }
    }
    public class OperationNegate : IValue {
        private IValue m_Value;

        public double Value {
            get { return -m_Value.Value; }
        }

        public OperationNegate(IValue aValue) {
            m_Value = aValue;
        }

        public override string ToString() {
            return "( -" + m_Value + " )";
        }
    }
    public class OperationReciprocal : IValue {
        private IValue m_Value;

        public double Value {
            get { return 1.0 / m_Value.Value; }
        }
        public OperationReciprocal(IValue aValue) {
            m_Value = aValue;
        }
        public override string ToString() {
            return "( 1/" + m_Value + " )";
        }
    }

    public class MultiParameterList : IValue {
        private IValue[] m_Values;

        public IValue[] Parameters { get { return m_Values; } }

        public double Value {
            get { return m_Values.Select(v => v.Value).FirstOrDefault(); }
        }

        public MultiParameterList(params IValue[] aValues) {
            m_Values = aValues;
        }

        public override string ToString() {
            return string.Join(", ", m_Values.Select(v => v.ToString()).ToArray());
        }
    }

    public class CustomFunction : IValue {
        private IValue[] m_Params;
        private Func<double[], double> m_Delegate;
        private string m_Name;

        public double Value {
            get {
                if (m_Params == null)
                    return m_Delegate(null);
                return m_Delegate(m_Params.Select(p => p.Value).ToArray());
            }
        }
        public CustomFunction(string aName, Func<double[], double> aDelegate, params IValue[] aValues) {
            m_Delegate = aDelegate;
            m_Params = aValues;
            m_Name = aName;
        }
        public override string ToString() {
            if (m_Params == null)
                return m_Name;
            return m_Name + "( " + string.Join(", ", m_Params.Select(v => v.ToString()).ToArray()) + " )";
        }
    }
    public class Parameter : Number {
        public string Name { get; private set; }

        public override string ToString() {
            return Name + "[" + base.ToString() + "]";
        }

        public Parameter(string aName) : base(0) {
            Name = aName;
        }
    }

    public class Expression : IValue {
        public Dictionary<string, Parameter> Parameters = new Dictionary<string, Parameter>();

        public IValue ExpressionTree { get; set; }

        public double Value {
            get { return ExpressionTree.Value; }
        }

        public double[] MultiValue {
            get {
                if (ExpressionTree is MultiParameterList t) {
                    double[] res = new double[t.Parameters.Length];
                    for (int i = 0; i < res.Length; i++)
                        res[i] = t.Parameters[i].Value;
                    return res;
                }
                return null;
            }
        }

        public override string ToString() {
            return ExpressionTree.ToString();
        }

        public ExpressionDelegate ToDelegate(params string[] aParamOrder) {
            var parameters = new List<Parameter>(aParamOrder.Length);
            for (int i = 0; i < aParamOrder.Length; i++) {
                if (Parameters.ContainsKey(aParamOrder[i]))
                    parameters.Add(Parameters[aParamOrder[i]]);
                else
                    parameters.Add(null);
            }
            var parameters2 = parameters.ToArray();

            return (p) => Invoke(p, parameters2);
        }

        public MultiResultDelegate ToMultiResultDelegate(params string[] aParamOrder) {
            var parameters = new List<Parameter>(aParamOrder.Length);
            for (int i = 0; i < aParamOrder.Length; i++) {
                if (Parameters.ContainsKey(aParamOrder[i]))
                    parameters.Add(Parameters[aParamOrder[i]]);
                else
                    parameters.Add(null);
            }
            var parameters2 = parameters.ToArray();


            return (p) => InvokeMultiResult(p, parameters2);
        }

        double Invoke(double[] aParams, Parameter[] aParamList) {
            int count = Math.Min(aParamList.Length, aParams.Length);
            for (int i = 0; i < count; i++) {
                if (aParamList[i] != null)
                    aParamList[i].Value = aParams[i];
            }
            return Value;
        }

        double[] InvokeMultiResult(double[] aParams, Parameter[] aParamList) {
            int count = Math.Min(aParamList.Length, aParams.Length);
            for (int i = 0; i < count; i++) {
                if (aParamList[i] != null)
                    aParamList[i].Value = aParams[i];
            }
            return MultiValue;
        }

        public static Expression Parse(string aExpression) {
            return new EquationCalculator().EvaluateExpression(aExpression);
        }

        public class ParameterException : Exception { public ParameterException(string aMessage) : base(aMessage) { } }
    }

    public delegate double ExpressionDelegate(params double[] aParams);
    public delegate double[] MultiResultDelegate(params double[] aParams);
    #endregion

    public class EquationCalculator {
        private List<string> m_BracketHeap = new List<string>();
        private Dictionary<string, Func<double>> m_Consts = new Dictionary<string, Func<double>>();
        private Dictionary<string, Func<double[], double>> m_Funcs = new Dictionary<string, Func<double[], double>>();
        private Expression m_Context;

        private static readonly Dictionary<string, Func<double>> buildInConsts = new Dictionary<string, Func<double>> {
            { "PI", () => Math.PI },
            { "e", () => Math.E },
            { "Sqrt2", () => 1.41421356237309504880168872420969807856967187537694807317667973799 },
            { "Sqrt3", () => 1.732050807568877293527446341505872366942805253810380628055806 },
            { "GoldenRatio", () => 1.6180339887498948482 },
            { "ConwaysConst", () => 0.5772156649015328606065120900824024310421 },
            { "Omega", () => 0.56714329040978387299996866221035554 },
            { "Deg2Rad", () => 0.01745329252 },
            { "Rad2Deg", () => 57.295779513 },
        };

        private static readonly Dictionary<string, Func<double[], double>> buildInFuncs = new Dictionary<string, Func<double[], double>>() {
            { "Sqrt", (p) => Math.Sqrt(p.FirstOrDefault()) },
            { "Abs", (p) => Math.Abs(p.FirstOrDefault()) },
            { "ln", (p) => Math.Log(p.FirstOrDefault()) },
            { "Floor", (p) => Math.Floor(p.FirstOrDefault()) },
            { "Ceiling", (p) => Math.Ceiling(p.FirstOrDefault()) },
            { "Round", (p) => Math.Round(p.FirstOrDefault()) },

            { "Sin", (p) => Math.Sin(p.FirstOrDefault()) },
            { "Cos", (p) => Math.Cos(p.FirstOrDefault()) },
            { "Tan", (p) => Math.Tan(p.FirstOrDefault()) },
            { "Cot", (p) => 1 / Math.Tan(p.FirstOrDefault()) },

            { "Asin", (p) => Math.Asin(p.FirstOrDefault()) },
            { "Acos", (p) => Math.Acos(p.FirstOrDefault()) },
            { "Atan", (p) => Math.Atan(p.FirstOrDefault()) },
            { "Atan2", (p) => Math.Atan2(p.FirstOrDefault(), p.ElementAtOrDefault(1)) },

            { "Min", (p) => Math.Min(p.FirstOrDefault(), p.ElementAtOrDefault(1)) },
            { "Max", (p) => Math.Max(p.FirstOrDefault(), p.ElementAtOrDefault(1)) },
        };

        public void AddFunc(string aName, Func<double[], double> aMethod) {
            if (m_Funcs.ContainsKey(aName))
                m_Funcs[aName] = aMethod;
            else
                m_Funcs.Add(aName, aMethod);
        }

        public void AddConst(string aName, Func<double> aMethod) {
            if (m_Consts.ContainsKey(aName))
                m_Consts[aName] = aMethod;
            else
                m_Consts.Add(aName, aMethod);
        }

        public void RemoveFunc(string aName) {
            if (m_Funcs.ContainsKey(aName))
                m_Funcs.Remove(aName);
        }

        public void RemoveConst(string aName) {
            if (m_Consts.ContainsKey(aName))
                m_Consts.Remove(aName);
        }

        int FindClosingBracket(ref string aText, int aStart, char aOpen, char aClose) {
            int counter = 0;
            for (int i = aStart; i < aText.Length; i++) {
                if (aText[i] == aOpen)
                    counter++;
                if (aText[i] == aClose)
                    counter--;
                if (counter == 0)
                    return i;
            }
            return -1;
        }

        void SubstitudeBracket(ref string aExpression, int aIndex) {
            int closing = FindClosingBracket(ref aExpression, aIndex, '(', ')');
            if (closing > aIndex + 1) {
                string inner = aExpression.Substring(aIndex + 1, closing - aIndex - 1);
                m_BracketHeap.Add(inner);
                string sub = "&" + (m_BracketHeap.Count - 1) + ";";
                aExpression = aExpression.Substring(0, aIndex) + sub + aExpression.Substring(closing + 1);
            } else throw new ParseException("Bracket not closed!");
        }

        IValue Parse(string aExpression) {
            aExpression = aExpression.Trim();
            int index = aExpression.IndexOf('(');
            while (index >= 0) {
                SubstitudeBracket(ref aExpression, index);
                index = aExpression.IndexOf('(');
            }

            if (aExpression.Contains(',')) {
                string[] parts = aExpression.Split(',');
                List<IValue> exp = new List<IValue>(parts.Length);
                for (int i = 0; i < parts.Length; i++) {
                    string s = parts[i].Trim();
                    if (!string.IsNullOrEmpty(s))
                        exp.Add(Parse(s));
                }

                return new MultiParameterList(exp.ToArray());
            } else if (aExpression.Contains('+')) {
                string[] parts = aExpression.Split('+');
                List<IValue> exp = new List<IValue>(parts.Length);

                for (int i = 0; i < parts.Length; i++) {
                    string s = parts[i].Trim();
                    if (!string.IsNullOrEmpty(s))
                        exp.Add(Parse(s));
                }
                if (exp.Count == 1)
                    return exp[0];

                return new OperationSum(exp.ToArray());
            } else if (aExpression.Contains('-')) {
                string[] parts = aExpression.Split('-');
                List<IValue> exp = new List<IValue>(parts.Length);

                if (!string.IsNullOrEmpty(parts[0].Trim()))
                    exp.Add(Parse(parts[0]));

                for (int i = 1; i < parts.Length; i++) {
                    string s = parts[i].Trim();
                    if (!string.IsNullOrEmpty(s))
                        exp.Add(new OperationNegate(Parse(s)));
                }
                if (exp.Count == 1)
                    return exp[0];

                return new OperationSum(exp.ToArray());
            } else if (aExpression.Contains('*')) {
                string[] parts = aExpression.Split('*');
                List<IValue> exp = new List<IValue>(parts.Length);

                for (int i = 0; i < parts.Length; i++) {
                    exp.Add(Parse(parts[i]));
                }
                if (exp.Count == 1)
                    return exp[0];

                return new OperationProduct(exp.ToArray());
            } else if (aExpression.Contains('/')) {
                string[] parts = aExpression.Split('/');
                List<IValue> exp = new List<IValue>(parts.Length);

                if (!string.IsNullOrEmpty(parts[0].Trim()))
                    exp.Add(Parse(parts[0]));
                for (int i = 1; i < parts.Length; i++) {
                    string s = parts[i].Trim();
                    if (!string.IsNullOrEmpty(s))
                        exp.Add(new OperationReciprocal(Parse(s)));
                }

                return new OperationProduct(exp.ToArray());
            } else if (aExpression.Contains('^')) {
                int pos = aExpression.IndexOf('^');
                var val = Parse(aExpression.Substring(0, pos));
                var pow = Parse(aExpression.Substring(pos + 1));
                return new OperationPower(val, pow);
            }
            int pPos = aExpression.IndexOf("&");
            if (pPos > 0) {
                string fName = aExpression.Substring(0, pPos);

                if (buildInFuncs.TryGetValue(fName, out var @delegate)) {
                    var inner = aExpression.Substring(fName.Length);
                    var param = Parse(inner);
                    IValue[] parameters;

                    if (param is MultiParameterList multiParams)
                        parameters = multiParams.Parameters;
                    else
                        parameters = new IValue[] { param };

                    return new CustomFunction(fName, @delegate, parameters);
                }

                if (m_Funcs.TryGetValue(fName, out @delegate)) {
                    var inner = aExpression.Substring(fName.Length);
                    var param = Parse(inner);
                    IValue[] parameters;

                    if (param is MultiParameterList multiParams)
                        parameters = multiParams.Parameters;
                    else
                        parameters = new IValue[] { param };

                    return new CustomFunction(fName, @delegate, parameters);
                }
            }

            if (buildInConsts.TryGetValue(aExpression, out var constDelegate)) {
                return new CustomFunction(aExpression, (p) => constDelegate(), null);
            }

            if (m_Consts.TryGetValue(aExpression, out constDelegate)) {
                return new CustomFunction(aExpression, (p) => constDelegate(), null);
            }

            int index2a = aExpression.IndexOf('&');
            int index2b = aExpression.IndexOf(';');

            if (index2a >= 0 && index2b >= 2) {
                var inner = aExpression.Substring(index2a + 1, index2b - index2a - 1);
                if (int.TryParse(inner, out int bracketIndex) && bracketIndex >= 0 && bracketIndex < m_BracketHeap.Count) {
                    return Parse(m_BracketHeap[bracketIndex]);
                } else
                    throw new ParseException("Can't parse substitude token");
            }

            if (double.TryParse(aExpression, out double doubleValue)) {
                return new Number(doubleValue);
            }

            if (ValidIdentifier(aExpression)) {
                if (m_Context.Parameters.ContainsKey(aExpression))
                    return m_Context.Parameters[aExpression];
                var val = new Parameter(aExpression);
                m_Context.Parameters.Add(aExpression, val);
                return val;
            }

            throw new ParseException("Reached unexpected end within the parsing tree");
        }

        private bool ValidIdentifier(string aExpression) {
            aExpression = aExpression.Trim();
            if (string.IsNullOrEmpty(aExpression))
                return false;
            if (aExpression.Length < 1)
                return false;
            if (aExpression.Contains(" "))
                return false;
            if (!"abcdefghijklmnopqrstuvwxyz§$".Contains(char.ToLower(aExpression[0])))
                return false;
            if (buildInConsts.ContainsKey(aExpression))
                return false;
            if (buildInFuncs.ContainsKey(aExpression))
                return false;
            if (m_Consts.ContainsKey(aExpression))
                return false;
            if (m_Funcs.ContainsKey(aExpression))
                return false;
            return true;
        }

        public Expression EvaluateExpression(string aExpression) {
            var val = new Expression();
            m_Context = val;
            val.ExpressionTree = Parse(aExpression);
            m_Context = null;
            m_BracketHeap.Clear();
            return val;
        }

        public double Evaluate(string aExpression) {
            return EvaluateExpression(aExpression).Value;
        }
        public static double Eval(string aExpression) {
            return new EquationCalculator().Evaluate(aExpression);
        }

        public class ParseException : Exception { public ParseException(string aMessage) : base(aMessage) { } }
    }
}

#pragma warning restore IDE0044
#pragma warning restore IDE0090