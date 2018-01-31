using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Rezgar.Utils.Parsing;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class ParsePostProcessor<TDataType> : PostProcessor
    {
        private readonly IParser<TDataType> _dataParser;

        public ParsePostProcessor(IParser<TDataType> dataParser)
        {
            _dataParser = dataParser;
        }

        public sealed override IEnumerable<string> Execute(IEnumerable<string> values)
        {
            return Execute(values.Select(_dataParser.Parse));
        }
        public sealed override IEnumerable<string> Execute(string value)
        {
            return Execute(_dataParser.Parse(value));
        }

        public virtual IEnumerable<string> Execute(IEnumerable<TDataType> values)
        {
            return values.SelectMany(Execute);
        }

        public virtual IEnumerable<string> Execute(TDataType value)
        {
            yield return _dataParser.ToString(value);
        }

        protected string ToString(TDataType data)
        {
            return _dataParser.ToString(data);
        }
    }
}
