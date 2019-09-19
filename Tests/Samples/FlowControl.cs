class FlowControl
{
    public static int Switch(int n) {
        switch(n) {
            case 0:
            case 1:
            case 2:
                return 0;
            case 3:
            case 5:
            case 6:
                return 1;
            default:
                return -1;
        }
    }

    public static void IfElse() {
        if (new object() != null) {
            if (new object() == null) { }
        } else if (new object() != null) {
        } else {
        }
    }

    public static void Loops() {
        for (var i = 0; i < 10; i++) {
            continue;
        }
        foreach (var i in new int[10]) {
            break;
        }
        while (false) { }
        do { } while (false);
    }

    static void Main()
    {
    }
}