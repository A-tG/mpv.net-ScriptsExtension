﻿using AtgScriptsExtension.Extensions;
using AtgScriptsExtension.Helpers;
using mpvnet;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using static libmpv;

namespace AtgScriptsExtension
{
    public class ScreenshotToClipboardScript
    {
        public const string Name = "atg_screenshot-to-clipboard";

        private const string scrRawCommand = "screenshot-raw";
        private CorePlayer m_core = Global.Core;

        public ScreenshotToClipboardScript()
        {
            m_core.ClientMessage += OnMessage;
        }

        public bool TryScreenshotToClipboard()
        {
            try
            {
                ScreenshotToClipboard();
                return true;
            }
            catch { }
            return false;
        }

        private void ScreenshotToClipboard()
        {
            var img = GetRawScreenshot();

            var thread = new Thread(() =>
            {
                // need to be done in STA thread
                Clipboard.SetImage(img);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            thread.Join();
        }

        [DllImport("mpv-2.dll")]
        internal static extern int mpv_command_node(IntPtr ctx, IntPtr args, IntPtr result);

        unsafe private Bitmap GetRawScreenshot()
        {
            var args = new mpv_node
            {
                format = mpv_format.MPV_FORMAT_NODE_ARRAY
            };
            var result = new mpv_node();

            var list = new mpv_node_list();
            var listValues = new mpv_node
            {
                format = mpv_format.MPV_FORMAT_STRING
            };

            byte* cmdPtr = stackalloc byte[scrRawCommand.Length + 1];
            MarHelper.CopyStrToByteStrBuff(scrRawCommand, cmdPtr);
            listValues.str = (IntPtr)cmdPtr;

            var lvPtr = stackalloc byte[Marshal.SizeOf(listValues)];
            var listValPtr = (IntPtr)lvPtr;
            Marshal.StructureToPtr(listValues, listValPtr, false);
            list.values = listValPtr;

            var lPtr = stackalloc byte[Marshal.SizeOf(list)];
            var listPtr = (IntPtr)lPtr;
            list.num = 1;
            args.list = listPtr;
            Marshal.StructureToPtr(list, listPtr, false);

            var rPtr = stackalloc byte[Marshal.SizeOf(result)];
            var resultPtr = (IntPtr)rPtr;
            Marshal.StructureToPtr(result, resultPtr, false);

            var aPtr = stackalloc byte[Marshal.SizeOf(args)];
            var argsPtr = (IntPtr)aPtr;
            Marshal.StructureToPtr(args, argsPtr, false);

            var res = (mpv_error)mpv_command_node(m_core.Handle, argsPtr, resultPtr);
            if (res != mpv_error.MPV_ERROR_SUCCESS)
            {
                throw new InvalidOperationException(
                    "Command returned error: " + 
                    ((int)res).ToString() + " " + 
                    res.ToString());
            }

            result = Marshal.PtrToStructure<mpv_node>(resultPtr);
            var resultList = Marshal.PtrToStructure<mpv_node_list>(result.list);

            var screenshot = BitmapFromMpvNodeList(resultList);

            mpv_free_node_contents(resultPtr);

            return screenshot;
        }
        private Bitmap BitmapFromMpvNodeList(mpv_node_list list)
        {
            Bitmap bm;
            long w, h, stride;
            w = h = stride = 0;
            string format = string.Empty;
            var ba = new mpv_byte_array();

            for (int i = 0; i < list.num; i++)
            {
                int keyOffset = i * IntPtr.Size;
                int nodeOffset = i * Marshal.SizeOf<mpv_node>();

                var ptrVal = Marshal.ReadInt64(list.keys + keyOffset);

                string key = Marshal.PtrToStringAnsi(new IntPtr(ptrVal));
                var node = Marshal.PtrToStructure<mpv_node>(list.values + nodeOffset);

                switch (key)
                {
                    case "w":
                        w = node.int64;
                        break;
                    case "h":
                        h = node.int64;
                        break;
                    case "stride":
                        stride = node.int64;
                        break;
                    case "format":
                        format = Marshal.PtrToStringAnsi(node.str);
                        break;
                    case "data":
                        ba = Marshal.PtrToStructure<mpv_byte_array>(node.ba);
                        break;
                    default:
                        break;
                }
            }
            switch (format)
            {
                case "bgr0":
                    bm = new Bitmap((int)w, (int)h, PixelFormat.Format24bppRgb);
                    bm.ReadRgbFromRgb0(ba.data, (int)ba.size.ToUInt64());
                    break;
                default:
                    throw new ArgumentException("Not supported color format");
            }
            return bm;
        }

        private void OnMessage(string[] args)
        {
            if ((args == null) || (args.Length == 0)) return;

            if (args[0] != Name) return;

            string text = "Copy Screenshot to clipboard";
            m_core.CommandV("show-text", text);

            text += TryScreenshotToClipboard() ?
                ": Succeded" :
                ": Failed";
            m_core.CommandV("show-text", text);
        }
    }
}
