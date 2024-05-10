using ClientTableAnalizer.Objects.Parsing;

namespace ClientTableAnalizer.Objects.Repositories
{
    public class ParsingRowEntityRepo : BaseRepo<int, List<ParsingRowEntity>>
    {
        public override bool RetrieveAndUpdateOrAddValue(int id, List<ParsingRowEntity> item)
        {
            if (Entiies.TryGetValue(id, out List<ParsingRowEntity>? retrievedValue) && retrievedValue is not null)
            {
                List<ParsingRowEntity> newCollection = new(retrievedValue);
                newCollection.ForEach(newCollection.Add);

                return Entiies.TryUpdate(id, retrievedValue, newCollection); 
            }
            else
            {
                return Entiies.TryAdd(id, item);
            }
        }
    }
}