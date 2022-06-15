using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Pair<T, U>
{
    public T first { get; set; }
    public U second { get; set; }

    public Pair()
    {
        first = default;
        second = default;
    }

    public Pair(T First, U Second)
    {
        first = First;
        second = Second;
    }
}