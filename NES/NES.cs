using System;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Drawing.Printing;
using System.Threading;
using DisplayEngine;
using Serilog;

namespace NES
{

    /// <summary>
    /// This is the main class that will bring all of the pieces together - those peices being the
    /// GUI/Game Engine side and the emulator itself
    /// This class will instantiate a game engine and use it to run the emulation
    /// </summary>
    public class NESSystem
    {
        public bool userQuit = false;

        // For random pixels
        //private Random rnd = new Random();

        // color swatch
        private ScreenColor c_white = new ScreenColor(255, 255, 255, 255);
        private ScreenColor c_black = new ScreenColor(0, 0, 0, 255);
        private ScreenColor c_red = new ScreenColor(255, 0, 0, 255);
        private ScreenColor c_green = new ScreenColor(0, 255, 0, 255);
        private ScreenColor c_blue = new ScreenColor(0, 0, 255, 255);
        private ScreenColor c_dgray = new ScreenColor(40, 40, 40, 255);
        private ScreenColor c_gray = new ScreenColor(170, 170, 170, 255);

        // Timing - TODO: Put timing into the game engine
        private ulong lastTick = 1;
        private ulong countedFrames = 1;
        private ulong SCREEN_FPS_CAP = 60;
        private ulong SCREEN_TICKS_PER_FRAME;
        public bool EmulationRun = false;
        public float ResidualTime = 0.0f;
        public float lastTime = 0.0f;

        // Disassembled program
        public Dictionary<ushort, string> asm = new Dictionary<ushort, string>();

        // The NES Bus - what really represents the NES + cartridge
        private Bus nes;
        private Cartridge cartridge;

        // Game engine
        public Settings settings = new Settings();
        public Engine engine;

        //public Engine statsWindow;

        public byte SelectedPalette = 0x00;

        // Vars for keyboard input -- TODO: Put this stuff into the game engine
        public List<string> KeysPressed = new List<string>();

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

        private bool c_latched = false;
        private bool space_latched = false;
        private bool p_latched = false;
        private bool f_latched = false;
        private bool l_latched = false;

        // Handle key down event from the game engine
        public void KeyDownHandler(object sender, KeyEventArgs e)
        {
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

        // key up event from the game engine
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

            if (e.KeyCode == "SDLK_c") { c_pressed = false; c_latched = false; }
            if (e.KeyCode == "SDLK_f") { f_pressed = false; f_latched = false; }
            if (e.KeyCode == "SDLK_l") { l_pressed = false; l_latched = false; }
            if (e.KeyCode == "SDLK_p") { p_pressed = false; p_latched = false; }
            if (e.KeyCode == "SDLK_SPACE") { space_pressed = false; space_latched = false; }
            if (e.KeyCode == "SDLK_r") r_pressed = false;
            if (e.KeyCode == "SDLK_q") q_pressed = false;

        }

        public void ButtonHandler(object sender, ControllerEventArgs e)
        {
            if (e.Button == ControllerButton.A) a_pressed = true;
            if (e.Button == ControllerButton.B) b_pressed = true;
            if (e.Button == ControllerButton.Select) sel_pressed = true;
            if (e.Button == ControllerButton.Start) st_pressed = true;
            if (e.Button == ControllerButton.Up) up_pressed = true;
            if (e.Button == ControllerButton.Down) down_pressed = true;
            if (e.Button == ControllerButton.Left) left_pressed = true;
            if (e.Button == ControllerButton.Right) right_pressed = true;
        }

        public void ButtonUpHandler(object sender, ControllerEventArgs e)
        {
            if (e.Button == ControllerButton.A) a_pressed = false;
            if (e.Button == ControllerButton.B) b_pressed = false;
            if (e.Button == ControllerButton.Select) sel_pressed = false;
            if (e.Button == ControllerButton.Start) st_pressed = false;
            if (e.Button == ControllerButton.Up) up_pressed = false;
            if (e.Button == ControllerButton.Down) down_pressed = false;
            if (e.Button == ControllerButton.Left) left_pressed = false;
            if (e.Button == ControllerButton.Right) right_pressed = false;
        }

        //public Sprite testSprite = new Sprite("controller.png");

