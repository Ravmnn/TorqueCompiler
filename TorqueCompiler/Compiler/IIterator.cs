using System.Collections.Generic;


namespace Torque.Compiler;




public interface IIterator<T> where T : notnull
{
    int Current { get; set; }


    IReadOnlyList<T> Source { get; }




    T Advance()
    {
        if (AtEnd())
            return Previous();

        return Source[Current++];
    }


    T Previous(int amount = 1)
    {
        if (Current <= amount - 1)
            return Peek();

        return Source[Current - amount];
    }

    T Peek()
        => AtEnd() ? Previous() : Source[Current];

    T Next(int amount = 1)
    {
        if (Current + amount >= Source.Count)
            return Peek();

        return Source[Current + amount];
    }


    bool AtEnd() => Current >= Source.Count;
}
