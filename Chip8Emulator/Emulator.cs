using System.Collections.Generic;
using System.Collections;
using System;
using System.IO;
//using System.Drawing;
using System.Diagnostics;
using Godot;

namespace Chip8Emulator
{
    public class Emulator
    {
        public byte[] Memory { get; set; } = new byte[4096];

        public byte[] VRegisters { get; set; } = new byte[16];

        public ushort IRegister { get; set; }

        public ushort ProgramCounter { get; set; } = 512;

        public byte StackPointer { get; set; }

        public FixedSizeStack<ushort> Stack { get; set; } = new FixedSizeStack<ushort>(16);

        public bool[,] Display { get; set; } = new bool[64,32];

        public ushort CurrentInstruction { get; set; }

        public byte DelayTimer { get; set; }

        public byte SoundTimer { get; set; }

        public int FilenameNumber { get; set; } = 0;

        public bool[] Inputs { get; set; } = new bool[16];

        public (bool, byte) IsKeyPressed { get; set; }

        public Stopwatch Timer { get; set; }

        public long TimeElapsed { get; set; } = 0;

        public long LeftoverMilliseconds { get; set; } = 0;

        public bool CarryOverTime { get; set; }

        public bool IsRomLoaded { get; set; }

        public int InstructionInterval { get; set; } = 2; //Measured in milliseconds

        public Image CurrentFrame { get; set; }

        public bool IsCurrentFrameRendered { get; set; } = true;

        public Color WhiteColor { get; set; } = new Color(255, 255, 255);

        public Color BlackColor { get; set; } = new Color(0, 0, 0);

        public Emulator()
        {
            //Font for 0
            Memory[0x050] = 0xF0;

            Memory[0x051] = 0x90;

            Memory[0x052] = 0x90;

            Memory[0x053] = 0x90;

            Memory[0x054] = 0xF0;

            //Font for 1
            Memory[0x055] = 0x20;

            Memory[0x056] = 0x60;

            Memory[0x057] = 0x20;

            Memory[0x058] = 0x20;

            Memory[0x059] = 0x70;

            //Font for 2
            Memory[0x05A] = 0xF0;

            Memory[0x05B] = 0x10;

            Memory[0x05C] = 0xF0;

            Memory[0x05D] = 0x80;

            Memory[0x05E] = 0xF0;

            //Font for 3
            Memory[0x05F] = 0xF0;

            Memory[0x060] = 0x10;

            Memory[0x061] = 0xF0;

            Memory[0x062] = 0x10;

            Memory[0x063] = 0xF0;

            //Font for 4
            Memory[0x064] = 0x90;

            Memory[0x065] = 0x90;

            Memory[0x066] = 0xF0;

            Memory[0x067] = 0x10;

            Memory[0x068] = 0x10;

            //Font for 5
            Memory[0x069] = 0xF0;

            Memory[0x06A] = 0x80;

            Memory[0x06B] = 0xF0;

            Memory[0x06C] = 0x10;

            Memory[0x06D] = 0xF0;

            //Font for 6
            Memory[0x06E] = 0xF0;

            Memory[0x06F] = 0x80;

            Memory[0x070] = 0xF0;

            Memory[0x071] = 0x90;

            Memory[0x072] = 0xF0;

            //Font for 7
            Memory[0x073] = 0xF0;

            Memory[0x074] = 0x10;

            Memory[0x075] = 0x20;

            Memory[0x076] = 0x40;

            Memory[0x077] = 0x40;

            //Font for 8
            Memory[0x078] = 0xF0;

            Memory[0x079] = 0x90;

            Memory[0x07A] = 0xF0;

            Memory[0x07B] = 0x90;

            Memory[0x07C] = 0xF0;

            //Font for 9
            Memory[0x07D] = 0xF0;

            Memory[0x07E] = 0x90;

            Memory[0x07F] = 0xF0;

            Memory[0x080] = 0x10;

            Memory[0x081] = 0xF0;

            //Font for A
            Memory[0x082] = 0xF0;

            Memory[0x083] = 0x90;

            Memory[0x084] = 0xF0;

            Memory[0x085] = 0x90;

            Memory[0x086] = 0x90;

            //Font for B
            Memory[0x087] = 0xE0;

            Memory[0x088] = 0x90;

            Memory[0x089] = 0xE0;

            Memory[0x08A] = 0x90;

            Memory[0x08B] = 0xE0;

            //Font for C
            Memory[0x08C] = 0xF0;

            Memory[0x08D] = 0x80;

            Memory[0x08E] = 0x80;

            Memory[0x08F] = 0x80;

            Memory[0x090] = 0xF0;

            //Font for D
            Memory[0x091] = 0xE0;

            Memory[0x092] = 0x90;

            Memory[0x093] = 0x90;

            Memory[0x094] = 0x90;

            Memory[0x095] = 0xE0;

            //Font for E
            Memory[0x096] = 0xF0;

            Memory[0x097] = 0x80;

            Memory[0x098] = 0xF0;

            Memory[0x099] = 0x80;

            Memory[0x09A] = 0xF0;

            //Font for F
            Memory[0x09B] = 0xF0;

            Memory[0x09C] = 0x80;

            Memory[0x09D] = 0xF0;

            Memory[0x09E] = 0x80;

            Memory[0x09F] = 0x80;

            Timer = new Stopwatch();

            Timer.Start();
        }

