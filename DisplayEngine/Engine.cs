using SDL2;

namespace DisplayEngine;

// DisplayEngine
// Primary task is to provide a window with requested width height and pixel size
// along with override method for drawing a frame


public class Engine
{
    private IntPtr window = IntPtr.Zero;
    private IntPtr renderer = IntPtr.Zero;
    private bool quitFlag = false;
    private WindowSize windowSize;

    public delegate void RenderFrameDelegate();

    public event EventHandler<KeyEventArgs> KeyDown;
    public event EventHandler<KeyEventArgs> KeyUp;

    public ulong GetElapsedTime()
    {
        return SDL.SDL_GetTicks64();
    }

    protected virtual void OnKeyDown(KeyEventArgs e)
    {
        EventHandler<KeyEventArgs> handler = KeyDown;
        handler?.Invoke(this, e);
    }

    protected virtual void OnKeyUp(KeyEventArgs e)
    {
        EventHandler<KeyEventArgs> handler = KeyUp;
        handler?.Invoke(this, e);
    }

    public (int, int) GetMousePos()
    {
        int x, y;
        uint btn = SDL.SDL_GetMouseState(out x, out y);
        return (x / windowSize.PixelSize, y / windowSize.PixelSize);
    }


    private uint[] FontTiles = new uint[] {
        0x00000000, 0x00000000, 0x18181818, 0x00180018, 0x00003636, 0x00000000, 0x367F3636, 0x0036367F,
        0x3C067C18, 0x00183E60, 0x1B356600, 0x0033566C, 0x6E16361C, 0x00DE733B, 0x000C1818, 0x00000000,
        0x0C0C1830, 0x0030180C, 0x3030180C, 0x000C1830, 0xFF3C6600, 0x0000663C, 0x7E181800, 0x00001818,
        0x00000000, 0x0C181800, 0x7E000000, 0x00000000, 0x00000000, 0x00181800, 0x183060C0, 0x0003060C,
        0x7E76663C, 0x003C666E, 0x181E1C18, 0x00181818, 0x3060663C, 0x007E0C18, 0x3860663C, 0x003C6660,
        0x33363C38, 0x0030307F, 0x603E067E, 0x003C6660, 0x3E060C38, 0x003C6666, 0x3060607E, 0x00181818,
        0x3C66663C, 0x003C6666, 0x7C66663C, 0x001C3060, 0x00181800, 0x00181800, 0x00181800, 0x0C181800,
        0x06186000, 0x00006018, 0x007E0000, 0x0000007E, 0x60180600, 0x00000618, 0x3060663C, 0x00180018,

        0x5A5A663C, 0x003C067A, 0x7E66663C, 0x00666666, 0x3E66663E, 0x003E6666, 0x06060C78, 0x00780C06,
        0x6666361E, 0x001E3666, 0x1E06067E, 0x007E0606, 0x1E06067E, 0x00060606, 0x7606663C, 0x007C6666,
        0x7E666666, 0x00666666, 0x1818183C, 0x003C1818, 0x60606060, 0x003C6660, 0x0F1B3363, 0x0063331B,
        0x06060606, 0x007E0606, 0x6B7F7763, 0x00636363, 0x7B6F6763, 0x00636373, 0x6666663C, 0x003C6666,
        0x3E66663E, 0x00060606, 0x3333331E, 0x007E3B33, 0x3E66663E, 0x00666636, 0x3C0E663C, 0x003C6670,
        0x1818187E, 0x00181818, 0x66666666, 0x003C6666, 0x66666666, 0x00183C3C, 0x6B636363, 0x0063777F,
        0x183C66C3, 0x00C3663C, 0x183C66C3, 0x00181818, 0x0C18307F, 0x007F0306, 0x0C0C0C3C, 0x003C0C0C,
        0x180C0603, 0x00C06030, 0x3030303C, 0x003C3030, 0x00663C18, 0x00000000, 0x00000000, 0x003F0000,

        0x00301818, 0x00000000, 0x603C0000, 0x007C667C, 0x663E0606, 0x003E6666, 0x063C0000, 0x003C0606,
        0x667C6060, 0x007C6666, 0x663C0000, 0x003C067E, 0x0C3E0C38, 0x000C0C0C, 0x667C0000, 0x3C607C66,
        0x663E0606, 0x00666666, 0x18180018, 0x00301818, 0x30300030, 0x1E303030, 0x36660606, 0x0066361E,
        0x18181818, 0x00301818, 0x7F370000, 0x0063636B, 0x663E0000, 0x00666666, 0x663C0000, 0x003C6666,
        0x663E0000, 0x06063E66, 0x667C0000, 0x60607C66, 0x663E0000, 0x00060606, 0x063C0000, 0x003E603C,
        0x0C3E0C0C, 0x00380C0C, 0x66660000, 0x007C6666, 0x66660000, 0x00183C66, 0x63630000, 0x00367F6B,
        0x36630000, 0x0063361C, 0x66660000, 0x0C183C66, 0x307E0000, 0x007E0C18, 0x0C181830, 0x00301818,
        0x18181818, 0x00181818, 0x3018180C, 0x000C1818, 0x003B6E00, 0x00000000, 0x00000000, 0x00000000
    };