        // Constructor
        public NESSystem()
        {
            // instantiate the game engine - set the key event handlers
            //engine = new Engine(settings.WindowSettings[WindowSettingTypes.CPUView], "SteveNES");
            engine = new Engine(settings.WindowSettings[WindowSettingTypes.NES_Triple], "SteveNES");
            engine.KeyDown += KeyDownHandler;
            engine.KeyUp += KeyUpHandler;
            engine.ButtonDown += ButtonHandler;
            engine.ButtonUp += ButtonUpHandler;

            //statsWindow = new Engine(settings.WindowSettings[WindowSettingTypes.HD_Double], "CPU");

            // instantiate the NES - inject the game engine dependency
            nes = new Bus(this);

            // Set up timer information
            SCREEN_TICKS_PER_FRAME = 1000 / SCREEN_FPS_CAP;

            // Load that cartridge

            // test
            //cartridge = new Cartridge("nestest.nes");
            //cartridge = new Cartridge("color_test.nes");
            //cartridge = new Cartridge("ppu_open_bus.nes");
            //cartridge = new Cartridge("test_ppu_read_buffer.nes");
            //cartridge = new Cartridge("palette_ram.nes"); <-- passed
            //cartridge = new Cartridge("power_up_palette.nes"); 
            //cartridge = new Cartridge("sprite_ram.nes");
            //cartridge = new Cartridge("vbl_clear_time.nes");
            //cartridge = new Cartridge("vram_access.nes");
            //cartridge = new Cartridge("official_only.nes");
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/240pee/240pee.nes");
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/blargg_nes_cpu_test5/official.nes");
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/blargg_nes_cpu_test5/cpu.nes");
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/blargg_ppu_tests_2005.09.15b/palette_ram.nes");
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/blargg_ppu_tests_2005.09.15b/power_up_palette.nes");
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/blargg_ppu_tests_2005.09.15b/sprite_ram.nes");
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/blargg_ppu_tests_2005.09.15b/vbl_clear_time.nes");  // to late
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/blargg_ppu_tests_2005.09.15b/vram_access.nes");  // PASSED
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/branch_timing_tests/3.Forward_Branch.nes");// PASSED
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/mmc3_irq_tests/1.Clocking.nes");  // 3) Should decrement when A12 is toggled via $2006
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/mmc3_irq_tests/6.MMC3_rev_B.nes");  // 2) Counter isn't working when reloaded with 255
            //2) IRQ should be set when reloading to 0 after clear
            //2) Should reload and set IRQ every clock when reload is 0
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/nmi_sync/demo_ntsc.nes");  //
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/oam_read/oam_read.nes");  // PASSED
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/oam_stress/oam_stress.nes");  // \Failes?
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/other/oam3.nes");  // 
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/scanline/scanline.nes");  // Passed!
            //cartridge = new Cartridge("../../../../../../EmulatorTests/nes-test-roms/scrolltest/scroll.nes");  // Passed!

            // mapper 0 test
            //cartridge = new Cartridge("../../../../../../EmulatorTests/testroms/M0_P32K_C8K_V.nes");  // pass

            // Mapper 1
            //cartridge = new Cartridge("../../../../../../EmulatorTests/testroms/M1_P128K.nes");  // ???
            //cartridge = new Cartridge("../../../../../../EmulatorTests/testroms/M1_P128K_C32K_S8K.nes");  // ???
            //cartridge = new Cartridge("../../../../../../EmulatorTests/testroms/M1_P128K_C128K.nes");  // ???

            // Map 2
            //cartridge = new Cartridge("../../../../../../EmulatorTests/testroms/M2_P128K_V.nes");  // ???





            // MAPPER 000
            //cartridge = new Cartridge("smb.nes");
            //cartridge = new Cartridge("Donkey Kong.nes");
            //cartridge = new Cartridge("1942 (Japan, USA).nes");
            //cartridge = new Cartridge("Kung Fu (Japan, USA).nes");
            //cartridge = new Cartridge("Excitebike (Japan, USA).nes");

            // MAPPER 001 - Worky
            //cartridge = new Cartridge("Tetris (USA).nes");
            //cartridge = new Cartridge("Castlevania II - Simon's Quest (USA).nes");
            //cartridge = new Cartridge("Mega Man 2 (USA).nes");
            //cartridge = new Cartridge("Ninja Gaiden (USA).nes");
            //cartridge = new Cartridge("Adventures in the Magic Kingdom (USA).nes");
            //cartridge = new Cartridge("Chip 'n Dale Rescue Rangers (USA).nes");
            //cartridge = new Cartridge("Monster Party (USA).nes");
            //cartridge = new Cartridge("Legend of Zelda, The (USA) (Rev A).nes");            
            //cartridge = new Cartridge("Final Fantasy (USA).nes");
            cartridge = new Cartridge("Metroid (USA).nes");

            // Mapper 001 no worky
            //cartridge = new Cartridge("Zelda II - The Adventure of Link (USA).nes");


            // MAPPER 002
            //cartridge = new Cartridge("DuckTales (USA).nes");
            //cartridge = new Cartridge("Guardian Legend, The (USA).nes");
            //cartridge = new Cartridge("Castlevania (USA) (Rev A).nes");
            //cartridge = new Cartridge("Mickey Mousecapade (USA).nes");
            //cartridge = new Cartridge("Gradius (USA).nes");

            // Mapper 004 - BUGGIES
            //cartridge = new Cartridge("Metal Storm (USA).nes");
            //cartridge = new Cartridge("StarTropics (USA).nes");
            //cartridge = new Cartridge("Super Mario Bros. 2 (USA) (Rev A).nes");
            //cartridge = new Cartridge("Super Mario Bros. 3 (USA) (Rev A).nes");
            //cartridge = new Cartridge("Mega Man 3 (USA).nes");
            //cartridge = new Cartridge("Ninja Gaiden II - The Dark Sword of Chaos (USA).nes");
            //cartridge = new Cartridge("Ninja Gaiden III - The Ancient Ship of Doom (USA).nes");

            nes.InsertCartridge(cartridge);

            // DISASSEMBLE - and a way to log it
            //asm = nes.cpu.Disassemble(0x0000, 0xFFFF);
            //var asString = string.Join(Environment.NewLine, asm);
            //Log.Debug($"{asString}");

            // Reset the CPU
            nes.cpu.Reset();


            // Finally - run the game engine which will call the function sent
            // to it as rapidly as possible
            engine.Run(renderFrame: RenderFrame);
            //await statsWindow.Run(renderFrame: RenderStats);

            //StartWindows();

            // Old test code
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


        }

