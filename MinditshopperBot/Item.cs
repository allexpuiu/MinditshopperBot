using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinditshopperBot
{
    public class Item
    {
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string CategoryCode { get; set; }
        public string Category { get; set; }
        public double SalesValue { get; set; }
        public int ItemRank { get; set; }
        public decimal RecommendationScore { get; set; }
    }
}