        public void MainLoop(string filepath)
        {
            TimeElapsed = Timer.ElapsedMilliseconds;

            Timer.Restart();

            if (DelayTimer > 0)
            {
                DelayTimer -= (byte)(TimeElapsed / 16);
            }

            if (SoundTimer > 0)
            {
                SoundTimer -= (byte)(TimeElapsed / 16);
            }

            if (!IsRomLoaded)
            {
                LoadROM(filepath);
            }

            for (var i = 0; i < TimeElapsed / InstructionInterval; i++)
            {
                Fetch();

                DecodeAndExecute();
            }
        }

        public void LoadROM(string filepath)
        {
            var loadedROM = System.IO.File.ReadAllBytes(filepath);

            System.Buffer.BlockCopy(loadedROM, 0, Memory, 512, loadedROM.Length);

            IsRomLoaded = true;
        }

        public void Fetch()
        {
            CurrentInstruction = BitConverter.ToUInt16(new byte[2] {  Memory[ProgramCounter + 1], Memory[ProgramCounter] }, 0);

            ProgramCounter += 2;
        }

        public void DecodeAndExecute()
        {
            var instructionInBytes = SplitUShortIntoBytes(CurrentInstruction);

            var nibblePair1 = SplitByteIntoNibbles(instructionInBytes[0]);

            var nibblePair2 = SplitByteIntoNibbles(instructionInBytes[1]);

            var finalThreeNibbles = GetFinalThreeNibbles(CurrentInstruction);

            if (CurrentInstruction == 0)
            {
                throw new Exception("Invalid Instruction: " + CurrentInstruction.ToString());
            }

            switch (nibblePair1[0])
            {
                case 0x0 when nibblePair2[0] == 0xE && nibblePair2[1] == 0xE:
                    RET();
                    break;

                case 0x0 when nibblePair2[0] == 0xE && nibblePair2[1] == 0x0:
                    CLS();
                    break;

                case 0x0:
                    SYS();
                    break;

                case 0x1:
                    JP(finalThreeNibbles);
                    break;

                case 0x2:
                    CALL(finalThreeNibbles);
                    break;

                case 0x3:
                    SEVx(nibblePair1[1], instructionInBytes[1]);
                    break;

                case 0x4:
                    SNEVx(nibblePair1[1], instructionInBytes[1]);
                    break;

                case 0x5:
                    SEVxVy(nibblePair1[1], nibblePair2[0]);
                    break;

                case 0x6:
                    LDVxByte(nibblePair1[1], instructionInBytes[1]);
                    break;

                case 0x7:
                    ADDVxByte(nibblePair1[1], instructionInBytes[1]);
                    break;

                case 0x8 when nibblePair2[1] == 0x0:
                    LDVxVy(nibblePair1[1], nibblePair2[0]);
                    break;

                case 0x8 when nibblePair2[1] == 0x1:
                    ORVxVy(nibblePair1[1], nibblePair2[0]);
                    break;

                case 0x8 when nibblePair2[1] == 0x2:
                    ANDVxVy(nibblePair1[1], nibblePair2[0]);
                    break;

                case 0x8 when nibblePair2[1] == 0x3:
                    XORVxVy(nibblePair1[1], nibblePair2[0]);
                    break;

                case 0x8 when nibblePair2[1] == 0x4:
                    ADDVxVy(nibblePair1[1], nibblePair2[0]);
                    break;

                case 0x8 when nibblePair2[1] == 0x5:
                    SUBVxVy(nibblePair1[1], nibblePair2[0]);
                    break;

                case 0x8 when nibblePair2[1] == 0x6:
                    SHRVxVy(nibblePair1[1], nibblePair2[0]);
                    break;

                case 0x8 when nibblePair2[1] == 0x7:
                    SUBNVxVy(nibblePair1[1], nibblePair2[0]);
                    break;

                case 0x8 when nibblePair2[1] == 0xE:
                    SHLVxVy(nibblePair1[1], nibblePair2[0]);
                    break;

                case 0x9:
                    SNEVxVy(nibblePair1[1], nibblePair2[0]);
                    break;

                case 0xA:
                    LDIAddr(finalThreeNibbles);
                    break;

                case 0xB:
                    JPV0Addr(finalThreeNibbles);
                    break;

                case 0xC:
                    RNDVxByte(nibblePair1[1], instructionInBytes[1]);
                    break;

                case 0xD:
                    DRWVxVyNibble(nibblePair1[1], nibblePair2[0], nibblePair2[1]);
                    break;

                case 0xE when nibblePair2[0] == 0x9:
                    SKPVx(nibblePair1[1]);
                    break;

                case 0xE when nibblePair2[0] == 0xA:
                    SKNPVx(nibblePair1[1]);
                    break;

                case 0xF when nibblePair2[0] == 0x0 && nibblePair2[1] == 0x7:
                    LDVxDT(nibblePair1[1]);
                    break;

                case 0xF when nibblePair2[0] == 0x0 && nibblePair2[1] == 0xA:
                    LDVxK(nibblePair1[1]);
                    break;

                case 0xF when nibblePair2[0] == 0x1 && nibblePair2[1] == 0x5:
                    LDDTVx(nibblePair1[1]);
                    break;

                case 0xF when nibblePair2[0] == 0x1 && nibblePair2[1] == 0x8:
                    LDSTVx(nibblePair1[1]);
                    break;

                case 0xF when nibblePair2[0] == 0x1 && nibblePair2[1] == 0xE:
                    ADDIVx(nibblePair1[1]);
                    break;

                case 0xF when nibblePair2[0] == 0x2 && nibblePair2[1] == 0x9:
                    LDFVx(nibblePair1[1]);
                    break;

                case 0xF when nibblePair2[0] == 0x3 && nibblePair2[1] == 0x3:
                    LDBVx(nibblePair1[1]);
                    break;

                case 0xF when nibblePair2[0] == 0x5 && nibblePair2[1] == 0x5:
                    LDIVx(nibblePair1[1]);
                    break;

                case 0xF when nibblePair2[0] == 0x6 && nibblePair2[1] == 0x5:
                    LDVxI(nibblePair1[1]);
                    break;

                default:
                    throw new Exception("Invalid Instruction: " + CurrentInstruction.ToString());
            }
        }

