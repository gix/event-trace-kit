namespace NOpt
{
    using System;

    [Flags]
    public enum OptionFlag
    {
        HiddenHelp = (1 << 0),
        RenderAsInput = (1 << 1),
        RenderJoined = (1 << 2),
        RenderSeparate = (1 << 3),
    }
}
