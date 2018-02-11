using Rezgar.Crawler.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;
using Rezgar.Crawler.DataExtraction.Dependencies;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    /// <summary>
    /// TODO: In case if required, migrate code for providing custom parameters
    /// </summary>
    public class EvalJavaScriptPostProcessor : PostProcessor
    {
        public static readonly CustomScriptAction.ActionParam[] EvalParams = new[] { new CustomScriptAction.ActionParam(CustomScriptAction.ActionParamType.StackValue, string.Empty) };
        public static readonly CustomScriptAction EvalAction = new CustomScriptAction("Eval", "javascript", JsEval);

        const string JsEval = @"function Eval(expr) { return eval(expr); }";

        public override IEnumerable<string> Execute(string value, DependencyDataSource dependencyDataSource)
        {
            return EvalAction.Execute(EvalParams, value);
        }
    }
}