        private void RenderDisplay()
        {
            CurrentFrame = new Godot.Image();

            CurrentFrame.Create(64, 32, false, Godot.Image.Format.Rgba8);

            CurrentFrame.Lock();

            for (int i = 0; i < Display.GetLength(0); i++)
            {
                for (int j = 0; j < Display.GetLength(1); j++)
                {
                    CurrentFrame.SetPixel(i, j, Display[i, j] ? WhiteColor : BlackColor);
                }
            }

            CurrentFrame.Unlock();

            IsCurrentFrameRendered = false;

            // var output = new Bitmap(Display.GetLength(0), Display.GetLength(1));

            // for (int i = 0; i < Display.GetLength(0); i++)
            // {
            //     for (int j = 0; j < Display.GetLength(1); j++)
            //     {
            //         output.SetPixel(i, j, Display[i, j] ? Color.White : Color.Black);
            //     }
            // }

            // output.Save("test" + FilenameNumber + ".bmp");
        }

        private void SYS()
        {
            //No need to implement, only exists in native implementations of the interpreter
        }

        private void CLS()
        {
            for (var i = 0; i < Display.GetLength(0); i++)
            {
                for (var j = 0; j < Display.GetLength(1); j++)
                {
                    Display[i, j] = false;
                }
            }

            RenderDisplay();
        }

