namespace ChattyApp
{
    public static class Extensions
    {
        public static Color ConvertToMauiColor(this System.Drawing.Color color)
        {
            var alpha = color.A / (float)255.0;
            var red = color.R / (float)255.0;
            var green = color.G / (float)255.0;
            var blue = color.B / (float)255.0;

            return new Color(red, green, blue, alpha);
        }
    }
}
