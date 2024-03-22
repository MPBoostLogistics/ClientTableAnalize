using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects
{
    public struct ArticleShortage
    {
        #region Properties
        public int ArticleNumber { get; }
        public string ArtileName { get; set; }
        public double AveragePrice { get; set; }
        public Dictionary<ShortagesPlace, int> Shortages { get; set; } = new();
        #endregion

        #region Constructors
        public ArticleShortage(int articleNumber, string artileName, double averagePrice)
        {
            ArticleNumber = articleNumber; ArtileName = artileName; AveragePrice = averagePrice;
        }

        public readonly void SetArticleShortage(ShortagesPlace shortagesPlace, int shortagesCount)
        {
            if (Shortages.ContainsKey(shortagesPlace))
            {
                Shortages[shortagesPlace] += shortagesCount;
            }
            else
            {
                Shortages[shortagesPlace] = shortagesCount;
            }
        }

        /// <summary>
        /// Returns amount of goods underdelivered at place.
        /// </summary>
        /// <param name="shortagesPlace">Place of under-delivery formation.</param>
        /// <returns></returns>
        public readonly int GetShortagesCountByPlace(ShortagesPlace shortagesPlace)
        {
            return Shortages
                .Where(el => el.Key == shortagesPlace)
                .Aggregate(0, (acc, el) => acc + el.Value);
        }
        #endregion
    }
}