using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ram._BOA_")]

namespace Boa
{
    public static partial class Util
    {
        public static T As<T>(this object o) => (T)o;
    }
}