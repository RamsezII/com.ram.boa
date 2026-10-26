using System;

namespace Boa
{
    public interface IContextList
    {
        void AddButton(string label, Action action);
        void AddLine();
        void AddSublist(string label, Action<IContextList> onSublist);
    }
}