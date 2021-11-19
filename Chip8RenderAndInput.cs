using Godot;
using System;
using Chip8Emulator;

namespace Chip8GodotInterface
{
    public class Chip8RenderAndInput : Sprite
    {
        // Declare member variables here. Examples:
        // private int a = 2;
        // private string b = "text";

        public Emulator Emulator { get; set; } = new Emulator();

        //private Viewport rootNode;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            //GD.Print(Test.Memory[0x09D].ToString());

            //rootNode = GetTree().Root;

            Emulator.LoadROM("");

            //RenderTargetSprite.SetScale();
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(float delta)
        {
            //this.Position
            CheckInput();

            if (Emulator.CarryOverTime)
            {
                Emulator.TimeElapsed += Emulator.Timer.ElapsedMilliseconds;
            }
            else
            {
                Emulator.TimeElapsed = Emulator.Timer.ElapsedMilliseconds + Emulator.LeftoverMilliseconds;
            }

            Emulator.Timer.Restart();

            Emulator.LeftoverMilliseconds = Emulator.TimeElapsed % 16;

            if (Emulator.DelayTimer > 0)
            {
                Emulator.DelayTimer = (Emulator.TimeElapsed / 16) > Emulator.DelayTimer ? (byte)0 : (byte)(Emulator.DelayTimer - (Emulator.TimeElapsed / 16));
            }

            //Emulator.WasDelayTimerModifiedLastInstruction = false;

            if (Emulator.SoundTimer > 0)
            {
                Emulator.SoundTimer = (Emulator.TimeElapsed / 16) > Emulator.SoundTimer ? (byte)0 : (byte)(Emulator.SoundTimer - (Emulator.TimeElapsed / 16));
            }

            if (!Emulator.IsCurrentFrameRendered)
            {
                var imageTexture = new ImageTexture();

                imageTexture.CreateFromImage(Emulator.CurrentFrame, 0);

                this.Texture = imageTexture;
            }

            Emulator.IsCurrentFrameRendered = true;

            if (Emulator.TimeElapsed > Emulator.InstructionInterval)
            {
                for (var i = 0; i < Emulator.TimeElapsed / Emulator.InstructionInterval; i++)
                {
                                    Emulator.Fetch();

                Emulator.DecodeAndExecute();

                Emulator.CarryOverTime = false;
                }
            }
            else
            {
                Emulator.CarryOverTime = true;
            }
        }

        private void CheckInput()
        {
            Emulator.Inputs[0] = Input.IsActionPressed("0");

            Emulator.Inputs[1] = Input.IsActionPressed("1");

            Emulator.Inputs[2] = Input.IsActionPressed("2");

            Emulator.Inputs[3] = Input.IsActionPressed("3");

            Emulator.Inputs[4] = Input.IsActionPressed("4");

            Emulator.Inputs[5] = Input.IsActionPressed("5");

            Emulator.Inputs[6] = Input.IsActionPressed("6");

            Emulator.Inputs[7] = Input.IsActionPressed("7");

            Emulator.Inputs[8] = Input.IsActionPressed("8");

            Emulator.Inputs[9] = Input.IsActionPressed("9");

            Emulator.Inputs[10] = Input.IsActionPressed("A");

            Emulator.Inputs[11] = Input.IsActionPressed("B");

            Emulator.Inputs[12] = Input.IsActionPressed("C");

            Emulator.Inputs[13] = Input.IsActionPressed("D");

            Emulator.Inputs[14] = Input.IsActionPressed("E");

            Emulator.Inputs[15] = Input.IsActionPressed("F");

            for (var i = 0; i < Emulator.Inputs.Length; i++)
            {
                if (Emulator.Inputs[i])
                {
                    Emulator.IsKeyPressed = (true, (byte)i);

                    break;
                }
                else
                {
                    Emulator.IsKeyPressed = (false, 0);
                }
            }
        }
    }
}