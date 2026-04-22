using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace NeoTreescan.Core.Interop;

public static class Privilege
{
    public static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// Attempts to enable SeBackupPrivilege + SeRestorePrivilege on the current process
    /// token. When granted (admin-only), directory enumeration can bypass DACLs, which
    /// is how backup tools read locations such as `C:\System Volume Information`.
    /// Returns true if at least SeBackupPrivilege was enabled.
    public static bool TryEnableBackupPrivilege()
    {
        if (!IsAdministrator()) return false;
        bool ok = EnablePrivilege("SeBackupPrivilege");
        EnablePrivilege("SeRestorePrivilege");
        EnablePrivilege("SeSecurityPrivilege");
        return ok;
    }

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint TOKEN_QUERY = 0x0008;
    private const uint SE_PRIVILEGE_ENABLED = 0x00000002;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct LUID { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct TOKEN_PRIVILEGE
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out SafeAccessTokenHandle TokenHandle);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeValueW(string? lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustTokenPrivileges(
        SafeAccessTokenHandle TokenHandle,
        [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGE NewState,
        uint BufferLength,
        IntPtr PreviousState,
        IntPtr ReturnLength);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    private static bool EnablePrivilege(string name)
    {
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var hTok))
            return false;
        using (hTok)
        {
            if (!LookupPrivilegeValueW(null, name, out var luid)) return false;
            var tp = new TOKEN_PRIVILEGE
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED,
            };
            if (!AdjustTokenPrivileges(hTok, false, ref tp, (uint)Marshal.SizeOf<TOKEN_PRIVILEGE>(), IntPtr.Zero, IntPtr.Zero))
                return false;
            // ERROR_NOT_ALL_ASSIGNED (1300) means privilege wasn't present in token.
            return Marshal.GetLastWin32Error() == 0;
        }
    }
}
