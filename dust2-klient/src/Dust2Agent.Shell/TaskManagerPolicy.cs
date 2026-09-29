using System.Runtime.Versioning;
using Dust2Agent.Common;
using Microsoft.Win32;

namespace Dust2Agent.Shell;

/// <summary>
/// Qulf paytida Task Manager'ni o'chiradi ([qaror 7](../../../docs/QARORLAR.md)).
///
/// Ctrl+Alt+Del ni ilgak bilan to'sib bo'lmaydi, lekin u ochadigan oynadan
/// Task Manager'ga o'tishni siyosat bilan to'sish mumkin.
///
/// Failsafe: qobiq har ishga tushganda siyosat majburan tiklanadi, shuning uchun
/// qobiq yiqilib qolsa ham kompyuter "Task Manager o'chiq" holatda qolmaydi.
/// Yopilishda ham tiklanadi.
/// </summary>
[SupportedOSPlatform("windows")]
public static class TaskManagerPolicy
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string ValueName = "DisableTaskMgr";

    public static void Apply(AgentLog log, bool disable)
    {
        try
        {
            if (disable)
            {
                using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
                key?.SetValue(ValueName, 1, RegistryValueKind.DWord);
                return;
            }

            using var existing = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
            if (existing?.GetValue(ValueName) is null) return;
            existing.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            log.Warn($"Task Manager siyosatini o'zgartirib bo'lmadi: {ex.Message}");
        }
    }
}
