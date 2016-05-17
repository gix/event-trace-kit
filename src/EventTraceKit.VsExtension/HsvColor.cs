namespace EventTraceKit.VsExtension
{
    public struct HsvColor
    {
        public HsvColor(double hue, double saturation, double value)
        {
            Hue = hue;
            Saturation = saturation;
            Value = value;
        }

        public double Hue { get; set; }
        public double Saturation { get; set; }
        public double Value { get; set; }
    }
}
