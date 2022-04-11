using AtgScriptsExtension.Extensions;
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
        private Bitmap m_bmp;
        private string m_errorMessage = string.Empty;

        public ScreenshotToClipboardScript()
        {
            m_core.ClientMessage += OnMessage;
        }

        public bool TryScreenshotToClipboard()
        {
            bool res = false;
            try
            {
                ScreenshotToClipboard();
                res = true;
            }
            catch (Exception ex)
            {
                m_errorMessage = $"{ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                m_bmp?.Dispose();
            }
            return res;
        }

        private void ScreenshotToClipboard()
        {
            GetRawScreenshot(out m_bmp);

            var thread = new Thread(() =>
            {
                // need to be done in STA thread
                Clipboard.SetImage(m_bmp);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            thread.Join();
        }

        [DllImport("mpv-2.dll")]
        internal static extern int mpv_command_node(IntPtr ctx, IntPtr args, IntPtr result);

        unsafe private void GetRawScreenshot(out Bitmap bmp, string flags = "")
        {
            bool hasFlags = !string.IsNullOrEmpty(flags);

            var argsNodeListFlagsVal = new mpv_node
            {
                format = mpv_format.MPV_FORMAT_STRING
            };
            if (hasFlags)
            {
                byte* flagsPtr = stackalloc byte[flags.Length + 1];
                MarHelper.CopyStrToByteStrBuff(flags, flagsPtr);
                argsNodeListFlagsVal.str = (IntPtr)flagsPtr;
            }

            var argsNodeListCmdVal = new mpv_node
            {
                format = mpv_format.MPV_FORMAT_STRING
            };
            byte* cmdPtr = stackalloc byte[scrRawCommand.Length + 1];
            MarHelper.CopyStrToByteStrBuff(scrRawCommand, cmdPtr);
            argsNodeListCmdVal.str = (IntPtr)cmdPtr;

            var argsNodeList = new mpv_node_list();
            int argsLen = hasFlags ? 2 : 1;
            var listValSize = Marshal.SizeOf(argsNodeListCmdVal);
            var lvPtr = stackalloc byte[listValSize * argsLen];
            var listValPtr = (IntPtr)lvPtr;
            Marshal.StructureToPtr(argsNodeListCmdVal, listValPtr, false);
            if (hasFlags)
            {
                Marshal.StructureToPtr(argsNodeListFlagsVal, listValPtr + listValSize, false);
            }
            argsNodeList.values = listValPtr;

            var argsNode = new mpv_node
            {
                format = mpv_format.MPV_FORMAT_NODE_ARRAY
            };
            var lPtr = stackalloc byte[Marshal.SizeOf(argsNodeList)];
            var listPtr = (IntPtr)lPtr;
            argsNodeList.num = argsLen;
            argsNode.list = listPtr;
            Marshal.StructureToPtr(argsNodeList, listPtr, false);

            var aPtr = stackalloc byte[Marshal.SizeOf(argsNode)];
            var argsPtr = (IntPtr)aPtr;
            Marshal.StructureToPtr(argsNode, argsPtr, false);

            var resultNode = new mpv_node();
            var rPtr = stackalloc byte[Marshal.SizeOf(resultNode)];
            var resultPtr = (IntPtr)rPtr;
            Marshal.StructureToPtr(resultNode, resultPtr, false);

            var res = (mpv_error)mpv_command_node(m_core.Handle, argsPtr, resultPtr);
            if (res != mpv_error.MPV_ERROR_SUCCESS)
            {
                throw new InvalidOperationException($"Command returned error: {((int)res)} {res}");
            }

            resultNode = Marshal.PtrToStructure<mpv_node>(resultPtr);
            var resultList = Marshal.PtrToStructure<mpv_node_list>(resultNode.list);

            GetBitmapFromMpvNodeList(out bmp, resultList);

            mpv_free_node_contents(resultPtr);

        }
        private void GetBitmapFromMpvNodeList(out Bitmap bmp, mpv_node_list list)
        {
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
                        format = Marshal.PtrToStringAnsi(node.str) ?? string.Empty;
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
                    bmp = new Bitmap((int)w, (int)h, PixelFormat.Format24bppRgb);
                    bmp.ReadRgbFromRgb0(ba.data, (int)ba.size.ToUInt64());
                    break;
                default:
                    throw new ArgumentException($"Unsupported color format: {format}");
            }
        }

        private void OnMessage(string[] args)
        {
            if ((args == null) || (args.Length == 0)) return;

            if (args[0] != Name) return;

            string text = "Copy Screenshot to clipboard";
            m_core.CommandV("show-text", text);

            string duration = m_core.GetPropertyOsdString("osd-duration");
            if (TryScreenshotToClipboard())
            {
                text += ": Succeded";
            }
            else
            {
                text += ": Failed";
                if (!string.IsNullOrEmpty(m_errorMessage))
                {
                    text += '\n' + m_errorMessage;
                    duration = 5000.ToString();
                }
            }
            m_errorMessage = string.Empty;

            m_core.CommandV("show-text", text, duration);
        }
    }
}
