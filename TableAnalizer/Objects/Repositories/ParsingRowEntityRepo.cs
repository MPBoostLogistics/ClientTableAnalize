using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.Repositories
{
    public class ParsingRowEntityRepo : BaseRepo<int, List<ParsingRowEntity>>
    {
        public override bool RetrieveAndUpdateOrAddValue(int id, List<ParsingRowEntity> item)
        {
            if (Entiies.TryGetValue(id, out List<ParsingRowEntity>? retrievedValue) && retrievedValue is not null)
            {
                List<ParsingRowEntity> newValue = new(retrievedValue);
                newValue.AddRange(item);
                return Entiies.TryUpdate(id, retrievedValue, newValue); 
            }
            else
            {
                return Entiies.TryAdd(id, item);
            }
        }
    }
}