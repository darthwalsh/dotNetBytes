using System;

public static class Event
{
    static event Action A;
    static event Action B { add { A += value; } remove { A -= value; } }
    static event Action C = () => { };

    static void Main()
    {
        B += C;
        A();
    }
}