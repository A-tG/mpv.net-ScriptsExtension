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
    public class ScreenshotToClipboardScript : MpvNetScriptBase
    {
        private const string scrRawCommand = "screenshot-raw";
        private Bitmap m_bmp;
        private string m_errorMessage = string.Empty;

        public ScreenshotToClipboardScript(string name) : base(name)
        {
            m_core.ClientMessage += OnMessage;
        }

        public bool TryScreenshotToClipboard(string flags = "")
        {
            bool res = false;
            try
            {
                ScreenshotToClipboard(flags);
                res = true;
            }
            catch (Exception ex)
            {
                m_errorMessage = $"{ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                m_bmp?.Dispose();
                m_bmp = null;
            }
            return res;
        }

        private void ScreenshotToClipboard(string flags = "")
        {
            GetRawScreenshot(out m_bmp, flags);

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
            const int flagsMaxLen = 256;
            if ((flags?.Length + 1) > flagsMaxLen) throw new ArgumentException($"Flags argument is too large (> {flagsMaxLen})");

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

            // adding command and flags to args Node
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
            var listPtr = stackalloc byte[Marshal.SizeOf(argsNodeList)];
            argsNodeList.num = argsLen;
            argsNode.list = (IntPtr)listPtr;
            Marshal.StructureToPtr(argsNodeList, (IntPtr)listPtr, false);

            var argsPtr = stackalloc byte[Marshal.SizeOf(argsNode)];
            Marshal.StructureToPtr(argsNode, (IntPtr)argsPtr, false);

            var resultNode = new mpv_node();
            var rPtr = stackalloc byte[Marshal.SizeOf(resultNode)];
            var resultPtr = (IntPtr)rPtr;
            Marshal.StructureToPtr(resultNode, resultPtr, false);

            var res = (mpv_error)mpv_command_node(m_core.Handle, (IntPtr)argsPtr, resultPtr);
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
            string format = "";
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
                    bmp = new Bitmap((int)w, (int)h, PixelFormat.Format32bppRgb);
                    bmp.Read32RgbFromPaddedRgb0((int)stride, ba.data, (int)ba.size.ToUInt64());
                    break;
                default:
                    throw new ArgumentException($"Unsupported color format: {format}");
            }
        }

        private void OnMessage(string[] args)
        {
            if ((args == null) || (args.Length == 0)) return;

            if (args[0] != Name) return;

            var flags = "";
            if (args.Length > 1)
            {
                flags = args[1];
            }

            string text = "Copy Screenshot to clipboard";
            m_core.CommandV("show-text", text);

            string duration = m_core.GetPropertyOsdString("osd-duration");
            if (TryScreenshotToClipboard(flags))
            {
                text += ": Succeded";
            }
            else
            {
                text += ": Failed";
                if (!string.IsNullOrEmpty(m_errorMessage))
                {
                    text += '\n' + m_errorMessage;
                    duration = "5000";
                }
            }
            m_errorMessage = "";

            m_core.CommandV("show-text", text, duration);
        }
    }
}
