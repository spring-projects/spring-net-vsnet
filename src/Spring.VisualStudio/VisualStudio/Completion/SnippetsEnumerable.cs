﻿#region License

/*
 * Copyright 2002-2011 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

// From Microsoft.Samples.VisualStudio.IronPython

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TextManager.Interop;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;
using VSConstants = Microsoft.VisualStudio.VSConstants;

namespace Spring.VisualStudio.Completion
{
    /// <summary>
    /// Enumerates the code snippets of Spring
    /// </summary>
    /// <author>Bruno Baia</author>
    internal class SnippetsEnumerable : IEnumerable<VsExpansion>
    {
        /// <summary>
        /// This structure is used to facilitate the interop calls to the method
        /// exposed by IVsExpansionEnumeration.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct ExpansionBuffer
        {
            public IntPtr pathPtr;
            public IntPtr titlePtr;
            public IntPtr shortcutPtr;
            public IntPtr descriptionPtr;
        }

        private IVsTextManager2 textManager;
        private Guid languageGuid;
        private bool shortcutOnly;

        /// <summary>
        /// This is a managed enumerator which wraps an COM-enumerator to make consumig it easier
        /// </summary>
        /// <param name="languageGuid">
        /// This is the language service GUID for which you want to enumerate snippets (Spring in our case)
        /// </param>
        public SnippetsEnumerable(IVsTextManager2 textManager, Guid languageGuid)
        {
            if (null == textManager)
            {
                throw new ArgumentNullException("textManager");
            }
            this.textManager = textManager;
            this.languageGuid = languageGuid;
        }

        public bool ShortcutOnly
        {
            get { return this.shortcutOnly; }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            set { this.shortcutOnly = value; }
        }

        #region IEnumerable<VsExpansion> Members

        public IEnumerator<VsExpansion> GetEnumerator()
        {
            IVsExpansionManager expansionManager;
            ErrorHandler.ThrowOnFailure(textManager.GetExpansionManager(out expansionManager));

            IVsExpansionEnumeration enumerator;
            int onlyShortcut = (this.ShortcutOnly ? 1 : 0);
            ErrorHandler.ThrowOnFailure(expansionManager.EnumerateExpansions(languageGuid, onlyShortcut, null, 0, 0, 0, out enumerator));

            ExpansionBuffer buffer = new ExpansionBuffer();
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                int hr = VSConstants.S_OK;
                uint fetched;
                // loop through the available snippets using the COM enumerator
                while (VSConstants.S_OK == (hr = enumerator.Next(1, new IntPtr[] { handle.AddrOfPinnedObject() }, out fetched)))
                {
                    buffer = (ExpansionBuffer)handle.Target;
                    try
                    {
                        handle.Free();
                        if (IntPtr.Zero != buffer.shortcutPtr)
                        {
                            // create a VsExpansion entry for each snippet found
                            VsExpansion expansion = new VsExpansion();
                            expansion.shortcut = Marshal.PtrToStringBSTR(buffer.shortcutPtr);
                            if (IntPtr.Zero != buffer.descriptionPtr)
                            {
                                expansion.description = Marshal.PtrToStringBSTR(buffer.descriptionPtr);
                            }
                            if (IntPtr.Zero != buffer.pathPtr)
                            {
                                expansion.path = Marshal.PtrToStringBSTR(buffer.pathPtr);
                            }
                            if (IntPtr.Zero != buffer.titlePtr)
                            {
                                expansion.title = Marshal.PtrToStringBSTR(buffer.titlePtr);
                            }
                            yield return expansion;
                            handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                        }
                    }
                    finally
                    {
                        // make sure we free up previously allocated memory
                        if (IntPtr.Zero != buffer.descriptionPtr)
                        {
                            Marshal.FreeBSTR(buffer.descriptionPtr);
                            buffer.descriptionPtr = IntPtr.Zero;
                        }
                        if (IntPtr.Zero != buffer.pathPtr)
                        {
                            Marshal.FreeBSTR(buffer.pathPtr);
                            buffer.pathPtr = IntPtr.Zero;
                        }
                        if (IntPtr.Zero != buffer.shortcutPtr)
                        {
                            Marshal.FreeBSTR(buffer.shortcutPtr);
                            buffer.shortcutPtr = IntPtr.Zero;
                        }
                        if (IntPtr.Zero != buffer.titlePtr)
                        {
                            Marshal.FreeBSTR(buffer.titlePtr);
                            buffer.titlePtr = IntPtr.Zero;
                        }
                    }
                }
                ErrorHandler.ThrowOnFailure(hr);
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        #endregion

        #region IEnumerable Members

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        #endregion
    }
}

