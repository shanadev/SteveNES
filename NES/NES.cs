using System;
using DisplayEngine;
//using Serilog;

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

        private Cartridge cartridge;

        public Settings settings = new Settings();
        public Engine engine;

        public List<string> KeysPressed = new List<string>();

        public bool EmulationRun = false;
        public float ResidualTime = 0.0f;

        public byte SelectedPalette = 0x00;

        private bool a_pressed = false;
        private bool b_pressed = false;
        private bool sel_pressed = false;
        private bool st_pressed = false;
        private bool up_pressed = false;
        private bool down_pressed = false;
        private bool left_pressed = false;
        private bool right_pressed = false;

        private bool q_pressed = false;
        private bool c_pressed = false;
        private bool space_pressed = false;
        private bool p_pressed = false;
        private bool f_pressed = false;
        private bool l_pressed = false;
        private bool r_pressed = false;


        public void KeyDownHandler(object sender, KeyEventArgs e)
        {
            //KeysPressed.Add(e.KeyCode);

            //a_pressed = false;
            //b_pressed = false;
            //sel_pressed = false;
            //st_pressed = false;
            //up_pressed = false;
            //down_pressed = false;
            //left_pressed = false;
            //right_pressed = false;

            //q_pressed = false;
            //c_pressed = false;
            //space_pressed = false;
            //p_pressed = false;
            //f_pressed = false;
            //l_pressed = false;
            //r_pressed = false;

            if (e.KeyCode == "SDLK_x") a_pressed = true;
            if (e.KeyCode == "SDLK_z") b_pressed = true;
            if (e.KeyCode == "SDLK_a") sel_pressed = true;
            if (e.KeyCode == "SDLK_s") st_pressed = true;
            if (e.KeyCode == "SDLK_UP") up_pressed = true;
            if (e.KeyCode == "SDLK_DOWN") down_pressed = true;
            if (e.KeyCode == "SDLK_LEFT") left_pressed = true;
            if (e.KeyCode == "SDLK_RIGHT") right_pressed = true;

            if (e.KeyCode == "SDLK_c") c_pressed = true;
            if (e.KeyCode == "SDLK_f") f_pressed = true;
            if (e.KeyCode == "SDLK_l") l_pressed = true;
            if (e.KeyCode == "SDLK_p") p_pressed = true;
            if (e.KeyCode == "SDLK_SPACE") space_pressed = true;
            if (e.KeyCode == "SDLK_r") r_pressed = true;
            if (e.KeyCode == "SDLK_q") q_pressed = true;

        }

        public void KeyUpHandler(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == "SDLK_x") a_pressed = false;
            if (e.KeyCode == "SDLK_z") b_pressed = false;
            if (e.KeyCode == "SDLK_a") sel_pressed = false;
            if (e.KeyCode == "SDLK_s") st_pressed = false;
            if (e.KeyCode == "SDLK_UP") up_pressed = false;
            if (e.KeyCode == "SDLK_DOWN") down_pressed = false;
            if (e.KeyCode == "SDLK_LEFT") left_pressed = false;
            if (e.KeyCode == "SDLK_RIGHT") right_pressed = false;

            if (e.KeyCode == "SDLK_c") c_pressed = false;
            if (e.KeyCode == "SDLK_f") f_pressed = false;
            if (e.KeyCode == "SDLK_l") l_pressed = false;
            if (e.KeyCode == "SDLK_p") p_pressed = false;
            if (e.KeyCode == "SDLK_SPACE") space_pressed = false;
            if (e.KeyCode == "SDLK_r") r_pressed = false;
            if (e.KeyCode == "SDLK_q") q_pressed = false;

        }

        //public Sprite testSprite = new Sprite("controller.png");

        public NESSystem()
        {


            nes = new Bus(engine);

            SCREEN_TICKS_PER_FRAME = 1000 / SCREEN_FPS_CAP;
            engine = new Engine(settings.WindowSettings[WindowSettingTypes.CPUView], "SteveNES");

            engine.KeyDown += KeyDownHandler;
            engine.KeyUp += KeyUpHandler;
            //
            cartridge = new Cartridge("nestest.nes");
            //cartridge = new Cartridge("smb.nes");
            //cartridge = new Cartridge("Donkey Kong.nes");
            

            nes.InsertCartridge(cartridge);



            //string program = "A20A8E0000A2038E0100AC0000A900186D010088D0FA8D0200EAEAEA";
            //byte[] progbytes = Convert.FromHexString(program);
            //ushort offset = 0x8000;
            //foreach (var bt in progbytes)
            //{
            //    nes.cpuRam[offset++] = bt;
            //}

            // set reset
            //nes.cpuRam[0xFFFC] = 0x00;
            //nes.cpuRam[0xFFFD] = 0x80;

            // get dissasembly

            asm = nes.cpu.Disassemble(0x0000, 0xFFFF);
            //var asString = string.Join(Environment.NewLine, asm);
            //Log.Debug($"{asString}");

            // reset
            nes.cpu.Reset();



            engine.Run(renderFrame: RenderFrame);
        }

        // Called by the display engine
        public void RenderFrame()
        {
            engine.ClearScreen(c_blue);


            nes.controller[0] = 0x00;
            nes.controller[0] |= (byte)(a_pressed ? 0x01 : 0x00);
            nes.controller[0] |= (byte)(b_pressed ? 0x02 : 0x00);
            nes.controller[0] |= (byte)(sel_pressed ? 0x04 : 0x00);
            nes.controller[0] |= (byte)(st_pressed ? 0x08 : 0x00);
            nes.controller[0] |= (byte)(up_pressed ? 0x10 : 0x00);
            nes.controller[0] |= (byte)(down_pressed ? 0x20 : 0x00);
            nes.controller[0] |= (byte)(left_pressed ? 0x40 : 0x00);
            nes.controller[0] |= (byte)(right_pressed ? 0x80 : 0x00);

            if (space_pressed) EmulationRun = !EmulationRun;
            if (r_pressed) nes.Reset();
            if (q_pressed) engine.Quit();
            if (p_pressed)
            {
                ++SelectedPalette;
                SelectedPalette &= 0x07;
            }


            if (EmulationRun)
            {
                if (ResidualTime > 0.0f)
                {
                    ResidualTime -= engine.GetElapsedTime() / 1000;
                }
                else
                {
                    ResidualTime += (1.0f / 60.0f) - (engine.GetElapsedTime() / 1000);
                    do { nes.Clock(); } while (!nes.ppu.FrameComplete);
                    nes.ppu.FrameComplete = false;
                }
            }
            else
            {
                if (c_pressed)
                {
                    while (!nes.cpu.Complete())
                    {
                        nes.Clock();
                    }
                    while (nes.cpu.Complete())
                    {
                        nes.Clock();
                    }
                }

                if (l_pressed)
                {
                    while (!nes.ppu.ScanlineComplete)
                    {
                        nes.Clock();
                    }
                    while (!nes.cpu.Complete())
                    {
                        nes.Clock();
                    }
                    nes.ppu.ScanlineComplete = false;
                }

                if (f_pressed)
                {
                    while (!nes.ppu.FrameComplete)
                    {
                        nes.Clock();
                    }
                    while (!nes.cpu.Complete())
                    {
                        nes.Clock();
                    }
                    nes.ppu.FrameComplete = false;
                }
            }

            int margin = 450;
                        
            //DrawRam(2, 260, 0x0000, 16, 16);
            //DrawRam(2, 182, 0x8000, 16, 16);
            DrawCPU(margin, 2);
            DrawCode(margin, 72, 26);
            DrawRam(2, 365, 0x0000, 16, 16);

            engine.DrawText(margin + 100, 30, Convert.ToString(nes.controller[0], toBase: 2).PadLeft(8, '0') + " [" + nes.controller[0] + "]", c_white);

            const int SwatchSize = 6;

            for (int p = 0; p < 8; p++)
            {
                for (int s = 0; s < 4; s++)
                {
                    int factor = p * (SwatchSize * 5) + s * SwatchSize;
                    engine.DrawQuadFilled(margin + factor, 341, margin + factor + SwatchSize, 341 + SwatchSize, nes.ppu.GetColorFromPaletteRam((byte)p, (byte)s));
                    // FillRect(265 + p * (SwatchSize * 5) + s * SwatchSize, 340, SwatchSize, SwatchSize, nes.ppu.GetColorFromPaletteRam(p, s));
                }
            }

            engine.DrawQuad(margin + SelectedPalette * (SwatchSize * 5) - 1, 341, margin + SelectedPalette * (SwatchSize * 5) + (SwatchSize * 4), 341 + SwatchSize, c_white);

            engine.DrawSprite(nes.ppu.GetPatternTable(0, SelectedPalette), margin, 350, Flip.NONE);
            engine.DrawSprite(nes.ppu.GetPatternTable(1, SelectedPalette), margin + 132, 350, Flip.NONE);

            engine.PixelDimensionTest(3);
            engine.DrawSprite(nes.ppu.GetScreen(), 0, 0, Flip.NONE);
            engine.PixelDimensionTest(2);

            //for (int y = 0; y < 30; y++)
            //{
            //    for (int x = 0; x < 32; x++)
            //    {
            //        engine.PixelDimensionTest(2);
            //        engine.DrawText(x * 16, y * 16, Hex((uint)(nes.ppu.nameTable[0, y * 32 + x]), 2), c_white);

            //    }
            //}


            //int mouseX, mouseY;
            //var mousePos = engine.GetMousePos();
            //mouseX = mousePos.Item1;
            //mouseY = mousePos.Item2;

            //engine.DrawText(mouseX, mouseY, $"({mouseX},{mouseY})", c_white);

            //engine.DrawSprite(testSprite, mouseX, mouseY, Flip.NONE);
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
                    offset += " " + Hex(nes.cpuRead(addr), 2);
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
            //int lineitem = Array.IndexOf(asm.Keys.ToList<INSTRUCTION>(), nes.cpu.pc);
            int lineitem = asm.Keys.ToList().IndexOf(nes.cpu.pc);
            //lineitem = asm[nes.cpu.pc].;

            //lineitem = Array.FindIndex<INSTRUCTION>(asm, row => row.)
            int lineY = (lines >> 1) * 10 + y;

            //if (lineitem > 0)
            //{
                try
                {
                    engine.DrawText(x, lineY, asm.ElementAt(lineitem).Value, c_gray);
                }
                catch (Exception ex)
                {
                    engine.DrawText(x, lineY, "NOT FOUND", c_white);
                }
                while (lineY < (lines * 10) + y)
                {
                    lineY += 10;
                    if (++lineitem > 0)
                    {
                        try
                        {
                            engine.DrawText(x, lineY, asm.ElementAt(lineitem).Value, c_white);
                        }
                        catch (Exception ex)
                        {
                            engine.DrawText(x, lineY, "NOT FOUND", c_white);
                        }
                    }
                }
            //}

            lineitem = Array.IndexOf(asm.Keys.ToArray(), nes.cpu.pc);
            lineY = (lines >> 1) * 10 + y;

            //if (lineitem > 0)
            //{
                while (lineY > y)
                {
                    lineY -= 10;
                    --lineitem;
                    //if (--lineitem > 0)
                    //{
                        try
                        {
                            engine.DrawText(x, lineY, asm.ElementAt(lineitem).Value, c_gray);
                        }
                        catch (Exception ex)
                        {
                            engine.DrawText(x, lineY, "NOT FOUND", c_white);
                        }
                    //}
                }
            //}

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


            //var bus = new Bus(enigne);
            //var cpu = new CPU(bus);

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
