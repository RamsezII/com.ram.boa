namespace Boa
{
    public readonly struct ButtonState
    {
        public readonly bool down, pressed, up;
        internal ButtonState(bool down, bool pressed, bool up)
        {
            this.down = down;
            this.pressed = pressed;
            this.up = up;
        }
    }
}