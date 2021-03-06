﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;
using System.Globalization;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    using ParsePostProcessors;
    using Rezgar.Crawler.DataExtraction.Dependencies;
    using Utils.Parsing;
    using Utils.Parsing.Parsers;

    public class SumPostProcessor : ParsePostProcessor<decimal?>
    {
        public SumPostProcessor() : base(new DecimalParser("F")) { }

        public override IEnumerable<string> Execute(IEnumerable<decimal?> values, DependencyDataSource dependencyDataSource)
        {
            yield return ToString(values.Sum());
        }

        public override IEnumerable<string> Execute(decimal? value, DependencyDataSource dependencyDataSource)
        {
            throw new NotSupportedException();
        }
    }
}