        //private async Task StartWindows()
        //{

        //    var tasks = new List<Task>();
        //    tasks.Add(Task.Run(() => { engine.Run(renderFrame: RenderFrame); }));
        //    tasks.Add(Task.Run(() => { statsWindow.Run(renderFrame: RenderStats); }));

        //    await Task.WhenAll(tasks);
        //}

        //public void RenderStats()
        //{
        //    statsWindow.ClearScreen(c_blue);

        //    DrawCPU(20, 20);         // Draw CPU information

        //    if (userQuit)
        //    {
        //        statsWindow.Quit();
        //    }
        //}

        // Called by the display engine - called as quickly as possible
        public void RenderFrame()
        {

            float fpsvalue;
            string fps = "";
            var currTime = (engine.GetElapsedTime() / 1000);

            if (currTime == 0)
            {
                fps = "xx";
            }
            else
            {
                fpsvalue = countedFrames / currTime;
                fps = fpsvalue.ToString();
            }

            engine.WindowTitle = "SteveNES FPS:" + fps;

            countedFrames++;


            // clear the screen
            engine.ClearScreen(c_dgray);

            // Read the controller status from the keyboard into the controller - simulate the PISO
            // this will be the current state of the controller
            nes.controller[0] = 0x00;
            nes.controller[0] |= (byte)(a_pressed ? 0x01 : 0x00);
            nes.controller[0] |= (byte)(b_pressed ? 0x02 : 0x00);
            nes.controller[0] |= (byte)(sel_pressed ? 0x04 : 0x00);
            nes.controller[0] |= (byte)(st_pressed ? 0x08 : 0x00);
            nes.controller[0] |= (byte)(up_pressed ? 0x10 : 0x00);
            nes.controller[0] |= (byte)(down_pressed ? 0x20 : 0x00);
            nes.controller[0] |= (byte)(left_pressed ? 0x40 : 0x00);
            nes.controller[0] |= (byte)(right_pressed ? 0x80 : 0x00);





            // if I want the input to switch to step emulation
            //if (nes.controller[0] > 0)
            //{
            //    Log.Debug($"Controller hit: 0b{Convert.ToString(nes.controller[0], toBase: 2).PadLeft(8,'0')}");
            //    EmulationRun = false;
            //    l_pressed = true;
            //}

            // Handle more key input
            if (space_pressed && !space_latched)    // Space = run the emulation striaght up, stop only when I hit space again
            {
                EmulationRun = !EmulationRun;
                space_latched = true;
            }
            if (r_pressed) nes.Reset();     // Reset the system
            if (q_pressed)
            {
                userQuit = true;
                engine.Quit();   // quit
            }
            if (p_pressed && !p_latched)    // Change the viewing palette applied to the viewable pattern table
            {
                ++SelectedPalette;
                SelectedPalette &= 0x07;
                p_latched = true;
            }


            if (EmulationRun)
            {
                // The idea is to wait untl residual time is up - waiting for the next frame
                // to stay at 60 per second. This is assuming my emulation is going to be
                // fast enough!
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
            else // else we're running in step mode
            {
                if (c_pressed && !c_latched) // c to step one line at a time
                {
                    while (!nes.cpu.Complete())
                    {
                        nes.Clock();
                    }
                    while (nes.cpu.Complete())
                    {
                        nes.Clock();
                    }
                    c_latched = true;
                }

                if (l_pressed && !l_latched)  // l to step one scanline at a time
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
                    l_latched = true;
                }

                if (f_pressed && !f_latched)    // f to step one while frame at a time
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
                    f_latched = true;
                }
            }


