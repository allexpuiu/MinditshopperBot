using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinditshopperBot
{
    public enum State
    {
        START = 0,
        CHOOSE_CATEGORY = 1,
        SELECTED_CATEGORY = 2,
        CHOOSE_CATEGORY_ITEM = 3,
        SELECTED_CATEGORY_ITEM = 4,
        CHOOSE_RECOMMENDED_ITEM = 5,
        SELECTED_RECOMMENDED_ITEM = 6,
        END = 7
    }
}
