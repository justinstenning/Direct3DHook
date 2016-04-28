using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EasyHook;

namespace Capture.Hook
{
    // Thanks to remcoros for the initial version of the following helper classes

    /// <summary>
    /// Extends <see cref="Hook"/> with support for accessing the Original method from within a hook delegate
    /// </summary>
    /// <typeparam name="T">A delegate type</typeparam>
    public class Hook<T> : Hook
        where T: class
    {
        /// <summary>
        /// When called from within the <see cref="Hook.NewFunc"/> delegate this will call the original function at <see cref="Hook.FuncToHook"/>.
        /// </summary>
        public T Original { get; private set; }

        /// <summary>
        /// Creates a new hook at <paramref name="funcToHook"/> redirecting to <paramref name="newFunc"/>. The hook starts inactive so a call to <see cref="Activate"/> is required to enable the hook.
        /// </summary>
        /// <param name="funcToHook">A pointer to the location to insert the hook</param>
        /// <param name="newFunc">The delegate to call from the hooked location</param>
        /// <param name="owner">The object to assign as the "callback" object within the <see cref="EasyHook.LocalHook"/> instance.</param>
        public Hook(IntPtr funcToHook, Delegate newFunc, object owner)
            : base(funcToHook, newFunc, owner)
        {
            // Debug assertion that T is a Delegate type
            Debug.Assert(typeof(Delegate).IsAssignableFrom(typeof(T)));

            Original = (T)(object)Marshal.GetDelegateForFunctionPointer(funcToHook, typeof(T));
        }
    }

    /// <summary>
    /// Wraps the <see cref="EasyHook.LocalHook"/> class with a simplified active/inactive state
    /// </summary>
    public class Hook: IDisposable
    {
        /// <summary>
        /// The hooked function location
        /// </summary>
        public IntPtr FuncToHook { get; }
        
        /// <summary>
        /// The replacement delegate
        /// </summary>
        public Delegate NewFunc { get; }
        
        /// <summary>
        /// The callback object passed to LocalHook constructor
        /// </summary>
        public object Owner { get; }
        
        /// <summary>
        /// The <see cref="EasyHook.LocalHook"/> instance
        /// </summary>
        public LocalHook LocalHook { get; private set; }
        
        /// <summary>
        /// Indicates whether the hook is currently active
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Creates a new hook at <paramref name="funcToHook"/> redirecting to <paramref name="newFunc"/>. The hook starts inactive so a call to <see cref="Activate"/> is required to enable the hook.
        /// </summary>
        /// <param name="funcToHook">A pointer to the location to insert the hook</param>
        /// <param name="newFunc">The delegate to call from the hooked location</param>
        /// <param name="owner">The object to assign as the "callback" object within the <see cref="EasyHook.LocalHook"/> instance.</param>
        public Hook(IntPtr funcToHook, Delegate newFunc, object owner)
        {
            FuncToHook = funcToHook;
            NewFunc = newFunc;
            Owner = owner;
            
            CreateHook();
        }

        ~Hook()
        {
            Dispose(false);
        }

        protected void CreateHook()
        {
            if (LocalHook != null) return;

            LocalHook = LocalHook.Create(FuncToHook, NewFunc, Owner);
        }

        protected void UnHook()
        {
            if (IsActive)
                Deactivate();

            if (LocalHook != null)
            {
                LocalHook.Dispose();
                LocalHook = null;
            }
        }

        /// <summary>
        /// Activates the hook
        /// </summary>
        public void Activate()
        {
            if (LocalHook == null)
                CreateHook();

            if (IsActive) return;
            
            IsActive = true;
            LocalHook.ThreadACL.SetExclusiveACL(new[] { 0 });
        }

        /// <summary>
        /// Deactivates the hook
        /// </summary>
        public void Deactivate()
        {
            if (!IsActive) return;

            IsActive = false;
            LocalHook.ThreadACL.SetInclusiveACL(new[] { 0 });
        }


        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposeManagedObjects)
        {
            // Only clean up managed objects if disposing (i.e. not called from destructor)
            if (disposeManagedObjects)
            {
                UnHook();
            }
        }
    }
}