    private char[] NormalCharacters = new char[]
    {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',
        ':', ';', '<', '=', '>', '?', '@', '[', '\\', ']', '^', '_', '`', '{', '|', '}', '~', ' '
    };

    private byte[] textLookupTable = Enumerable.Repeat<byte>(0, 256).ToArray();


    


    public Engine(WindowSize winSize, string winTitle)
    {
        // Set internal privates for the engine

        // Window size info
        windowSize = winSize;

        // Set up lookup table for Drawing Text
        for (int i = 0; i < 96; i++)
        {
            textLookupTable[i + 32] = (byte)i;
        }

        // Initialize SDL, the window and the renderer
        try
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0) throw new ApplicationException(SDL.SDL_GetError());

            window = SDL.SDL_CreateWindow(winTitle,
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                winSize.Width,
                winSize.Height,
                SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS |
                SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS);

            if (window == IntPtr.Zero) throw new ApplicationException(SDL.SDL_GetError());

            renderer = SDL.SDL_CreateRenderer(window,
                -1,
                SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC |
                SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

            if (renderer == IntPtr.Zero) throw new ApplicationException(SDL.SDL_GetError());
        }
        catch (ApplicationException ex)
        {
            Console.WriteLine($"Problem Initializing Graphics: {ex.Message}");
        }

    }


    public void Run(RenderFrameDelegate renderFrame)
    {
        while (!quitFlag)
        {
            while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
            {
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        SDL.SDL_Quit();
                        quitFlag = true;
                        break;
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                        Console.WriteLine($"{e.key.keysym.sym}");
                        //KeyDown.Invoke(this, new KeyEventArgs() { KeyCode = e.key.keysym.sym.ToString() });
                        OnKeyDown(new KeyEventArgs() { KeyCode = e.key.keysym.sym.ToString() });
                        break;
                    case SDL.SDL_EventType.SDL_KEYUP:
                        //KeyUp.Invoke(this, new KeyEventArgs() { KeyCode = e.key.keysym.sym.ToString() });
                        OnKeyUp(new KeyEventArgs() { KeyCode = e.key.keysym.sym.ToString() });
                        break;
                }
            }

            SDL.SDL_RenderSetScale(renderer, windowSize.PixelSize, windowSize.PixelSize);

            renderFrame();
            SDL.SDL_RenderSetScale(renderer, windowSize.PixelSize, windowSize.PixelSize);

            SDL.SDL_RenderPresent(renderer);

            
        }

    }

    public void PixelDimensionTest(int newdim)
    {
        SDL.SDL_RenderSetScale(renderer, newdim, newdim);

    }

    public void Delay(ulong time)
    {
        SDL.SDL_Delay((uint)time);
    }

    public void Quit()
    {
        quitFlag = true;
        SDL.SDL_Quit();
    }

    // milliseconds since SDL library init
    public ulong GetTicks()
    {
        return SDL.SDL_GetTicks64();
    }


    // Drawing functions

    public void ClearScreen(ScreenColor c)
    {
        SDL.SDL_SetRenderDrawColor(renderer, c.red, c.green, c.blue, c.alpha);
        SDL.SDL_RenderClear(renderer);
    }

    public void DrawLine (int x1, int y1, int x2, int y2, ScreenColor c)
    {
        SDL.SDL_SetRenderDrawColor(renderer, c.red, c.green, c.blue, c.alpha);
        SDL.SDL_RenderDrawLine(renderer, x1, y1, x2, y2);
    }

    private SDL.SDL_Rect PrepQuad(int x1, int y1, int x2, int y2)
    {
        int tlx = 0, tly = 0, brx = 0, bry = 0;

        if (x1 > x2)
        {
            tlx = x2;
            brx = x1;
        }
        else
        {
            tlx = x1;
            brx = x2;
        }

        if (y1 > y2)
        {
            tly = y2;
            bry = y1;
        }
        else
        {
            tly = y1;
            bry = y2;
        }

        SDL.SDL_Rect rectum;
        rectum.x = tlx;
        rectum.y = tly;
        rectum.w = brx - tlx;
        rectum.h = bry - tly;

        return rectum;
    }

    public void DrawQuad (int x1, int y1, int x2, int y2, ScreenColor c)
    {
        SDL.SDL_Rect rectum = PrepQuad(x1,y1,x2,y2);
        SDL.SDL_SetRenderDrawColor(renderer, c.red, c.green, c.blue, c.alpha);
        SDL.SDL_RenderDrawRect(renderer, ref rectum);
    }

    public void DrawQuadFilled (int x1, int y1, int x2, int y2, ScreenColor c)
    {
        SDL.SDL_Rect rectum = PrepQuad(x1, y1, x2, y2);
        SDL.SDL_SetRenderDrawColor(renderer, c.red, c.green, c.blue, c.alpha);
        SDL.SDL_RenderFillRect(renderer, ref rectum);
    }

    public void DrawPixel(int x, int y, ScreenColor col)
    {
        SDL.SDL_SetRenderDrawColor(renderer, col.red, col.green, col.blue, col.alpha);
        SDL.SDL_RenderDrawPoint(renderer, x, y);
    }

    public void DrawSprite(Sprite spr, int x, int y, Flip flip)
    {
        int fxs = 0, fxm = 1, fx = 0;
        int fys = 0, fym = 1, fy = 0;

        if (flip == Flip.HORIZ) { fxs = spr.Width - 1; fxm = -1; }
        if (flip == Flip.VERT) { fys = spr.Width - 1; fym = -1; }

        fx = fxs;
        for (int i = 0; i < spr.Width; i++, fx += fxm)
        {
            fy = fys;
            for (int j = 0; j < spr.Height; j++, fy += fym)
            {
                ScreenColor px = spr.GetPixel(fx, fy);
                DrawPixel(x + i, y + j, px);

            }
        }

    }

    public void DrawPartialSprite(Sprite spr, int x, int y, int imgX, int imgY, int width, int height)
    {

    }

    private void DrawCharTile(int x, int y, int tileIndex, ScreenColor color)
    {

        byte[] a = BitConverter.GetBytes(FontTiles[tileIndex]);
        byte[] b = BitConverter.GetBytes(FontTiles[tileIndex+1]);
        byte[] c = a.Concat(b).ToArray();

        for (int iy = 0; iy < 8; iy++)
        {
            var row = c[iy];
            var ix = x;
            while (row > 0)
            {
                if ((byte)(row & 1) == 1)
                {
                    DrawPixel(ix, iy + y, color);
                }
                row >>= 1;
                ix += 1;
            }
        }
    }

    private void DrawChar(int x, int y, int charNum, ScreenColor color)
    {
        int tileIndex = 2 * textLookupTable[charNum];
        DrawCharTile(x, y, tileIndex, color);
        //DrawCharTile(x, y * 4, tileIndex + 1);
    }

    public void DrawText(int x, int y, string text, ScreenColor? color)
    {
        int xpos = x;
        int ypos = y;
        ScreenColor col = color ?? new ScreenColor(255, 255, 255, 255);


        foreach (var chr in text)
        {
            switch (chr)
            {
                case '\n':
                    ypos += 8;
                    xpos = x;
                    break;
                case '\t':
                    xpos += 24;
                    break;
                default:
                    if (NormalCharacters.Contains(chr))
                    {
                        int charNum = (int)chr;
                        DrawChar(xpos, ypos, charNum, col);
                        xpos += 8;
                    }
                    break;
            }
        }
    }


    ~Engine()
    {
        //Console.WriteLine("Engine Destructed");
        SDL.SDL_DestroyWindow(window);
        SDL.SDL_DestroyRenderer(renderer);
        //Console.ReadKey();
    }
}