        private void RET()
        {
            ProgramCounter = Stack.Pop();
        }

        private void JP(ushort finalThreeNibbles)
        {
            ProgramCounter = finalThreeNibbles;
        }

        private void CALL(ushort finalThreeNibbles)
        {
            Stack.PushCheckForSize(ProgramCounter);

            ProgramCounter = finalThreeNibbles;
        }

        private void SEVx(byte secondNibble, byte secondByte)
        {
            if (VRegisters[secondNibble] == secondByte)
            {
                ProgramCounter += 2;
            }
        }

        private void SNEVx(byte secondNibble, byte secondByte)
        {
            if (VRegisters[secondNibble] != secondByte)
            {
                ProgramCounter += 2;
            }
        }

        private void SEVxVy(byte secondNibble, byte thirdNibble)
        {
            if (VRegisters[secondNibble] == VRegisters[thirdNibble])
            {
                ProgramCounter += 2;
            }
        }

        private void LDVxByte(byte secondNibble, byte secondByte)
        {
            VRegisters[secondNibble] = secondByte;
        }

        private void ADDVxByte(byte secondNibble, byte secondByte)
        {
            VRegisters[secondNibble] = (byte)((VRegisters[secondNibble] + secondByte) & 0x000000FF);
        }

        private void LDVxVy(byte secondNibble, byte thirdNibble)
        {
            VRegisters[secondNibble] = VRegisters[thirdNibble];
        }

        private void ORVxVy(byte secondNibble, byte thirdNibble)
        {
            VRegisters[secondNibble] = (byte)(VRegisters[secondNibble] | VRegisters[thirdNibble]);
        }

        private void ANDVxVy(byte secondNibble, byte thirdNibble)
        {
            VRegisters[secondNibble] = (byte)(VRegisters[secondNibble] & VRegisters[thirdNibble]);
        }

        private void XORVxVy(byte secondNibble, byte thirdNibble)
        {
            VRegisters[secondNibble] = (byte)(VRegisters[secondNibble] ^ VRegisters[thirdNibble]);
        }

        private void ADDVxVy(byte secondNibble, byte thirdNibble)
        {
            var additionResult = VRegisters[secondNibble] + VRegisters[thirdNibble];

            if (additionResult > 0xFF)
            {
                VRegisters[0xF] = 1;
            }
            else
            {
                VRegisters[0xF] = 0;
            }

            VRegisters[secondNibble] = (byte)(additionResult & 0x000000FF);
        }

        private void SUBVxVy(byte secondNibble, byte thirdNibble)
        {
            if (VRegisters[secondNibble] > VRegisters[thirdNibble])
            {
                VRegisters[0xF] = 1;
            }
            else
            {
                VRegisters[0xF] = 0;
            }

            VRegisters[secondNibble] -= VRegisters[thirdNibble];
        }

        private void SHRVxVy(byte secondNibble, byte thirdNibble)
        {
            VRegisters[0xF] = (byte)(VRegisters[secondNibble] & 0b00000001);

            VRegisters[secondNibble] = (byte)(VRegisters[secondNibble] >> 1);
        }

