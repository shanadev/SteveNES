using SDL2;
using System;
namespace DisplayEngine
{
    public struct ScreenColor
    {
        public byte red;
        public byte green;
        public byte blue;
        public byte alpha;

        public ScreenColor(byte r, byte g, byte b, byte a)
        {
            red = r;
            green = g;
            blue = b;
            alpha = a;
        }
    }

    public enum WindowSettingTypes
    {
        NES,
        NES_Double,
        NES_Triple,
        SD,
        SD_Double,
        HD,
        HD_Double,
        FullHD,
        QuadHD,
        UHD,
        FullUHD
    }

    public struct WindowSize
    {
        public int Width;
        public int Height;
        public int PixelSize;

        public WindowSize(int width, int height, int pixelSize)
        {
            this.Width = width;
            this.Height = height;
            this.PixelSize = pixelSize;
        }

        public float AspectRatio
        {
            get { return (float)Height / (float)Width; }
        }
    }

}