            // Draw crap to the screen - Start with the emulation screen sprite:
            //engine.PixelDimensionTest(3);   // scale the screen by 3
            engine.DrawSprite(nes.ppu.GetScreen(), 0, 0, Flip.NONE);
            //engine.PixelDimensionTest(1);   // put the scale back
            // TODO: formalize this scaling feature

            int margin = 780;

            //DrawRam(2, 260, 0x0000, 16, 16);
            //DrawRam(2, 182, 0x8000, 16, 16);
            //engine.PixelDimensionTest(2);
            //DrawPatternAndPalette(margin / 2, 5 / 2);
            //DrawCPU(margin / 2, 150);         // Draw CPU information
            //DrawController(1170 / 2, 150);    // Draw the controller info
            //engine.PixelDimensionTest(1);

            //DrawCode(margin, 220 + 220, 25);       // Draw the disassembled code

            //engine.PixelDimensionTest(1);
            //DrawOAM(margin + 240, 230 + 220, 25);    /// draw top OAM entries
            //engine.PixelDimensionTest(2);
            //DrawRam(margin, 485 + 220, 0xFF00, 16, 16);    // Draw the zero page of RAM








            //for (int y = 0; y < 30; y++)
            //{
            //    for (int x = 0; x < 32; x++)
            //    {
            //        engine.PixelDimensionTest(2);
            //        engine.DrawText(x * 16, y * 16, Hex((uint)(nes.ppu.nameTable[0, y * 32 + x]), 2), c_white);

            //    }
            //}

            // Get mouse information, draw coordinates of the mouse to position shit
            //int mouseX, mouseY;
            //var mousePos = engine.GetMousePos();
            //mouseX = mousePos.Item1;
            //mouseY = mousePos.Item2;
            //engine.DrawText(mouseX, mouseY, $"({mouseX},{mouseY})", c_white);


        }

        public void DrawPatternAndPalette(int x, int y)
        {
            // Draw the palettes and pattern tables
            int margin = x;
            int starty = y;
            //int downfactor = 40;
            const int SwatchSize = 6;

            for (int p = 0; p < 8; p++)
            {
                for (int s = 0; s < 4; s++)
                {
                    int factor = p * (SwatchSize * 5) + s * SwatchSize;
                    engine.DrawQuadFilled(margin + factor, starty, margin + factor + SwatchSize, starty + SwatchSize, nes.ppu.GetColorFromPaletteRam((byte)p, (byte)s));
                    // FillRect(265 + p * (SwatchSize * 5) + s * SwatchSize, 340, SwatchSize, SwatchSize, nes.ppu.GetColorFromPaletteRam(p, s));
                }
            }
            engine.DrawQuad(margin + SelectedPalette * (SwatchSize * 5) - 1, starty, margin + SelectedPalette * (SwatchSize * 5) + (SwatchSize * 4), starty + SwatchSize, c_white);

            engine.DrawSprite(nes.ppu.GetPatternTable(0, SelectedPalette), margin, starty + 9, Flip.NONE);
            engine.DrawSprite(nes.ppu.GetPatternTable(1, SelectedPalette), margin + 132, starty + 9, Flip.NONE);
        }