        private void SUBNVxVy(byte secondNibble, byte thirdNibble)
        {
            if (VRegisters[thirdNibble] > VRegisters[secondNibble])
            {
                VRegisters[0xF] = 1;
            }
            else
            {
                VRegisters[0xF] = 0;
            }

            VRegisters[thirdNibble] -= VRegisters[secondNibble];
        }

        private void SHLVxVy(byte secondNibble, byte thirdNibble)
        {
            VRegisters[0xF] = (byte)((VRegisters[secondNibble] & 0b10000000) >> 7);

            VRegisters[secondNibble] = (byte)(VRegisters[secondNibble] << 1);
        }

        private void SNEVxVy(byte secondNibble, byte thirdNibble)
        {
            if (VRegisters[secondNibble] != VRegisters[thirdNibble])
            {
                ProgramCounter += 2;
            }
        }

        private void LDIAddr(ushort finalThreeNibbles)
        {
            IRegister = finalThreeNibbles;
        }

        private void JPV0Addr(ushort finalThreeNibbles)
        {
            ProgramCounter = (ushort)(finalThreeNibbles + VRegisters[0]);
        }

        private void RNDVxByte(byte secondNibble, byte secondByte)
        {
            var randomNumberGenerator = new Random();

            var randomNumber = (byte)randomNumberGenerator.Next(0, 266);

            VRegisters[secondNibble] = (byte)(randomNumber & secondByte);
        }

        private void DRWVxVyNibble(byte secondNibble, byte thirdNibble, byte fourthNibble)
        {
            var xCoordinate = VRegisters[secondNibble] % 64;

            var yCoordinate = VRegisters[thirdNibble] % 32;

            VRegisters[15] = 0;

            for (var i = 0; i < fourthNibble; i++)
            {
                xCoordinate = VRegisters[secondNibble] % 64;

                if (yCoordinate >= 32)
                {
                    break;
                }

                var spriteRow = new BitArray(new byte[] { Memory[IRegister + i] });

                for (int j = spriteRow.Length - 1; j > -1; j--)
                {
                    if (xCoordinate >= 64)
                    {
                        break;
                    }

                    // if (spriteRow[j])
                    // {
                    //     Console.WriteLine("New pixel");
                    // }

                    if (spriteRow[j] & Display[xCoordinate, yCoordinate])
                    {
                        VRegisters[15] = 1;
                    }

                    Display[xCoordinate, yCoordinate] = spriteRow[j] ^ Display[xCoordinate, yCoordinate];

                    xCoordinate++;
                }

                yCoordinate++;
            }

            RenderDisplay();

            FilenameNumber++;
        }

        private void SKPVx(byte secondNibble)
        {
            if (Inputs[VRegisters[secondNibble]])
            {
                ProgramCounter += 2;
            }
        }

        private void SKNPVx(byte secondNibble)
        {
            if (!Inputs[VRegisters[secondNibble]])
            {
                ProgramCounter += 2;
            }
        }

        private void LDVxDT(byte secondNibble)
        {
            VRegisters[secondNibble] = DelayTimer;
        }

        private void LDVxK(byte secondNibble)
        {
            if (IsKeyPressed.Item1)
            {
                VRegisters[secondNibble] = IsKeyPressed.Item2;
            }
            else
            {
                ProgramCounter -= 2;
            }
        }

        private void LDDTVx(byte secondNibble)
        {
            DelayTimer = VRegisters[secondNibble];
        }

        private void LDSTVx(byte secondNibble)
        {
            SoundTimer = VRegisters[secondNibble];
        }

        private void ADDIVx(byte secondNibble)
        {
            if ((IRegister + VRegisters[secondNibble] > 0xFFF))
            {
                VRegisters[0xF] = 1;
            }

            IRegister = (ushort)((IRegister + VRegisters[secondNibble]) & 0x00000FFF);
        }

