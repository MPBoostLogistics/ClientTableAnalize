using System.Diagnostics;
using Org.BouncyCastle.Asn1.X509.Qualified;
using TableAnalizer.Objects.Parsing;
using TableAnalizer.Objects.SKUItems.SKUBalances;

namespace TableAnalizer.Objects.Repositories
{
    public class ParsingRowEntityRepo : BaseRepo<int, List<ParsingRowEntity>>
    {
        public override bool RetrieveAndUpdateOrAddValue(int id, List<ParsingRowEntity> item, bool isMulty = false) =>
            Entiies.TryGetValue(id, out List<ParsingRowEntity>? retrievedValue) && retrievedValue is not null ? 
            Entiies.TryUpdate(id, retrievedValue, item) : 
            Entiies.TryAdd(id, item);
    }
}