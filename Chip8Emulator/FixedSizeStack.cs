using System;
using System.Collections.Generic;

namespace Chip8Emulator
{
    public class FixedSizeStack<T> : Stack<T>
    {
        private readonly int maxSize;

        public FixedSizeStack(int maxSize)
        {
            this.maxSize = maxSize;
        }

        public void PushCheckForSize(T item)
        {
            if (Count < maxSize)
            {
                base.Push(item);
            }
            else
            {
                throw new Exception("Exceeded size of stack");
            }
        }
    }
}