        private void LDFVx(byte secondNibble)
        {
            switch (secondNibble)
            {
                case 0x0:
                    IRegister = 0x050;
                    break;

                case 0x1:
                    IRegister = 0x055;
                    break;

                case 0x2:
                    IRegister = 0x05A;
                    break;

                case 0x3:
                    IRegister = 0x05F;
                    break;

                case 0x4:
                    IRegister = 0x064;
                    break;

                case 0x5:
                    IRegister = 0x069;
                    break;

                case 0x6:
                    IRegister = 0x06E;
                    break;

                case 0x7:
                    IRegister = 0x073;
                    break;

                case 0x8:
                    IRegister = 0x078;
                    break;

                case 0x9:
                    IRegister = 0x07D;
                    break;

                case 0xA:
                    IRegister = 0x082;
                    break;

                case 0xB:
                    IRegister = 0x087;
                    break;

                case 0xC:
                    IRegister = 0x08C;
                    break;

                case 0xD:
                    IRegister = 0x091;
                    break;

                case 0xE:
                    IRegister = 0x096;
                    break;

                case 0xF:
                    IRegister = 0x09B;
                    break;
            }
        }

        private void LDBVx(byte secondNibble)
        {
            var numberToSplit = VRegisters[secondNibble];

            for (var i = 0; i < 3; i++)
            {
                Memory[IRegister + 2 - i] = (byte)(numberToSplit % 10);

                numberToSplit /= 10;
            }

            // var numberAsString = VRegisters[secondNibble].ToString();

            // if (numberAsString.Length == 3)
            // {
            //     Memory[IRegister] = Byte.Parse(numberAsString[0].ToString());

            //     Memory[IRegister + 1] = Byte.Parse(numberAsString[1].ToString());

            //     Memory[IRegister + 2] = Byte.Parse(numberAsString[2].ToString());
            // }
            // else if (numberAsString.Length == 2)
            // {
            //     Memory[IRegister] = 0;

            //     Memory[IRegister + 1] = Byte.Parse(numberAsString[1].ToString());

            //     Memory[IRegister + 2] = Byte.Parse(numberAsString[2].ToString());
            // }
            // else
            // {
            //     Memory[IRegister] = 0;

            //     Memory[IRegister + 1] = 0;

            //     Memory[IRegister + 2] = Byte.Parse(numberAsString[2].ToString());
            // }
        }

        private void LDIVx(byte secondNibble)
        {
            for (int i = 0; i <= secondNibble; i++)
            {
                Memory[IRegister + i] = VRegisters[i];
            }
        }

        private void LDVxI(byte secondNibble)
        {
            for (int i = 0; i <= secondNibble; i++)
            {
                VRegisters[i] = Memory[IRegister + i];
            }
        }

        //Super Chip-48 Exclusive Instructions

        private void SCDNibble()
        {
            throw new NotImplementedException();
        }

        private void SCR()
        {
            throw new NotImplementedException();
        }

        private void SCL()
        {
            throw new NotImplementedException();
        }

        private void EXIT()
        {
            throw new NotImplementedException();
        }

        private void LOW()
        {
            throw new NotImplementedException();
        }

        private void HIGH()
        {
            throw new NotImplementedException();
        }

        private void DRWVxVy0()
        {
            throw new NotImplementedException();
        }

        private void LDHFVx()
        {
            throw new NotImplementedException();
        }

        private void LDRVx()
        {
            throw new NotImplementedException();
        }

        private void LDVxR()
        {
            throw new NotImplementedException();
        }

        //Helper methods

        private byte[] SplitUShortIntoBytes(ushort valueToBeSplit)
        {
            byte[] result = new byte[2];

            result[0] = (byte)(valueToBeSplit >> 8);

            result[1] = (byte)valueToBeSplit;

            return result;
        }

        private byte[] SplitByteIntoNibbles(byte valueToBeSplit)
        {
            byte[] result = new byte[2];

            result[0] = (byte)((valueToBeSplit & 0xF0) >> 4);

            result[1] = (byte)(valueToBeSplit & 0x0F);

            return result;
        }

        private ushort GetFinalThreeNibbles(ushort valueToBeSplit)
        {
            return (ushort)(0x0FFF & valueToBeSplit);
        }
    }
}
