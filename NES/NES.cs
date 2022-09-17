using System;
using DisplayEngine;

namespace NES
{
    public class NESSystem
    {
        private Random rnd = new Random();

        private ScreenColor c_white = new ScreenColor(255, 255, 255, 255);
        private ScreenColor c_black = new ScreenColor(0, 0, 0, 255);
        private ScreenColor c_red = new ScreenColor(255, 0, 0, 255);
        private ScreenColor c_green = new ScreenColor(0, 255, 0, 255);
        private ScreenColor c_blue = new ScreenColor(0, 0, 255, 255);
        private ScreenColor c_gray = new ScreenColor(170, 170, 170, 255);

        private ulong lastTick = 1;
        private ulong countedFrames = 1;
        private ulong SCREEN_FPS_CAP = 60;
        private ulong SCREEN_TICKS_PER_FRAME;

        private Dictionary<ushort, string> asm = new Dictionary<ushort, string>();

        private Bus nes;

        public Settings settings = new Settings();
        public Engine engine;

        public List<string> KeysPressed = new List<string>();

        public void KeyDownHandler(object sender, KeyEventArgs e)
        {
            //KeysPressed.Add(e.KeyCode);

            switch (e.KeyCode)
            {
                case "SDLK_q":
                    engine.Quit();
                    break;
                case "SDLK_SPACE":
                    while (!nes.cpu.Complete())
                    {
                        nes.cpu.Clock();
                    }
                    nes.cpu.Clock();
                    break;
            }
        }

        public void KeyUpHandler(object sender, KeyEventArgs e)
        {
            KeysPressed.Remove(e.KeyCode);
        }

        public NESSystem()
        {
            nes = new Bus();

            SCREEN_TICKS_PER_FRAME = 1000 / SCREEN_FPS_CAP;
            engine = new Engine(settings.WindowSettings[WindowSettingTypes.CPUView], "SteveNES");

            engine.KeyDown += KeyDownHandler;
            engine.KeyUp += KeyUpHandler;

            string program = "A20A8E0000A2038E0100AC0000A900186D010088D0FA8D0200EAEAEA";
            byte[] progbytes = Convert.FromHexString(program);
            ushort offset = 0x8000;
            foreach (var bt in progbytes)
            {
                nes.ram[offset++] = bt;
            }

            // set reset
            nes.ram[0xFFFC] = 0x00;
            nes.ram[0xFFFD] = 0x80;

            // get dissasembly

            asm = nes.cpu.Disassemble(0x0000, 0xFFFF);

            // reset
            nes.cpu.Reset();

            engine.Run(renderFrame: RenderFrame);
        }

        // Called by the display engine
        public void RenderFrame()
        {
            engine.ClearScreen(c_blue);

            // Handle key input here
            // SDLK_SPACE
            // SDLK_c





            DrawRam(2, 2, 0x0000, 16, 16);
            DrawRam(2, 182, 0x8000, 16, 16);
            DrawCPU(448, 2);
            DrawCode(448, 72, 26);
        }


        public void DrawRam(int x, int y, ushort addr, int rows, int cols)
        {
            int cX = x;
            int cY = y;

            for (int row = 0; row < rows; row++)
            {
                string offset = "$" + Hex(addr, 4) + ": ";
                for (int col = 0; col < cols; col++)
                {
                    offset += " " + Hex(nes.read(addr), 2);
                    addr += 1;
                }
                engine.DrawText(cX, cY, offset, c_white);
                cY += 10;
            }
        }

