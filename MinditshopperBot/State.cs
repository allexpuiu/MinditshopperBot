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
        CHOOSE_CATEGORY_ITEM = 2,
        CHOOSE_RECOMMENDED_ITEM = 3
    }
}
