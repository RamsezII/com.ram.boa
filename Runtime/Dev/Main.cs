using _BOA_;
using System;

[Serializable]
public abstract partial class BoaComponent : IBoaAssembly
{

    //----------------------------------------------------------------------------------------------------------

    public virtual void Start()
    {
    }

    //----------------------------------------------------------------------------------------------------------

    public virtual void Update(float deltaTime)
    {
    }

    //----------------------------------------------------------------------------------------------------------

    public virtual void Stop()
    {
    }
}