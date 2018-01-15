using Rezgar.Utils.Collections;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Scripting
{
    public class CustomScriptAction
    {
        public enum ActionParamType
        {
            Constant,
            Reference,
            StackValue
        }

        public class ActionParam
        {
            public readonly ActionParamType Type;
            public readonly string Value;

            private const string StackValueReference = "_"; //NOTE: {_} signifies reference to item rule stack value
            private static readonly Regex ReferenceRegex = new Regex(@"^{(\w+?)}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            public ActionParam(string param)
            {
                var match = ReferenceRegex.Match(param);
                if (match.Success)
                {
                    var refParam = match.Groups[1].Value;

                    if (refParam == StackValueReference)
                    {
                        Type = ActionParamType.StackValue;
                    }
                    else
                    {
                        Type = ActionParamType.Reference;
                        Value = refParam;
                    }
                }
                else
                {
                    Type = ActionParamType.Constant;
                    Value = param;
                }
            }

            public ActionParam(ActionParamType type, string value)
            {
                Type = type;
                Value = value;
            }

            public string GetValue(string stackValue)
            {
                switch (Type)
                {
                    case ActionParamType.Constant:
                        return Value;
                    //case ActionParamType.Reference:
                    //    return extractedItems.GetNonEmptyValue(Value);
                    case ActionParamType.StackValue:
                        return stackValue;
                    default:
                        Debug.Assert(false);
                        break;
                }

                return null;
            }
        }

        public readonly string Id;
        public readonly string Language;
        public readonly string Code;

        private string Class;
        public Assembly Assembly { get; private set; }
        public MethodInfo Method { get; private set; }
        public Type Type { get; private set; }
        public object Instance { get; private set; }
        
        public CustomScriptAction(string id, string language, string code)
        {
            Id = id;
            Language = language;
            Code = code;
            
            Class = string.Format("{0}{1}", Id, "Class");

            var provider = CodeDomProvider.CreateProvider(Language);
            var info = CodeDomProvider.GetCompilerInfo(Language);

            var parameters = info.CreateDefaultCompilerParameters();
            parameters.MainClass = Class;
            parameters.IncludeDebugInformation = false;
            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);

            var classCode = string.Format(@"class {0} {{ {1} }}", Class, Code);
            var compiled = provider.CompileAssemblyFromSource(parameters, classCode);

            #region Log compiler output
            if (compiled.Errors.Count > 0)
            {
                foreach (var error in compiled.Errors)
                    Trace.TraceError(error.ToString());

                throw new Exception();
            }

            foreach (var output in compiled.Output)
                Trace.TraceInformation(output);
            #endregion

            Assembly = compiled.CompiledAssembly;
            Type = Assembly.GetType(parameters.MainClass);
            Instance = Activator.CreateInstance(Type);
            Method = Type.GetMethod(Id);
        }
        
        public static IEnumerable<ActionParam> ParseParameters(string parameters)
        {
            return parameters.Split(',')
                .Select(param => new ActionParam(param));
        }

        public IEnumerable<string> Execute(IEnumerable<ActionParam> parameters, string stackValue)
        {
            var execParams = parameters
                .Select(pred => pred.GetValue(stackValue))
                .ToArray();

            return Execute(execParams);
        }

        private IEnumerable<string> Execute(object[] parameters)
        {
            dynamic result = Method.Invoke(Instance, parameters);
            if (result == null)
                yield break;

            var enumerable = result as System.Collections.IEnumerable;
            if (enumerable != null)
            {
                foreach (var obj in enumerable)
                {
                    var value = result[obj];
                    if (value != null)
                        yield return value.ToString();
                }
            }
            else
                yield return result.ToString();
        }
    }
}
