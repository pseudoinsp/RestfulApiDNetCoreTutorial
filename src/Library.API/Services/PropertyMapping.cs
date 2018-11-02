using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Services
{
    public class PropertyMapping<Tsoruce, TDestination> : IPropertyMapping
    {
        public PropertyMapping(Dictionary<string, PropertyMappingValue> mappingDictionary)
        {
            _mappingDictionary = mappingDictionary;
        }

        public Dictionary<string, PropertyMappingValue> _mappingDictionary { get; private set; }
    }
}
