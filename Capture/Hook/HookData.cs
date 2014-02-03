namespace Capture.Hook
{
    using System;
    using System.Runtime.InteropServices;

    using EasyHook;

    public class HookData<T> : HookData
        where T : class
    {
        private readonly T original;

        public HookData(IntPtr func, Delegate inNewProc, object owner)
            : base(func, inNewProc, owner)
        {
            original = (T)(object)Marshal.GetDelegateForFunctionPointer(func, typeof(T));
        }

        public T Original
        {
            get
            {
                return original;
            }
        }
    }

    public class HookData
    {
        private readonly IntPtr func;

        private readonly Delegate inNewProc;

        private readonly object owner;

        private LocalHook localHook;

        private bool isHooked;

        public HookData(IntPtr func, Delegate inNewProc, object owner)
        {
            this.func = func;
            this.inNewProc = inNewProc;
            this.owner = owner;
            CreateHook();
        }

        public LocalHook Hook
        {
            get
            {
                return this.localHook;
            }
        }

        public void CreateHook()
        {
            if (localHook != null) return;
            this.localHook = LocalHook.Create(func, inNewProc, owner);
        }

        public void UnHook()
        {
            //if (!this.isHooked) return;
            //this.isHooked = false;
            //this.Hook.ThreadACL.SetInclusiveACL(new Int32[] { 0 });

            //if (localHook == null) return;
            //this.isHooked = false;
            //this.Hook.ThreadACL.SetInclusiveACL(new Int32[] { 0 });
            //localHook.Dispose();
            //localHook = null;
        }

        public void ReHook()
        {
            if (this.isHooked) return;
            this.isHooked = true;
            this.Hook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
        }
    }
}