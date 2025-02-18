using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.Repositories
{
    public class ParsingRowEntityRepo : BaseRepo<int, List<ParsingRowEntity>>
    {
        public override bool RetrieveAndUpdateOrAddValue(int id, List<ParsingRowEntity> item, bool isMulty = false)
        {
            if (Entiies.TryGetValue(id, out List<ParsingRowEntity>? retrievedValue) && retrievedValue is not null)
            {
                List<ParsingRowEntity> newValue = [.. retrievedValue, .. item];
                return Entiies.TryUpdate(id, retrievedValue, newValue); 
            }
            else
            {
                return Entiies.TryAdd(id, item);
            }
        }
    }
}