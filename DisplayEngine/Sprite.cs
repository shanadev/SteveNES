using System;
using System.Drawing;
using SDL2;
namespace DisplayEngine
{
    // Sprite class
    // - read from file
    // - draw to screen at x y pos
    // - flip horizontally or vertically
    // 
    public class Sprite
    {
        private List<ScreenColor> colorData = new List<ScreenColor>();

        public int Width { get; }
        public int Height { get; }
        public int DataSize { get { return this.Width * this.Height; } }

        public Sprite(int width, int height)
        {
            // store all blank data - 0,0,0,0
            this.Width = width;
            this.Height = height;
            colorData.AddRange(Enumerable.Repeat<ScreenColor>(new ScreenColor(0, 0, 0, 0), this.DataSize));
        }

        public Sprite(string filename)
        {
            if (!LoadFromFile(filename))
            {
                // couldn't load
                Console.WriteLine("Couldn't Load");
                colorData.Clear();
            }
        }

        public bool LoadFromFile(string filename)
        {
            //int flags = (int)SDL_image.IMG_InitFlags.IMG_INIT_PNG;
            //int initted = SDL_image.IMG_Init((SDL_image.IMG_InitFlags)flags);
            //if ((initted & flags) != flags)
            //{
            //    Console.WriteLine("Problem");
            //}
            //else
            //{
            //    unsafe
            //    {
            //        SDL.SDL_Surface surface = (SDL.SDL_Surface)SDL_image.IMG_Load(filename);
            //        int bytesPerPixel = surface.format ;

            //    }


            //}

            //(int)(SDL_image.IMG_Init(flags) & flags)


            //if (!(SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG) & SDL_image.IMG_InitFlags.IMG_INIT_PNG))
            //{

            //}


            //Bitmap newSprite = new Bitmap(filename);
            //for (int x = 0; x < this.Width; x++)
            //{
            //    for (int y = 0; y < this.Height; y++)
            //    {
            //        Color samp = newSprite.GetPixel(x, y);
            //        this.colorData.Add(new ScreenColor(samp.R, samp.G, samp.B, samp.A));
            //    }
            //}
            //return false;
            return false;
        }

        public List<ScreenColor> GetData()
        {
            return colorData;
        }

        public ScreenColor GetPixel(int x, int y)
        {
            return this.colorData[this.Width * y + x];
        }

        public bool SetPixel(int x, int y, ScreenColor c)
        {
            if (x < this.Width && x >=0 && y < this.Height && y >= 0)
            {
                this.colorData[this.Width * y + x] = c;
                return true;
            }
            else
            {
                return false;
            }
        }

        ~Sprite()
        {
            colorData.Clear();
        }
    }
}

