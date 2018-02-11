using Rezgar.Crawler.DataExtraction.Dependencies;
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

        public sealed override IEnumerable<string> Execute(IEnumerable<string> values, DependencyDataSource dependencyDataSource)
        {
            return Execute(values.Select(_dataParser.Parse), dependencyDataSource);
        }
        public sealed override IEnumerable<string> Execute(string value, DependencyDataSource dependencyDataSource)
        {
            return Execute(_dataParser.Parse(value), dependencyDataSource);
        }

        public virtual IEnumerable<string> Execute(IEnumerable<TDataType> values, DependencyDataSource dependencyDataSource)
        {
            return values.SelectMany(value => Execute(value, dependencyDataSource));
        }

        public virtual IEnumerable<string> Execute(TDataType value, DependencyDataSource dependencyDataSource)
        {
            yield return _dataParser.ToString(value);
        }

        protected string ToString(TDataType data)
        {
            return _dataParser.ToString(data);
        }
    }
}