        public void DrawCPU(int x, int y)
        {
            string status = "STATUS: ";
            engine.DrawText(x, y, "STATUS:", c_white);
            engine.DrawText(x + 64, y, "N", (byte)(nes.cpu.status & (byte)CPU.FLAGS6502.N) > 0 ? c_green : c_red);
            engine.DrawText(x + 80, y, "V", (byte)(nes.cpu.status & (byte)CPU.FLAGS6502.V) > 0 ? c_green : c_red);
            engine.DrawText(x + 96, y, "-", (byte)(nes.cpu.status & (byte)CPU.FLAGS6502.U) > 0 ? c_green : c_red);
            engine.DrawText(x + 112, y, "B", (byte)(nes.cpu.status & (byte)CPU.FLAGS6502.B) > 0 ? c_green : c_red);
            engine.DrawText(x + 128, y, "D", (byte)(nes.cpu.status & (byte)CPU.FLAGS6502.D) > 0 ? c_green : c_red);
            engine.DrawText(x + 144, y, "I", (byte)(nes.cpu.status & (byte)CPU.FLAGS6502.I) > 0 ? c_green : c_red);
            engine.DrawText(x + 160, y, "Z", (byte)(nes.cpu.status & (byte)CPU.FLAGS6502.Z) > 0 ? c_green : c_red);
            engine.DrawText(x + 178, y, "C", (byte)(nes.cpu.status & (byte)CPU.FLAGS6502.C) > 0 ? c_green : c_red);
            engine.DrawText(x, y + 10, "PC: $" + Hex(nes.cpu.pc, 4), c_white);
            engine.DrawText(x, y + 20, "A: $" + Hex(nes.cpu.a, 2) + "  [" + nes.cpu.a.ToString() + "]", c_white);
            engine.DrawText(x, y + 30, "X: $" + Hex(nes.cpu.x, 2) + "  [" + nes.cpu.x.ToString() + "]", c_white);
            engine.DrawText(x, y + 40, "Y: $" + Hex(nes.cpu.y, 2) + "  [" + nes.cpu.y.ToString() + "]", c_white);
            engine.DrawText(x, y + 50, "Stack P: $" + Hex(nes.cpu.stkp, 4), c_white);
        }

        public void DrawCode(int x, int y, int lines)
        {
            int lineitem = Array.IndexOf(asm.Keys.ToArray(), nes.cpu.pc);
            int lineY = (lines >> 1) * 10 + y;

            if (lineitem != asm.Count - 1)
            {
                engine.DrawText(x, lineY, asm.ElementAt(lineitem).Value, c_gray);
                while (lineY < (lines * 10) + y)
                {
                    lineY += 10;
                    if (++lineitem != asm.Count -1)
                    {
                        engine.DrawText(x, lineY, asm.ElementAt(lineitem).Value, c_white);
                    }
                }
            }

            lineitem = Array.IndexOf(asm.Keys.ToArray(), nes.cpu.pc);
            lineY = (lines >> 1) * 10 + y;

            if (lineitem != asm.Count - 1)
            {
                while (lineY > y)
                {
                    lineY -= 10;
                    if (--lineitem != asm.Count - 1)
                    {
                        engine.DrawText(x, lineY, asm.ElementAt(lineitem).Value, c_white);
                    }
                }
            }

        }

        public static string Hex(uint num, int pad)
        {
            return Convert.ToString(num, toBase: 16).ToUpper().PadLeft(pad, '0');
        }


        private static string Nice8bits(byte input)
        {
            return "0b" + Convert.ToString(input, toBase: 2).PadLeft(8, '0');
        }





        public void TestShit()
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

            //cpu.status = 0b10101011;
            //engine.DrawText(10, 10, "Status: " + Nice8bits(cpu.status));
            //cpu.SetFlag(CPU.FLAGS6502.V, true);
            //cpu.SetFlag(CPU.FLAGS6502.N, false);
            //engine.DrawText(10, 19, "Set V:  " + Nice8bits(cpu.status));


            lastTick = ticks;
            countedFrames++;

            //if (ticksPassed < SCREEN_TICKS_PER_FRAME)
            //{
            //    engine.Delay(SCREEN_TICKS_PER_FRAME - ticksPassed);
            //}
        }


    }
}
