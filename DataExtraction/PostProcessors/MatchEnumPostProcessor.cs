using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class MatchEnumPostProcessor : PostProcessor
    {
        public readonly StringWithDependencies EnumTypeName;
        private readonly Type EnumType;
        private readonly IDictionary<string, int> EnumConstants;

        public MatchEnumPostProcessor(StringWithDependencies enumTypeName)
        {
            EnumTypeName = enumTypeName;
            EnumType = Type.GetType(EnumTypeName);

            var enumConstantNames = Enum.GetNames(EnumType);
            var enumConstantValues = Enum.GetValues(EnumType);

            EnumConstants = new Dictionary<string, int>();
            for (var i = 0; i < enumConstantNames.Length; i++)
            {
                EnumConstants.Add(enumConstantNames[i], (int)enumConstantValues.GetValue(i));
            }
        }

        public override IEnumerable<string> Execute(string value)
        {
            if (EnumConstants.ContainsKey(value))
                yield return value;
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return EnumTypeName;
        }
    }
}