        // helper function to draw the controller input
        public void DrawController(int x, int y)
        {
            nes.controller[0] |= (byte)(a_pressed ? 0x01 : 0x00);
            nes.controller[0] |= (byte)(b_pressed ? 0x02 : 0x00);
            nes.controller[0] |= (byte)(sel_pressed ? 0x04 : 0x00);
            nes.controller[0] |= (byte)(st_pressed ? 0x08 : 0x00);
            nes.controller[0] |= (byte)(up_pressed ? 0x10 : 0x00);
            nes.controller[0] |= (byte)(down_pressed ? 0x20 : 0x00);
            nes.controller[0] |= (byte)(left_pressed ? 0x40 : 0x00);
            nes.controller[0] |= (byte)(right_pressed ? 0x80 : 0x00);

            engine.DrawText(x, y, Convert.ToString(nes.controller[0] & 0x01, toBase:2).PadLeft(1,'0') + " A (x)", c_white);
            engine.DrawText(x, y+8, Convert.ToString((nes.controller[0] >> 1) & 0x01, toBase:2).PadLeft(1,'0') + " B (z)", c_white);
            engine.DrawText(x, y+8*2, Convert.ToString((nes.controller[0] >> 2) & 0x01, toBase:2).PadLeft(1,'0') + " Sel (a)", c_white);
            engine.DrawText(x, y+8*3, Convert.ToString((nes.controller[0] >> 3) & 0x01, toBase:2).PadLeft(1,'0') + " St (s)", c_white);
            engine.DrawText(x, y + 8 * 4, Convert.ToString((nes.controller[0] >> 4) & 0x01, toBase:2).PadLeft(1,'0') + " U (up)", c_white);
            engine.DrawText(x, y + 8 * 5, Convert.ToString((nes.controller[0] >> 5) & 0x01, toBase:2).PadLeft(1,'0') + " D (dn)", c_white);
            engine.DrawText(x, y + 8 * 6, Convert.ToString((nes.controller[0] >> 6) & 0x01, toBase:2).PadLeft(1,'0') + " L (lt)", c_white);
            engine.DrawText(x, y + 8 * 7, Convert.ToString((nes.controller[0] >> 7) & 0x01, toBase:2).PadLeft(1,'0') + " R (rt)", c_white);
        }

        // Drawing memory
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

        public void DrawOAM(int x, int y, int showCount)
        {
            for (int i = 0; i < showCount; i++)
            {
                string output = Hex((uint)i, 2) +
                    ": (" + Convert.ToString(nes.ppu.OAM[i * 4 + 3], toBase: 10) +
                    ", " + Convert.ToString(nes.ppu.OAM[i * 4 + 0], toBase: 10) + ") " +
                    "ID: " + Hex(nes.ppu.OAM[i * 4 + 1], 2) +
                    " AT: " + Hex(nes.ppu.OAM[i * 4 + 2], 2);
                engine.DrawText(x, y + (i * 10), output, c_white);
            }

        }

        // Drawing CPU information
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

        // Draw code lines using disassembled code
        public void DrawCode(int x, int y, int lines)
        {
            int lineitem = asm.Keys.ToList().IndexOf(nes.cpu.pc);

            int lineY = (lines >> 1) * 10 + y;

            //if (lineitem > 0)
            //{
                try
                {
                    engine.DrawText(x, lineY, asm.ElementAt(lineitem).Value, c_gray);
                }
                catch (Exception ex)
                {
                    engine.DrawText(x, lineY, "NO DATA", c_white);
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

        // Gimme hex
        public static string Hex(uint num, int pad)
        {
            return Convert.ToString(num, toBase: 16).ToUpper().PadLeft(pad, '0');
        }

        // for debugging, show me bits
        private static string Nice8bits(byte input)
        {
            return "0b" + Convert.ToString(input, toBase: 2).PadLeft(8, '0');
        }


    }
}
