struct Conversion
{
    int value;
    public Conversion(int v)
    {
        value = v;
    }

    static public implicit operator Conversion(int value) => new Conversion(value);
    static public explicit operator int(Conversion roman) => roman.value;
    static public implicit operator string(Conversion roman) => "Conversion not yet implemented";

    static void Main(string[] args)
    {
    }
}