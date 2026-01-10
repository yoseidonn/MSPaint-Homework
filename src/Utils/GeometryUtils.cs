namespace MSPaint.Utils
{
    public static class GeometryUtils
    {
        // helper to swap
        public static void Order(ref int a, ref int b)
        {
            if (a > b) { var t = a; a = b; b = t; }
        }
    }
}