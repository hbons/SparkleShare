//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SparkleShare {

   public class Shortcut : IDisposable {
        
        private IShellLinkW link;
        
        public Shortcut ()
        {
        }
        
        
        public void Create (string file_path, string target_path)
        {
            link = (IShellLinkW) new CShellLink ();
            this.link.SetShowCmd (1);        
            this.link.SetPath (target_path);
            (this.link as IPersistFile).Save (file_path, true);
        }
            
        
        public void Dispose () {
            if (this.link == null )
                return;
            
            Marshal.ReleaseComObject (this.link);
            this.link = null;
        }
        
        
        ~Shortcut ()
        {
            Dispose ();
        }
        

        private class UnManagedMethods {
            [DllImport ("Shell32", CharSet = CharSet.Auto)]
            internal extern static int ExtractIconEx (
                [MarshalAs(UnmanagedType.LPTStr)] string lpszFile, int nIconIndex,
                IntPtr[] phIconLarge, IntPtr[] phIconSmall,    int nIcons);
        
            [DllImport ("user32")]
            internal static extern int DestroyIcon (IntPtr hIcon);
        }
        
        
        [StructLayoutAttribute (LayoutKind.Sequential, Pack = 4, Size = 0)]
        private struct _FILETIME {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }  
        
        
        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Unicode)]
        private struct _WIN32_FIND_DATAW {
            public uint dwFileAttributes;
            public _FILETIME ftCreationTime;
            public _FILETIME ftLastAccessTime;
            public _FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            
            [MarshalAs(UnmanagedType.ByValTStr , SizeConst = 260)]
            public string cFileName;
            
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }
        
        
        [ComImportAttribute ()]
        [GuidAttribute ("0000010C-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute (ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPersist {
            [PreserveSig]
            void GetClassID (out Guid pClassID);
        }
        
        
        [ComImportAttribute ()]
        [GuidAttribute ("0000010B-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute (ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPersistFile {
            [PreserveSig]
            void GetClassID (out Guid pClassID);
            void IsDirty ();
            
            void Load(
                [MarshalAs (UnmanagedType.LPWStr)] string pszFileName, 
                uint dwMode);
            
            void Save(
                [MarshalAs (UnmanagedType.LPWStr)] string pszFileName, 
                [MarshalAs (UnmanagedType.Bool)] bool fRemember);
            
            void SaveCompleted(
                [MarshalAs (UnmanagedType.LPWStr)] string pszFileName);
            
            void GetCurFile (
                [MarshalAs (UnmanagedType.LPWStr)] out string ppszFileName);
        }
        
        
        [ComImportAttribute()]
        [GuidAttribute("000214F9-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath(
                [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, 
                int cchMaxPath, 
                ref _WIN32_FIND_DATAW pfd, 
                uint fFlags);
            
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            
            void GetDescription(
                [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
                int cchMaxName);
            
            void SetDescription(
                [MarshalAs(UnmanagedType.LPWStr)] string pszName);
                
            void GetWorkingDirectory(
                [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir,
                int cchMaxPath);
            
            void SetWorkingDirectory(
                [MarshalAs(UnmanagedType.LPWStr)] string pszDir);
                
            void GetArguments(
                [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, 
                int cchMaxPath);
            
            void SetArguments(
                [MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
                
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short pwHotkey);
            
            void GetShowCmd(out uint piShowCmd);
            void SetShowCmd(uint piShowCmd);
            
            void GetIconLocation(
                [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, 
                int cchIconPath, 
                out int piIcon);
            
            void SetIconLocation(
                [MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, 
                int iIcon);
            
            void SetRelativePath(
                [MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, 
                uint dwReserved);
            
            void Resolve(IntPtr hWnd, uint fFlags);
            
            void SetPath(
                [MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }
    
        
        [GuidAttribute ("00021401-0000-0000-C000-000000000046")]
        [ClassInterfaceAttribute (ClassInterfaceType.None)]
        [ComImportAttribute ()]
        private class CShellLink {}
    }
}
