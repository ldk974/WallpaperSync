using System;
using System.Runtime.InteropServices;

namespace WallpaperSync
{
    public static class AcrylicEffect
    {
        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private const int WCA_ACCENT_POLICY = 19;

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        public static bool IsAcrylicSupported()
        {
            // Acrylic só existe no Windows 11 build >= 22000
            return Environment.OSVersion.Version.Build >= 22000;
        }

        public static void ApplyAcrylic(IntPtr handle, byte opacity = 180)
        {
            uint color = ((uint)opacity << 24) | 0x202020;

            var policy = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 2,
                GradientColor = color,
                AnimationId = 0
            };

            int size = Marshal.SizeOf(policy);
            IntPtr policyPtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(policy, policyPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WCA_ACCENT_POLICY,
                SizeOfData = size,
                Data = policyPtr
            };

            SetWindowCompositionAttribute(handle, ref data);

            Marshal.FreeHGlobal(policyPtr);
        }
    }
}
