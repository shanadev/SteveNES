using System;
using DisplayEngine;

namespace NES
{
    public class NESSystem
    {
        private Random rnd = new Random();

        private ScreenColor c_black = new ScreenColor(0, 0, 0, 255);
        private ScreenColor c_red = new ScreenColor(255, 0, 0, 255);
        private ScreenColor c_green = new ScreenColor(0, 255, 0, 255);
        private ScreenColor c_blue = new ScreenColor(0, 0, 255, 255);
        private ScreenColor c_gray = new ScreenColor(170, 170, 170, 255);

        private ulong lastTick = 1;
        private ulong countedFrames = 1;
        private ulong SCREEN_FPS_CAP = 60;
        private ulong SCREEN_TICKS_PER_FRAME;

        public Settings settings = new Settings();
        public Engine engine;

        public NESSystem()
        {
            SCREEN_TICKS_PER_FRAME = 1000 / SCREEN_FPS_CAP;
            engine = new Engine(settings.WindowSettings[WindowSettingTypes.HD_Double], "SteveNES");
            engine.Run(renderFrame: RenderFrame);
        }

        // Called by the display engine
        public void RenderFrame()
        {
            //Console.WriteLine("Render frame");

            //ScreenColor myColor = new ScreenColor(200, 240, 243, 255);
            //engine.DrawPixel(100, 100, myColor);

            //for (int x = 0; x < 254; x++)
            //{
            //    for (int y = 0; y < 240; y++)
            //    {
            //        engine.DrawPixel(x, y, new ScreenColor((byte)rnd.Next(255), (byte)rnd.Next(255), (byte)rnd.Next(255), 255));

            //        //
            //    }
            //}

            //engine.DrawText(10, 10, "This is some text! You like it, bitch!");

            engine.ClearScreen(c_black);

            //engine.DrawLine(10, 10, 30, 50, c_blue);
            //engine.DrawQuad(50, 30, 100, 100, c_red);
            //engine.DrawQuadFilled(120, 120, 200, 200, c_green);

            var ticks = engine.GetTicks();
            if (ticks == 0) ticks = 1;
            var fps = countedFrames / (float)(ticks / 1000);
            if (fps > 2000000)
            {
                fps = 0;
            }
            var ticksPassed = ticks - lastTick;
            //engine.DrawText(230, 10, $"{ticks}");
            //engine.DrawText(230, 19, $"FPS Avg: {fps}");
            //engine.DrawText(230, 28, $"Ticks Passed: {ticksPassed}");


            var bus = new Bus();
            var cpu = new CPU(bus);

            cpu.status = 0b10101011;
            engine.DrawText(10, 10, "Status: " + Nice8bits(cpu.status));
            cpu.SetFlag(CPU.FLAGS6502.V, true);
            cpu.SetFlag(CPU.FLAGS6502.N, false);
            engine.DrawText(10, 19, "Set V:  " + Nice8bits(cpu.status));


            lastTick = ticks;
            countedFrames++;

            //if (ticksPassed < SCREEN_TICKS_PER_FRAME)
            //{
            //    engine.Delay(SCREEN_TICKS_PER_FRAME - ticksPassed);
            //}



        }

        private static string Nice8bits(byte input)
        {
            return "0b" + Convert.ToString(input, toBase: 2).PadLeft(8, '0');
        }
    }
}
