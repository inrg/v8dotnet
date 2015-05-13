﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

#if !(V1_1 || V2 || V3 || V3_5)
using System.Dynamic;
#endif

namespace V8.Net
{
    // ========================================================================================================================
    // The handles section has methods to deal with creating and disposing of managed handles (which wrap native V8 handles).
    // This helps to reuse existing handles to prevent having to create new ones every time, thus greatly speeding things up.

    public unsafe partial class V8Engine
    {
        // --------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Holds an index of all handle proxies created for this engine instance.
        /// </summary>
        internal HandleProxy*[] _HandleProxies = new HandleProxy*[1000];

        /// <summary>
        /// Returns all the handles currently known on the managed side.
        /// Each InternalHandle is only a wrapper for a tracked HandleProxy native object and does not need to be disposed.
        /// Because of this, no reference counts are incremented, and thus, disposing them may destroy handles in use.
        /// This list is mainly provided for debugging purposes only.
        /// </summary>
        public IEnumerable<InternalHandle> Handles_All
        {
            get
            {
                lock (_HandleProxies)
                {
                    List<InternalHandle> handles = new List<InternalHandle>(_HandleProxies.Length);
                    for (var i = 0; i < _HandleProxies.Length; ++i)
                        if (_HandleProxies[i] != null)
                            handles.Add(InternalHandle._WrapOnly(_HandleProxies[i]));
                    return handles.ToArray();
                }
            }
        }

        public IEnumerable<InternalHandle> Handles_Active { get { return from h in Handles_All where h.__HandleProxy->Disposed == 0 select h; } }
        public IEnumerable<InternalHandle> Handles_ManagedSideDisposeReady { get { return from h in Handles_All where h.__HandleProxy->Disposed == 1 select h; } }
        public IEnumerable<InternalHandle> Handles_NativeSideWeak { get { return from h in Handles_All where h.__HandleProxy->Disposed == 2 select h; } }
        public IEnumerable<InternalHandle> Handles_DisposedAndCached { get { return from h in Handles_All where h.__HandleProxy->Disposed == 3 select h; } }

        /// <summary>
        /// Total number of handle proxy references in the V8.NET system (for proxy use).
        /// </summary>
        public int TotalHandles
        {
            get
            {
                lock (_HandleProxies)
                {
                    var c = 0;
                    foreach (var item in _HandleProxies)
                        if (item != null) c++;
                    return c;
                }
            }
        }

        /// <summary>
        /// Total number of handle proxy references in the V8.NET system that are in the process of being disposed by the V8.NET garbage collector (the worker thread).
        /// </summary>
        public int TotalHandlesBeingDisposed
        {
            get
            {
                lock (_HandleProxies)
                {
                    var c = 0;
                    foreach (var item in _HandleProxies)
                        if (item != null && item->IsBeingDisposed) c++;
                    return c;
                }
            }
        }


        /// <summary>
        /// Total number of handles in the V8.NET system that are cached and ready to be reused.
        /// </summary>
        public int TotalHandlesCached
        {
            get
            {
                lock (_HandleProxies)
                {
                    var c = 0;
                    foreach (var item in _HandleProxies)
                        if (item != null && item->IsDisposed) c++;
                    return c;
                }
            }
        }

        // --------------------------------------------------------------------------------------------------------------------

        void _Initialize_Handles()
        {
        }

        // --------------------------------------------------------------------------------------------------------------------
    }

    // ========================================================================================================================
}
