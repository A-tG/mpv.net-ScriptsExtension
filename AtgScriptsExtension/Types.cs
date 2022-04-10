using System;
using System.Runtime.InteropServices;

namespace AtgScriptsExtension
{
    [StructLayout(LayoutKind.Sequential)]
    struct mpv_node_list
    {
        public int num;
        public IntPtr values;
        public IntPtr keys;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct mpv_byte_array
    {
        public IntPtr data;
        public UIntPtr size;
    }
}
