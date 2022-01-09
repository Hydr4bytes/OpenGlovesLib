using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace OpenGlovesLib
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VRFFBInput
    {
        //Curl goes between 0-1000
        public VRFFBInput(short thumbCurl, short indexCurl, short middleCurl, short ringCurl, short pinkyCurl)
        {
            this.thumbCurl = thumbCurl;
            this.indexCurl = indexCurl;
            this.middleCurl = middleCurl;
            this.ringCurl = ringCurl;
            this.pinkyCurl = pinkyCurl;
        }
        public short thumbCurl;
        public short indexCurl;
        public short middleCurl;
        public short ringCurl;
        public short pinkyCurl;
    };

    public class ForceFeedbackLink
    {
        private NamedPipeClientStream pipe;

        public enum Handness
        {
            Left,
            Right
        }

        public ForceFeedbackLink(Handness handness)
        {
            string hand = handness == Handness.Right ? "right" : "left";
            pipe = new NamedPipeClientStream(".", $"vrapplication\\ffb\\curl\\{hand}", PipeDirection.Out);
            OpenGlovesLogger.Log($"Connecting to {hand} hand pipe...");
            try
            {
                pipe.Connect(5000);
            }
            catch (Exception e) { OpenGlovesLogger.Error($"Failed to connect ({e.Message.TrimEnd('\r', '\n')})"); return; }
            if (pipe.IsConnected) { OpenGlovesLogger.Error($"Connected! CanWrite:{pipe.CanWrite}"); } else { OpenGlovesLogger.Error("Failed to connect"); return; }
        }

        public void Relax()
        {
            Write(new VRFFBInput(0, 0, 0, 0, 0));
        }

        public void Write(VRFFBInput input)
        {
            OpenGlovesLogger.Log($"{input.thumbCurl}:{input.indexCurl}:{input.middleCurl}:{input.ringCurl}:{input.pinkyCurl}");

            if (!pipe.IsConnected) return;

            int size = Marshal.SizeOf(input);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(input, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            pipe.Write(arr, 0, size);
        }
    }

    public static class OpenGlovesLogger
    {
        private static Action<string> callback;
        private static string header = "[OpenGloves] ";
        private static string errorheader = "[OpenGloves][ERROR] ";

        /// <summary>
        /// Initializes the logger.
        /// </summary>
        /// <param name="logCallback"></param>
        public static void Init(Action<string> logCallback)
        {
            callback = logCallback;
        }

        /// <summary>
        /// Logs a message to the callback.
        /// </summary>
        /// <param name="message"></param>
        internal static void Log(string message)
        {
            callback?.Invoke(header + message);
        }

        /// <summary>
        /// Logs an error to the callback.
        /// </summary>
        /// <param name="message"></param>
        internal static void Error(string message)
        {
            callback?.Invoke(errorheader + message);
        }
    }
}
